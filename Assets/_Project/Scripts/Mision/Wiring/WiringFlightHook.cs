using UnityEngine;

// Connects the wiring minigame (task 43) to the flight phases (task 11). Signed: claude_c.
// Put it on the ALWAYS ACTIVE Lanzamiento object of 02_Lanzamiento, next to FlightPhases and MaintenanceFlightHook
// (SpaceApps > Wiring minigame > Connect to launch scene does it). The WiringMinigame instance can be inactive.
//
// 1. Wiring fault: at settings.faultAtProgress (70 %) of a phase, if NO minigame opened in that phase, a "wiring fault"
//    happens and asks for an Engineering minigame with severity Minor (it only costs points, it never aborts).
//    Today Departure always has piloting and Lunar orbit research, so it mostly happens in Return, and in any phase
//    whose planned minigame was skipped.
// 2. Engineering dispatcher: FlightPhases has one handler per role, so this hook takes the Engineering role in Start
//    (after MaintenanceFlightHook registered in Awake) and opens:
//      - wiring for its own fault, and for the events in settings.wiringEvents (EV-DE-02 "Unstable telemetry signal");
//      - maintenance (MaintenanceFlightHook.Open) for every other Engineering event; wiring if there is no maintenance.
// 3. Boards: each opening is one round of 3 panels (3, 4 and 5 colors, 40 s in total). The first round of the flight
//    plays boards 1-3 of WiringSettings.boards, the next one boards 4-6, and so on (wrapping around).
// On "Continue" it calls FlightPhases.CompleteMinigame(Engineering, RawResult): raw, once, skill applied by the scoring.
public class WiringFlightHook : MonoBehaviour
{
    public const string FaultSource = "Wiring fault";

    [Tooltip("Empty = the WiringMinigame in the scene (active or not)")]
    public WiringMinigame minigame;
    [Tooltip("Empty = the FlightPhases on this object, or the one in the scene")]
    public FlightPhases phases;
    [Tooltip("Empty = the LaunchScreen on this object, or the one in the scene")]
    public LaunchScreen screen;
    [Tooltip("Empty = the MaintenanceFlightHook in the scene. Opens maintenance for the other Engineering events")]
    public MaintenanceFlightHook maintenance;
    [Tooltip("Turn off to keep only the event-driven wiring (no fault when a phase has no minigame)")]
    public bool faultWhenNoMinigame = true;

    bool ready;
    bool open;            // wiring is the open minigame
    bool faultPending;    // our fault was requested and has not opened yet
    bool faultThisPhase;  // the fault was already requested in this phase
    bool minigameThisPhase;
    int roundsPlayed;

    WiringSettings S
    {
        get { return minigame != null && minigame.settings != null ? minigame.settings : WiringSettings.Default; }
    }

    void Awake()
    {
        if (minigame == null) minigame = FindAnyObjectByType<WiringMinigame>(FindObjectsInactive.Include);
        if (phases == null) phases = GetComponent<FlightPhases>();
        if (phases == null) phases = FindAnyObjectByType<FlightPhases>(FindObjectsInactive.Include);
        if (screen == null) screen = GetComponent<LaunchScreen>();
        if (screen == null) screen = FindAnyObjectByType<LaunchScreen>(FindObjectsInactive.Include);
        if (maintenance == null) maintenance = FindAnyObjectByType<MaintenanceFlightHook>(FindObjectsInactive.Include);
        if (minigame == null || phases == null || screen == null)
        {
            Debug.LogWarning("WiringFlightHook: missing WiringMinigame, FlightPhases or LaunchScreen; the wiring minigame will not appear in the flight.");
            return;
        }
        ready = true;
        minigame.Completed += OnCompleted;
        minigame.gameObject.SetActive(false); // hidden until the flight asks for it
    }

    // Like FlightPhases, subscribe in OnEnable so the first PhaseStarted (Departure) is not missed
    void OnEnable()
    {
        if (!ready) return;
        screen.PhaseStarted += OnPhaseStarted;
        phases.MinigameRequested += OnMinigameOpened;
    }

    void OnDisable()
    {
        if (!ready) return;
        screen.PhaseStarted -= OnPhaseStarted;
        phases.MinigameRequested -= OnMinigameOpened;
    }

    // Start runs after every Awake, so this replaces the handler MaintenanceFlightHook registered
    void Start()
    {
        if (ready) phases.RegisterMinigame(TrainingFocus.Engineering, OpenEngineering);
    }

    void OnDestroy()
    {
        if (phases != null) phases.UnregisterMinigame(TrainingFocus.Engineering, OpenEngineering);
        if (minigame != null) minigame.Completed -= OnCompleted;
    }

    void OnPhaseStarted(EventPhase phase)
    {
        minigameThisPhase = false;
        faultThisPhase = false;
        faultPending = false;
    }

    void OnMinigameOpened(TrainingFocus role)
    {
        minigameThisPhase = true;
    }

    void Update()
    {
        if (!ready || !faultWhenNoMinigame || faultThisPhase || minigameThisPhase) return;
        if (screen.Finished || phases.Terminated || phases.CurrentMinigame != TrainingFocus.None) return;
        if (screen.PhaseProgress < S.faultAtProgress) return;

        faultThisPhase = true;
        faultPending = true;
        screen.Log("Problem: wiring fault! Some cables came loose. The engineer must reconnect the systems.");
        phases.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Minor, FaultSource);
    }

    // FlightPhases asks for an Engineering minigame
    void OpenEngineering()
    {
        bool wiring = faultPending || IsWiringEvent(phases.LastEvent, S) || maintenance == null || !maintenance.isActiveAndEnabled;
        faultPending = false;
        if (!wiring)
        {
            maintenance.Open();
            return;
        }

        open = true;
        minigame.SetRound(roundsPlayed);
        minigame.SetContext(PhaseName(screen.CurrentPhase));
        minigame.gameObject.SetActive(true); // OnEnable builds the board and shows the intro
    }

    void OnCompleted(float raw)
    {
        if (!open) return;
        open = false;
        roundsPlayed++; // the next wiring fault gets new panels
        phases.CompleteMinigame(TrainingFocus.Engineering, raw); // ignored if Engineering is no longer the open minigame
    }

    // True if this event's Engineering minigame is wiring (matches the asset name or the eventName)
    public static bool IsWiringEvent(EventData e, WiringSettings s)
    {
        if (e == null || s == null || s.wiringEvents == null) return false;
        foreach (string id in s.wiringEvents)
        {
            if (string.IsNullOrEmpty(id)) continue;
            if (e.name == id || e.eventName == id) return true;
        }
        return false;
    }

    static string PhaseName(EventPhase phase)
    {
        if (phase == EventPhase.LunarOrbit) return "Lunar orbit";
        return phase.ToString();
    }
}
