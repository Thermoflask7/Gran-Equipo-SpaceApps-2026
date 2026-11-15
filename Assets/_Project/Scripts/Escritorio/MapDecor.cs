using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Decoration around the planning wall world map, built at runtime by LaunchSiteSelector:
/// a wooden frame, a "LAUNCH SITES" plaque, faint latitude/longitude lines and a dashed
/// equator (the closer a site is to it, the more speed Earth's spin gives an eastward launch).
/// A world-space canvas without a raycaster, so it never blocks clicks on the pins.
/// It sits between the map and the pins; fades with the map through SetAlpha.
/// </summary>
public class MapDecor : MonoBehaviour
{
    const float CanvasWidth = 1000f;

    static readonly Color FrameColor = new Color(0.30f, 0.21f, 0.13f, 1f);
    static readonly Color FrameLip = new Color(0.80f, 0.68f, 0.45f, 1f);
    static readonly Color PlaqueColor = new Color(0.08f, 0.10f, 0.20f, 1f);
    static readonly Color AccentColor = new Color(0.95f, 0.55f, 0.15f, 1f);
    static readonly Color GridColor = new Color(1f, 1f, 1f, 0.28f);
    static readonly Color GridText = new Color(1f, 1f, 1f, 0.75f);

    CanvasGroup group;
    RectTransform root;

    /// <summary>Create the decoration for a map of the given size (meters), centered on parent.</summary>
    public static MapDecor Create(Transform parent, float mapWidth, float mapHeight, float zOffset)
    {
        var go = new GameObject("MapDecor", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var decor = go.AddComponent<MapDecor>();
        decor.Build(mapWidth, mapHeight, zOffset);
        return decor;
    }

    void Build(float mapWidth, float mapHeight, float zOffset)
    {
        root = (RectTransform)transform;
        float scale = mapWidth / CanvasWidth;
        float h = mapHeight / scale;
        root.sizeDelta = new Vector2(CanvasWidth, h);
        root.localPosition = new Vector3(0f, 0f, zOffset);
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one * scale;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
        group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        // Latitude lines every 30 degrees (the equator is dashed and orange)
        for (int lat = -60; lat <= 60; lat += 30)
        {
            float y = 1f - LaunchSiteSelector.GeoToUv(lat, 0f).y;
            if (y <= 0.01f || y >= 0.99f) continue;
            if (lat == 0) { Equator(y); continue; }
            var line = Line("Lat" + lat, new Vector2(0f, y), new Vector2(1f, y), GridColor);
            line.sizeDelta = new Vector2(0f, 1.5f);
            Label(Mathf.Abs(lat) + "°" + (lat > 0 ? "N" : "S"), new Vector2(0f, y), new Vector2(6f, 9f), 13f, GridText);
        }

        // Longitude lines every 30 degrees
        for (int lon = -180; lon <= 360; lon += 30)
        {
            float x = LaunchSiteSelector.GeoToUv(0f, lon).x;
            if (x <= 0.01f || x >= 0.99f) continue;
            var line = Line("Lon" + lon, new Vector2(x, 0f), new Vector2(x, 1f), GridColor);
            line.sizeDelta = new Vector2(1.5f, 0f);
        }

        // Wooden frame around the map (4 bars, so it never covers the map)
        const float t = 26f;
        Bar("FrameTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-t, 0f), new Vector2(t, t));
        Bar("FrameBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-t, -t), new Vector2(t, 0f));
        Bar("FrameLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-t, 0f), new Vector2(0f, 0f));
        Bar("FrameRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(t, 0f));
        Lip(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(3f, 3f));
        Lip(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-3f, -3f), new Vector2(3f, 0f));
        Lip(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-3f, 0f), Vector2.zero);
        Lip(new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(3f, 0f));

        // Plaque on the top bar
        var plaque = MakeImage("Plaque", root, PlaqueColor);
        plaque.anchorMin = plaque.anchorMax = new Vector2(0.5f, 1f);
        plaque.pivot = new Vector2(0.5f, 0.5f);
        plaque.anchoredPosition = new Vector2(0f, t * 0.5f + 4f);
        plaque.sizeDelta = new Vector2(330f, 50f);
        var accent = MakeImage("Accent", plaque, AccentColor);
        accent.anchorMin = Vector2.zero;
        accent.anchorMax = new Vector2(1f, 0f);
        accent.offsetMin = Vector2.zero;
        accent.offsetMax = new Vector2(0f, 3f);
        var title = MakeText("Title", plaque, "LAUNCH SITES", 26f, Color.white);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
    }

    void Equator(float y)
    {
        const int dashes = 60;
        for (int i = 0; i < dashes; i++)
        {
            float x0 = (i + 0.15f) / dashes;
            float x1 = (i + 0.75f) / dashes;
            var dash = Line("Equator" + i, new Vector2(x0, y), new Vector2(x1, y), AccentColor);
            dash.sizeDelta = new Vector2(0f, 3f);
        }
        Label("EQUATOR  0°", new Vector2(0f, y), new Vector2(6f, 11f), 15f, AccentColor).fontStyle = FontStyles.Bold;
    }

    /// <summary>Fade with the map (0..1).</summary>
    public void SetAlpha(float alpha)
    {
        if (group != null) group.alpha = alpha;
    }

    // ---------- Helpers ----------

    RectTransform Line(string name, Vector2 from, Vector2 to, Color color)
    {
        var rt = MakeImage(name, root, color);
        rt.anchorMin = from;
        rt.anchorMax = to;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    void Bar(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = MakeImage(name, root, FrameColor);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    void Lip(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = MakeImage("Lip", root, FrameLip);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    TextMeshProUGUI Label(string text, Vector2 anchor, Vector2 offset, float size, Color color)
    {
        var t = MakeText("Label", root, text, size, color);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(200f, 24f);
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 160);
        return t;
    }

    static RectTransform MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }
}
