using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space info window above the calendar on the planning wall (task 6).
/// Same look as the SpaceOS apps (SpaceMart window): dark blue window, title bar with an
/// orange accent line, a location line, 3 fact tiles (latitude, free speed from Earth's spin,
/// launch direction) and a scrollable text area. Shows the selected launch site's info.
/// Built at runtime by PlanningStation; fades together with the world map.
/// </summary>
public class LaunchSiteInfoPanel : MonoBehaviour
{
    public const string PanelName = "LaunchSiteInfoPanel";

    static readonly Color WindowColor = new Color(0.05f, 0.08f, 0.16f, 0.98f);
    static readonly Color TitleColor = new Color(0.08f, 0.10f, 0.20f, 1f);
    static readonly Color AccentColor = new Color(0.95f, 0.55f, 0.15f, 1f);
    static readonly Color BodyText = new Color(0.86f, 0.88f, 0.93f, 1f);
    static readonly Color TileColor = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color LabelColor = new Color(0.60f, 0.70f, 0.90f, 1f);

    const float CanvasWidth = 640f;
    const float CanvasHeight = 440f;

    Canvas canvas;
    CanvasGroup group;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    TextMeshProUGUI regionText;
    GameObject tiles;
    readonly TextMeshProUGUI[] tileValues = new TextMeshProUGUI[3];
    RectTransform scrollArea;
    ScrollRect scroll;

    static readonly string[] TileLabels = { "LATITUDE", "FREE SPEED FROM EARTH'S SPIN", "LAUNCHES TOWARD" };

