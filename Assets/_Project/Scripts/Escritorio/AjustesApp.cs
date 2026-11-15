using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SpaceOS Settings app: audio, graphics, controls and about.
/// Registers with ComputerDesktop.RegisterApp("ajustes") and builds its UI at runtime.
/// </summary>
public class AjustesApp : MonoBehaviour
{
    enum Section { Audio, Graficos, Controles, Informacion }

    ComputerDesktop desktop;
    bool ready;
    GameObject window;
    Section current = Section.Audio;

    readonly Button[] tabButtons = new Button[4];
    readonly GameObject[] panels = new GameObject[4];

    Slider masterSlider;
    Slider musicSlider;
    Slider sfxSlider;
    TMP_Text masterValue;
    TMP_Text musicValue;
    TMP_Text sfxValue;
    TMP_Text qualityValue;
    TMP_Text frameValue;
    TMP_Text fullscreenValue;
    TMP_Text crtValue;
    Slider sensitivitySlider;
    TMP_Text sensitivityValue;

    static readonly string[] FrameRateLabels = { "VSync", "30 FPS", "60 FPS", "120 FPS", "Unlimited" };

    void Awake() => Setup();
    void Start() => Setup();

    void OnDestroy()
    {
        if (desktop != null)
        {
            desktop.onHidden.RemoveListener(Close);
            desktop.onShown.RemoveListener(Close);
        }
        SpaceOsSettings.Changed -= RefreshControls;
    }

    void Setup()
    {
        if (ready) return;
        desktop = GetComponent<ComputerDesktop>();
        if (desktop == null) desktop = ComputerDesktop.Instance;
        if (desktop == null) return;

        SpaceOsSettings.Load();
        SpaceOsSettings.Apply();
        BuildWindow();
        desktop.RegisterApp(ComputerDesktop.AppAjustes, "Settings", new Color(0.45f, 0.5f, 0.6f), Open);
        desktop.onHidden.AddListener(Close);
        desktop.onShown.AddListener(Close);
        SpaceOsSettings.Changed += RefreshControls;
        ready = true;
        Close();
    }

    public void Open()
    {
        if (!ready) Setup();
        if (window == null) return;
        RefreshControls();
        ShowSection(current);
        window.SetActive(true);
        window.transform.SetAsLastSibling();
        var apagada = desktop.transform.Find("Root/Pantalla/Apagada");
        if (apagada != null) apagada.SetAsLastSibling();
    }

    public void Close()
    {
        if (window != null) window.SetActive(false);
    }

