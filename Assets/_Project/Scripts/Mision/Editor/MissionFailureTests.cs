using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Task 31 tests: mission failure, abort and recovery. Signed: claude_c.
// Menu: SpaceApps -> Mission failure -> Tests (Edit mode only). Writes each test with OK / FAILED and a summary.
// Part 1 tests the pure rules (MissionFailureRules, MissionResults, MissionScoring).
// Part 2 drives a real FlightPhases + LaunchScreen pair without a scene: Unity does not call Awake/OnEnable/Update
// in edit mode, so the test calls them by reflection. A full Play mode run in 02_Lanzamiento is still a manual check
// (see Docs/Task31/MISSION_FAILURE_RULES.md).
// Then it runs the existing Scoring and Flight phases tests (case 21).
public static class MissionFailureTests
{
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static int passed;
    static int failed;
    static MissionFailureSettings S { get { return MissionFailureSettings.Default; } }

    [MenuItem("SpaceApps/Mission failure/Tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        Rules();
        Recording();
        Scoring();
        Flow();

        string summary = "Mission failure tests: " + passed + " OK, " + failed + " FAILED.";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);

        // 21. The existing suites must still pass (they log their own summary)
        MissionScoringTests.RunAll();
        FlightPhasesTests.RunAll();
    }

    [MenuItem("SpaceApps/Mission failure/Tests", true)]
    static bool RunAllValid()
    {
        return !Application.isPlaying;
    }

    // ---------- Part 1: pure rules ----------

