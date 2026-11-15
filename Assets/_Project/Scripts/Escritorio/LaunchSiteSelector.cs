using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Launch site markers on the planning wall world map (task 6). Lives on the WorldMap quad.
/// Markers are map pins built at runtime from <see cref="sites"/>: white, orange under the mouse,
/// red with a small bounce when clicked. The tip of the pin sits on the site.
/// Only clickable while the PlanningStation is zoomed in.
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
    [SerializeField] float markerSize = 0.05f;
    [SerializeField] float labelFontSize = 0.28f;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color hoverColor = new Color(1f, 0.6f, 0.15f, 1f);
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
    MapDecor decor;
    Material markerMaterial;
    Texture2D pinTexture;
    MaterialPropertyBlock block;
    PlanningStation station;
    int selectedIndex = -1;
    int hoverIndex = -1;
    float selectedTime = -10f;
    readonly List<Vector3> sitePositions = new List<Vector3>(); // where each pin tip touches the map
    float alpha = 1f;

    const string H = "<color=#F28C26><b>";
    const string HE = "</b></color>";

    /// <summary>Real-world info about each launch site (English, TMP rich text). No agency or program names.</summary>
    public static string DefaultInfo(string id)
    {
        switch (id)
        {
            case "KSC":
                return H + "WHY IT MATTERS" + HE + "\n" +
                    "Rockets launch east over the Atlantic, so Earth's spin adds speed for free and the flight path stays away from towns. " +
                    "That makes it the classic site for crewed missions to the Moon.\n\n" +
                    H + "HISTORY" + HE + "\n" +
                    "• The first crewed Moon landing mission lifted off from here in July 1969.\n" +
                    "• The space shuttles launched from here from 1981 to 2011.\n" +
                    "• A giant assembly building stacks the rockets before they roll out to the pad.";
            case "CCSFS":
                return H + "WHY IT MATTERS" + HE + "\n" +
                    "Right next to Merritt Island, with the same eastward boost over the ocean. " +
                    "It is the busiest launch range in the country, with many commercial pads, so the schedule is crowded.\n\n" +
                    H + "HISTORY" + HE + "\n" +
                    "• First launch from the Cape: July 1950.\n" +
                    "• The first US satellite launched from here in 1958.\n" +
                    "• The first US astronauts flew from here in 1961 and 1962.\n" +
                    "• Many Mars rovers and planetary probes started their trip here.";
            case "VSFB":
                return H + "WHY IT MATTERS" + HE + "\n" +
                    "Rockets fly south over the open Pacific into polar orbits, which pass over the whole planet: great for Earth-watching satellites. " +
                    "But launching south gets no help from Earth's spin, so going to the Moon costs more fuel.\n\n" +
                    H + "HISTORY" + HE + "\n" +
                    "• First launch in 1959, toward a polar orbit.\n" +
                    "• A pad was built for the space shuttle, but none ever flew from it.\n" +
                    "• Many Earth-observation satellites were launched here.\n" +
                    "• An asteroid-deflection test mission launched here in 2021.";
            default:
                return "No information for this site yet.";
        }
    }

    /// <summary>Where the site is, in a few words.</summary>
    public static string Region(string id)
    {
        if (id == "KSC" || id == "CCSFS") return "Florida, USA  |  Atlantic coast";
        if (id == "VSFB") return "California, USA  |  Pacific coast";
        return "";
    }

    /// <summary>True if rockets from this site launch east (and get Earth's spin boost).</summary>
    public static bool LaunchesEast(string id)
    {
        return id != "VSFB";
    }

    /// <summary>
    /// Latitude/longitude to a UV on WorldMap.png ((0,0) = top-left). Linear in both, fitted to the
    /// hand-calibrated Merritt Island and Vandenberg spots; good enough for the grid lines and the equator.
    /// </summary>
    public static Vector2 GeoToUv(float latitude, float longitude)
    {
        const float kLat = 28.6273f, kLon = -80.6209f, kU = 0.1704f, kV = 0.3559f;
        const float vLat = 34.7420f, vLon = -120.5724f, vU = 0.0609f, vV = 0.3165f;
        float u = kU + (longitude - kLon) * (kU - vU) / (kLon - vLon);
        float v = kV + (latitude - kLat) * (kV - vV) / (kLat - vLat);
        return new Vector2(u, v);
    }

    /// <summary>Speed Earth's rotation gives an eastward launch at this latitude (m/s).</summary>
    public static float SpinBoost(float latitude)
    {
        return 465.1f * Mathf.Cos(latitude * Mathf.Deg2Rad);
    }

    public static List<Site> DefaultSites()
    {
        return new List<Site>
        {
            new Site { id = "KSC", displayName = "Merritt Island, Pad 39B", mapUv = new Vector2(0.1704f, 0.3559f),
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
        if (pinTexture != null) Destroy(pinTexture);
    }

    void Build()
    {
        if (container != null) Destroy(container.gameObject);
        markerRenderers.Clear();
        labels.Clear();
        markerColliders.Clear();
        sitePositions.Clear();

        // Undo the map's non-uniform scale so children work in meters.
        var go = new GameObject("LaunchSites");
        container = go.transform;
        container.SetParent(transform, false);
        Vector3 s = transform.localScale;
        container.localScale = new Vector3(1f / Mathf.Max(1e-4f, s.x), 1f / Mathf.Max(1e-4f, s.y), 1f);
        float width = s.x, height = s.y;

        // Frame, plaque, grid and equator: between the map and the pins
        decor = MapDecor.Create(container, width, height, -wallOffset * 0.5f);

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
            marker.transform.localRotation = Quaternion.identity;
            sitePositions.Add(pos);
            PlacePin(marker.transform, pos, 1f);
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
            // Next to the round head of the pin
            labelGo.transform.localPosition = pos + new Vector3(side * markerSize * 0.4f, markerSize * 0.68f, -0.001f);
            labelGo.transform.localRotation = Quaternion.identity;
            // Drawn after the (transparent) map, so the map never hides it
            tmp.fontMaterial.renderQueue = 3002;
            labels.Add(tmp);
        }
        Refresh();
    }

    void EnsureMaterial()
    {
        if (pinTexture == null) pinTexture = MakePin(128);
        if (markerMaterial != null) return;
        // Copy the map material so the marker uses the same (already included) URP Unlit transparent variant.
        var mapRenderer = GetComponent<Renderer>();
        Material src = mapRenderer != null ? mapRenderer.sharedMaterial : null;
        markerMaterial = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        markerMaterial.name = "LaunchSiteMarker (runtime)";
        markerMaterial.SetTexture(BaseMapId, pinTexture);
        markerMaterial.mainTexture = pinTexture;
        markerMaterial.renderQueue = 3001;
    }

    // Map pin: round head on top, point at the bottom center of the texture.
    // Body is white (tinted by the marker color), with a dark rim and a dark hole in the head.
    static Texture2D MakePin(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LaunchSitePin" };
        var px = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float cy = size * 0.66f;       // head center
        float r = size * 0.3f;         // head radius
        float tipY = size * 0.02f;     // point of the pin
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - cx, dy = y - cy;
            float headDist = Mathf.Sqrt(dx * dx + dy * dy) / r; // < 1 inside the head
            // Cone from the tip up to the head: half width grows from 0 at the tip to r at the head center
            float coneHalf = y < cy && y > tipY ? r * (y - tipY) / (cy - tipY) : -1f;
            float coneDist = coneHalf > 0f ? Mathf.Abs(dx) / coneHalf : 2f;
            float d = Mathf.Min(headDist, coneDist);
            byte a = (byte)(Mathf.Clamp01((1f - d) * r * 0.5f) * 255f);
            byte v = 255;
            if (d > 0.8f) v = 40;               // rim
            if (headDist < 0.38f) v = 50;       // hole in the head
            px[y * size + x] = new Color32(v, v, v, a);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    // The quad is centered, so move it up half its size: the pin tip lands on the site
    void PlacePin(Transform pin, Vector3 sitePos, float scale)
    {
        float size = markerSize * scale;
        pin.localScale = new Vector3(size, size, size);
        pin.localPosition = sitePos + new Vector3(0f, size * 0.49f, 0f);
    }

    void Update()
    {
        if (station == null) station = GetComponentInParent<PlanningStation>();
        AnimatePins();
        if (station == null || !station.IsOpen) { SetHover(-1); return; }
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { SetHover(-1); return; }

        int index = PinUnderMouse(mouse);
        SetHover(index);
        if (index >= 0 && mouse.leftButton.wasPressedThisFrame) Select(index);
    }

    int PinUnderMouse(Mouse mouse)
    {
        var dc = DeskCamera.Instance;
        Camera cam = dc != null ? dc.GetComponent<Camera>() : Camera.main;
        if (cam == null) return -1;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var hits = Physics.RaycastAll(ray, maxClickDistance, ~0, QueryTriggerInteraction.Collide);
        float best = float.PositiveInfinity;
        int index = -1;
        foreach (var h in hits)
        {
            int i = markerColliders.IndexOf(h.collider);
            if (i >= 0 && h.distance < best) { best = h.distance; index = i; }
        }
        return index;
    }

    void SetHover(int index)
    {
        if (index == hoverIndex) return;
        hoverIndex = index;
        Refresh();
    }

    // Hovered pins grow a little; the selected one bounces when clicked
    void AnimatePins()
    {
        for (int i = 0; i < markerRenderers.Count && i < sitePositions.Count; i++)
        {
            if (markerRenderers[i] == null) continue;
            float scale = 1f;
            if (i == hoverIndex) scale = 1.2f;
            if (i == selectedIndex)
            {
                float t = Time.time - selectedTime;
                scale = 1.25f * (1f + 0.3f * Mathf.Exp(-6f * t) * Mathf.Sin(18f * t));
            }
            PlacePin(markerRenderers[i].transform, sitePositions[i], scale);
        }
    }

    /// <summary>Select a site by index (-1 clears). The selected marker turns red.</summary>
    public void Select(int index)
    {
        if (index < -1 || index >= sites.Count) return;
        if (index == selectedIndex) return;
        selectedIndex = index;
        selectedTime = Time.time;
        Refresh();
        SelectionChanged?.Invoke(Selected);
    }

    /// <summary>Fade markers and labels together with the map (0..1).</summary>
    public void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        if (decor != null) decor.SetAlpha(alpha);
        Refresh();
    }

    void Refresh()
    {
        if (block == null) block = new MaterialPropertyBlock();
        for (int i = 0; i < markerRenderers.Count; i++)
        {
            var r = markerRenderers[i];
            if (r == null) continue;
            Color c = i == selectedIndex ? selectedColor : (i == hoverIndex ? hoverColor : normalColor);
            c.a *= alpha;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }
        for (int i = 0; i < labels.Count; i++)
        {
            var t = labels[i];
            if (t == null) continue;
            t.color = i == selectedIndex ? selectedColor : (i == hoverIndex ? hoverColor : Color.white);
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
