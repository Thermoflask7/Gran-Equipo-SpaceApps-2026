using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 7-day launch calendar drawn on the calendar's white sheet (task 6).
/// One card per day (4 + 3 grid): weekday, date, weather icon, wind, rain chance and a risk bar.
/// Cards follow the site picked on the map (LaunchWeatherModel). Clicking a card marks it red;
/// with a site and a day picked, the launch is saved in GameManager.
/// Built at runtime by PlanningStation; fades together with the world map.
/// </summary>
public class CalendarWeatherPanel : MonoBehaviour
{
    public const string PanelName = "CalendarWeatherPanel";

    // Where the white sheet is inside the calendar's bounds (0..1, from the bottom-left, viewer side).
    [SerializeField] Rect sheetRect = new Rect(0.05f, 0.04f, 0.90f, 0.64f);
    // Red band at the top of the calendar (for the date range text).
    [SerializeField] Rect headerRect = new Rect(0.05f, 0.70f, 0.90f, 0.13f);

    static readonly Color CardColor = Color.white;
    static readonly Color CardBorder = new Color(0.75f, 0.75f, 0.77f, 1f);
    static readonly Color SelectedFill = new Color(1f, 0.94f, 0.94f, 1f);
    static readonly Color SelectedBorder = new Color(0.86f, 0.12f, 0.12f, 1f);
    static readonly Color DarkText = new Color(0.16f, 0.16f, 0.2f, 1f);
    static readonly Color GrayText = new Color(0.45f, 0.45f, 0.5f, 1f);
    static readonly Color BarBack = new Color(0.88f, 0.88f, 0.9f, 1f);
    static readonly Color RiskLow = new Color(0.24f, 0.67f, 0.35f, 1f);
    static readonly Color RiskMid = new Color(0.92f, 0.63f, 0.16f, 1f);
    static readonly Color RiskHigh = new Color(0.82f, 0.2f, 0.2f, 1f);

    const float SheetPixelsWidth = 800f;

    class Card
    {
        public GameObject root;
        public Image border;
        public Image fill;
        public TextMeshProUGUI weekday;
        public TextMeshProUGUI date;
        public Image icon;
        public TextMeshProUGUI wind;
        public TextMeshProUGUI rain;
        public RectTransform riskBar;
        public Image riskFill;
    }

    readonly List<Card> cards = new List<Card>();
    readonly Dictionary<string, LaunchSiteData> runtimeSites = new Dictionary<string, LaunchSiteData>();
    static readonly Dictionary<WeatherCondition, Sprite> icons = new Dictionary<WeatherCondition, Sprite>();

    CanvasGroup sheetGroup;
    CanvasGroup headerGroup;
    Canvas sheetCanvas;
    Canvas headerCanvas;
    TextMeshProUGUI siteText;
    TextMeshProUGUI hintText;
    TextMeshProUGUI rangeText;
    GameObject grid;

    LaunchSiteSelector.Site site;
    int selectedDay = -1;

    public int SelectedDay => selectedDay;
    public event Action<int> DaySelected;

    /// <summary>Create the calendar overlay in front of the calendar model.</summary>
    public static CalendarWeatherPanel Create(Transform parent, Transform map, Bounds calendarBounds, Camera eventCamera)
    {
        var go = new GameObject(PanelName);
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<CalendarWeatherPanel>();
        panel.Build(map, calendarBounds, eventCamera);
        panel.SetSite(null);
        LaunchWeatherModel.ForecastChanged += panel.Refresh;
        return panel;
    }

    void OnDestroy()
    {
        LaunchWeatherModel.ForecastChanged -= Refresh;
        foreach (var data in runtimeSites.Values) if (data != null) Destroy(data);
    }