    static void Rules()
    {
        // 1. New mission starts with no failure
        MissionResults f = new MissionResults();
        Check("01 New mission has no failure", !f.HasFailed && !f.aborted && f.failureReason == "" && f.recoveryAttempts == 0);

        // Bands (effective = raw x skill)
        Check("Bands: 0.10 critical, 0.30 poor, 0.50 acceptable, 0.80 good, 0.95 excellent",
            MissionFailureRules.BandOf(0.10f, S) == MissionFailureRules.Band.Critical
            && MissionFailureRules.BandOf(0.30f, S) == MissionFailureRules.Band.Poor
            && MissionFailureRules.BandOf(0.50f, S) == MissionFailureRules.Band.Acceptable
            && MissionFailureRules.BandOf(0.80f, S) == MissionFailureRules.Band.Good
            && MissionFailureRules.BandOf(0.95f, S) == MissionFailureRules.Band.Excellent);
        Check("Band edges: 0.20 is poor, 0.40 is acceptable",
            MissionFailureRules.BandOf(0.20f, S) == MissionFailureRules.Band.Poor
            && MissionFailureRules.BandOf(0.40f, S) == MissionFailureRules.Band.Acceptable);

        // 2. Good piloting continues
        MissionFailureRules.Outcome o = Judge(TrainingFocus.Piloting, EventPhase.Departure, 0.9f, 0.8f, EventSeverity.Minor, 0);
        Check("02 Good departure piloting continues", o.decision == MissionFailureRules.Decision.Continue && o.band == MissionFailureRules.Band.Good);

        // 3. Moderate (poor) piloting does not abort
        o = Judge(TrainingFocus.Piloting, EventPhase.Departure, 0.5f, 0.6f, EventSeverity.Minor, 0); // 0.30
        Check("03 Poor departure piloting (0.30) continues", o.decision == MissionFailureRules.Decision.Continue && o.band == MissionFailureRules.Band.Poor);

        // 4. Critical piloting in departure fails
        o = Judge(TrainingFocus.Piloting, EventPhase.Departure, 0.1f, 0.8f, EventSeverity.Minor, 0); // 0.08
        Check("04 Critical departure piloting (0.08) fails DEPARTURE_FAILURE",
            o.decision == MissionFailureRules.Decision.Fail && o.code == MissionFailureRules.DepartureFailure && o.message.StartsWith("Departure aborted"));
        o = Judge(TrainingFocus.Piloting, EventPhase.LunarOrbit, 0.1f, 0.8f, EventSeverity.Minor, 0);
        Check("Critical piloting in lunar orbit without an incident is not safety-critical", o.decision == MissionFailureRules.Decision.Continue);
        o = Judge(TrainingFocus.Piloting, EventPhase.Return, 0.05f, 0.9f, EventSeverity.Minor, 0);
        Check("Critical return piloting fails RETURN_FAILURE", o.decision == MissionFailureRules.Decision.Fail && o.code == MissionFailureRules.ReturnFailure);
        MissionFailureSettings lenient = ScriptableObject.CreateInstance<MissionFailureSettings>();
        lenient.departurePilotingIsCritical = false;
        o = MissionFailureRules.Judge(TrainingFocus.Piloting, EventPhase.Departure, 0.1f, 0.8f, EventSeverity.Minor, 0, lenient);
        Check("Settings can make departure piloting non-critical", o.decision == MissionFailureRules.Decision.Continue);
        Object.DestroyImmediate(lenient);

        // 5. Serious maintenance resolved
        o = Judge(TrainingFocus.Engineering, EventPhase.LunarOrbit, 0.8f, 0.7f, EventSeverity.Serious, 0);
        Check("05 Serious incident with a good repair is resolved", o.decision == MissionFailureRules.Decision.Continue && o.message.Contains("resolved"));
        o = Judge(TrainingFocus.Engineering, EventPhase.LunarOrbit, 0.5f, 0.6f, EventSeverity.Serious, 0); // 0.30 poor
        Check("Serious incident with a poor repair is still resolved (no terminal failure)", o.decision == MissionFailureRules.Decision.Continue);

        // 6. Unresolved maintenance
        o = Judge(TrainingFocus.Engineering, EventPhase.LunarOrbit, 0.1f, 0.7f, EventSeverity.Serious, 0);
        Check("06a Serious incident, first critical repair: one more attempt", o.decision == MissionFailureRules.Decision.Retry);
        o = Judge(TrainingFocus.Engineering, EventPhase.LunarOrbit, 0.1f, 0.7f, EventSeverity.Serious, 1);
        Check("06b Serious incident, second critical repair: LUNAR_ORBIT_FAILURE",
            o.decision == MissionFailureRules.Decision.Fail && o.code == MissionFailureRules.LunarOrbitFailure && o.message.StartsWith("Critical maintenance issue"));
        o = Judge(TrainingFocus.Engineering, EventPhase.Departure, 0.1f, 0.7f, EventSeverity.Critical, 0);
        Check("06c Explicitly critical incident, critical repair: DEPARTURE_FAILURE at once",
            o.decision == MissionFailureRules.Decision.Fail && o.code == MissionFailureRules.DepartureFailure);
        o = Judge(TrainingFocus.Engineering, EventPhase.LunarOrbit, 0.1f, 0.7f, EventSeverity.Minor, 0);
        Check("Planned (minor) maintenance with a critical result does not fail", o.decision == MissionFailureRules.Decision.Continue);

        // Science never aborts
        o = Judge(TrainingFocus.Research, EventPhase.LunarOrbit, 0f, 0.2f, EventSeverity.Critical, 5);
        Check("Research never aborts the flight, even critical", o.decision == MissionFailureRules.Decision.Continue);

        // 7. Event severity: negative score/money alone is not critical; Auto never becomes Critical
        EventData bad = Event(false, TrainingFocus.None, -3, -200f);
        Check("07 Bad event without a minigame is Minor", MissionFailureRules.SeverityOf(bad) == EventSeverity.Minor);
        EventData badRole = Event(false, TrainingFocus.Engineering, -2, 0f);
        Check("Bad event with a minigame (Auto) is Serious, not Critical", MissionFailureRules.SeverityOf(badRole) == EventSeverity.Serious);
        EventData good = Event(true, TrainingFocus.Engineering, 2, 0f);
        Check("Good event is Minor", MissionFailureRules.SeverityOf(good) == EventSeverity.Minor);
        badRole.severity = EventSeverity.Critical;
        Check("Explicit Critical is kept", MissionFailureRules.SeverityOf(badRole) == EventSeverity.Critical);
        Check("Null event is Minor", MissionFailureRules.SeverityOf(null) == EventSeverity.Minor);
        Object.DestroyImmediate(bad);
        Object.DestroyImmediate(badRole);
        Object.DestroyImmediate(good);

        // 16. Invalid values
        Check("16 NaN and infinity count as 0", MissionFailureRules.Effective(float.NaN, 1f) == 0f && MissionFailureRules.Clean(float.PositiveInfinity) == 0f);
        Check("Out of range values are clamped", Near(MissionFailureRules.Effective(1.5f, 2f), 1f) && MissionFailureRules.Effective(-1f, 0.5f) == 0f);
        o = MissionFailureRules.Judge(TrainingFocus.Piloting, EventPhase.Departure, 0.9f, 0.9f, EventSeverity.Minor, 0, null);
        Check("Null settings use the defaults", o.decision == MissionFailureRules.Decision.Continue);
        Check("Null game has no preflight problem", MissionFailureRules.PreflightProblem(null) == "");
    }

