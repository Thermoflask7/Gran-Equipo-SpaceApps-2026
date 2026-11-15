using UnityEngine;

// Launch site chosen on the globe. The real values are filled in by P5.
[CreateAssetMenu(menuName = "SpaceApps/Launch site", fileName = "NewSite")]
public class LaunchSiteData : ScriptableObject
{
    public string siteName;
    [TextArea] public string description;
    public float latitude;
    public float longitude;

    // Bad-weather risk per calendar day (0 = clear, 1 = storm).
    // The index is the day chosen in the calendar.
    [Range(0f, 1f)] public float[] weatherRiskByDay = new float[14];
}