    void Build(Transform map, Bounds cal, Camera eventCamera)
    {
        Vector3 right = map != null ? map.right : Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 fwd = map != null ? map.forward : Vector3.forward; // into the wall
        Quaternion rot = map != null ? map.rotation : Quaternion.identity;

        float width = 2f * (Mathf.Abs(right.x) * cal.extents.x + Mathf.Abs(right.z) * cal.extents.z);
        float height = cal.size.y;
        float depth = Mathf.Abs(fwd.x) * cal.extents.x + Mathf.Abs(fwd.z) * cal.extents.z;
        Vector3 bottomLeft = cal.center - right * (width * 0.5f) - up * (height * 0.5f) - fwd * (depth + 0.003f);

        sheetCanvas = MakeCanvas("Sheet", sheetRect, bottomLeft, right, up, width, height, rot, eventCamera, out sheetGroup, out var sheetRt);
        headerCanvas = MakeCanvas("Header", headerRect, bottomLeft, right, up, width, height, rot, eventCamera, out headerGroup, out var headerRt);
        headerGroup.blocksRaycasts = false;

        rangeText = CreateText("DateRange", headerRt, "", 34f, TextAlignmentOptions.MidlineRight, Color.white, true);
        Stretch(rangeText.rectTransform, 0.03f, 0f, 0.97f, 1f);

        // Sheet: site name + hint on top, 4 + 3 cards below.
        siteText = CreateText("Site", sheetRt, "", 30f, TextAlignmentOptions.TopLeft, DarkText, true);
        Stretch(siteText.rectTransform, 0.03f, 0.88f, 0.97f, 0.985f);
        hintText = CreateText("Hint", sheetRt, "", 22f, TextAlignmentOptions.TopLeft, GrayText, false);
        Stretch(hintText.rectTransform, 0.03f, 0.80f, 0.97f, 0.885f);

        grid = new GameObject("Days", typeof(RectTransform));
        grid.transform.SetParent(sheetRt, false);
        var gridRt = (RectTransform)grid.transform;
        Stretch(gridRt, 0.02f, 0.02f, 0.98f, 0.78f);

        const float gap = 0.025f;
        float cw = (1f - 3f * gap) / 4f;
        float rh = (1f - gap) / 2f;
        for (int i = 0; i < LaunchWeatherModel.Days; i++)
        {
            int row = i < 4 ? 0 : 1;
            int col = i < 4 ? i : i - 4;
            float x0 = col * (cw + gap) + (row == 1 ? (cw + gap) * 0.5f : 0f);
            float y1 = 1f - row * (rh + gap);
            cards.Add(MakeCard(gridRt, i, x0, y1 - rh, x0 + cw, y1));
        }
    }

