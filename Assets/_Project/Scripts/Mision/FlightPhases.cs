using System;
using System.Collections.Generic;
using UnityEngine;

// Flight phases (task 11): what happens during departure, lunar orbit and return. Signed: claude_c.
// Goes on the same object as LaunchScreen in 02_Lanzamiento (menu SpaceApps -> Flight phases -> Add to open scene).
// LaunchScreen moves the rocket and the cameras; this script decides what happens in each phase:
//   - One random event per phase (RandomEvents: weather and crew change the chance), written in the mission control log.
//   - The planned minigames of each phase (GDD: piloting in the departure, research in lunar orbit),
//     plus the minigame of any event with a minigameRole.
//   - The flight milestones in GameManager.flight (departureDone, lunarOrbitDone, safeReturnDone, flightFinished),
//     which MissionScoring uses for the final score.
// A phase does not end until its event and its minigames are done (it turns off LaunchScreen.autoAdvance).
//
// For the minigames (tasks 12, 13, 14):
//   flightPhases.MinigameRequested += role => { if (role == TrainingFocus.Piloting) OpenMyMinigame(); };
//   // when it ends:
//   FlightPhases.SetResult(GameManager.Instance.flight, TrainingFocus.Piloting, rawResult0to1);
//   flightPhases.MinigameDone();
// The flight is paused (LaunchScreen.paused) while a minigame is open. If no minigame listens to
// MinigameRequested yet, the flight does not stop: the log says the minigame was skipped.
public class FlightPhases : MonoBehaviour
{
    [Serializable]
    public class PlannedMinigame
    {
        public EventPhase phase = EventPhase.Departure;
        public TrainingFocus role = TrainingFocus.Piloting;
        [Range(0f, 1f)] public float atProgress = 0.5f; // point of the phase where it opens (0 to 1)
    }

    [Tooltip("Empty = the LaunchScreen on this object, or the one in the scene")]
    public LaunchScreen screen;

    [Tooltip("Empty = GameData.Instance.events (every imported event)")]
    public List<EventData> events = new List<EventData>();

    [Tooltip("Point of each phase (0 to 1) where the random event is rolled")]
    [Range(0f, 1f)] public float eventAtProgress = 0.5f;

    // Departure: piloting during the liftoff view (first 35 %). Lunar orbit: research.
    // Return has none by default: a second piloting result would be averaged with the first one (see SetResult).
    public List<PlannedMinigame> plannedMinigames = new List<PlannedMinigame>
    {
        new PlannedMinigame { phase = EventPhase.Departure, role = TrainingFocus.Piloting, atProgress = 0.2f },
        new PlannedMinigame { phase = EventPhase.LunarOrbit, role = TrainingFocus.Research, atProgress = 0.5f },
    };

    // A minigame must open (the flight is already paused). Answer with MinigameDone().
    public event Action<TrainingFocus> MinigameRequested;
    // A random event happened in the flight (for example, to show it on the big screen).
    public event Action<EventData> EventHappened;

    public TrainingFocus CurrentMinigame { get; private set; } = TrainingFocus.None; // None = no minigame open
    public EventData LastEvent { get; private set; }

    readonly List<TrainingFocus> queue = new List<TrainingFocus>();
    readonly List<int> plannedDone = new List<int>(); // indexes of plannedMinigames already handled in this phase
    bool eventRolled;

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
        if (screen == null || screen.Finished) return;
        if (CurrentMinigame != TrainingFocus.None) return; // waiting for MinigameDone()

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
            queue.Add(m.role);
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
        // The phase before this one ended well (also when the test "Skip" button jumps ahead)
        EventPhase before;
        if (PhaseBefore(phase, out before)) MarkPhaseDone(GameManager.Instance.flight, before);

        eventRolled = false;
        plannedDone.Clear();
        queue.Clear();
        if (CurrentMinigame != TrainingFocus.None) MinigameDone();
    }

    void OnMissionFinished()
    {
        MarkPhaseDone(GameManager.Instance.flight, EventPhase.Return);
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

        EventData e = RandomEvents.RollAndApply(list, phase);
        if (e == null) return;

        LastEvent = e;
        screen.Log(EventLogText(e));
        if (EventHappened != null) EventHappened(e);

        // A bad event on a role opens that role's minigame (for example, a failure -> maintenance)
        if (!e.isGood && e.minigameRole != TrainingFocus.None) queue.Add(e.minigameRole);
    }

    // ---------- Minigames ----------

    void OpenNextMinigame()
    {
        TrainingFocus role = queue[0];
        queue.RemoveAt(0);

        if (MinigameRequested == null)
        {
            screen.Log(RoleName(role) + " minigame not available yet: skipped.");
            return;
        }

        CurrentMinigame = role;
        screen.paused = true;
        screen.Log(RoleName(role) + " minigame.");
        MinigameRequested(role);
    }

    // Called by the minigame when it ends (after writing its result). Resumes the flight.
    public void MinigameDone()
    {
        if (CurrentMinigame == TrainingFocus.None) return;
        CurrentMinigame = TrainingFocus.None;
        if (screen != null) screen.paused = false;
    }

    // ---------- Helpers (static, also used by the tests) ----------

    // Writes the milestone of a phase that ended well. The return also ends the flight.
    public static void MarkPhaseDone(MissionResults flight, EventPhase phase)
    {
        if (flight == null) return;
        if (phase == EventPhase.Departure) flight.departureDone = true;
        else if (phase == EventPhase.LunarOrbit) flight.lunarOrbitDone = true;
        else if (phase == EventPhase.Return)
        {
            flight.safeReturnDone = true;
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
        raw = Mathf.Clamp01(raw);
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
