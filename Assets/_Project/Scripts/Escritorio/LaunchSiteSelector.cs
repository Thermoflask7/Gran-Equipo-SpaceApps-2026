using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Launch site markers on the planning wall world map (task 6). Lives on the WorldMap quad.
/// Markers are built at runtime from <see cref="sites"/>; clicking one selects it (red),
/// the others stay white. Only clickable while the PlanningStation is zoomed in.
/// Positions are UVs on WorldMap.png (0,0 = top-left), calibrated by hand because the
/// map does not use an equirectangular projection.
/// </summary>
public class LaunchSiteSelector : MonoBehaviour
{
    [Serializable]
    public class Site
    {
        public string id;
        public string displayName;
        [Tooltip("Optional game data for this site (weather, coordinates).")]
        public LaunchSiteData data;
        [Tooltip("Real position on WorldMap.png as UV, (0,0) = top-left, (1,1) = bottom-right.")]
        public Vector2 mapUv;
        [Tooltip("Extra offset in meters on the wall, to separate sites that are too close.")]
        public Vector2 nudge;
        [Tooltip("Label on the right of the marker (otherwise on the left).")]
        public bool labelOnRight = true;
        public float latitude;
        public float longitude;
        [Tooltip("Info text shown in the panel above the calendar (TMP rich text). Empty = built-in text.")]
        [TextArea(4, 12)] public string info;

        /// <summary>The info text, falling back to the built-in text for this site id.</summary>
        public string InfoText => string.IsNullOrEmpty(info) ? DefaultInfo(id) : info;
    }

    [SerializeField] List<Site> sites = DefaultSites();

    [Header("Look")]
    [SerializeField] float markerSize = 0.028f;
    [SerializeField] float labelFontSize = 0.16f;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = new Color(0.95f, 0.12f, 0.12f, 1f);
    [SerializeField] float wallOffset = 0.002f;
    [SerializeField] float maxClickDistance = 50f;

    public event Action<Site> SelectionChanged;
    public Site Selected => selectedIndex >= 0 && selectedIndex < sites.Count ? sites[selectedIndex] : null;
    public IReadOnlyList<Site> Sites => sites;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    readonly List<Renderer> markerRenderers = new List<Renderer>();
    readonly List<TextMeshPro> labels = new List<TextMeshPro>();
    readonly List<Collider> markerColliders = new List<Collider>();
    Transform container;
    Material markerMaterial;
    Texture2D circleTexture;
    MaterialPropertyBlock block;
    PlanningStation station;
    int selectedIndex = -1;
    float alpha = 1f;

    const string H = "<color=#F28C26><b>";
    const string HE = "</b></color>";

