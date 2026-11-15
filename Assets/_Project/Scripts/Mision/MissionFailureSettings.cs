using UnityEngine;

// Tunable thresholds for mission failure (task 31, claude_c). These are GAMEPLAY values for an educational
// game, chosen for playability; they are NOT NASA flight-certification limits.
// Optional asset: Create > SpaceApps > Mission failure settings, then drag it to FlightPhases.failureSettings.
// Without an asset, MissionFailureSettings.Default (the values below) is used.
//
// Performance is judged on effective = raw minigame result x crew SkillMultiplier (0 to 1).
// That effective value is ONLY used to decide failure; MissionResults always stores the raw result,
// and MissionScoring applies the skill once when it calculates points.
[CreateAssetMenu(menuName = "SpaceApps/Mission failure settings", fileName = "MissionFailureSettings")]
public class MissionFailureSettings : ScriptableObject
{
    [Header("Performance bands (effective = raw x skill)")]
    [Tooltip("Below this: critical. Only matters for safety-critical actions.")]
    [Range(0f, 1f)] public float criticalBelow = 0.20f;
    [Tooltip("Below this (and above critical): poor. The flight continues with lower points.")]
    [Range(0f, 1f)] public float poorBelow = 0.40f;
    [Range(0f, 1f)] public float goodFrom = 0.70f;
    [Range(0f, 1f)] public float excellentFrom = 0.90f;

    [Header("Safety-critical piloting")]
    [Tooltip("A critical piloting result in the departure aborts the mission")]
    public bool departurePilotingIsCritical = true;
    [Tooltip("A critical piloting result in the return fails the safe return (only if a return piloting minigame is planned)")]
    public bool returnPilotingIsCritical = true;

    [Header("Recovery attempts after a critical result")]
    [Tooltip("Extra attempts for a Serious incident (bad event with a minigame). 1 = one second chance")]
    [Min(0)] public int seriousRetries = 1;
    [Tooltip("Extra attempts for an explicitly Critical incident")]
    [Min(0)] public int criticalRetries = 0;

    static MissionFailureSettings defaults;

    // The values above, for code and tests that have no asset
    public static MissionFailureSettings Default
    {
        get
        {
            if (defaults == null)
            {
                defaults = CreateInstance<MissionFailureSettings>();
                defaults.hideFlags = HideFlags.HideAndDontSave;
            }
            return defaults;
        }
    }
}
