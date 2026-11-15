using System;
using System.Collections.Generic;
using UnityEngine;

// Flight phases (task 11): what happens during departure, lunar orbit and return. Signed: claude_c.
// Goes on the same object as LaunchScreen in 02_Lanzamiento (menu SpaceApps -> Flight phases -> Add to open scene).
// LaunchScreen moves the rocket and the cameras; this script decides what happens in each phase:
//   - One random event per phase (RandomEvents: weather and crew change the chance), written in the mission control log.
//   - The planned minigames of each phase (GDD: piloting in the departure, research in lunar orbit),
//     plus the minigame of any bad event with a minigameRole.
//   - The flight milestones in GameManager.flight (departureDone, lunarOrbitDone, safeReturnDone, flightFinished),
//     which MissionScoring uses for the final score.
//   - Task 31: failure, abort and recovery (MissionFailureRules). A critical result in a safety-critical action
//     ends the mission; a serious incident gets a recovery attempt; science never aborts the flight.
// A phase does not end until its event and its minigames are done (it turns off LaunchScreen.autoAdvance).
// Flight plan (task 46): each crew member has one guaranteed moment (Departure = pilot, Lunar orbit = scientist,
// Return = engineer, the last one via WiringFlightHook) and a bad event can add one incident. Two limits keep the
// flight short and never repeat a minigame: one minigame per crew role per phase (a bad event whose role already
// played, or whose role has a planned minigame in that phase, only applies its points and lesson), and at most
// maxMinigamesPerPhase minigames per phase. Recovery retries (task 31) are never blocked.
//
// For the minigames (tasks 12, 13, 14), register one handler per role (in Awake or OnEnable):
//   flightPhases.RegisterMinigame(TrainingFocus.Piloting, OpenMyMinigame);
//   // when it ends (RAW result 0..1, never multiplied by the skill):
//   flightPhases.CompleteMinigame(TrainingFocus.Piloting, rawResult0to1);
// CompleteMinigame stores the result, applies the failure rules and resumes (or ends) the flight.
// The old pair SetResult + MinigameDone still works, but it skips the failure rules.
// The flight is paused (LaunchScreen.paused) while a minigame is open. A role with no registered handler
// is skipped safely and never causes a failure (audit F06 / task 31). MinigameRequested is informative only.
public class FlightPhases : MonoBehaviour
{
    [Serializable]
    public class PlannedMinigame
    {
        public EventPhase phase = EventPhase.Departure;
        public TrainingFocus role = TrainingFocus.Piloting;
        [Range(0f, 1f)] public float atProgress = 0.5f; // point of the phase where it opens (0 to 1)
    }

    // One minigame waiting to be played: why it was asked for and which attempt it is
    struct Request
    {
        public TrainingFocus role;
        public EventSeverity incident; // Minor for planned minigames
        public string source;          // event name, "" if planned
        public int attempt;            // 0 = first try
    }

    [Tooltip("Empty = the LaunchScreen on this object, or the one in the scene")]
    public LaunchScreen screen;

    [Tooltip("Empty = GameData.Instance.events (every imported event)")]
    public List<EventData> events = new List<EventData>();

    [Tooltip("Point of each phase (0 to 1) where the random event is rolled")]
    [Range(0f, 1f)] public float eventAtProgress = 0.5f;

    [Tooltip("Task 31 thresholds. Empty = MissionFailureSettings.Default")]
    public MissionFailureSettings failureSettings;

    [Tooltip("Most minigames that can open in one phase (recovery retries not counted). 0 = no limit")]
    public int maxMinigamesPerPhase = 2;

    // Departure: piloting during the liftoff view (first 35 %). Lunar orbit: research.
    // Return has none by default: a second piloting result would be averaged with the first one (see SetResult).
    public List<PlannedMinigame> plannedMinigames = new List<PlannedMinigame>
    {
        new PlannedMinigame { phase = EventPhase.Departure, role = TrainingFocus.Piloting, atProgress = 0.2f },
        new PlannedMinigame { phase = EventPhase.LunarOrbit, role = TrainingFocus.Research, atProgress = 0.5f },
    };

    // A minigame opened (the flight is already paused). Informative only: to handle a role, use RegisterMinigame.
    public event Action<TrainingFocus> MinigameRequested;
    // A random event happened in the flight (for example, to show it on the big screen).
    public event Action<EventData> EventHappened;
    // The mission ended early or failed (task 31). Fired once, with the failure reason.
    public event Action<string> MissionTerminated;

    public TrainingFocus CurrentMinigame { get; private set; } = TrainingFocus.None; // None = no minigame open
    public EventData LastEvent { get; private set; }
    public bool Terminated { get { return Flight != null && Flight.HasFailed; } }

