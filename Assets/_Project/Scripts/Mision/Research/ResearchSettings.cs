using UnityEngine;

// Every tunable number of the research minigame (task 14, claude_c). Gameplay values, not real mission data.
// Asset: Data/Minigames/ResearchSettings.asset (created by SpaceApps > Research minigame > Create prefab).
// Design: Docs/Task14/RESEARCH_MINIGAME_DESIGN.md
[CreateAssetMenu(menuName = "SpaceApps/Research settings", fileName = "ResearchSettings")]
public class ResearchSettings : ScriptableObject
{
    [Header("Game 1: Moon Photo Safari")]
    [Min(5f)] public float safariSeconds = 30f;
    [Min(0)] public int landmarkTargets = 6;   // ART-SCI-LUNAR (and any unknown instrument)
    [Min(0)] public int sparkTargets = 12;     // ART-SCI-RAD: radiation balls from every direction
    [Min(0)] public int iceTargets = 3;        // ART-SCI-HEAVY
    [Min(0.5f)] public float landmarkSeconds = 3.5f; // time a landmark / ice target takes to cross the screen
    [Min(0.3f)] public float sparkSeconds = 2.2f;    // time a spark takes to cross the whole width (shorter paths are faster)
    [Min(0.2f)] public float iceHoldSeconds = 1.5f;  // hold the click this long on the ice to scan it
    [Min(10f)] public float viewfinderRadius = 90f;  // px, in a 1920x1080 reference canvas
    [Min(5f)] public float goldRadius = 35f;         // centered closer than this = gold star photo

    [Header("Game 2: Signal Catcher")]
    [Min(5f)] public float catcherSeconds = 25f;
    [Range(2f, 45f)] public float beamHalfAngle = 12f;     // degrees
    [Min(0.5f)] public float minBeamSeconds = 8f;          // beam time to send everything, plus...
    [Min(0f)] public float extraBeamSeconds = 10f;         // ... this x (real downlink time / mission max downlink time)
    [Min(1f)] public float earthPeriodSeconds = 9f;        // Earth swings left-right once in this time
    [Min(1f)] public float earthBobSeconds = 5.5f;         // ... and up-down once in this time (always above the Moon)

    [Header("Result (raw) weights; they should add up to 1")]
    [Range(0f, 1f)] public float experimentWeight = 0.45f;
    [Range(0f, 1f)] public float downlinkWeight = 0.45f;
    [Range(0f, 1f)] public float goldWeight = 0.10f;

    static ResearchSettings defaults;

    public static ResearchSettings Default
    {
        get
        {
            if (defaults == null)
            {
                defaults = CreateInstance<ResearchSettings>();
                defaults.hideFlags = HideFlags.HideAndDontSave;
            }
            return defaults;
        }
    }
}
