using UnityEngine;

// Connects the research minigame (task 14) to the flight phases (task 11). Signed: claude_c.
// Put it on an ALWAYS ACTIVE object of 02_Lanzamiento (the Lanzamiento object, next to FlightPhases);
// SpaceApps > Research minigame > Connect to launch scene does it. The minigame instance can be inactive.
// When FlightPhases asks for Research (lunar orbit), it opens the minigame. On "Continue" it writes
// flight.experimentFraction and flight.downlinkFraction (the player's real values, never invented) and then calls
// FlightPhases.CompleteMinigame(Research, raw) once: raw is stored without the scientist skill, research never aborts.
public class ResearchFlightHook : MonoBehaviour
{
    [Tooltip("Empty = the ResearchMinigame in the scene (active or not)")]
    public ResearchMinigame minigame;
    [Tooltip("Empty = the FlightPhases on this object, or the one in the scene")]
    public FlightPhases phases;

    bool open;

    void Awake()
    {
        if (minigame == null) minigame = FindAnyObjectByType<ResearchMinigame>(FindObjectsInactive.Include);
        if (phases == null) phases = GetComponent<FlightPhases>();
        if (phases == null) phases = FindAnyObjectByType<FlightPhases>(FindObjectsInactive.Include);
        if (minigame == null || phases == null)
        {
            Debug.LogWarning("ResearchFlightHook: missing ResearchMinigame or FlightPhases; research will be skipped in the flight.");
            return;
        }
        phases.RegisterMinigame(TrainingFocus.Research, Open);
        minigame.Completed += OnCompleted;
        minigame.gameObject.SetActive(false); // hidden until the flight asks for it
    }

    void OnDestroy()
    {
        if (phases != null) phases.UnregisterMinigame(TrainingFocus.Research, Open);
        if (minigame != null) minigame.Completed -= OnCompleted;
    }

    void Open()
    {
        open = true;
        minigame.gameObject.SetActive(true); // OnEnable reads the rocket and shows the intro
    }

    void OnCompleted(ResearchResult result)
    {
        if (!open || result == null) return;
        open = false;
        Write(GameManager.Instance.flight, result);
        phases.CompleteMinigame(TrainingFocus.Research, result.raw); // ignored if research is no longer the open minigame
    }

    // Science of the flight. If research is played twice, the best values are kept (never lower what was already achieved).
    public static void Write(MissionResults flight, ResearchResult r)
    {
        if (flight == null || r == null) return;
        float experiments = ResearchMath.Clean(r.experimentFraction);
        float downlink = Mathf.Min(ResearchMath.Clean(r.downlinkFraction), experiments);
        flight.experimentFraction = Mathf.Max(flight.experimentFraction, experiments);
        flight.downlinkFraction = Mathf.Min(Mathf.Max(flight.downlinkFraction, downlink), flight.experimentFraction);
    }
}