    /// <summary>Real-world info about each launch site (English, TMP rich text).</summary>
    public static string DefaultInfo(string id)
    {
        switch (id)
        {
            case "KSC":
                return H + "Location" + HE + "\n" +
                    "Merritt Island, Florida, USA (28.6\u00b0 N, 80.6\u00b0 W), on the Atlantic coast next to Cape Canaveral.\n\n" +
                    H + "Who uses it" + HE + "\n" +
                    "NASA's main launch center. Launch Complex 39A is leased to SpaceX; Launch Complex 39B is NASA's pad for the SLS rocket.\n\n" +
                    H + "What it is used for" + HE + "\n" +
                    "Crewed and heavy-lift launches. Launching east over the ocean adds speed from Earth's rotation, which suits low Earth orbit, the Moon and deep space.\n\n" +
                    H + "Key operations" + HE + "\n" +
                    "\u2022 Apollo 11 lifted off from LC-39A on July 16, 1969.\n" +
                    "\u2022 Home of the Space Shuttle program (1981-2011).\n" +
                    "\u2022 SpaceX Demo-2 (May 30, 2020) restarted crewed launches from US soil.\n" +
                    "\u2022 Artemis I launched on SLS from LC-39B on November 16, 2022.\n" +
                    "\u2022 The Vehicle Assembly Building stacks rockets before they roll to the pad.";
            case "CCSFS":
                return H + "Location" + HE + "\n" +
                    "Cape Canaveral, Florida, USA (28.5\u00b0 N, 80.6\u00b0 W), just south of Kennedy Space Center.\n\n" +
                    H + "Who uses it" + HE + "\n" +
                    "Run by the US Space Force (Space Launch Delta 45). Commercial pads include SpaceX SLC-40, ULA SLC-41 and Blue Origin LC-36.\n\n" +
                    H + "What it is used for" + HE + "\n" +
                    "The busiest US launch range: satellites, cargo and crew to the ISS, military payloads and planetary probes.\n\n" +
                    H + "Key operations" + HE + "\n" +
                    "\u2022 First launch from the Cape: Bumper 8, July 24, 1950.\n" +
                    "\u2022 Explorer 1, the first US satellite, January 31, 1958.\n" +
                    "\u2022 Mercury flights: Alan Shepard (1961) and John Glenn's first US orbit (1962).\n" +
                    "\u2022 The Gemini crewed missions of the 1960s.\n" +
                    "\u2022 New Horizons (2006) and the Mars rovers Curiosity (2011) and Perseverance (2020).\n\n" +
                    "<i>Note: in real life SLS only launches from Kennedy LC-39B.</i>";
            case "VSFB":
                return H + "Location" + HE + "\n" +
                    "Near Lompoc, Santa Barbara County, California, USA (34.7\u00b0 N, 120.6\u00b0 W), on the Pacific coast.\n\n" +
                    H + "Who uses it" + HE + "\n" +
                    "Run by the US Space Force (Space Launch Delta 30). Used by SpaceX (SLC-4E), ULA, Firefly and NASA science missions.\n\n" +
                    H + "What it is used for" + HE + "\n" +
                    "Polar and sun-synchronous orbits: rockets fly south over the open Pacific without passing over cities. Ideal for Earth-observation satellites. Also used for missile tests.\n\n" +
                    H + "Key operations" + HE + "\n" +
                    "\u2022 First launch: Discoverer 1, February 28, 1959, aimed at a polar orbit.\n" +
                    "\u2022 SLC-6 was built for Space Shuttle launches, but no Shuttle ever flew from it.\n" +
                    "\u2022 Landsat Earth-observation satellites.\n" +
                    "\u2022 NASA's DART asteroid-impact mission launched here in November 2021.\n" +
                    "\u2022 Minuteman III missile test launches.\n\n" +
                    "<i>Note: in real life SLS only launches from Kennedy LC-39B.</i>";
            default:
                return "No information for this site yet.";
        }
    }

    public static List<Site> DefaultSites()
    {
        return new List<Site>
        {
            new Site { id = "KSC", displayName = "Kennedy Space Center", mapUv = new Vector2(0.1704f, 0.3559f),
                       nudge = new Vector2(-0.004f, 0.016f), labelOnRight = true, latitude = 28.6273f, longitude = -80.6209f },
            new Site { id = "CCSFS", displayName = "Cape Canaveral SFS", mapUv = new Vector2(0.1709f, 0.3586f),
                       nudge = new Vector2(0.006f, -0.016f), labelOnRight = true, latitude = 28.4889f, longitude = -80.5778f },
            new Site { id = "VSFB", displayName = "Vandenberg SFB", mapUv = new Vector2(0.0609f, 0.3165f),
                       nudge = Vector2.zero, labelOnRight = true, latitude = 34.7420f, longitude = -120.5724f },
        };
    }

    void Awake()
    {
        station = GetComponentInParent<PlanningStation>();
        Build();
    }

    void OnDestroy()
    {
        if (markerMaterial != null) Destroy(markerMaterial);
        if (circleTexture != null) Destroy(circleTexture);
    }

