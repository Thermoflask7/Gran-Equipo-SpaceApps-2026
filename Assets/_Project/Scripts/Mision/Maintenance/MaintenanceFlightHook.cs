using UnityEngine;

// Connects the maintenance minigame (task 13) to the flight phases (task 11). Signed: claude_c (audit F08).
// Put it on an ALWAYS ACTIVE object of 02_Lanzamiento (for example the Lanzamiento object, next to FlightPhases).
// The MaintenanceMinigame prefab instance can be inactive: it is found with FindObjectsInactive.Include.
// When FlightPhases asks for an Engineering minigame, it opens the minigame. When the player presses
// "Continue", it stores the RAW result (MaintenanceMinigame.RawResult, without SkillMultiplier) in
// gm.flight.repairResult and resumes the flight. MissionScoring applies the engineer skill once.
public class MaintenanceFlightHook : MonoBehaviour
{
    [Tooltip("Empty = the MaintenanceMinigame in the scene (active or not)")]
    public MaintenanceMinigame minigame;
    [Tooltip("Empty = the FlightPhases on this object, or the one in the scene")]
    public FlightPhases phases;

    bool open;

    void Awake()
    {
        if (minigame == null) minigame = FindAnyObjectByType<MaintenanceMinigame>(FindObjectsInactive.Include);
        if (phases == null) phases = GetComponent<FlightPhases>();
        if (phases == null) phases = FindAnyObjectByType<FlightPhases>(FindObjectsInactive.Include);
        if (minigame == null || phases == null)
        {
            Debug.LogWarning("MaintenanceFlightHook: missing MaintenanceMinigame or FlightPhases; maintenance will be skipped in the flight.");
            return;
        }
        phases.RegisterMinigame(TrainingFocus.Engineering, Open);
        minigame.Completed += OnCompleted;
        minigame.gameObject.SetActive(false); // hidden until the flight asks for it
    }

    void OnDestroy()
    {
        if (phases != null) phases.UnregisterMinigame(TrainingFocus.Engineering, Open);
        if (minigame != null) minigame.Completed -= OnCompleted;
    }

    void Open()
    {
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