    readonly Dictionary<TrainingFocus, Action> handlers = new Dictionary<TrainingFocus, Action>();
    readonly List<Request> queue = new List<Request>();
    readonly List<int> plannedDone = new List<int>(); // indexes of plannedMinigames already handled in this phase
    readonly HashSet<TrainingFocus> playedThisPhase = new HashSet<TrainingFocus>(); // roles whose minigame opened in this phase
    int openedThisPhase; // minigames opened in this phase (first attempts only)
    Request current;

    // Task 46: did this role's minigame already open in the current phase? How many opened in it?
    public bool PlayedThisPhase(TrainingFocus role) { return playedThisPhase.Contains(role); }
    public int MinigamesThisPhase { get { return openedThisPhase; } }
    bool eventRolled;

    MissionResults Flight { get { return GameManager.Instance.flight; } }
    MissionFailureSettings Settings { get { return failureSettings != null ? failureSettings : MissionFailureSettings.Default; } }

    void Awake()
    {
        if (screen == null) screen = GetComponent<LaunchScreen>();
        if (screen == null) screen = FindAnyObjectByType<LaunchScreen>();
        if (screen == null) Debug.LogError("FlightPhases: there is no LaunchScreen in the scene.");
    }

    // OnEnable runs before LaunchScreen.Start, so the first PhaseStarted (Departure) is not missed
    void OnEnable()
    {
        if (screen == null) return;
        screen.PhaseStarted += OnPhaseStarted;
        screen.MissionFinished += OnMissionFinished;
    }

    void OnDisable()
    {
        if (screen == null) return;
        screen.PhaseStarted -= OnPhaseStarted;
        screen.MissionFinished -= OnMissionFinished;
    }

    void Start()
    {
        if (screen != null) screen.autoAdvance = false; // this script ends each phase
    }

    void Update()
    {
        if (screen == null || screen.Finished || Terminated) return;
        if (CurrentMinigame != TrainingFocus.None) return; // waiting for CompleteMinigame / MinigameDone

        EventPhase phase = screen.CurrentPhase;
        float progress = screen.PhaseProgress;

        if (!eventRolled && progress >= eventAtProgress)
        {
            eventRolled = true;
            RollEvent(phase);
        }

        for (int i = 0; i < plannedMinigames.Count; i++)
        {
            PlannedMinigame m = plannedMinigames[i];
            if (m == null || m.phase != phase || plannedDone.Contains(i)) continue;
            if (progress < m.atProgress) continue;
            plannedDone.Add(i);
            queue.Add(new Request { role = m.role, incident = EventSeverity.Minor, source = "", attempt = 0 });
        }

        if (queue.Count > 0)
        {
            OpenNextMinigame();
            return;
        }

        // Everything in this phase is done: go to the next one when the progress bar is full
        if (progress >= 1f) screen.NextPhase();
    }

    // ---------- Phases ----------

    void OnPhaseStarted(EventPhase phase)
    {
        if (Terminated) return;

        // The phase before this one ended well (also when the test "Skip" button jumps ahead)
        EventPhase before;
        if (PhaseBefore(phase, out before)) MarkPhaseDone(Flight, before);

        eventRolled = false;
        plannedDone.Clear();
        playedThisPhase.Clear();
        openedThisPhase = 0;
        queue.Clear();
        if (CurrentMinigame != TrainingFocus.None) MinigameDone();

        // Departure: an invalid spacecraft cannot fly, whatever the crew training (only in a real launched game)
        if (phase == EventPhase.Departure)
        {
            string problem = MissionFailureRules.PreflightProblem(GameManager.Instance);
            if (problem != "") FailMission(EventPhase.Departure, MissionFailureRules.DepartureFailure, problem);
        }
    }

    void OnMissionFinished()
    {
        MarkPhaseDone(Flight, EventPhase.Return); // does nothing if the mission already failed
        queue.Clear();
        if (screen != null) screen.paused = false;
        CurrentMinigame = TrainingFocus.None;
    }

    void RollEvent(EventPhase phase)
    {
        List<EventData> list = events;
        if (list == null || list.Count == 0)
        {
            GameData data = GameData.Instance;
            list = data != null ? data.events : null;
        }
        if (list == null || list.Count == 0) return;

        EventData e = RandomEvents.RollAndApply(list, phase); // never repeats an event that already happened
        if (e == null) return;

        LastEvent = e;
        screen.Log(EventLogText(e));
        if (EventHappened != null) EventHappened(e);

        // A bad event on a role opens that role's minigame (for example, a failure -> maintenance).
        // Its severity (Auto = Serious) decides what a critical result means. A negative score or money alone never fails the mission.
        if (!e.isGood && e.minigameRole != TrainingFocus.None)
        {
            // Task 46: that crew member already has a minigame in this phase, so they handle it without a second one
            if (playedThisPhase.Contains(e.minigameRole) || HasPlanned(phase, e.minigameRole))
                screen.Log("The " + CrewWord(e.minigameRole) + " is already on it: no extra minigame.");
            else
                RequestMinigame(e.minigameRole, MissionFailureRules.SeverityOf(e), e.eventName);
        }
    }

