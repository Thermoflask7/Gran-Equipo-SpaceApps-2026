using UnityEngine;

// The mission (for now: Artemis-style crewed lunar orbit, Cristina's mission V13).
// Defines what the spacecraft needs in order to launch.
[CreateAssetMenu(menuName = "SpaceApps/Mission", fileName = "NewMission")]
public class DestinationData : ScriptableObject
{
    public string destinationName;
    [TextArea] public string description;
    public float launchDeltaV;   // no longer used: the SLS is a complete service (kept so assets do not break)
    public float requiredDeltaV; // m/s the spacecraft must provide after translunar injection: lunar orbit arrival + return + reserve
    public float maxMass;        // no longer used (the limit is the capacity of the chosen SLS)
    public int missionDays;
    public bool locked; // true = shown as "coming soon"

    [Header("Mass at translunar injection (model AM-19)")]
    public float tliExtraMass = 682f; // kg: mass at translunar injection (26,536) minus mass after it (25,854)

    [Header("Crew and consumables")]
    public int crewCount = 3;
    public float waterPerPersonDay = 2.5f;  // kg
    public float oxygenPerPersonDay = 0.84f;
    public float foodPerPersonDay = 0.75f;
    public float baseWaterKg = 101.25f;        // already included in Orion's mass
    public float baseOxygenKg = 35.25f;
    public float baseFoodKg = 33f;

    [Header("Science")]
    public float minSciencePoints = 40f;
    public float maxDownlinkSeconds = 3600f; // downlink all the data in at most 1 hour
}