    // ---------- Recording and milestones ----------

    static void Recording()
    {
        // 10. Failed departure
        MissionResults f = new MissionResults();
        f.departureDone = true; // even if something set it before
        Check("10a Departure failure recorded", MissionFailureRules.RecordFailure(f, EventPhase.Departure, MissionFailureRules.DepartureFailure, "test"));
        FlightPhases.MarkPhaseDone(f, EventPhase.Departure);
        Check("10b Failed departure cannot award departureDone", !f.departureDone && f.aborted && f.flightFinished && f.failurePhase == EventPhase.Departure);

        // 11. Failed lunar orbit
        f = new MissionResults();
        FlightPhases.MarkPhaseDone(f, EventPhase.Departure);
        MissionFailureRules.RecordFailure(f, EventPhase.LunarOrbit, MissionFailureRules.LunarOrbitFailure, "test");
        FlightPhases.MarkPhaseDone(f, EventPhase.LunarOrbit);
        Check("11 Failed lunar orbit keeps departure, cannot award lunarOrbitDone",
            f.departureDone && !f.lunarOrbitDone && !f.safeReturnDone && f.aborted && f.flightFinished);

        // 12. Failed return
        f = new MissionResults();
        FlightPhases.MarkPhaseDone(f, EventPhase.Departure);
        FlightPhases.MarkPhaseDone(f, EventPhase.LunarOrbit);
        MissionFailureRules.RecordFailure(f, EventPhase.Return, MissionFailureRules.ReturnFailure, "test");
        FlightPhases.MarkPhaseDone(f, EventPhase.Return);
        Check("12 Failed return: finished, no safe return, not an abort",
            f.flightFinished && !f.safeReturnDone && f.lunarOrbitDone && !f.aborted && f.failureCode == MissionFailureRules.ReturnFailure);

        // 14. Only once
        bool second = MissionFailureRules.RecordFailure(f, EventPhase.Departure, MissionFailureRules.DepartureFailure, "other");
        Check("14 Outcome recorded only once", !second && f.failureCode == MissionFailureRules.ReturnFailure && f.failureReason == "test");
        Check("Null flight does not throw", !MissionFailureRules.RecordFailure(null, EventPhase.Return, "", ""));

        // Milestones need the previous one
        MissionResults order = new MissionResults();
        FlightPhases.MarkPhaseDone(order, EventPhase.LunarOrbit);
        FlightPhases.MarkPhaseDone(order, EventPhase.Return);
        Check("Milestones out of order are not awarded (return still ends the flight)",
            !order.lunarOrbitDone && !order.safeReturnDone && order.flightFinished);

        // 17. Raw results stored as raw
        MissionResults raw = new MissionResults();
        FlightPhases.SetResult(raw, TrainingFocus.Engineering, 0.6f);
        Check("17 Raw result stored without skill", Near(raw.repairResult, 0.6f));
        FlightPhases.SetResult(raw, TrainingFocus.Piloting, float.NaN);
        Check("NaN result stored as 0", Near(raw.pilotingResult, 0f));
    }

    // ---------- Scoring ----------