    void Build()
    {
        if (container != null) Destroy(container.gameObject);
        markerRenderers.Clear();
        labels.Clear();
        markerColliders.Clear();

        // Undo the map's non-uniform scale so children work in meters.
        var go = new GameObject("LaunchSites");
        container = go.transform;
        container.SetParent(transform, false);
        Vector3 s = transform.localScale;
        container.localScale = new Vector3(1f / Mathf.Max(1e-4f, s.x), 1f / Mathf.Max(1e-4f, s.y), 1f);
        float width = s.x, height = s.y;

        EnsureMaterial();
        for (int i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            // Unity's Quad: local +X is the viewer's right, UV (0,0) is bottom-left.
            Vector3 pos = new Vector3((site.mapUv.x - 0.5f) * width + site.nudge.x,
                                      (0.5f - site.mapUv.y) * height + site.nudge.y,
                                      -wallOffset);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = "Site_" + site.id;
            var mc = marker.GetComponent<Collider>();
            if (mc != null) Destroy(mc);
            var box = marker.AddComponent<BoxCollider>();
            box.size = new Vector3(1.8f, 1.8f, 0.2f);
            marker.transform.SetParent(container, false);
            marker.transform.localPosition = pos;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = new Vector3(markerSize, markerSize, markerSize);
            var r = marker.GetComponent<Renderer>();
            r.sharedMaterial = markerMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            markerRenderers.Add(r);
            markerColliders.Add(box);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(container, false);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = site.displayName;
            tmp.fontSize = labelFontSize;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = site.labelOnRight ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);
            tmp.rectTransform.sizeDelta = new Vector2(0.3f, 0.03f);
            float side = site.labelOnRight ? 1f : -1f;
            tmp.rectTransform.pivot = new Vector2(site.labelOnRight ? 0f : 1f, 0.5f);
            labelGo.transform.localPosition = pos + new Vector3(side * markerSize * 0.75f, 0f, -0.001f);
            labelGo.transform.localRotation = Quaternion.identity;
            labels.Add(tmp);
        }
        Refresh();
    }

    void EnsureMaterial()
    {
        if (circleTexture == null) circleTexture = MakeCircle(128);
        if (markerMaterial != null) return;
        // Copy the map material so the marker uses the same (already included) URP Unlit transparent variant.
        var mapRenderer = GetComponent<Renderer>();
        Material src = mapRenderer != null ? mapRenderer.sharedMaterial : null;
        markerMaterial = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        markerMaterial.name = "LaunchSiteMarker (runtime)";
        markerMaterial.SetTexture(BaseMapId, circleTexture);
        markerMaterial.mainTexture = circleTexture;
        markerMaterial.renderQueue = 3001;
    }

    static Texture2D MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LaunchSiteCircle" };
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
            byte a = (byte)(Mathf.Clamp01((1f - d) * c * 0.5f) * 255f);
            // White fill with a dark rim so the marker reads on land and ocean.
            byte v = d > 0.78f ? (byte)30 : (byte)255;
            px[y * size + x] = new Color32(v, v, v, a);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    void Update()
    {
        if (station == null) station = GetComponentInParent<PlanningStation>();
        if (station == null || !station.IsOpen) return;
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var dc = DeskCamera.Instance;
        Camera cam = dc != null ? dc.GetComponent<Camera>() : Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var hits = Physics.RaycastAll(ray, maxClickDistance, ~0, QueryTriggerInteraction.Collide);
        float best = float.PositiveInfinity;
        int index = -1;
        foreach (var h in hits)
        {
            int i = markerColliders.IndexOf(h.collider);
            if (i >= 0 && h.distance < best) { best = h.distance; index = i; }
        }
        if (index >= 0) Select(index);
    }

    /// <summary>Select a site by index (-1 clears). The selected marker turns red.</summary>
    public void Select(int index)
    {
        if (index < -1 || index >= sites.Count) return;
        if (index == selectedIndex) return;
        selectedIndex = index;
        Refresh();
        SelectionChanged?.Invoke(Selected);
    }

    /// <summary>Fade markers and labels together with the map (0..1).</summary>
    public void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        Refresh();
    }

    void Refresh()
    {
        if (block == null) block = new MaterialPropertyBlock();
        for (int i = 0; i < markerRenderers.Count; i++)
        {
            var r = markerRenderers[i];
            if (r == null) continue;
            Color c = i == selectedIndex ? selectedColor : normalColor;
            c.a *= alpha;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }
        for (int i = 0; i < labels.Count; i++)
        {
            var t = labels[i];
            if (t == null) continue;
            t.color = i == selectedIndex ? selectedColor : Color.white;
            t.alpha = alpha;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Preview marker spots in the Scene view (markers are built in Play mode).
        Vector3 s = transform.localScale;
        Gizmos.color = Color.red;
        foreach (var site in sites)
        {
            Vector3 local = new Vector3(site.mapUv.x - 0.5f + site.nudge.x / Mathf.Max(1e-4f, s.x),
                                        0.5f - site.mapUv.y + site.nudge.y / Mathf.Max(1e-4f, s.y), 0f);
            Gizmos.DrawWireSphere(transform.TransformPoint(local), markerSize * 0.5f);
        }
    }
#endif
}
