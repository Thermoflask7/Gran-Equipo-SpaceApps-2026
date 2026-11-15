using UnityEngine;

// Connects the piloting minigame (task 12) to the flight phases (task 11). By Claude-M, same pattern as
// MaintenanceFlightHook. Put it on an ALWAYS ACTIVE object of 02_Lanzamiento (for example the Lanzamiento
// object, next to FlightPhases). The PilotingMinigame prefab instance can be inactive: it is found with
// FindObjectsInactive.Include.
// When FlightPhases asks for a Piloting minigame, it picks the command sequence of the current phase
// (ascent, lunar orbit or reentry) and opens the minigame. When the player presses "Continue", it reports
// the RAW result (PilotingMinigame.RawResult, without SkillMultiplier) with CompleteMinigame, which stores it
// in gm.flight.pilotingResult and applies the task 31 failure rules. MissionScoring applies the pilot skill once.
public class PilotingFlightHook : MonoBehaviour
{
    [Tooltip("Empty = the PilotingMinigame in the scene (active or not)")]
    public PilotingMinigame minigame;
    [Tooltip("Empty = the FlightPhases on this object, or the one in the scene")]
    public FlightPhases phases;
    [Tooltip("Empty = FlightPhases.screen, or the LaunchScreen in the scene")]
    public LaunchScreen screen;

    bool open;

    void Awake()
    {
        if (minigame == null) minigame = FindAnyObjectByType<PilotingMinigame>(FindObjectsInactive.Include);
        if (phases == null) phases = GetComponent<FlightPhases>();
        if (phases == null) phases = FindAnyObjectByType<FlightPhases>(FindObjectsInactive.Include);
        if (minigame == null || phases == null)
        {
            Debug.LogWarning("PilotingFlightHook: missing PilotingMinigame or FlightPhases; piloting will be skipped in the flight.");
            return;
        }
        phases.RegisterMinigame(TrainingFocus.Piloting, Open);
        minigame.Completed += OnCompleted;
        minigame.gameObject.SetActive(false); // hidden until the flight asks for it
    }

    void OnDestroy()
    {
        if (phases != null) phases.UnregisterMinigame(TrainingFocus.Piloting, Open);
        if (minigame != null) minigame.Completed -= OnCompleted;
    }

    void Open()
    {
        open = true;
        LaunchScreen s = screen != null ? screen : phases.screen;
        if (s == null) s = FindAnyObjectByType<LaunchScreen>();
        minigame.SetPhase(s != null ? s.CurrentPhase : EventPhase.Departure);
        minigame.gameObject.SetActive(true); // shows the intro; the player presses "Start"
    }

    // The argument is FinalResult (with skill): ignored on purpose, RawResult is what the scoring expects.
    // A recovery retry (task 31) opens this minigame again through Open().
    void OnCompleted(float finalResultIgnored)
    {
        if (!open) return;
        open = false;
        // Returns false (and does nothing) if piloting is no longer the open minigame: a late or duplicate callback
        phases.CompleteMinigame(TrainingFocus.Piloting, minigame.RawResult);
    }
}