    void ShowSection(Section section)
    {
        current = section;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) panels[i].SetActive(i == (int)section);
            if (tabButtons[i] == null) continue;
            var img = tabButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = i == (int)section ? new Color(0.45f, 0.5f, 0.6f) : new Color(0.12f, 0.14f, 0.22f, 1f);
        }
    }

    void RefreshControls()
    {
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(SpaceOsSettings.MasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(SpaceOsSettings.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SpaceOsSettings.SfxVolume);
        if (masterValue != null) masterValue.text = Percent(SpaceOsSettings.MasterVolume);
        if (musicValue != null) musicValue.text = Percent(SpaceOsSettings.MusicVolume);
        if (sfxValue != null) sfxValue.text = Percent(SpaceOsSettings.SfxVolume);
        if (qualityValue != null) qualityValue.text = CurrentQualityName();
        if (frameValue != null) frameValue.text = FrameRateLabels[FrameRateIndex()];
        SetToggleVisual(fullscreenValue, SpaceOsSettings.Fullscreen, "On", "Off");
        SetToggleVisual(crtValue, SpaceOsSettings.CrtFilter, "On", "Off");
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(SpaceOsSettings.CameraSensitivity);
        if (sensitivityValue != null) sensitivityValue.text = SpaceOsSettings.CameraSensitivity.ToString("0.00") + "x";
    }

    static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

    static string CurrentQualityName()
    {
        var names = QualitySettings.names;
        if (names == null || names.Length == 0) return "-";
        int i = Mathf.Clamp(SpaceOsSettings.QualityLevel, 0, names.Length - 1);
        return names[i];
    }

    static int FrameRateIndex()
    {
        if (SpaceOsSettings.VSync) return 0;
        switch (SpaceOsSettings.TargetFrameRate)
        {
            case 30: return 1;
            case 60: return 2;
            case 120: return 3;
            case -1: return 4;
            default: return 2;
        }
    }

    void SetFrameRateIndex(int index)
    {
        index = (index + FrameRateLabels.Length) % FrameRateLabels.Length;
        switch (index)
        {
            case 0:
                SpaceOsSettings.VSync = true;
                SpaceOsSettings.TargetFrameRate = 60;
                break;
            case 1:
                SpaceOsSettings.VSync = false;
                SpaceOsSettings.TargetFrameRate = 30;
                break;
            case 2:
                SpaceOsSettings.VSync = false;
                SpaceOsSettings.TargetFrameRate = 60;
                break;
            case 3:
                SpaceOsSettings.VSync = false;
                SpaceOsSettings.TargetFrameRate = 120;
                break;
            default:
                SpaceOsSettings.VSync = false;
                SpaceOsSettings.TargetFrameRate = -1;
                break;
        }
        Commit();
    }

    void CycleQuality(int delta)
    {
        var names = QualitySettings.names;
        if (names == null || names.Length == 0) return;
        int count = names.Length;
        SpaceOsSettings.QualityLevel = (SpaceOsSettings.QualityLevel + delta + count) % count;
        Commit();
    }

    void Commit()
    {
        SpaceOsSettings.Apply();
        SpaceOsSettings.Save();
        RefreshControls();
    }

    void ResetDefaults()
    {
        SpaceOsSettings.ResetToDefaults();
        RefreshControls();
    }

    // ---- UI ----

    void BuildWindow()
    {
        if (window != null) return;
        Transform pantalla = desktop.transform.Find("Root/Pantalla");
        if (pantalla == null) pantalla = desktop.transform;

        window = new GameObject("SettingsWindow", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(pantalla, false);
        var windowRt = (RectTransform)window.transform;
        Stretch(windowRt, 24f, 72f, 24f, 100f);
        window.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.16f, 0.98f);

        var title = CreateImage("Titulo", window.transform, new Color(0.08f, 0.10f, 0.20f, 1f));
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = Vector2.zero;
        title.sizeDelta = new Vector2(0f, 52f);
        var accent = CreateImage("Acento", title, new Color(0.45f, 0.5f, 0.6f, 1f));
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(1f, 0f);
        accent.pivot = new Vector2(0.5f, 0f);
        accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(0f, 3f);
        var titleLabel = CreateText("Name", title, "Settings", 26f, TextAlignmentOptions.MidlineLeft);
        titleLabel.rectTransform.offsetMin = new Vector2(16f, 0f);
        titleLabel.rectTransform.offsetMax = new Vector2(-80f, 0f);

        var close = CreateButton("Close", title, new Color(0.75f, 0.20f, 0.20f, 1f));
        var closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-10f, 0f);
        closeRt.sizeDelta = new Vector2(44f, 36f);
        CreateText("X", close.transform, "X", 22f, TextAlignmentOptions.Center);
        close.onClick.AddListener(Close);

        var tabs = CreateRect("Pestanas", window.transform);
        tabs.anchorMin = new Vector2(0f, 1f);
        tabs.anchorMax = new Vector2(1f, 1f);
        tabs.pivot = new Vector2(0.5f, 1f);
        tabs.anchoredPosition = new Vector2(0f, -52f);
        tabs.sizeDelta = new Vector2(0f, 48f);
        var tabRow = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabRow.padding = new RectOffset(8, 8, 6, 6);
        tabRow.spacing = 6f;
        tabRow.childAlignment = TextAnchor.MiddleLeft;
        tabRow.childForceExpandWidth = true;
        tabRow.childForceExpandHeight = true;

        string[] tabNames = { "Audio", "Graphics", "Controls", "About" };
        Section[] sections = { Section.Audio, Section.Graficos, Section.Controles, Section.Informacion };
        for (int i = 0; i < tabNames.Length; i++)
        {
            Section section = sections[i];
            var tab = CreateButton("Tab_" + section, tabs, new Color(0.12f, 0.14f, 0.22f, 1f));
            var le = tab.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 36f;
            CreateText("Etiqueta", tab.transform, tabNames[i], 18f, TextAlignmentOptions.Center);
            tab.onClick.AddListener(() => ShowSection(section));
            tabButtons[i] = tab;
        }

        var reset = CreateButton("Reset", window.transform, new Color(0.22f, 0.24f, 0.32f, 1f));
        var resetRt = (RectTransform)reset.transform;
        resetRt.anchorMin = new Vector2(0.5f, 0f);
        resetRt.anchorMax = new Vector2(0.5f, 0f);
        resetRt.pivot = new Vector2(0.5f, 0f);
        resetRt.anchoredPosition = new Vector2(0f, 10f);
        resetRt.sizeDelta = new Vector2(280f, 44f);
        CreateText("Label", reset.transform, "Reset to defaults", 18f, TextAlignmentOptions.Center);
        reset.onClick.AddListener(ResetDefaults);

        var body = CreateRect("Cuerpo", window.transform);
        Stretch(body, 8f, 62f, 8f, 108f);

        panels[0] = BuildAudioPanel(body);
        panels[1] = BuildGraphicsPanel(body);
        panels[2] = BuildControlsPanel(body);
        panels[3] = BuildInfoPanel(body);
        ShowSection(Section.Audio);
    }

    GameObject BuildAudioPanel(RectTransform body)
    {
        var content = CreateScrollContent(body, "Audio");
        CreateSectionLabel(content, "Volume");
        CreateVolumeSlider(content, "Master", SpaceOsSettings.MasterVolume, v =>
        {
            SpaceOsSettings.MasterVolume = v;
            Commit();
        }, out masterSlider, out masterValue);
        CreateVolumeSlider(content, "Music", SpaceOsSettings.MusicVolume, v =>
        {
            SpaceOsSettings.MusicVolume = v;
            Commit();
        }, out musicSlider, out musicValue);
        CreateVolumeSlider(content, "Sound effects (SFX)", SpaceOsSettings.SfxVolume, v =>
        {
            SpaceOsSettings.SfxVolume = v;
            Commit();
        }, out sfxSlider, out sfxValue);
        CreateHint(content, "Music and SFX are stored so the game audio can read them later. Master is already applied to AudioListener.");
        return content.parent.parent.gameObject;
    }

    GameObject BuildGraphicsPanel(RectTransform body)
    {
        var content = CreateScrollContent(body, "Graphics");
        CreateSectionLabel(content, "Quality and display");
        qualityValue = CreateCycleRow(content, "Quality", CurrentQualityName(), () => CycleQuality(-1), () => CycleQuality(1));
        fullscreenValue = CreateToggleRow(content, "Fullscreen", () => SpaceOsSettings.Fullscreen, on =>
        {
            SpaceOsSettings.Fullscreen = on;
            Commit();
        });
        crtValue = CreateToggleRow(content, "Monitor CRT filter", () => SpaceOsSettings.CrtFilter, on =>
        {
            SpaceOsSettings.CrtFilter = on;
            Commit();
        });
        frameValue = CreateCycleRow(content, "Frame rate limit", FrameRateLabels[FrameRateIndex()],
            () => SetFrameRateIndex(FrameRateIndex() - 1),
            () => SetFrameRateIndex(FrameRateIndex() + 1));
        CreateHint(content, "Fullscreen has no visible effect on WebGL if the browser blocks it. The CRT filter swaps the monitor screen material on or off.");
        return content.parent.parent.gameObject;
    }

    GameObject BuildControlsPanel(RectTransform body)
    {
        var content = CreateScrollContent(body, "Controls");
        CreateSectionLabel(content, "Camera");
        CreateLabeledSlider(content, "Camera / mouse sensitivity", 0.2f, 2f, SpaceOsSettings.CameraSensitivity, v =>
        {
            SpaceOsSettings.CameraSensitivity = v;
            Commit();
        }, out sensitivitySlider, out sensitivityValue, v => v.ToString("0.00") + "x");
        CreateHint(content, "Sensitivity is stored in SpaceOsSettings.CameraSensitivity for stations that move the camera.");
        CreateSectionLabel(content, "Current controls");
        CreateInfoLine(content, "Left click", "Select objects and icons, and drag sliders.");
        CreateInfoLine(content, "Esc", "Close the station and return to the desk.");
        CreateInfoLine(content, "Mouse wheel", "Scroll lists and windows (SpaceMart, Settings).");
        CreateInfoLine(content, "Back", "Button in the SpaceOS corner; same action as Esc.");
        return content.parent.parent.gameObject;
    }

    GameObject BuildInfoPanel(RectTransform body)
    {
        var content = CreateScrollContent(body, "About");
        CreateSectionLabel(content, "The game");
        CreateInfoLine(content, "Title", "Space Mission Simulator Astra");
        CreateInfoLine(content, "Team", "Gran Equipo");
        CreateInfoLine(content, "Challenge", "Space mission design game");
        CreateInfoLine(content, "Version", Application.version);
        CreateInfoLine(content, "Unity", Application.unityVersion);
        CreateInfoLine(content, "Platform", Application.platform.ToString());
        CreateSectionLabel(content, "Description");
        CreateParagraph(content, "Educational game about designing a crewed lunar-orbit mission, inspired by real Moon missions. From the planning desk you buy parts, assemble the rocket and launch the mission.");
        CreateSectionLabel(content, "Credits");
        CreateInfoLine(content, "Team", "Gran Equipo");
        CreateInfoLine(content, "Members", "Mateo Zaragoza, Mar Fr\u00edas, Cristina Mendoza, Fer Gautr\u00edn, Elena Fierros");
        return content.parent.parent.gameObject;
    }

    void CreateVolumeSlider(RectTransform parent, string label, float value, Action<float> onChanged, out Slider slider, out TMP_Text valueLabel)
    {
        CreateLabeledSlider(parent, label, 0f, 1f, value, onChanged, out slider, out valueLabel, Percent);
    }

    void CreateLabeledSlider(RectTransform parent, string label, float min, float max, float value, Action<float> onChanged, out Slider slider, out TMP_Text valueLabel, Func<float, string> format)
    {
        var row = CreateImage("Slider_" + label, parent, new Color(1f, 1f, 1f, 0.04f));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 72f;
        le.preferredHeight = 72f;
        le.flexibleWidth = 1f;

        var name = CreateText("Etiqueta", row, label, 18f, TextAlignmentOptions.MidlineLeft);
        name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        name.rectTransform.anchorMax = new Vector2(0.42f, 1f);
        name.rectTransform.offsetMin = new Vector2(12f, 0f);
        name.rectTransform.offsetMax = new Vector2(-8f, 0f);

        valueLabel = CreateText("Valor", row, format(value), 18f, TextAlignmentOptions.MidlineRight);
        valueLabel.rectTransform.anchorMin = new Vector2(0.72f, 0.5f);
        valueLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        valueLabel.rectTransform.offsetMin = new Vector2(0f, 0f);
        valueLabel.rectTransform.offsetMax = new Vector2(-12f, 0f);
        valueLabel.color = new Color(1f, 0.85f, 0.40f);

        slider = CreateSliderControl(row, min, max, value);
        var srt = (RectTransform)slider.transform;
        srt.anchorMin = new Vector2(0.02f, 0.08f);
        srt.anchorMax = new Vector2(0.98f, 0.50f);
        srt.offsetMin = new Vector2(12f, 4f);
        srt.offsetMax = new Vector2(-12f, 0f);
        TMP_Text labelRef = valueLabel;
        Func<float, string> fmt = format;
        slider.onValueChanged.AddListener(v =>
        {
            if (labelRef != null) labelRef.text = fmt(v);
            onChanged(v);
        });
    }

    TMP_Text CreateCycleRow(RectTransform parent, string label, string current, Action onPrev, Action onNext)
    {
        var row = CreateImage("Ciclo_" + label, parent, new Color(1f, 1f, 1f, 0.04f));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 64f;
        le.preferredHeight = 64f;
        le.flexibleWidth = 1f;

        var name = CreateText("Etiqueta", row, label, 18f, TextAlignmentOptions.MidlineLeft);
        name.rectTransform.anchorMin = Vector2.zero;
        name.rectTransform.anchorMax = new Vector2(0.38f, 1f);
        name.rectTransform.offsetMin = new Vector2(12f, 0f);
        name.rectTransform.offsetMax = new Vector2(-8f, 0f);

        var prev = CreateButton("Prev", row, new Color(0.20f, 0.22f, 0.30f, 1f));
        var prt = (RectTransform)prev.transform;
        prt.anchorMin = new Vector2(0.40f, 0.18f);
        prt.anchorMax = new Vector2(0.48f, 0.82f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        CreateText("Etiqueta", prev.transform, "<", 22f, TextAlignmentOptions.Center);
        prev.onClick.AddListener(() => onPrev());

        var value = CreateText("Valor", row, current, 18f, TextAlignmentOptions.Center);
        value.rectTransform.anchorMin = new Vector2(0.48f, 0f);
        value.rectTransform.anchorMax = new Vector2(0.84f, 1f);
        value.rectTransform.offsetMin = value.rectTransform.offsetMax = Vector2.zero;
        value.color = new Color(1f, 0.85f, 0.40f);

        var next = CreateButton("Next", row, new Color(0.20f, 0.22f, 0.30f, 1f));
        var nrt = (RectTransform)next.transform;
        nrt.anchorMin = new Vector2(0.84f, 0.18f);
        nrt.anchorMax = new Vector2(0.98f, 0.82f);
        nrt.offsetMin = Vector2.zero;
        nrt.offsetMax = new Vector2(-8f, 0f);
        CreateText("Etiqueta", next.transform, ">", 22f, TextAlignmentOptions.Center);
        next.onClick.AddListener(() => onNext());
        return value;
    }

    static void SetToggleVisual(TMP_Text label, bool on, string onText, string offText)
    {
        if (label == null) return;
        label.text = on ? onText : offText;
        var img = label.transform.parent != null ? label.transform.parent.GetComponent<Image>() : null;
        if (img != null)
            img.color = on ? new Color(0.25f, 0.50f, 0.35f, 1f) : new Color(0.20f, 0.22f, 0.30f, 1f);
    }

    TMP_Text CreateToggleRow(RectTransform parent, string label, Func<bool> getCurrent, Action<bool> onChanged)
    {
        bool on = getCurrent();
        var row = CreateImage("Toggle_" + label, parent, new Color(1f, 1f, 1f, 0.04f));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 64f;
        le.preferredHeight = 64f;
        le.flexibleWidth = 1f;

        var name = CreateText("Etiqueta", row, label, 18f, TextAlignmentOptions.MidlineLeft);
        name.rectTransform.anchorMin = Vector2.zero;
        name.rectTransform.anchorMax = new Vector2(0.58f, 1f);
        name.rectTransform.offsetMin = new Vector2(12f, 0f);
        name.rectTransform.offsetMax = new Vector2(-8f, 0f);

        var button = CreateButton("Valor", row, on ? new Color(0.25f, 0.50f, 0.35f, 1f) : new Color(0.20f, 0.22f, 0.30f, 1f));
        var brt = (RectTransform)button.transform;
        brt.anchorMin = new Vector2(0.60f, 0.18f);
        brt.anchorMax = new Vector2(0.98f, 0.82f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = new Vector2(-8f, 0f);
        string onText = "On";
        string offText = "Off";
        var value = CreateText("Texto", button.transform, on ? onText : offText, 18f, TextAlignmentOptions.Center);
        button.onClick.AddListener(() => onChanged(!getCurrent()));
        return value;
    }

    void CreateSectionLabel(RectTransform parent, string text)
    {
        var label = CreateText("Seccion", parent, text, 22f, TextAlignmentOptions.MidlineLeft);
        var le = label.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 36f;
        le.preferredHeight = 36f;
        label.color = new Color(0.95f, 0.55f, 0.15f);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
    }

    void CreateHint(RectTransform parent, string text)
    {
        var label = CreateText("Nota", parent, text, 16f, TextAlignmentOptions.TopLeft);
        var le = label.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 56f;
        le.preferredHeight = 56f;
        label.color = new Color(0.70f, 0.74f, 0.82f);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        label.rectTransform.offsetMax = new Vector2(-8f, 0f);
    }

    void CreateInfoLine(RectTransform parent, string key, string value)
    {
        var row = CreateImage("Info_" + key, parent, new Color(1f, 1f, 1f, 0.04f));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 48f;
        le.preferredHeight = 48f;
        le.flexibleWidth = 1f;
        var k = CreateText("Clave", row, key, 16f, TextAlignmentOptions.MidlineLeft);
        k.rectTransform.anchorMin = Vector2.zero;
        k.rectTransform.anchorMax = new Vector2(0.34f, 1f);
        k.rectTransform.offsetMin = new Vector2(12f, 0f);
        k.rectTransform.offsetMax = new Vector2(-6f, 0f);
        k.color = new Color(0.70f, 0.74f, 0.82f);
        var v = CreateText("Valor", row, value, 16f, TextAlignmentOptions.MidlineLeft);
        v.rectTransform.anchorMin = new Vector2(0.34f, 0f);
        v.rectTransform.anchorMax = Vector2.one;
        v.rectTransform.offsetMin = new Vector2(0f, 0f);
        v.rectTransform.offsetMax = new Vector2(-12f, 0f);
    }

    void CreateParagraph(RectTransform parent, string text)
    {
        var label = CreateText("Parrafo", parent, text, 17f, TextAlignmentOptions.TopLeft);
        var le = label.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 110f;
        le.preferredHeight = 110f;
        label.color = new Color(0.90f, 0.92f, 0.95f);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        label.rectTransform.offsetMax = new Vector2(-8f, 0f);
    }

    RectTransform CreateScrollContent(RectTransform body, string name)
    {
        var host = CreateRect(name, body);
        Stretch(host, 0f, 0f, 0f, 0f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(host, false);
        var vpRt = (RectTransform)viewport.transform;
        Stretch(vpRt, 0f, 0f, 0f, 0f);
        var vpImage = viewport.GetComponent<Image>();
        vpImage.color = new Color(0f, 0f, 0f, 0.12f);
        vpImage.raycastTarget = true;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        var content = (RectTransform)contentGo.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = host.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = vpRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.inertia = true;
        return content;
    }

    static Slider CreateSliderControl(RectTransform parent, float min, float max, float value)
    {
        var go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.18f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = (RectTransform)fillArea.transform;
        Stretch(faRt, 6f, 6f, 6f, 6f);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(fillArea.transform, false);
        var fillRt = (RectTransform)fillGo.transform;
        Stretch(fillRt, 0f, 0f, 0f, 0f);
        fillGo.GetComponent<Image>().color = new Color(0.95f, 0.55f, 0.15f, 1f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var haRt = (RectTransform)handleArea.transform;
        Stretch(haRt, 8f, 0f, 8f, 0f);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(handleArea.transform, false);
        var handleRt = (RectTransform)handleGo.transform;
        handleRt.sizeDelta = new Vector2(22f, 0f);
        handleGo.GetComponent<Image>().color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleGo.GetComponent<Image>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;
        return slider;
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
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static Button CreateButton(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateImage(name, parent, color);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = color;
        button.colors = colors;
        return button;
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
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}
