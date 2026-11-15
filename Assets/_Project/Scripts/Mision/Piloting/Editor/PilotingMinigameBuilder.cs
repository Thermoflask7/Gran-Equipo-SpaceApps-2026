using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Unity menus for the piloting minigame (task 12), by Claude-M:
//   SpaceApps > Piloting minigame > Create prefab
//       Creates Assets/_Project/Prefabs/PilotingMinigame.prefab. Can be run again.
//       If missing, it also creates Assets/_Project/Data/Minigames/PilotingSettings.asset (balance numbers and
//       step texts) and assigns it to the prefab. If it already exists, it is reused and its values are kept.
//   SpaceApps > Piloting minigame > Add to open scene
//       Puts the prefab in the open scene's overlay canvas (creates it and an EventSystem if missing).
//       To test it alone: open your Sandbox scene, use this menu and press Play.
//   SpaceApps > Piloting minigame > Connect to launch scene  (PilotingFlightHookBuilder)
//       Hooks the minigame into 02_Lanzamiento under CanvasLanzamiento. Save the scene after.
//
// Layout (1920x1080 reference screen): one centered 1200 x 780 panel.
public static class PilotingMinigameBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/PilotingMinigame.prefab";
    const string SettingsFolder = "Assets/_Project/Data/Minigames";
    const string SettingsPath = SettingsFolder + "/PilotingSettings.asset";
    static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.5f);
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f, 0.95f);
    static readonly Color BarBackColor = new Color(0.2f, 0.22f, 0.28f);
    static readonly Color WindowColor = new Color(0.2f, 0.8f, 0.35f, 0.55f);
    static readonly Color PerfectColor = new Color(0.45f, 1f, 0.6f, 0.9f);
    static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.8f);
    static readonly Color FireColor = new Color(0.95f, 0.45f, 0.15f);
    static readonly Color HintColor = new Color(0.65f, 0.7f, 0.8f);
    static readonly Vector2 BoxSize = new Vector2(1200, 780);

    static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    [MenuItem("SpaceApps/Piloting minigame/Create prefab")]
    static void CreatePrefab()
    {
        // Root: covers the whole screen with a dark background (blocks clicks to the flight behind)
        GameObject root = new GameObject("PilotingMinigame", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = OverlayColor;
        PilotingMinigame game = root.AddComponent<PilotingMinigame>();
        game.settings = GetOrCreateSettings();

        GameObject boxGo = CreateImage(root.transform, "Panel", BoxColor);
        RectTransform boxRt = (RectTransform)boxGo.transform;
        boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = BoxSize;
        Transform box = boxGo.transform;

        TextMeshProUGUI title = CreateText(box, "Title", "Flight control: ascent", 44);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0, -50), new Vector2(1100, 60));
        game.titleLabel = title;

        TextMeshProUGUI counter = CreateText(box, "StepCounter", "5 commands", 28);
        counter.color = HintColor;
        Place(counter.rectTransform, new Vector2(0, -110), new Vector2(1100, 40));
        game.stepCounterLabel = counter;

        TextMeshProUGUI stepName = CreateText(box, "StepName", "", 50);
        stepName.fontStyle = FontStyles.Bold;
        Place(stepName.rectTransform, new Vector2(0, -175), new Vector2(1100, 70));
        game.stepNameLabel = stepName;

        TextMeshProUGUI description = CreateText(box, "Description", "", 26);
        description.color = HintColor;
        description.textWrappingMode = TextWrappingModes.Normal;
        Place(description.rectTransform, new Vector2(0, -255), new Vector2(1060, 90));
        game.descriptionLabel = description;

        // Gauge: background bar, green window, brighter perfect zone, white marker
        GameObject gauge = CreateImage(box, "Gauge", BarBackColor);
        Place((RectTransform)gauge.transform, new Vector2(0, -380), new Vector2(1000, 70));
        game.gauge = (RectTransform)gauge.transform;

        GameObject window = CreateImage(gauge.transform, "Window", WindowColor);
        window.GetComponent<Image>().raycastTarget = false;
        game.window = (RectTransform)window.transform;

        GameObject perfect = CreateImage(window.transform, "Perfect", PerfectColor);
        perfect.GetComponent<Image>().raycastTarget = false;
        game.perfectZone = (RectTransform)perfect.transform;

        GameObject marker = CreateImage(gauge.transform, "Marker", Color.white);
        marker.GetComponent<Image>().raycastTarget = false;
        game.marker = (RectTransform)marker.transform;

        TextMeshProUGUI feedback = CreateText(box, "Feedback", "", 46);
        feedback.fontStyle = FontStyles.Bold;
        Place(feedback.rectTransform, new Vector2(0, -470), new Vector2(1100, 60));
        game.feedbackLabel = feedback;

        TextMeshProUGUI info = CreateText(box, "Info", "", 24);
        info.textWrappingMode = TextWrappingModes.Normal;
        Place(info.rectTransform, new Vector2(0, -565), new Vector2(1100, 110));
        game.infoLabel = info;

        game.fireButton = CreateButton(box, "Fire", "FIRE  (click)", new Vector2(0, -690), new Vector2(380, 84), FireColor);
        game.fireButton.gameObject.SetActive(false);
        game.startButton = CreateButton(box, "Start", "Start", new Vector2(0, -690), new Vector2(320, 70), ButtonColor);
        game.continueButton = CreateButton(box, "Continue", "Continue", new Vector2(0, -690), new Vector2(320, 70), ButtonColor);
        game.continueButton.gameObject.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    // Uses the settings asset if it exists (so tuning is not lost); otherwise creates it
    static PilotingSettings GetOrCreateSettings()
    {
        PilotingSettings settings = AssetDatabase.LoadAssetAtPath<PilotingSettings>(SettingsPath);
        if (settings != null) return settings;

        if (!AssetDatabase.IsValidFolder(SettingsFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Minigames");
        settings = ScriptableObject.CreateInstance<PilotingSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        Debug.Log("Settings created: " + SettingsPath);
        return settings;
    }

    [MenuItem("SpaceApps/Piloting minigame/Add to open scene")]
    static void AddToScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            CreatePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // Overlay canvas only, never an arbitrary Canvas of the scene (audit F02)
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add piloting minigame");
        Selection.activeGameObject = instance;
    }

    // ---------- Helpers ----------

    static GameObject CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, size);
        Image img = go.GetComponent<Image>();
        img.sprite = UISprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, 30);
        label.fontStyle = FontStyles.Bold;
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
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

    // Position relative to the parent's top edge
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
