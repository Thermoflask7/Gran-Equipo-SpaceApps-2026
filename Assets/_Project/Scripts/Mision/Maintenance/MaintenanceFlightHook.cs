using UnityEngine;

// Connects the maintenance minigame (task 13) to the flight phases (task 11). Signed: claude_c (audit F08).
// Put it on an ALWAYS ACTIVE object of 02_Lanzamiento (for example the Lanzamiento object, next to FlightPhases).
// The MaintenanceMinigame prefab instance can be inactive: it is found with FindObjectsInactive.Include.
// When FlightPhases asks for an Engineering minigame, it opens the minigame. When the player presses
// "Continue", it stores the RAW result (MaintenanceMinigame.RawResult, without SkillMultiplier) in
// gm.flight.repairResult and resumes the flight. MissionScoring applies the engineer skill once.
//
// Guaranteed maintenance (Claude-M): at scheduledAtProgress (80 %) of scheduledPhase (Lunar orbit) the engineer runs
// a routine check, so maintenance happens in every flight, not only when a bad engineering event rolls. It asks
// FlightPhases for an Engineering minigame with severity Minor (it only costs points, it never aborts), and it is
// skipped if the engineer already played in that phase (for example, a bad event already opened maintenance).
// Lunar orbit has room: research is its only planned minigame and the limit is 2 per phase. Return keeps the
// engineer's wiring moment (WiringFlightHook). While RoutinePending is true, WiringFlightHook opens maintenance.
public class MaintenanceFlightHook : MonoBehaviour
{
    public const string ScheduledSource = "Routine maintenance";

    [Tooltip("Empty = the MaintenanceMinigame in the scene (active or not)")]
    public MaintenanceMinigame minigame;
    [Tooltip("Empty = the FlightPhases on this object, or the one in the scene")]
    public FlightPhases phases;
    [Tooltip("Empty = the LaunchScreen on this object, or the one in the scene")]
    public LaunchScreen screen;

    [Header("Guaranteed maintenance")]
    [Tooltip("Open maintenance once per flight on a schedule, not only when a bad engineering event rolls")]
    public bool scheduled = true;
    [Tooltip("Phase of the routine maintenance check")]
    public EventPhase scheduledPhase = EventPhase.LunarOrbit;
    [Tooltip("Point of that phase (0 to 1) where the check happens. After research (0.5) and the random event (0.5)")]
    [Range(0f, 1f)] public float scheduledAtProgress = 0.8f;

    // True from the scheduled request until maintenance opens (or the phase ends): the Engineering dispatcher
    // (WiringFlightHook) must open maintenance, not wiring.
    public bool RoutinePending { get; private set; }

    bool ready;
    bool open;
    bool checkedThisPhase;

    void Awake()
    {
        if (minigame == null) minigame = FindAnyObjectByType<MaintenanceMinigame>(FindObjectsInactive.Include);
        if (phases == null) phases = GetComponent<FlightPhases>();
        if (phases == null) phases = FindAnyObjectByType<FlightPhases>(FindObjectsInactive.Include);
        if (screen == null) screen = GetComponent<LaunchScreen>();
        if (screen == null) screen = FindAnyObjectByType<LaunchScreen>(FindObjectsInactive.Include);
        if (minigame == null || phases == null)
        {
            Debug.LogWarning("MaintenanceFlightHook: missing MaintenanceMinigame or FlightPhases; maintenance will be skipped in the flight.");
            return;
        }
        ready = true;
        phases.RegisterMinigame(TrainingFocus.Engineering, Open);
        minigame.Completed += OnCompleted;
        minigame.gameObject.SetActive(false); // hidden until the flight asks for it
    }

    // Like FlightPhases, subscribe in OnEnable so the first PhaseStarted (Departure) is not missed
    void OnEnable()
    {
        if (ready && screen != null) screen.PhaseStarted += OnPhaseStarted;
    }

    void OnDisable()
    {
        if (ready && screen != null) screen.PhaseStarted -= OnPhaseStarted;
    }

    void OnDestroy()
    {
        if (phases != null) phases.UnregisterMinigame(TrainingFocus.Engineering, Open);
        if (minigame != null) minigame.Completed -= OnCompleted;
    }

    void OnPhaseStarted(EventPhase phase)
    {
        checkedThisPhase = false;
        RoutinePending = false; // a request the phase dropped must not hijack a later Engineering request
    }

    void Update()
    {
        if (!ready || !scheduled || screen == null || checkedThisPhase) return;
        if (screen.Finished || phases.Terminated || phases.CurrentMinigame != TrainingFocus.None) return;
        if (screen.CurrentPhase != scheduledPhase || screen.PhaseProgress < scheduledAtProgress) return;

        checkedThisPhase = true; // decided once per phase
        if (!ShouldSchedule(phases.PlayedThisPhase(TrainingFocus.Engineering), phases.MinigamesThisPhase,
                phases.maxMinigamesPerPhase))
            return;

        RoutinePending = true;
        screen.Log("Scheduled check: the engineer runs routine maintenance on the ship's systems.");
        phases.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Minor, ScheduledSource);
    }

    // Is there room for the routine check? Not if the engineer already played in this phase or the phase is full.
    public static bool ShouldSchedule(bool engineerPlayed, int minigamesThisPhase, int maxPerPhase)
    {
        if (engineerPlayed) return false;
        if (maxPerPhase > 0 && minigamesThisPhase >= maxPerPhase) return false;
        return true;
    }

    // Public so WiringFlightHook (task 44), which owns the Engineering role when it is in the scene, can open maintenance
    public void Open()
    {
        RoutinePending = false;
        open = true;
        minigame.gameObject.SetActive(true); // shows the intro; the player presses "Start"
    }

    // The argument is FinalResult (with skill): ignored on purpose, RawResult is what the scoring expects.
    // Task 31: CompleteMinigame stores the raw result once and applies the failure rules (resolve, retry or abort).
    // A retry opens this minigame again on the next frame through Open().
    void OnCompleted(float finalResultIgnored)
    {
        if (!open) return;
        open = false;
        // Returns false (and does nothing) if maintenance is no longer the open minigame: a late or duplicate callback
        phases.CompleteMinigame(TrainingFocus.Engineering, minigame.RawResult);
    }
}