    static void Scoring()
    {
        CatalogImporter.Import();
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (mission == null)
        {
            Check("Catalog for scoring tests (run SpaceApps -> Import Artemis catalog)", false);
            return;
        }
        GameObject go = new GameObject("Test GameManager");
        go.hideFlags = HideFlags.HideAndDontSave;
        GameManager gm = go.AddComponent<GameManager>();
        try
        {
            // 20. Successful full mission
            MissionScoringTests.FillExampleGame(gm, mission);
            ScoreResult ok = MissionScoring.EvaluateGame(gm);
            Check("20 Successful full mission is SUCCESS (" + string.Join(",", ok.failedGates) + ")", ok.status == "SUCCESS" && !ok.aborted && Near(ok.competitive, ok.total));
            float successTotal = ok.total;

            // 19. Departure abort: diagnostic kept, official 0, gates
            MissionScoringTests.FillExampleGame(gm, mission);
            MissionFailureRules.RecordFailure(gm.flight, EventPhase.Departure, MissionFailureRules.DepartureFailure, "Departure aborted: test.");
            ScoreResult r = MissionScoring.EvaluateGame(gm);
            Check("19a Abort: FAILURE with MISSION_ABORT and DEPARTURE_FAILURE first (" + string.Join(",", r.failedGates) + ")",
                r.status == "FAILURE" && r.aborted && r.failedGates.Count > 1
                && r.failedGates[0] == MissionFailureRules.AbortGate && r.failedGates[1] == MissionFailureRules.DepartureFailure);
            Check("19b Abort: official 0, diagnostic still shown", r.competitive == 0f && r.total > 0f && r.total < successTotal);
            Check("19c Abort keeps the other gates (DEPARTURE) and has no duplicates",
                r.failedGates.Contains("DEPARTURE") && NoDuplicates(r.failedGates));
            Check("19d Failure reason reaches the score", r.failureReason == "Departure aborted: test.");
            Check("19e Results report explains the abort",
                ResultsScreen.BuildReport(r, gm).Contains("Departure aborted: test.") && ResultsScreen.FailureLesson(MissionFailureRules.DepartureFailure).Length > 0);

            // 13. A failed mission cannot become successful later
            gm.flight.departureDone = true;   // a late callback tries to fix the flags
            gm.flight.lunarOrbitDone = true;
            gm.flight.safeReturnDone = true;
            r = MissionScoring.EvaluateGame(gm);
            Check("13 Failed mission stays FAILURE even if flags are set later", r.status == "FAILURE" && r.competitive == 0f);

            // Never PENDING after a terminal failure
            gm.flight.flightFinished = false;
            Check("Terminal failure is never PENDING", MissionScoring.EvaluateGame(gm).status == "FAILURE");

            // Return failure is not an abort
            MissionScoringTests.FillExampleGame(gm, mission);
            gm.flight.safeReturnDone = false;
            MissionFailureRules.RecordFailure(gm.flight, EventPhase.Return, MissionFailureRules.ReturnFailure, "Return unsuccessful: test.");
            r = MissionScoring.EvaluateGame(gm);
            Check("Return failure: FAILURE, not aborted, RETURN_FAILURE (" + string.Join(",", r.failedGates) + ")",
                r.status == "FAILURE" && !r.aborted && r.failedGates.Contains(MissionFailureRules.ReturnFailure) && !r.failedGates.Contains(MissionFailureRules.AbortGate));

            // Science only: safe flight, science missing
            MissionScoringTests.FillExampleGame(gm, mission);
            gm.flight.experimentFraction = -1f;
            gm.flight.downlinkFraction = -1f;
            r = MissionScoring.EvaluateGame(gm);
            Check("Science missing: FAILURE without abort, explained as incomplete science",
                r.status == "FAILURE" && !r.aborted && r.failureReason == ""
                && ResultsScreen.BuildReport(r, gm).Contains("returned safely, but the scientific objectives were incomplete"));

            // 18. ResetGame clears the failure state
            MissionFailureRules.RecordFailure(gm.flight, EventPhase.LunarOrbit, MissionFailureRules.LunarOrbitFailure, "x");
            gm.flight.recoveryAttempts = 2;
            gm.flight.recoveryCompleted = true;
            gm.ResetGame();
            Check("18 ResetGame clears the failure state",
                !gm.flight.HasFailed && !gm.flight.aborted && gm.flight.failureReason == "" && gm.flight.recoveryAttempts == 0 && !gm.flight.recoveryCompleted);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // ---------- Part 2: FlightPhases flow ----------

    static void Flow()
    {
        // FlightPhases uses the singleton. In edit mode Awake never runs, so this is always a fresh temporary
        // object created by the getter: hide it so it is not saved in the open scene, and destroy it at the end.
        GameManager gm = GameManager.Instance;
        GameObject gmObject = gm.gameObject;
        gmObject.hideFlags = HideFlags.HideAndDontSave;
        gm.ResetGame();
        List<GameObject> temp = new List<GameObject>();
        try
        {
            int opened;

            // 2/9. Planned piloting and research with no handlers (tasks 12/14 missing): no failure, no invented result
            FlightPhases p = NewFlight(temp, out LaunchScreen screen);
            SetProgress(screen, 1f);
            Tick(p);                       // piloting queued + skipped
            Tick(p);
            Check("09a Missing piloting minigame is skipped, no failure", !gm.flight.HasFailed && !screen.paused && p.CurrentMinigame == TrainingFocus.None);
            Check("09b No piloting result is invented", gm.flight.pilotingResult < 0f);

            // 8. Event with a role but no handler does not freeze
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.plannedMinigames.Clear();
            p.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Serious, "Test failure");
            SetProgress(screen, 0.3f);
            Tick(p);
            Check("08 Incident without a maintenance handler: skipped, not paused, no failure",
                !screen.paused && p.CurrentMinigame == TrainingFocus.None && !gm.flight.HasFailed);

            // 5 / 17 / 15. Serious incident resolved through CompleteMinigame
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.plannedMinigames.Clear();
            opened = 0;
            p.RegisterMinigame(TrainingFocus.Engineering, () => opened++);
            p.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Serious, "Test failure");
            SetProgress(screen, 0.3f);
            Tick(p);
            Check("Maintenance opens and pauses the flight", opened == 1 && screen.paused && p.CurrentMinigame == TrainingFocus.Engineering);
            bool first = p.CompleteMinigame(TrainingFocus.Engineering, 0.6f);
            Check("05 Recovered incident resumes the flight", first && !screen.paused && !gm.flight.HasFailed && gm.flight.recoveryCompleted && gm.flight.recoveryAttempts == 1);
            Check("17 Flow stores the raw result (0.6), not raw x skill", Near(gm.flight.repairResult, 0.6f));
            bool duplicate = p.CompleteMinigame(TrainingFocus.Engineering, 0f);
            Check("15 Duplicate completion is ignored", !duplicate && Near(gm.flight.repairResult, 0.6f) && gm.flight.recoveryAttempts == 1);
            Check("Wrong role completion is ignored", !p.CompleteMinigame(TrainingFocus.Piloting, 1f) && gm.flight.pilotingResult < 0f);

            // 6. Serious incident: critical, retry, critical again -> abort in lunar orbit
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.plannedMinigames.Clear();
            opened = 0;
            p.RegisterMinigame(TrainingFocus.Engineering, () => opened++);
            screen.SetPhase(EventPhase.LunarOrbit);
            gm.flight.departureDone = true;
            p.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Serious, "Test failure");
            SetProgress(screen, 0.3f);
            Tick(p);
            p.CompleteMinigame(TrainingFocus.Engineering, 0.05f);
            Check("06d First critical repair: no failure yet, flight resumes", !gm.flight.HasFailed && !screen.paused && !gm.flight.recoveryCompleted);
            Tick(p);
            Check("Recovery attempt opens the minigame again", opened == 2 && screen.paused);
            p.CompleteMinigame(TrainingFocus.Engineering, 0.05f);
            Check("06e Second critical repair aborts in lunar orbit",
                gm.flight.HasFailed && gm.flight.aborted && gm.flight.failureCode == MissionFailureRules.LunarOrbitFailure
                && gm.flight.departureDone && !gm.flight.lunarOrbitDone && !gm.flight.safeReturnDone);
            Check("Abort ends the flight cleanly (finished, not paused, nothing open)",
                screen.Finished && !screen.paused && p.CurrentMinigame == TrainingFocus.None && gm.flight.flightFinished);
            Check("Late completion after abort is ignored", !p.CompleteMinigame(TrainingFocus.Engineering, 1f));
            screen.NextPhase();
            Check("No milestone after the abort", !gm.flight.lunarOrbitDone && !gm.flight.safeReturnDone);

            // 4/10. Critical departure piloting through the flow
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.RegisterMinigame(TrainingFocus.Piloting, () => { });
            SetProgress(screen, 0.25f);
            Tick(p);
            p.CompleteMinigame(TrainingFocus.Piloting, 0.05f); // pilot null -> skill 0.5 -> effective 0.025
            Check("04/10 Critical departure piloting aborts, no departureDone",
                gm.flight.failureCode == MissionFailureRules.DepartureFailure && !gm.flight.departureDone && screen.Finished);

            // 3. Moderate piloting continues through the flow
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.RegisterMinigame(TrainingFocus.Piloting, () => { });
            SetProgress(screen, 0.25f);
            Tick(p);
            p.CompleteMinigame(TrainingFocus.Piloting, 0.6f); // effective 0.30: poor
            Check("03 Poor piloting continues through the flow", !gm.flight.HasFailed && !screen.paused && Near(gm.flight.pilotingResult, 0.6f));

            // 22. Two queued minigames: one at a time; an abort drops the rest
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.plannedMinigames.Clear();
            int pil = 0, eng = 0;
            p.RegisterMinigame(TrainingFocus.Piloting, () => pil++);
            p.RegisterMinigame(TrainingFocus.Engineering, () => eng++);
            p.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Critical, "Test A");
            p.RequestMinigame(TrainingFocus.Piloting, EventSeverity.Minor, "Test B");
            SetProgress(screen, 0.3f);
            Tick(p);
            Tick(p); // a minigame is open: nothing else opens
            Check("22a Queued minigames open one at a time", eng == 1 && pil == 0);
            p.CompleteMinigame(TrainingFocus.Engineering, 0.0f); // critical incident -> departure abort
            Tick(p);
            Check("22b Abort drops the queued minigame and never pauses again", pil == 0 && !screen.paused && gm.flight.failureCode == MissionFailureRules.DepartureFailure);

