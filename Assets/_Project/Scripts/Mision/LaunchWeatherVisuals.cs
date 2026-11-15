using UnityEngine;
using UnityEngine.Rendering;

// Scene 02_Lanzamiento: the weather of the day chosen in the planning calendar
// (GameManager.launchWeather) changes the liftoff view: sky color, sunlight, ambient light, fog and clouds.
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
        [Header("Clouds (the 'Nube' spheres of the liftoff view)")]
        public Color cloudColor;
        [Tooltip("Size multiplier of the clouds")]
        public float cloudScale;
        [Tooltip("Height multiplier of the clouds (lower = closer to the pad)")]
        public float cloudHeight;
        [Tooltip("Extra clouds added around the pad")]
        public int extraClouds;
    }

    public static readonly Color ClearSky = new Color(0.45f, 0.7f, 0.95f);

    /// <summary>Sky color LaunchScreen uses near the ground during liftoff.</summary>
    public static Color LiftoffSky { get; private set; } = ClearSky;

    public static LaunchWeatherVisuals Instance { get; private set; }

    public Preset sunny  = new Preset { sky = ClearSky,                       sunIntensity = 1f,    sunTint = Color.white,                   ambient = 1f,    fogDensity = 0f,
                                        cloudColor = Color.white,                cloudScale = 1f,   cloudHeight = 1f,    extraClouds = 0 };
    public Preset cloudy = new Preset { sky = new Color(0.62f, 0.68f, 0.75f), sunIntensity = 0.65f, sunTint = new Color(0.92f, 0.94f, 0.97f), ambient = 0.85f, fogDensity = 0.004f,
                                        cloudColor = new Color(0.82f, 0.84f, 0.87f), cloudScale = 1.4f, cloudHeight = 0.85f, extraClouds = 5 };
    public Preset rain   = new Preset { sky = new Color(0.42f, 0.47f, 0.53f), sunIntensity = 0.42f, sunTint = new Color(0.85f, 0.88f, 0.93f), ambient = 0.65f, fogDensity = 0.010f,
                                        cloudColor = new Color(0.55f, 0.58f, 0.62f), cloudScale = 1.7f, cloudHeight = 0.7f,  extraClouds = 9 };
    public Preset storm  = new Preset { sky = new Color(0.18f, 0.20f, 0.25f), sunIntensity = 0.22f, sunTint = new Color(0.80f, 0.85f, 1f),    ambient = 0.45f, fogDensity = 0.018f,
                                        cloudColor = new Color(0.30f, 0.32f, 0.36f), cloudScale = 2f,   cloudHeight = 0.6f,  extraClouds = 12 };

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

    class Cloud { public Transform t; public Renderer r; public Vector3 pos, scale; }
    readonly System.Collections.Generic.List<Cloud> clouds = new System.Collections.Generic.List<Cloud>();
    readonly System.Collections.Generic.List<GameObject> extras = new System.Collections.Generic.List<GameObject>();
    MaterialPropertyBlock block;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

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
        FindClouds();
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
        ApplyClouds();
    }

    // ---------- Clouds ----------

    void FindClouds()
    {
        Transform site = launch != null ? launch.liftoffSite : null;
        if (site == null) return;
        foreach (Transform child in site)
        {
            if (!child.name.StartsWith("Nube")) continue;
            Renderer r = child.GetComponent<Renderer>();
            if (r == null) continue;
            clouds.Add(new Cloud { t = child, r = r, pos = child.localPosition, scale = child.localScale });
        }
    }

    void ApplyClouds()
    {
        foreach (GameObject go in extras) if (go != null) Destroy(go);
        extras.Clear();
        if (clouds.Count == 0) return;

        Preset p = Current;
        foreach (Cloud c in clouds)
        {
            c.t.localPosition = new Vector3(c.pos.x, c.pos.y * p.cloudHeight, c.pos.z);
            c.t.localScale = Scaled(c.scale, p.cloudScale);
            Tint(c.r, p.cloudColor);
        }

        // Extra clouds: copies of the originals in a ring around the pad, same layout every run
        var rng = new System.Random(1234 + (int)Condition);
        for (int i = 0; i < p.extraClouds; i++)
        {
            Cloud src = clouds[i % clouds.Count];
            GameObject go = Instantiate(src.t.gameObject, src.t.parent);
            go.name = "NubeClima" + (i + 1);
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            float radius = Mathf.Lerp(15f, 50f, (float)rng.NextDouble());
            float height = Mathf.Lerp(22f, 72f, (float)rng.NextDouble()) * p.cloudHeight;
            go.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            go.transform.localRotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
            go.transform.localScale = Scaled(src.scale, p.cloudScale * Mathf.Lerp(0.8f, 1.2f, (float)rng.NextDouble()));
            Tint(go.GetComponent<Renderer>(), p.cloudColor);
            extras.Add(go);
        }
    }

    static Vector3 Scaled(Vector3 s, float k)
    {
        // Wider and a bit thicker, so they read as heavy clouds and not just bigger ones
        return new Vector3(s.x * k, s.y * Mathf.Lerp(1f, k, 0.6f), s.z * k);
    }

    // Per-renderer color: the shared 'Nube' material asset is never modified
    void Tint(Renderer r, Color color)
    {
        if (r == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        r.SetPropertyBlock(block);
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