    bool HasPlanned(EventPhase phase, TrainingFocus role)
    {
        foreach (PlannedMinigame m in plannedMinigames)
            if (m != null && m.phase == phase && m.role == role) return true;
        return false;
    }

    // ---------- Minigames ----------

    void OpenNextMinigame()
    {
        Request request = queue[0];
        queue.RemoveAt(0);
        TrainingFocus role = request.role;

        // Task 46: one minigame per crew role per phase, and at most maxMinigamesPerPhase. Retries always open.
        if (request.attempt == 0)
        {
            if (playedThisPhase.Contains(role))
            {
                screen.Log("The " + CrewWord(role) + " is already on it: no extra minigame.");
                return;
            }
            if (maxMinigamesPerPhase > 0 && openedThisPhase >= maxMinigamesPerPhase)
            {
                screen.Log("Mission Control handles it from Earth: no extra minigame in this phase.");
                return;
            }
        }

        Action open;
        if (!handlers.TryGetValue(role, out open) || open == null)
        {
            // Not implemented yet (tasks 12/14) or not in the scene: never a failure, and no result is invented
            screen.Log(RoleName(role) + " minigame not available yet: skipped.");
            return;
        }

        current = request;
        CurrentMinigame = role;
        playedThisPhase.Add(role);
        if (request.attempt == 0) openedThisPhase++;
        screen.paused = true;
        screen.Log(RoleName(role) + " minigame" + (request.attempt > 0 ? " (recovery attempt " + (request.attempt + 1) + ")." : "."));
        Sfx.Play(Sfx.MinigamePopup);
        if (MinigameRequested != null) MinigameRequested(role);
        try
        {
            open();
        }
        catch (Exception ex)
        {
            // A broken minigame must not freeze the flight
            Debug.LogException(ex);
            screen.Log(RoleName(role) + " minigame failed to open: skipped.");
            MinigameDone();
        }
    }

    // A minigame says it handles this role. Only one handler per role (a second call replaces the first).
    public void RegisterMinigame(TrainingFocus role, Action open)
    {
        if (role == TrainingFocus.None || open == null) return;
        handlers[role] = open;
    }

    public void UnregisterMinigame(TrainingFocus role, Action open)
    {
        Action existing;
        if (handlers.TryGetValue(role, out existing) && existing == open) handlers.Remove(role);
    }

    public bool HasMinigame(TrainingFocus role)
    {
        return handlers.ContainsKey(role);
    }

    // Queues a minigame for an incident (for example a scripted failure). It opens on the next Update,
    // one at a time. Ignored after the mission has failed.
    public void RequestMinigame(TrainingFocus role, EventSeverity incident, string source)
    {
        if (role == TrainingFocus.None || Terminated) return;
        if (incident == EventSeverity.Auto) incident = EventSeverity.Serious;
        queue.Add(new Request { role = role, incident = incident, source = source ?? "", attempt = 0 });
    }

    // Task 31: the authoritative way to end a minigame. Stores the RAW result once, applies the failure rules
    // and resumes the flight, asks for a recovery attempt, or ends the mission.
    // Returns false (and changes nothing) if that minigame is not the open one: duplicate callbacks are ignored.
    public bool CompleteMinigame(TrainingFocus role, float rawResult)
    {
        if (role == TrainingFocus.None || role != CurrentMinigame || Terminated) return false;
        if (float.IsNaN(rawResult) || float.IsInfinity(rawResult))
        {
            Debug.LogWarning("FlightPhases: invalid " + RoleName(role) + " result (" + rawResult + "), counted as 0.");
            rawResult = 0f;
        }

        MissionResults f = Flight;
        SetResult(f, role, rawResult); // raw, clamped; the scoring applies the skill once
        GameManager gm = GameManager.Instance;
        float skill = gm.SkillMultiplier(RandomEvents.AstronautFor(role), role);
        EventPhase phase = screen != null ? screen.CurrentPhase : EventPhase.Departure;
        MissionFailureRules.Outcome o = MissionFailureRules.Judge(role, phase, rawResult, skill, current.incident, current.attempt, Settings);

        bool recovery = current.incident == EventSeverity.Serious || current.incident == EventSeverity.Critical;
        if (recovery)
        {
            f.recoveryAttempts++;
            f.recoveryCompleted = o.decision == MissionFailureRules.Decision.Continue;
        }
        if (o.message != "" && screen != null) screen.Log(o.message);

        Request done = current;
        CurrentMinigame = TrainingFocus.None;

        if (o.decision == MissionFailureRules.Decision.Fail)
        {
            FailMission(phase, o.code, o.message);
            return true;
        }
        if (o.decision == MissionFailureRules.Decision.Retry)
        {
            done.attempt++;
            queue.Insert(0, done); // opens next frame, after the minigame has closed itself
        }
        if (screen != null) screen.paused = false;
        return true;
    }