            // 7. A negative event alone (no minigame) never fails
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            EventData bad = Event(false, TrainingFocus.None, -3, -500f);
            bad.probability = 1f;
            bad.phase = EventPhase.Departure;
            p.events = new List<EventData> { bad };
            p.plannedMinigames.Clear();
            SetProgress(screen, 0.6f);
            Tick(p);
            Check("07b Negative score/money event alone does not fail the mission", gm.eventsHappened.Contains(bad) && !gm.flight.HasFailed && !screen.paused);
            Tick(p);
            Check("Event is not applied twice", CountOf(gm.eventsHappened, bad) == 1);
            Object.DestroyImmediate(bad);

            // Preflight: a launched game with an invalid rocket aborts at departure
            gm.ResetGame();
            gm.flight.launched = true; // no parts -> invalid rocket
            p = NewFlight(temp, out screen);
            Call(screen, "Start"); // fires PhaseStarted(Departure)
            Check("Invalid spacecraft aborts at departure, training cannot compensate",
                gm.flight.failureCode == MissionFailureRules.DepartureFailure && gm.flight.aborted && screen.Finished);

            // 20b. Normal ending without failure awards the milestones in order
            gm.ResetGame();
            p = NewFlight(temp, out screen);
            p.plannedMinigames.Clear();
            screen.NextPhase(); // -> lunar orbit
            screen.NextPhase(); // -> return
            screen.NextPhase(); // -> finish
            Check("20b Normal flight awards departure, lunar orbit and safe return",
                gm.flight.departureDone && gm.flight.lunarOrbitDone && gm.flight.safeReturnDone && gm.flight.flightFinished && !gm.flight.HasFailed);
        }
        finally
        {
            foreach (GameObject go in temp) if (go != null) Object.DestroyImmediate(go);
            Object.DestroyImmediate(gmObject);
        }
    }

    // LaunchScreen + FlightPhases on a hidden object, wired like in the scene (no random events by default)
    static FlightPhases NewFlight(List<GameObject> temp, out LaunchScreen screen)
    {
        GameObject go = new GameObject("TestFlight");
        go.hideFlags = HideFlags.HideAndDontSave;
        temp.Add(go);
        screen = go.AddComponent<LaunchScreen>();
        FlightPhases p = go.AddComponent<FlightPhases>();
        p.screen = screen;
        EventData none = Event(false, TrainingFocus.None, 0, 0f);
        none.probability = 0f; // non-empty list: GameData events are not used
        p.events = new List<EventData> { none };
        Call(p, "OnEnable");
        Call(p, "Start");
        return p;
    }

    static void Tick(FlightPhases p)
    {
        Call(p, "Update");
    }

    static void SetProgress(LaunchScreen screen, float progress)
    {
        typeof(LaunchScreen).GetProperty("PhaseProgress").SetValue(screen, progress);
    }

    static void Call(object target, string method)
    {
        MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null) m.Invoke(target, null);
    }

    static MissionFailureRules.Outcome Judge(TrainingFocus role, EventPhase phase, float raw, float skill, EventSeverity incident, int attempt)
    {
        return MissionFailureRules.Judge(role, phase, raw, skill, incident, attempt, S);
    }

    static EventData Event(bool isGood, TrainingFocus role, int score, float money)
    {
        EventData e = ScriptableObject.CreateInstance<EventData>();
        e.hideFlags = HideFlags.HideAndDontSave;
        e.eventName = "Test event";
        e.isGood = isGood;
        e.minigameRole = role;
        e.scoreChange = score;
        e.moneyChange = money;
        return e;
    }

    static int CountOf(List<EventData> list, EventData e)
    {
        int n = 0;
        foreach (EventData x in list) if (x == e) n++;
        return n;
    }

    static bool NoDuplicates(List<string> list)
    {
        return new HashSet<string>(list).Count == list.Count;
    }

    static bool Near(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.0001f;
    }

    static void Check(string name, bool ok)
    {
        if (ok)
        {
            passed++;
            Debug.Log("OK: " + name);
        }
        else
        {
            failed++;
            Debug.LogError("FAILED: " + name);
        }
    }
}