    Canvas MakeCanvas(string name, Rect r, Vector3 bottomLeft, Vector3 right, Vector3 up, float calW, float calH,
                      Quaternion rot, Camera eventCamera, out CanvasGroup group, out RectTransform rt)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        rt = (RectTransform)go.transform;
        float w = r.width * calW;
        float h = r.height * calH;
        float scale = w / SheetPixelsWidth;
        rt.sizeDelta = new Vector2(SheetPixelsWidth, h / scale);
        Vector3 center = bottomLeft + right * ((r.x + r.width * 0.5f) * calW) + up * ((r.y + r.height * 0.5f) * calH);
        rt.SetPositionAndRotation(center, rot);
        rt.localScale = Vector3.one * scale;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = eventCamera;
        go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
        go.AddComponent<GraphicRaycaster>();
        group = go.AddComponent<CanvasGroup>();
        return canvas;
    }

    Card MakeCard(RectTransform parent, int index, float x0, float y0, float x1, float y1)
    {
        var c = new Card();
        var borderRt = CreateImage("Day" + (index + 1), parent, CardBorder);
        Stretch(borderRt, x0, y0, x1, y1);
        c.root = borderRt.gameObject;
        c.border = borderRt.GetComponent<Image>();
        var button = c.root.AddComponent<Button>();
        button.targetGraphic = c.border;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        int day = index;
        button.onClick.AddListener(() => SelectDay(day));

        var fillRt = CreateImage("Fill", borderRt, CardColor);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);
        c.fill = fillRt.GetComponent<Image>();
        c.fill.raycastTarget = false;

        c.weekday = CreateText("Weekday", fillRt, "", 26f, TextAlignmentOptions.TopLeft, DarkText, true);
        Stretch(c.weekday.rectTransform, 0.08f, 0.78f, 0.7f, 0.96f);
        c.date = CreateText("Date", fillRt, "", 22f, TextAlignmentOptions.TopRight, GrayText, false);
        Stretch(c.date.rectTransform, 0.5f, 0.78f, 0.92f, 0.96f);

        var iconRt = CreateImage("Icon", fillRt, Color.white);
        Stretch(iconRt, 0.25f, 0.42f, 0.75f, 0.8f);
        c.icon = iconRt.GetComponent<Image>();
        c.icon.preserveAspect = true;
        c.icon.raycastTarget = false;

        c.wind = CreateText("Wind", fillRt, "", 20f, TextAlignmentOptions.MidlineLeft, DarkText, false);
        Stretch(c.wind.rectTransform, 0.08f, 0.28f, 0.95f, 0.42f);
        c.rain = CreateText("Rain", fillRt, "", 20f, TextAlignmentOptions.MidlineLeft, DarkText, false);
        Stretch(c.rain.rectTransform, 0.08f, 0.15f, 0.95f, 0.29f);

        var barRt = CreateImage("RiskBar", fillRt, BarBack);
        Stretch(barRt, 0.08f, 0.05f, 0.92f, 0.12f);
        barRt.GetComponent<Image>().raycastTarget = false;
        var riskRt = CreateImage("Risk", barRt, RiskLow);
        riskRt.anchorMin = Vector2.zero;
        riskRt.anchorMax = new Vector2(0.1f, 1f);
        riskRt.offsetMin = riskRt.offsetMax = Vector2.zero;
        c.riskBar = riskRt;
        c.riskFill = riskRt.GetComponent<Image>();
        c.riskFill.raycastTarget = false;
        return c;
    }

    /// <summary>Show the forecast of a site (null = ask to pick a site first).</summary>
    public void SetSite(LaunchSiteSelector.Site value)
    {
        site = value;
        Refresh();
        if (site != null && selectedDay >= 0) SaveLaunch();
    }

    void Refresh()
    {
        bool hasSite = site != null;
        grid.SetActive(hasSite);
        DateTime first = DateTime.Today.AddDays(1);
        rangeText.text = first.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture) + " - " +
                         first.AddDays(LaunchWeatherModel.Days - 1).ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture);
        if (!hasSite)
        {
            siteText.text = "Launch calendar";
            hintText.text = "Pick a launch site on the map first.";
            return;
        }

        siteText.text = site.displayName;
        DayWeather[] days = LaunchWeatherModel.GetForecast(site.id);
        for (int i = 0; i < cards.Count && i < days.Length; i++)
        {
            DayWeather d = days[i];
            Card c = cards[i];
            c.weekday.text = d.Weekday;
            c.date.text = d.date.Day.ToString();
            c.icon.sprite = IconFor(d.condition);
            c.wind.text = "Wind " + d.windKnots.ToString("0") + " kt";
            c.rain.text = "Rain " + d.precipitationChance + "%";
            c.riskBar.anchorMax = new Vector2(Mathf.Max(0.06f, d.risk), 1f);
            c.riskFill.color = d.risk < 0.35f ? RiskLow : (d.risk < 0.7f ? RiskMid : RiskHigh);
            bool sel = i == selectedDay;
            c.border.color = sel ? SelectedBorder : CardBorder;
            c.fill.color = sel ? SelectedFill : CardColor;
            var fillRt = c.fill.rectTransform;
            float b = sel ? 7f : 3f;
            fillRt.offsetMin = new Vector2(b, b);
            fillRt.offsetMax = new Vector2(-b, -b);
        }
        if (selectedDay >= 0 && selectedDay < days.Length)
        {
            DayWeather d = days[selectedDay];
            hintText.text = d.Weekday + " " + d.date.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture) +
                            " selected: " + d.condition + ", launch risk " + Mathf.RoundToInt(d.risk * 100f) + "%";
        }
        else hintText.text = "Pick a launch day.";
    }

    void SelectDay(int day)
    {
        if (site == null) return;
        selectedDay = day;
        Refresh();
        SaveLaunch();
        DaySelected?.Invoke(day);
    }

    /// <summary>Save site + day in GameManager (risk from the weather model).</summary>
    void SaveLaunch()
    {
        var gm = GameManager.Instance;
        if (gm == null || site == null || selectedDay < 0) return;
        if (!runtimeSites.TryGetValue(site.id, out var data) || data == null)
        {
            data = ScriptableObject.CreateInstance<LaunchSiteData>();
            data.name = "Runtime_" + site.id;
            runtimeSites[site.id] = data;
        }
        data.siteName = site.displayName;
        data.description = site.data != null ? site.data.description : "";
        data.latitude = site.latitude;
        data.longitude = site.longitude;
        DayWeather[] days = LaunchWeatherModel.GetForecast(site.id);
        data.weatherRiskByDay = new float[days.Length];
        for (int i = 0; i < days.Length; i++) data.weatherRiskByDay[i] = days[i].risk;
        gm.SetLaunch(data, selectedDay);
    }

    /// <summary>Fade with the map. Hidden and not clickable at 0.</summary>
    public void SetAlpha(float alpha)
    {
        bool on = alpha > 0.001f;
        bool interactive = alpha > 0.99f;
        if (sheetGroup != null)
        {
            sheetGroup.alpha = alpha;
            sheetGroup.interactable = interactive;
            sheetGroup.blocksRaycasts = interactive;
        }
        if (headerGroup != null) headerGroup.alpha = alpha;
        if (sheetCanvas != null) sheetCanvas.enabled = on;
        if (headerCanvas != null) headerCanvas.enabled = on;
    }

    // ---------- Weather icons (drawn once at runtime, no image assets needed) ----------

    static Sprite IconFor(WeatherCondition c)
    {
        if (icons.TryGetValue(c, out var s) && s != null) return s;
        s = DrawIcon(c);
        icons[c] = s;
        return s;
    }

    struct Shape
    {
        public Func<Vector2, bool> inside;
        public Color color;
    }

    static Sprite DrawIcon(WeatherCondition c)
    {
        var shapes = new List<Shape>();
        Color sun = new Color(0.96f, 0.71f, 0.12f);
        Color bolt = new Color(0.98f, 0.82f, 0.16f);
        Color drop = new Color(0.24f, 0.47f, 0.86f);
        switch (c)
        {
            case WeatherCondition.Sunny:
                shapes.Add(Circle(new Vector2(0.5f, 0.5f), 0.2f, sun));
                for (int a = 0; a < 8; a++)
                {
                    float r = a * Mathf.PI / 4f;
                    var dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
                    shapes.Add(Segment(new Vector2(0.5f, 0.5f) + dir * 0.28f, new Vector2(0.5f, 0.5f) + dir * 0.4f, 0.035f, sun));
                }
                break;
            case WeatherCondition.Cloudy:
                AddCloud(shapes, 0.5f, new Color(0.6f, 0.62f, 0.68f));
                break;
            case WeatherCondition.Rain:
                AddCloud(shapes, 0.6f, new Color(0.47f, 0.51f, 0.6f));
                for (int i = 0; i < 4; i++)
                {
                    float x = 0.28f + i * 0.15f;
                    shapes.Add(Segment(new Vector2(x, 0.3f), new Vector2(x - 0.05f, 0.14f), 0.025f, drop));
                }
                break;
            default:
                AddCloud(shapes, 0.6f, new Color(0.32f, 0.34f, 0.4f));
                shapes.Add(Triangle(new Vector2(0.55f, 0.42f), new Vector2(0.38f, 0.22f), new Vector2(0.52f, 0.22f), bolt));
                shapes.Add(Triangle(new Vector2(0.52f, 0.28f), new Vector2(0.62f, 0.28f), new Vector2(0.44f, 0.04f), bolt));
                break;
        }

        const int size = 96;
        const int ss = 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color acc = Color.clear;
            for (int sy = 0; sy < ss; sy++)
            for (int sx = 0; sx < ss; sx++)
            {
                var p = new Vector2((x + (sx + 0.5f) / ss) / size, (y + (sy + 0.5f) / ss) / size);
                Color col = Color.clear;
                foreach (var sh in shapes) if (sh.inside(p)) col = sh.color;
                acc += col;
            }
            acc /= ss * ss;
            if (acc.a > 0f) { acc.r /= acc.a; acc.g /= acc.a; acc.b /= acc.a; }
            pixels[y * size + x] = acc;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static void AddCloud(List<Shape> shapes, float cy, Color col)
    {
        shapes.Add(Circle(new Vector2(0.32f, cy - 0.02f), 0.14f, col));
        shapes.Add(Circle(new Vector2(0.5f, cy + 0.06f), 0.19f, col));
        shapes.Add(Circle(new Vector2(0.68f, cy - 0.02f), 0.14f, col));
        shapes.Add(Box(new Vector2(0.18f, cy - 0.16f), new Vector2(0.82f, cy - 0.02f), col));
    }

    static Shape Circle(Vector2 c, float r, Color col) =>
        new Shape { inside = p => (p - c).sqrMagnitude <= r * r, color = col };

    static Shape Box(Vector2 min, Vector2 max, Color col) =>
        new Shape { inside = p => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y, color = col };

    static Shape Segment(Vector2 a, Vector2 b, float halfWidth, Color col) =>
        new Shape
        {
            inside = p =>
            {
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                return (p - (a + ab * t)).sqrMagnitude <= halfWidth * halfWidth;
            },
            color = col
        };

    static Shape Triangle(Vector2 a, Vector2 b, Vector2 c, Color col) =>
        new Shape
        {
            inside = p =>
            {
                float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0;
                bool pos = d1 > 0 || d2 > 0 || d3 > 0;
                return !(neg && pos);
            },
            color = col
        };

    static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

    // ---------- UI helpers ----------

    static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size,
                                      TextAlignmentOptions align, Color color, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }
}
