using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Unity menus for the maintenance minigame (task 13):
//   SpaceApps > Maintenance minigame > Create prefab
//       Creates Assets/_Project/Prefabs/MaintenanceMinigame.prefab. Can be run again.
//       If missing, it also creates Assets/_Project/Data/Minigames/MaintenanceSettings.asset (the balance
//       numbers) and assigns it to the prefab. If it already exists, it is reused and its values are kept.
//   SpaceApps > Maintenance minigame > Add to open scene
//       Puts the prefab in the open scene's Canvas (creates a Canvas and EventSystem if missing).
//       To test it alone: open your Sandbox scene, use this menu and press Play.
//
// Layout (1920x1080 reference screen, everything fits in 1860 x 990, 30 apart):
//   top    (640 tall): Oxygen (400, vertical) | main panel (1000) | Propulsion (400, vertical)
//   bottom (320 tall): Energy, fuses (915)    | Communications, radio (915)
public static class MaintenanceMinigameBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/MaintenanceMinigame.prefab";
    const string SettingsFolder = "Assets/_Project/Data/Minigames";
    const string SettingsPath = SettingsFolder + "/MaintenanceSettings.asset";
    static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.5f);
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f, 0.95f);
    static readonly Color BarBackColor = new Color(0.2f, 0.22f, 0.28f);
    static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.8f);
    static readonly Color HintColor = new Color(0.65f, 0.7f, 0.8f);

    // Layout sizes
    static readonly Vector2 SideSize = new Vector2(400, 640);    // Oxygen and Propulsion
    static readonly Vector2 MainSize = new Vector2(1000, 640);
    static readonly Vector2 BottomSize = new Vector2(915, 320);  // Energy and Communications
    const float TopY = 175f;      // center of the top row
    const float BottomY = -335f;  // center of the bottom row

    static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    // Square sprite for the bar fills (falls back to Unity's if missing)
    const string SquarePath = "Assets/_Project/Art/Square.png";
    static Sprite BarSprite
    {
        get
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            if (s == null)
            {
                Debug.LogWarning(SquarePath + " was not found as a Sprite; the bars use UISprite.");
                s = UISprite;
            }
            return s;
        }
    }

    [MenuItem("SpaceApps/Maintenance minigame/Create prefab")]
    static void CreatePrefab()
    {
        // Root: covers the whole screen with a dark background (blocks clicks to the flight behind)
        GameObject root = new GameObject("MaintenanceMinigame", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = OverlayColor;
        MaintenanceMinigame game = root.AddComponent<MaintenanceMinigame>();
        game.settings = GetOrCreateSettings();

        // Top row
        game.oxygen = CreateOxygenPanel(root.transform, new Vector2(-730, TopY), game.settings);
        Transform main = CreateBox(root.transform, "MainPanel", new Vector2(0, TopY), MainSize);
        BuildMainPanel(main, game);
        game.propulsion = CreatePropulsionPanel(root.transform, new Vector2(730, TopY));

        // Bottom row
        game.fuses = CreateFusePanel(root.transform, new Vector2(-472.5f, BottomY));
        game.radio = CreateRadioPanel(root.transform, new Vector2(472.5f, BottomY));

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    // Uses the settings asset if it exists (so tuning is not lost); otherwise creates it
    static MaintenanceSettings GetOrCreateSettings()
    {
        MaintenanceSettings settings = AssetDatabase.LoadAssetAtPath<MaintenanceSettings>(SettingsPath);
        if (settings != null) return settings;

        if (!AssetDatabase.IsValidFolder(SettingsFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Minigames");
        settings = ScriptableObject.CreateInstance<MaintenanceSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Settings created: " + SettingsPath);
        return settings;
    }

    // Title, timer, one row per system (name, bar, status, button or panel hint) and info
    static void BuildMainPanel(Transform main, MaintenanceMinigame game)
    {
        TextMeshProUGUI title = CreateText(main, "Title", "Ship maintenance", 44);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(-70, -45), new Vector2(820, 60));
        game.titleLabel = title;

        TextMeshProUGUI timer = CreateText(main, "Timer", "20 s", 40);
        timer.alignment = TextAlignmentOptions.Right;
        Place(timer.rectTransform, new Vector2(400, -45), new Vector2(160, 60));
        game.timerLabel = timer;

        int count = game.systemNames.Length;
        game.bars = new Image[count];
        game.statusLabels = new TMP_Text[count];
        game.repairButtons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            float y = -130 - i * 90;

            TextMeshProUGUI name = CreateText(main, "Name" + (i + 1), game.systemNames[i], 30);
            name.alignment = TextAlignmentOptions.Left;
            Place(name.rectTransform, new Vector2(-330, y), new Vector2(280, 60));

            GameObject back = CreateImage(main, "Bar" + (i + 1), BarBackColor);
            Place((RectTransform)back.transform, new Vector2(10, y), new Vector2(380, 40));
            game.bars[i] = CreateFill(back.transform, Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Left);

            TextMeshProUGUI status = CreateText(main, "Status" + (i + 1), "OK", 28);
            Place(status.rectTransform, new Vector2(260, y), new Vector2(140, 60));
            game.statusLabels[i] = status;

            bool hasPanel = i == game.propulsionIndex || i == game.oxygenIndex || i == game.radioIndex
                || i == game.fusesIndex;
            if (hasPanel)
            {
                TextMeshProUGUI hint = CreateText(main, "Hint" + (i + 1), "Use its panel", 26);
                hint.color = HintColor;
                Place(hint.rectTransform, new Vector2(410, y), new Vector2(160, 60));
            }
            else
            {
                game.repairButtons[i] = CreateButton(main, "Repair" + (i + 1), "Repair [" + (i + 1) + "]",
                    new Vector2(410, y), new Vector2(160, 64));
            }
        }

        TextMeshProUGUI info = CreateText(main, "Info", "", 24);
        Place(info.rectTransform, new Vector2(0, -495), new Vector2(940, 100));
        info.textWrappingMode = TextWrappingModes.Normal;
        game.infoLabel = info;

        game.startButton = CreateButton(main, "Start", "Start", new Vector2(0, -590), new Vector2(320, 64));
        game.continueButton = CreateButton(main, "Continue", "Continue", new Vector2(0, -590), new Vector2(320, 64));
        game.continueButton.gameObject.SetActive(false);
    }

    // Oxygen panel (vertical): tall level bar with its green zone, and the pump lever next to it
    static OxygenPanel CreateOxygenPanel(Transform parent, Vector2 pos, MaintenanceSettings settings)
    {
        Transform box = CreateBox(parent, "OxygenPanel", pos, SideSize);
        OxygenPanel panel = box.gameObject.AddComponent<OxygenPanel>();
        AddPanelHeader(box, SideSize.x, "Oxygen", "Pump the lever up and down. Keep the level in the green zone.");
        panel.stateLabel = AddStateLabel(box, new Vector2(0, -180), SideSize.x - 40, TextAlignmentOptions.Center);

        // Level bar: background, fill (vertical fillAmount) and a semi-transparent green zone on top
        GameObject back = CreateImage(box, "OxygenBar", BarBackColor);
        Place((RectTransform)back.transform, new Vector2(-80, -400), new Vector2(90, 360));
        panel.oxygenFill = CreateFill(back.transform, Image.FillMethod.Vertical, (int)Image.OriginVertical.Bottom);
        panel.oxygenFill.fillAmount = 0f;

        GameObject zone = CreateImage(back.transform, "GreenZone", new Color(0.2f, 0.9f, 0.35f, 0.35f));
        RectTransform zoneRt = (RectTransform)zone.transform;
        // starting position; OxygenPanel repositions it from the settings during play
        zoneRt.anchorMin = new Vector2(0f, settings.sweetMin);
        zoneRt.anchorMax = new Vector2(1f, settings.sweetMax);
        zoneRt.offsetMin = zoneRt.offsetMax = Vector2.zero;
        zone.GetComponent<Image>().raycastTarget = false;
        panel.sweetZone = zoneRt;

        TextMeshProUGUI levelLabel = CreateText(box, "LevelLabel", "O2 level", 26);
        Place(levelLabel.rectTransform, new Vector2(-80, -610), new Vector2(160, 40));

        panel.pump = CreateSlider(box, "Pump", new Vector2(90, -400), new Vector2(60, 360), Slider.Direction.BottomToTop);

        TextMeshProUGUI pumpLabel = CreateText(box, "PumpLabel", "Pump", 26);
        Place(pumpLabel.rectTransform, new Vector2(90, -610), new Vector2(160, 40));
        return panel;
    }

    // Propulsion panel (vertical): 4 rows, each with its LED on the left and its switch on the right
    static PropulsionPanel CreatePropulsionPanel(Transform parent, Vector2 pos)
    {
        Transform box = CreateBox(parent, "PropulsionPanel", pos, SideSize);
        PropulsionPanel panel = box.gameObject.AddComponent<PropulsionPanel>();
        AddPanelHeader(box, SideSize.x, "Propulsion", "Set each switch to match the LED on its left.");
        panel.stateLabel = AddStateLabel(box, new Vector2(0, -180), SideSize.x - 40, TextAlignmentOptions.Center);

        const int count = 4;
        panel.leds = new Image[count];
        panel.toggles = new Toggle[count];
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite check = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        for (int i = 0; i < count; i++)
        {
            float y = -260 - i * 100;

            GameObject led = CreateImage(box, "LED" + (i + 1), panel.ledOffColor);
            Place((RectTransform)led.transform, new Vector2(-70, y), new Vector2(64, 64));
            led.GetComponent<Image>().sprite = circle;
            panel.leds[i] = led.GetComponent<Image>();

            GameObject toggleGo = new GameObject("Switch" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleGo.transform.SetParent(box, false);
            Place((RectTransform)toggleGo.transform, new Vector2(70, y), new Vector2(76, 76));
            Image back = toggleGo.GetComponent<Image>();
            back.sprite = UISprite;
            back.type = Image.Type.Sliced;
            back.color = new Color(0.85f, 0.85f, 0.9f);

            GameObject mark = CreateImage(toggleGo.transform, "Checkmark", new Color(0.1f, 0.1f, 0.15f));
            Stretch((RectTransform)mark.transform);
            Image markImg = mark.GetComponent<Image>();
            markImg.sprite = check;

            Toggle toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = back;
            toggle.graphic = markImg;
            toggle.isOn = false;
            panel.toggles[i] = toggle;
        }
        return panel;
    }

    // Energy panel: a row of 8 fuses (buttons); a burned one is replaced with a click
    static FusePanel CreateFusePanel(Transform parent, Vector2 pos)
    {
        Transform box = CreateBox(parent, "EnergyPanel", pos, BottomSize);
        FusePanel panel = box.gameObject.AddComponent<FusePanel>();
        AddPanelHeader(box, BottomSize.x, "Energy", "Fuses burn out. Click a burned one (X) to replace it.");
        panel.stateLabel = AddStateLabel(box, TopRight(BottomSize.x), 260, TextAlignmentOptions.Right);

        const int count = 8;
        panel.fuses = new Button[count];
        panel.fuseLabels = new TMP_Text[count];
        for (int i = 0; i < count; i++)
        {
            float x = -385 + i * 110;
            Button fuse = CreateButton(box, "Fuse" + (i + 1), "OK", new Vector2(x, -220), new Vector2(95, 120));
            fuse.image.color = panel.okColor;
            ColorBlock colors = fuse.colors;
            colors.disabledColor = Color.white; // FusePanel sets the color; this keeps it from turning gray at the end
            fuse.colors = colors;
            panel.fuses[i] = fuse;
            panel.fuseLabels[i] = fuse.GetComponentInChildren<TMP_Text>();
        }
        return panel;
    }

    // Communications panel: radio with frequency readout, dial and signal meter
    static RadioPanel CreateRadioPanel(Transform parent, Vector2 pos)
    {
        Transform box = CreateBox(parent, "CommunicationsPanel", pos, BottomSize);
        RadioPanel panel = box.gameObject.AddComponent<RadioPanel>();
        AddPanelHeader(box, BottomSize.x, "Communications", "Turn the dial to tune in Earth. Follow the signal: the frequency drifts.");
        panel.stateLabel = AddStateLabel(box, TopRight(BottomSize.x), 260, TextAlignmentOptions.Right);

        TextMeshProUGUI freq = CreateText(box, "Frequency", "2250.0 MHz", 48);
        freq.fontStyle = FontStyles.Bold;
        Place(freq.rectTransform, new Vector2(-290, -215), new Vector2(300, 70));
        panel.frequencyLabel = freq;

        TextMeshProUGUI dialLabel = CreateText(box, "DialLabel", "Dial", 26);
        Place(dialLabel.rectTransform, new Vector2(-100, -185), new Vector2(80, 40));
        panel.dial = CreateSlider(box, "Dial", new Vector2(185, -185), new Vector2(480, 36), Slider.Direction.LeftToRight);
        panel.dial.value = 0.5f;

        TextMeshProUGUI signalLabel = CreateText(box, "SignalLabel", "Signal", 26);
        Place(signalLabel.rectTransform, new Vector2(-100, -255), new Vector2(80, 40));
        GameObject back = CreateImage(box, "SignalBar", BarBackColor);
        Place((RectTransform)back.transform, new Vector2(185, -255), new Vector2(480, 26));
        panel.signalFill = CreateFill(back.transform, Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Left);
        panel.signalFill.fillAmount = 0f;
        return panel;
    }

    // uGUI Slider built by hand: background + handle area + handle (vertical or horizontal)
    static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size, Slider.Direction direction)
    {
        bool vertical = direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, size);

        GameObject back = CreateImage(go.transform, "Background", BarBackColor);
        Stretch((RectTransform)back.transform);
        back.GetComponent<Image>().sprite = UISprite;
        back.GetComponent<Image>().type = Image.Type.Sliced;

        GameObject area = new GameObject("HandleArea", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        RectTransform areaRt = (RectTransform)area.transform;
        areaRt.anchorMin = Vector2.zero;
        areaRt.anchorMax = Vector2.one;
        // margin so the handle does not stick out at the ends
        areaRt.offsetMin = vertical ? new Vector2(0, 20) : new Vector2(20, 0);
        areaRt.offsetMax = vertical ? new Vector2(0, -20) : new Vector2(-20, 0);

        GameObject handle = CreateImage(area.transform, "Handle", new Color(0.9f, 0.9f, 0.95f));
        RectTransform handleRt = (RectTransform)handle.transform;
        // 40 long along the slider, 30 thicker than the slider
        handleRt.sizeDelta = vertical ? new Vector2(30, 40) : new Vector2(40, 30);
        Image handleImg = handle.GetComponent<Image>();
        handleImg.sprite = UISprite;
        handleImg.type = Image.Type.Sliced;

        Slider slider = go.GetComponent<Slider>();
        slider.direction = direction;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        return slider;
    }

    [MenuItem("SpaceApps/Maintenance minigame/Add to open scene")]
    static void AddToScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            CreatePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        }
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add maintenance minigame");
        Selection.activeGameObject = instance;
    }

    // ---------- Helpers ----------

    // Box with a background, positioned relative to the parent's center
    static Transform CreateBox(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateImage(parent, name, BoxColor);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go.transform;
    }

    // Title at the top left and instructions below, both with a 20 margin on each side
    static void AddPanelHeader(Transform box, float boxWidth, string title, string instructions)
    {
        float width = boxWidth - 40;
        TextMeshProUGUI t = CreateText(box, "Title", title, 40);
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Left;
        Place(t.rectTransform, new Vector2(0, -40), new Vector2(width, 56));

        TextMeshProUGUI i = CreateText(box, "Instructions", instructions, 24);
        i.color = HintColor;
        i.textWrappingMode = TextWrappingModes.Normal;
        Place(i.rectTransform, new Vector2(0, -110), new Vector2(width, 80));
    }

    // Top right corner (same height as the title) for a 260-wide text
    static Vector2 TopRight(float boxWidth)
    {
        return new Vector2(boxWidth / 2 - 20 - 130, -40);
    }

    // Panel status text: "In range", "Aligned", etc.
    static TextMeshProUGUI AddStateLabel(Transform box, Vector2 pos, float width, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI s = CreateText(box, "State", "", 28);
        s.fontStyle = FontStyles.Bold;
        s.alignment = alignment;
        Place(s.rectTransform, pos, new Vector2(width, 56));
        return s;
    }

    // Bar fill: a Filled Image driven by fillAmount.
    // A Filled Image needs a sprite; without one Unity draws it full.
    static Image CreateFill(Transform back, Image.FillMethod method, int origin)
    {
        GameObject fill = CreateImage(back, "Fill", Color.white);
        Stretch((RectTransform)fill.transform);
        Image img = fill.GetComponent<Image>();
        img.sprite = BarSprite;
        img.type = Image.Type.Filled;
        img.fillMethod = method;
        img.fillOrigin = origin;
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

    static GameObject CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, size);
        Image img = go.GetComponent<Image>();
        img.sprite = UISprite;
        img.type = Image.Type.Sliced;
        img.color = ButtonColor;
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, 28);
        label.fontStyle = FontStyles.Bold;
        Stretch(label.rectTransform);
        return button;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // Fills the whole parent
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Position relative to the parent's top edge
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