    // Resumes the flight without judging the result (old API, and used when a minigame cannot open).
    public void MinigameDone()
    {
        if (CurrentMinigame == TrainingFocus.None) return;
        CurrentMinigame = TrainingFocus.None;
        if (screen != null) screen.paused = false;
    }

    // Ends the mission ONCE with a failure: records it, drops every queued minigame and finishes the flight.
    // Usable by future minigames (for example a critical return action). Returns false if it had already failed.
    public bool FailMission(EventPhase phase, string code, string reason)
    {
        if (!MissionFailureRules.RecordFailure(Flight, phase, code, reason)) return false;
        queue.Clear();
        CurrentMinigame = TrainingFocus.None;
        if (screen != null)
        {
            screen.paused = false;
            screen.Log(reason);
            screen.Finish(); // shows the end panel; OnMissionFinished awards nothing because the mission failed
        }
        if (MissionTerminated != null) MissionTerminated(reason);
        return true;
    }

    // ---------- Helpers (static, also used by the tests) ----------

    // Writes the milestone of a phase that ended well. The return also ends the flight.
    // Task 31: nothing is awarded after a failure, and a milestone needs the previous one.
    public static void MarkPhaseDone(MissionResults flight, EventPhase phase)
    {
        if (flight == null || flight.HasFailed) return;
        if (phase == EventPhase.Departure) flight.departureDone = true;
        else if (phase == EventPhase.LunarOrbit)
        {
            if (flight.departureDone) flight.lunarOrbitDone = true;
        }
        else if (phase == EventPhase.Return)
        {
            if (flight.departureDone && flight.lunarOrbitDone) flight.safeReturnDone = true;
            flight.flightFinished = true;
        }
    }

    // Phase that comes before this one in the flight (false for Departure and Planning)
    public static bool PhaseBefore(EventPhase phase, out EventPhase before)
    {
        before = EventPhase.Departure;
        if (phase == EventPhase.LunarOrbit) return true;
        if (phase == EventPhase.Return)
        {
            before = EventPhase.LunarOrbit;
            return true;
        }
        return false;
    }

    // Stores the RAW result (0 to 1, without SkillMultiplier) of a minigame in its MissionResults field.
    // If that minigame was already played in this flight, the two results are averaged.
    // Piloting -> pilotingResult, Engineering -> repairResult, Research -> researchResult.
    public static void SetResult(MissionResults flight, TrainingFocus role, float raw)
    {
        if (flight == null) return;
        raw = MissionFailureRules.Clean(raw);
        if (role == TrainingFocus.Piloting) flight.pilotingResult = Combine(flight.pilotingResult, raw);
        else if (role == TrainingFocus.Engineering) flight.repairResult = Combine(flight.repairResult, raw);
        else if (role == TrainingFocus.Research) flight.researchResult = Combine(flight.researchResult, raw);
    }

    static float Combine(float old, float raw)
    {
        return old < 0f ? raw : (old + raw) / 2f; // -1 = not played yet
    }

    public static string RoleName(TrainingFocus role)
    {
        if (role == TrainingFocus.Piloting) return "Piloting";
        if (role == TrainingFocus.Engineering) return "Maintenance";
        if (role == TrainingFocus.Research) return "Research";
        return "None";
    }

    // Crew member of a minigame role, for the log ("pilot", "engineer", "scientist")
    public static string CrewWord(TrainingFocus role)
    {
        if (role == TrainingFocus.Piloting) return "pilot";
        if (role == TrainingFocus.Engineering) return "engineer";
        if (role == TrainingFocus.Research) return "scientist";
        return "crew";
    }

    // Text of an event for the mission control log
    public static string EventLogText(EventData e)
    {
        if (e == null) return "";
        string text = (e.isGood ? "Good news: " : "Problem: ") + e.eventName;
        if (!string.IsNullOrEmpty(e.description)) text += ". " + e.description;
        if (e.moneyChange != 0f) text += " (" + (e.moneyChange > 0f ? "+" : "") + e.moneyChange.ToString("0") + " credits)";
        return text;
    }
}
