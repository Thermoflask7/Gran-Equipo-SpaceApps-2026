using UnityEngine;
using UnityEngine.Rendering;

// Scene 02_Lanzamiento: the weather of the day chosen in the planning calendar
// (GameManager.launchWeather) changes the liftoff view: sky color, sunlight, ambient light and fog.
// Only the liftoff close-up uses it; the space route and the splashdown keep the normal look.
// LaunchScreen adds this component at runtime, so the scene builder does not need to know about it.
// Test from the menu SpaceApps > Debug > Launch weather.
public class LaunchWeatherVisuals : MonoBehaviour
{
    [System.Serializable]
    public struct Preset
    {
        public Color sky;
        [Tooltip("Multiplier of the directional light intensity")]
        public float sunIntensity;
        public Color sunTint;
        [Tooltip("Multiplier of the ambient light")]
        public float ambient;
        [Tooltip("Exponential fog density near the ground (0 = no fog)")]
        public float fogDensity;
    }

    public static readonly Color ClearSky = new Color(0.45f, 0.7f, 0.95f);

    /// <summary>Sky color LaunchScreen uses near the ground during liftoff.</summary>
    public static Color LiftoffSky { get; private set; } = ClearSky;

    public static LaunchWeatherVisuals Instance { get; private set; }

    public Preset sunny  = new Preset { sky = ClearSky,                       sunIntensity = 1f,    sunTint = Color.white,                   ambient = 1f,    fogDensity = 0f };
    public Preset cloudy = new Preset { sky = new Color(0.62f, 0.68f, 0.75f), sunIntensity = 0.65f, sunTint = new Color(0.92f, 0.94f, 0.97f), ambient = 0.85f, fogDensity = 0.004f };
    public Preset rain   = new Preset { sky = new Color(0.42f, 0.47f, 0.53f), sunIntensity = 0.42f, sunTint = new Color(0.85f, 0.88f, 0.93f), ambient = 0.65f, fogDensity = 0.010f };
    public Preset storm  = new Preset { sky = new Color(0.18f, 0.20f, 0.25f), sunIntensity = 0.22f, sunTint = new Color(0.80f, 0.85f, 1f),    ambient = 0.45f, fogDensity = 0.018f };

    public WeatherCondition Condition { get; private set; }

    LaunchScreen launch;
    Light sun;
    float sunIntensity0;
    Color sunColor0;
    SphericalHarmonicsL2 ambient0;
    bool fog0;
    FogMode fogMode0;
    float fogDensity0;
    Color fogColor0;

    void Awake()
    {
        Instance = this;
        launch = GetComponent<LaunchScreen>();
        sun = RenderSettings.sun;
        if (sun == null)
            foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (sun != null) { sunIntensity0 = sun.intensity; sunColor0 = sun.color; }
        ambient0 = RenderSettings.ambientProbe;
        fog0 = RenderSettings.fog;
        fogMode0 = RenderSettings.fogMode;
        fogDensity0 = RenderSettings.fogDensity;
        fogColor0 = RenderSettings.fogColor;
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        LiftoffSky = ClearSky;
        Restore();
    }

    /// <summary>Reads GameManager.launchWeather again (used by the debug menu).</summary>
    public void Refresh()
    {
        Condition = GameManager.Instance.launchWeather;
        LiftoffSky = Current.sky;
    }

    public Preset Current
    {
        get
        {
            switch (Condition)
            {
                case WeatherCondition.Cloudy: return cloudy;
                case WeatherCondition.Rain: return rain;
                case WeatherCondition.Storm: return storm;
                default: return sunny;
            }
        }
    }

    void LateUpdate()
    {
        bool liftoff = launch == null || (!launch.Finished && launch.CurrentStage == FlightStage.Liftoff);
        if (!liftoff) { Restore(); return; }

        Preset p = Current;
        // 1 near the ground, 0 out of the atmosphere (same curve as the sky color in LaunchScreen)
        float air = launch != null ? launch.Atmosphere : 1f;

        if (sun != null)
        {
            sun.intensity = sunIntensity0 * Mathf.Lerp(1f, p.sunIntensity, air);
            sun.color = Color.Lerp(sunColor0, sunColor0 * p.sunTint, air);
        }
        RenderSettings.ambientProbe = ambient0 * Mathf.Lerp(1f, p.ambient, air);

        bool foggy = p.fogDensity > 0f && air > 0.01f;
        RenderSettings.fog = foggy;
        if (foggy)
        {
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = p.fogDensity * air;
            RenderSettings.fogColor = launch != null && launch.viewCamera != null ? launch.viewCamera.backgroundColor : p.sky;
        }
    }

    void Restore()
    {
        if (sun != null) { sun.intensity = sunIntensity0; sun.color = sunColor0; }
        RenderSettings.ambientProbe = ambient0;
        RenderSettings.fog = fog0;
        RenderSettings.fogMode = fogMode0;
        RenderSettings.fogDensity = fogDensity0;
        RenderSettings.fogColor = fogColor0;
    }
}
