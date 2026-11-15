using UnityEngine;

// Every balance number of the maintenance minigame (task 13), in one place.
// Asset: Assets/_Project/Data/Minigames/MaintenanceSettings.asset (the menu
// SpaceApps > Maintenance minigame > Create prefab creates it if missing). You can also make one with
// right click > Create > SpaceApps > Maintenance settings.
// It can be changed while the game runs: the panels read these values every frame
// (time ranges apply on the next change).
[CreateAssetMenu(fileName = "MaintenanceSettings", menuName = "SpaceApps/Maintenance settings")]
public class MaintenanceSettings : ScriptableObject
{
    [Header("General")]
    [Tooltip("How long the minigame lasts, in seconds")]
    public float duration = 30f;
    [Tooltip("Wear per second of each repair bar (each system picks a random value between min and max)")]
    public float minDrain = 0.02f;
    public float maxDrain = 0.04f;
    [Tooltip("Chance per second of a sudden failure (spread across the systems), for an engineer with skill 0.5")]
    public float failureChance = 0.2f;
    [Tooltip("Failure chance multiplier for an engineer with skill 0 (worst). Skill 0.5 always gives x1")]
    public float failureMultiplierNoSkill = 1.5f;
    [Tooltip("Failure chance multiplier for an engineer with skill 1 (best)")]
    public float failureMultiplierFullSkill = 0.5f;
    [Tooltip("How much a bar drops on a sudden failure")]
    public float failureDamage = 0.2f;
    [Tooltip("How much a bar rises per second while its panel is OK")]
    public float panelRefill = 0.06f;
    [Tooltip("How much a bar rises with the simple Repair button (only systems without a panel)")]
    public float repairAmount = 0.25f;

    // Failure chance per second for an engineer skill (0 to 1): a better engineer keeps the systems from dropping.
    // Skill 0 -> failureChance x failureMultiplierNoSkill, 0.5 -> x1, 1 -> x failureMultiplierFullSkill.
    public float FailureChanceFor(float skill)
    {
        skill = Mathf.Clamp01(skill);
        float multiplier = skill < 0.5f
            ? Mathf.Lerp(failureMultiplierNoSkill, 1f, skill * 2f)
            : Mathf.Lerp(1f, failureMultiplierFullSkill, (skill - 0.5f) * 2f);
        return Mathf.Max(0f, failureChance * multiplier);
    }

    [Header("Energy (fuses)")]
    [Tooltip("How often a fuse burns out, in seconds (random between min and max)")]
    public float minBurnSeconds = 1.5f;
    public float maxBurnSeconds = 3f;

    [Header("Oxygen (pump)")]
    [Tooltip("Green zone of the oxygen level (0 to 1)")]
    [Range(0f, 1f)] public float sweetMin = 0.4f;
    [Range(0f, 1f)] public float sweetMax = 0.8f;
    [Tooltip("How much the oxygen drops per second")]
    public float oxygenDecay = 0.06f;
    [Tooltip("Oxygen gained per full stroke of the lever")]
    public float pumpGain = 0.25f;
    [Tooltip("Starting level (below the green zone so the player has to pump)")]
    [Range(0f, 1f)] public float oxygenStartLevel = 0.4f;

    [Header("Communications (radio)")]
    [Tooltip("Frequencies shown on the dial, in MHz (S-band, the one Orion uses)")]
    public float minFrequency = 2200f;
    public float maxFrequency = 2300f;
    [Tooltip("How close to the correct frequency the dial must be (fraction of the dial)")]
    [Range(0.005f, 0.1f)] public float tuneTolerance = 0.025f;
    [Tooltip("At this distance from the correct frequency there is no signal (fraction of the dial)")]
    [Range(0.05f, 0.5f)] public float signalRange = 0.25f;
    [Tooltip("How often the frequency drifts, in seconds (random between min and max)")]
    public float radioMinChangeSeconds = 5f;
    public float radioMaxChangeSeconds = 9f;

    [Header("Propulsion (LEDs)")]
    [Tooltip("How often the LEDs change, in seconds (random between min and max)")]
    public float ledMinChangeSeconds = 3f;
    public float ledMaxChangeSeconds = 6f;
}
