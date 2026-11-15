using UnityEditor;
using UnityEngine;

// FlightPhases tests (task 11). Signed: claude_c.
// Menu: SpaceApps -> Flight phases -> Tests. Writes each test to the console with OK or FAILED.
// Only the static helpers: the phase flow itself needs Play mode in 02_Lanzamiento.
public static class FlightPhasesTests
{
    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Flight phases/Tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        // Milestones, in flight order
        MissionResults f = new MissionResults();
        Check("New flight has no milestones", !f.departureDone && !f.lunarOrbitDone && !f.safeReturnDone && !f.flightFinished);
        FlightPhases.MarkPhaseDone(f, EventPhase.Departure);
        Check("Departure done", f.departureDone && !f.lunarOrbitDone && !f.flightFinished);
        FlightPhases.MarkPhaseDone(f, EventPhase.LunarOrbit);
        Check("Lunar orbit done", f.lunarOrbitDone && !f.safeReturnDone && !f.flightFinished);
        FlightPhases.MarkPhaseDone(f, EventPhase.Return);
        Check("Return done ends the flight", f.safeReturnDone && f.flightFinished);
        MissionResults p = new MissionResults();
        FlightPhases.MarkPhaseDone(p, EventPhase.Planning);
        Check("Planning marks nothing", !p.departureDone && !p.lunarOrbitDone && !p.safeReturnDone && !p.flightFinished);
        FlightPhases.MarkPhaseDone(null, EventPhase.Return); // must not throw
        Check("Null flight does not throw", true);

        // Phase before
        EventPhase before;
        Check("Nothing before departure", !FlightPhases.PhaseBefore(EventPhase.Departure, out before));
        Check("Departure before lunar orbit", FlightPhases.PhaseBefore(EventPhase.LunarOrbit, out before) && before == EventPhase.Departure);
        Check("Lunar orbit before return", FlightPhases.PhaseBefore(EventPhase.Return, out before) && before == EventPhase.LunarOrbit);
        Check("Nothing before planning", !FlightPhases.PhaseBefore(EventPhase.Planning, out before));

        // Minigame results
        MissionResults r = new MissionResults();
        FlightPhases.SetResult(r, TrainingFocus.Piloting, 0.8f);
        Check("Piloting goes to pilotingResult", Near(r.pilotingResult, 0.8f) && r.repairResult < 0f && r.researchResult < 0f);
        FlightPhases.SetResult(r, TrainingFocus.Engineering, 0.4f);
        Check("Engineering goes to repairResult", Near(r.repairResult, 0.4f));
        FlightPhases.SetResult(r, TrainingFocus.Research, 1f);
        Check("Research goes to researchResult", Near(r.researchResult, 1f));
        FlightPhases.SetResult(r, TrainingFocus.Piloting, 0.4f);
        Check("Second piloting is averaged", Near(r.pilotingResult, 0.6f));
        MissionResults c = new MissionResults();
        FlightPhases.SetResult(c, TrainingFocus.Piloting, 1.7f);
        FlightPhases.SetResult(c, TrainingFocus.Research, -0.5f);
        Check("Results are clamped to 0..1", Near(c.pilotingResult, 1f) && Near(c.researchResult, 0f));
        MissionResults n = new MissionResults();
        FlightPhases.SetResult(n, TrainingFocus.None, 0.5f);
        Check("Role None changes nothing", n.pilotingResult < 0f && n.repairResult < 0f && n.researchResult < 0f);

        // Texts
        Check("Role names", FlightPhases.RoleName(TrainingFocus.Engineering) == "Maintenance" && FlightPhases.RoleName(TrainingFocus.Research) == "Research");
        EventData e = ScriptableObject.CreateInstance<EventData>();
        e.eventName = "Thruster fault";
        e.description = "A thruster stops responding";
        e.isGood = false;
        e.moneyChange = -50f;
        string text = FlightPhases.EventLogText(e);
        Check("Event log text", text.StartsWith("Problem: Thruster fault") && text.Contains("-50 credits"));
        e.isGood = true;
        e.moneyChange = 0f;
        text = FlightPhases.EventLogText(e);
        Check("Good event without money", text.StartsWith("Good news:") && !text.Contains("credits"));
        Object.DestroyImmediate(e);
        Check("Null event gives empty text", FlightPhases.EventLogText(null) == "");

        // Default plan follows the GDD: piloting in the departure, research in lunar orbit
        GameObject temp = new GameObject("TestFlightPhases");
        temp.hideFlags = HideFlags.HideAndDontSave;
        FlightPhases phases = temp.AddComponent<FlightPhases>();
        bool piloting = false;
        bool research = false;
        foreach (FlightPhases.PlannedMinigame m in phases.plannedMinigames)
        {
            if (m.phase == EventPhase.Departure && m.role == TrainingFocus.Piloting) piloting = true;
            if (m.phase == EventPhase.LunarOrbit && m.role == TrainingFocus.Research) research = true;
        }
        Check("Default plan: departure piloting and lunar orbit research", piloting && research);
        Object.DestroyImmediate(temp);

        string summary = "Flight phases tests: " + passed + " OK, " + failed + " FAILED.";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
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