    /// <summary>Create the panel above the calendar, on the wall plane of the map.</summary>
    public static LaunchSiteInfoPanel Create(Transform parent, Transform map, Bounds calendarBounds,
                                             Camera eventCamera, float gapAboveCalendar = 0.04f, float widthScale = 1.7f, float offsetRight = 0.1f)
    {
        var go = new GameObject(PanelName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<LaunchSiteInfoPanel>();
        panel.Build(eventCamera);

        // Width = calendar width seen from the front; keep the canvas aspect.
        Vector3 right = map != null ? map.right : Vector3.right;
        float calWidth = 2f * (Mathf.Abs(right.x) * calendarBounds.extents.x + Mathf.Abs(right.z) * calendarBounds.extents.z);
        float width = Mathf.Max(0.3f, calWidth * widthScale); // grows left/right, bottom stays above the calendar
        float scale = width / CanvasWidth;
        float height = CanvasHeight * scale;

        Vector3 center = new Vector3(calendarBounds.center.x, calendarBounds.max.y + gapAboveCalendar + height * 0.5f, calendarBounds.center.z);
        center += right * offsetRight; // shifted right, away from the map
        Quaternion rotation = map != null ? map.rotation : Quaternion.identity;
        if (map != null)
        {
            // Same wall plane as the map, a little toward the camera.
            Vector3 n = map.forward; // points into the wall
            center -= n * Vector3.Dot(center - map.position, n);
            center -= n * 0.005f;
        }
        var rt = (RectTransform)go.transform;
        rt.SetPositionAndRotation(center, rotation);
        rt.localScale = Vector3.one * scale;
        panel.ShowSite(null);
        return panel;
    }

    void Build(Camera eventCamera)
    {
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = eventCamera;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 3f;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();

        var window = CreateImage("Window", transform, WindowColor);
        Stretch(window, 0f, 0f, 0f, 0f);

        var title = CreateImage("Title", window, TitleColor);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = Vector2.zero;
        title.sizeDelta = new Vector2(0f, 52f);
        var accent = CreateImage("Accent", title, AccentColor);
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(1f, 0f);
        accent.pivot = new Vector2(0.5f, 0f);
        accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(0f, 3f);
        titleText = CreateText("Name", title, "Launch site", 26f, TextAlignmentOptions.MidlineLeft);
        titleText.rectTransform.offsetMin = new Vector2(16f, 3f);
        titleText.rectTransform.offsetMax = new Vector2(-16f, 0f);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Truncate;

        // Location line under the title
        regionText = CreateText("Region", window, "", 17f, TextAlignmentOptions.MidlineLeft);
        regionText.color = LabelColor;
        regionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        regionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        regionText.rectTransform.offsetMin = new Vector2(16f, -88f);
        regionText.rectTransform.offsetMax = new Vector2(-16f, -58f);

        // 3 fact tiles
        var tilesRt = CreateRect("Tiles", window);
        tilesRt.anchorMin = new Vector2(0f, 1f);
        tilesRt.anchorMax = new Vector2(1f, 1f);
        tilesRt.offsetMin = new Vector2(12f, -170f);
        tilesRt.offsetMax = new Vector2(-12f, -92f);
        tiles = tilesRt.gameObject;
        for (int i = 0; i < 3; i++)
        {
            var tile = CreateImage("Tile" + (i + 1), tilesRt, TileColor);
            tile.anchorMin = new Vector2(i / 3f, 0f);
            tile.anchorMax = new Vector2((i + 1) / 3f, 1f);
            tile.offsetMin = new Vector2(4f, 0f);
            tile.offsetMax = new Vector2(-4f, 0f);
            var strip = CreateImage("Strip", tile, AccentColor);
            strip.anchorMin = new Vector2(0f, 1f);
            strip.anchorMax = Vector2.one;
            strip.offsetMin = new Vector2(0f, -3f);
            strip.offsetMax = Vector2.zero;

            var label = CreateText("Label", tile, TileLabels[i], 12f, TextAlignmentOptions.TopLeft);
            label.color = LabelColor;
            label.characterSpacing = 2f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 12f;
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-6f, -8f);

            var value = CreateText("Value", tile, "", 24f, TextAlignmentOptions.BottomLeft);
            value.fontStyle = FontStyles.Bold;
            value.enableAutoSizing = true;
            value.fontSizeMin = 12f;
            value.fontSizeMax = 24f;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.rectTransform.offsetMin = new Vector2(8f, 6f);
            value.rectTransform.offsetMax = new Vector2(-6f, -26f);
            tileValues[i] = value;
        }

        // Scroll area with a slim scrollbar on the right.
        var area = CreateRect("Scroll", window);
        Stretch(area, 12f, 12f, 12f, 64f);
        scrollArea = area;

        var viewport = CreateImage("Viewport", area, new Color(0f, 0f, 0f, 0.15f));
        Stretch(viewport, 0f, 0f, 14f, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bodyText = CreateText("Body", content, "", 20f, TextAlignmentOptions.TopLeft);
        bodyText.color = BodyText;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        bodyText.richText = true;

        var barBg = CreateImage("Scrollbar", area, new Color(1f, 1f, 1f, 0.06f));
        barBg.anchorMin = new Vector2(1f, 0f);
        barBg.anchorMax = new Vector2(1f, 1f);
        barBg.pivot = new Vector2(1f, 0.5f);
        barBg.anchoredPosition = Vector2.zero;
        barBg.sizeDelta = new Vector2(8f, 0f);
        var slide = CreateRect("Sliding Area", barBg);
        Stretch(slide, 0f, 0f, 0f, 0f);
        var handle = CreateImage("Handle", slide, AccentColor);
        Stretch(handle, 0f, 0f, 0f, 0f);
        var scrollbar = barBg.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    /// <summary>Show a site's info, or a hint when nothing is selected.</summary>
    public void ShowSite(LaunchSiteSelector.Site site)
    {
        bool hasSite = site != null;
        tiles.SetActive(hasSite);
        regionText.gameObject.SetActive(hasSite);
        scrollArea.offsetMax = new Vector2(-12f, hasSite ? -180f : -64f);
        if (!hasSite)
        {
            titleText.text = "Launch site";
            bodyText.text = "<size=130%><b>Click a pin on the map</b></size> to compare the launch sites.\n\n" +
                "<color=#F28C26><b>TIP</b></color>\nThe closer a site is to the equator, the more speed Earth's rotation gives " +
                "a rocket that launches east. Free speed means less fuel!";
        }
        else
        {
            titleText.text = site.displayName;
            regionText.text = LaunchSiteSelector.Region(site.id);
            bool east = LaunchSiteSelector.LaunchesEast(site.id);
            tileValues[0].text = Mathf.Abs(site.latitude).ToString("0.0") + "\u00b0 " + (site.latitude >= 0f ? "N" : "S");
            tileValues[1].text = east ? LaunchSiteSelector.SpinBoost(site.latitude).ToString("0") + " m/s" : "0 m/s";
            tileValues[2].text = east ? "East (ocean)" : "South (polar)";
            bodyText.text = site.InfoText;
        }
        Canvas.ForceUpdateCanvases();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>Fade with the map. Hidden and not clickable at 0.</summary>
    public void SetAlpha(float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = alpha > 0.99f;
        group.blocksRaycasts = alpha > 0.99f;
        if (canvas != null) canvas.enabled = alpha > 0.001f;
    }

    /// <summary>World-space bounds of the window, for the camera framing.</summary>
    public Bounds WorldBounds()
    {
        var corners = new Vector3[4];
        ((RectTransform)transform).GetWorldCorners(corners);
        var b = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < 4; i++) b.Encapsulate(corners[i]);
        return b;
    }

    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateRect(name, parent);
        rt.gameObject.AddComponent<Image>().color = color;
        return rt;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rt = CreateRect(name, parent);
        Stretch(rt, 0f, 0f, 0f, 0f);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }
}
