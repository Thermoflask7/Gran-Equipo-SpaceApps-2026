using UnityEngine;

// Random event during planning or the flight (good or bad).
// Rolled by RandomEvents (Scripts/Mision).
[CreateAssetMenu(menuName = "SpaceApps/Event", fileName = "NewEvent")]
public class EventData : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;
    [Range(0f, 1f)] public float probability = 0.3f; // base probability
    public bool isGood;
    public float moneyChange; // fictional game credits, can be negative
    public int scoreChange;

    [Header("When and how it happens")]
    public EventPhase phase = EventPhase.Departure;
    public bool weatherSensitive; // if true, the weather on the chosen day changes its probability
    public TrainingFocus minigameRole = TrainingFocus.None; // minigame it triggers (None = none)
    [TextArea] public string lesson; // educational explanation for Results (optional)
}
