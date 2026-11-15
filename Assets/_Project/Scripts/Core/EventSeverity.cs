// How serious a flight event is (task 31, claude_c). Used by MissionFailureRules.
//   Auto     = default for every existing EventData asset: a bad event that requests a minigame is Serious,
//              anything else is Minor. No existing event is ever treated as Critical by default.
//   Minor    = the flight continues; only its score/money/lesson apply (Level A).
//   Serious  = recoverable: its minigame must be played; a critical result gets one more recovery attempt,
//              and only if that attempt is also critical the mission is aborted (Level B).
//   Critical = must be set explicitly on the asset: a critical result in its minigame aborts the mission (Level C).
public enum EventSeverity
{
    Auto,
    Minor,
    Serious,
    Critical
}
