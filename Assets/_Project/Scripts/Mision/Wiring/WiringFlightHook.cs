using UnityEngine;

// Connects the wiring minigame (task 44) to the flight phases (task 11). Signed: claude_c.
// Put it on the ALWAYS ACTIVE Lanzamiento object of 02_Lanzamiento, next to FlightPhases and MaintenanceFlightHook
// (SpaceApps > Wiring minigame > Connect to launch scene does it). The WiringMinigame instance can be inactive.
//
// 1. Wiring fault (task 46 flight plan): at settings.faultAtProgress (70 %) of a phase a "wiring fault" happens and asks
//    for an Engineering minigame with severity Minor (it only costs points, it never aborts):
//      - in guaranteedPhase (Return), whenever the engineer has not played yet in it: the engineer's guaranteed moment
//        (if a bad event already opened maintenance in Return, that was the engineer's turn and there is no wiring);
//      - in any other phase, only if NO minigame opened in it (faultWhenNoMinigame, Cristina's "like a fault" idea);
//      - never when the phase already reached FlightPhases.maxMinigamesPerPhase.
//    ShouldFault() holds this rule.
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
    [Tooltip("Phase where the engineer always gets the wiring minigame (unless maintenance already happened there)")]
    public EventPhase guaranteedPhase = EventPhase.Return;
    [Tooltip("In the other phases, a wiring fault happens when no minigame opened in them")]
    public bool faultWhenNoMinigame = true;

    bool ready;
    bool open;            // wiring is the open minigame
    bool faultPending;    // our fault was requested and has not opened yet
    bool faultThisPhase;  // the fault was already requested in this phase
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
    }

    void OnDisable()
    {
        if (!ready) return;
        screen.PhaseStarted -= OnPhaseStarted;
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
        faultThisPhase = false;
        faultPending = false;
    }

    void Update()
    {
        if (!ready || faultThisPhase) return;
        if (screen.Finished || phases.Terminated || phases.CurrentMinigame != TrainingFocus.None) return;
        if (screen.PhaseProgress < S.faultAtProgress) return;

        faultThisPhase = true; // decided once per phase
        if (!ShouldFault(screen.CurrentPhase, guaranteedPhase, faultWhenNoMinigame,
                phases.PlayedThisPhase(TrainingFocus.Engineering), phases.MinigamesThisPhase, phases.maxMinigamesPerPhase))
            return;

        faultPending = true;
        screen.Log("Problem: wiring fault! Some cables came loose. The engineer must reconnect the systems.");
        phases.RequestMinigame(TrainingFocus.Engineering, EventSeverity.Minor, FaultSource);
    }

    // Task 46 rule: does a wiring fault happen at 70 % of this phase?
    public static bool ShouldFault(EventPhase phase, EventPhase guaranteedPhase, bool faultWhenNoMinigame,
                                   bool engineerPlayed, int minigamesThisPhase, int maxPerPhase)
    {
        if (engineerPlayed) return false;                                     // one minigame per crew role per phase
        if (maxPerPhase > 0 && minigamesThisPhase >= maxPerPhase) return false; // phase is full
        if (phase == guaranteedPhase) return true;                            // the engineer's guaranteed moment
        return faultWhenNoMinigame && minigamesThisPhase == 0;                // a quiet phase gets a fault
    }

    // FlightPhases asks for an Engineering minigame
    void OpenEngineering()
    {
        bool maintenanceReady = maintenance != null && maintenance.isActiveAndEnabled;
        // Claude-M: the scheduled routine maintenance (MaintenanceFlightHook) always opens maintenance, even if
        // LastEvent is still a wiring event from an earlier phase
        bool wiring = !(maintenanceReady && maintenance.RoutinePending)
            && (faultPending || IsWiringEvent(phases.LastEvent, S) || !maintenanceReady);
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
