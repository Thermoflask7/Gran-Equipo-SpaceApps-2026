using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Unity menus for the rocket model (task 9b, P2):
//   SpaceApps > Rocket model > Create prefab        -> Prefabs/RocketModel.prefab (the panel UI)
//   SpaceApps > Rocket model > Add to open scene    -> panel on the overlay canvas (inactive in 01_Planeacion)
//   SpaceApps > Desk > Add rocket model station     -> RocketModelStation on the desk rocket (selected object or
//                                                      "Stylized Rocket"), linked to the panel. Save the scene.
//   SpaceApps > Rocket model > Test: ...            -> Play mode helpers
// No YAML is edited by hand: the prefab and the scene objects are made here.
public static class RocketModelBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/RocketModel.prefab";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";
    const string DeskObjectName = "Stylized Rocket"; // the decorative rocket already on the desk

    static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.13f, 0.92f);
    static readonly Color HeaderColor = new Color(0.95f, 0.55f, 0.15f, 1f);
    static readonly Color TextColor = new Color(0.92f, 0.94f, 0.97f, 1f);
    static readonly Color RowColor = new Color(0.18f, 0.22f, 0.3f, 1f);
    static readonly Color ButtonColor = new Color(0.95f, 0.55f, 0.15f, 1f);
    static readonly Color CloseColor = new Color(0.75f, 0.2f, 0.2f, 1f);

    // ---------- Prefab ----------

    [MenuItem("SpaceApps/Rocket model/Create prefab")]
    static void CreatePrefab()
    {
        GameObject root = NewUI("RocketModel", null, typeof(RocketModel));
        Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RocketModel model = root.GetComponent<RocketModel>();

        // Dark full-screen backdrop: a different view from the desk, and clicks do not reach it
        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.97f);

        // Title bar
        GameObject top = NewUI("TitleBar", root.transform, typeof(Image));
        top.GetComponent<Image>().color = PanelColor;
        Stretch((RectTransform)top.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0, -80), Vector2.zero);
        TextMeshProUGUI title = CreateText(top.transform, "Title", "ROCKET MODEL  ·  mount the parts you bought", 36, HeaderColor);
        title.fontStyle = FontStyles.Bold;
        Stretch(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(30, 0), new Vector2(-120, 0));
        title.alignment = TextAlignmentOptions.MidlineLeft;
        model.closeButton = CreateButton(top.transform, "Close", "X", CloseColor, Color.white);
        Place((RectTransform)model.closeButton.transform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(64, 64));

        // ----- Left half: parts -----
        GameObject left = NewUI("PartsHalf", root.transform);
        Stretch((RectTransform)left.transform, new Vector2(0.01f, 0.02f), new Vector2(0.495f, 1f), Vector2.zero, new Vector2(0, -95));

        // Tray (bought, not mounted) and rocket list side by side in the upper part
        GameObject tray = Box(left.transform, "Tray", new Vector2(0f, 0.42f), new Vector2(0.495f, 1f));
        TextMeshProUGUI trayTitle = CreateText(tray.transform, "Heading", "Bought parts (click to mount)", 24, HeaderColor);
        Stretch(trayTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(14, -50), new Vector2(-14, -8));
        trayTitle.alignment = TextAlignmentOptions.MidlineLeft;
        model.trayContent = ScrollList(tray.transform, "TrayList", new Vector2(8, 84), new Vector2(-8, -54));
        model.trayHint = CreateText(tray.transform, "Hint", "", 22, TextColor);
        Stretch(model.trayHint.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 1f), new Vector2(16, 0), new Vector2(-16, -60));
        model.mountAllButton = CreateButton(tray.transform, "MountAll", "Mount all", ButtonColor, Color.white);
        Stretch((RectTransform)model.mountAllButton.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(10, 12), new Vector2(-10, 72));

        GameObject onRocket = Box(left.transform, "OnRocket", new Vector2(0.505f, 0.42f), new Vector2(1f, 1f));
        TextMeshProUGUI rocketTitle = CreateText(onRocket.transform, "Heading", "On the rocket (click to take off)", 24, HeaderColor);
        Stretch(rocketTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(14, -50), new Vector2(-14, -8));
        rocketTitle.alignment = TextAlignmentOptions.MidlineLeft;
        model.rocketContent = ScrollList(onRocket.transform, "RocketList", new Vector2(8, 84), new Vector2(-8, -54));
        model.clearButton = CreateButton(onRocket.transform, "Clear", "Take all off", RowColor, TextColor);
        Stretch((RectTransform)model.clearButton.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(10, 12), new Vector2(-10, 72));

        // Status and mission numbers in the lower part
        GameObject info = Box(left.transform, "Status", Vector2.zero, new Vector2(1f, 0.4f));
        model.statusText = CreateText(info.transform, "Status", "", 24, TextColor);
        Stretch(model.statusText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(18, -70), new Vector2(-18, -10));
        model.statusText.alignment = TextAlignmentOptions.TopLeft;
        model.statsText = CreateText(info.transform, "Stats", "", 20, TextColor);
        Stretch(model.statsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 10), new Vector2(-18, -74));
        model.statsText.alignment = TextAlignmentOptions.TopLeft;

        // ----- Right half: the low poly rocket, live -----
        GameObject right = Box(root.transform, "ModelHalf", new Vector2(0.505f, 0.02f), new Vector2(0.99f, 1f));
        ((RectTransform)right.transform).offsetMax = new Vector2(0, -95);
        GameObject view = NewUI("ModelView", right.transform, typeof(RawImage));
        Stretch((RectTransform)view.transform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -56));
        model.modelView = view.GetComponent<RawImage>();
        model.modelView.raycastTarget = false;
        TextMeshProUGUI viewTitle = CreateText(right.transform, "Heading", "Your rocket", 24, HeaderColor);
        Stretch(viewTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(14, -50), new Vector2(-14, -8));
        viewTitle.alignment = TextAlignmentOptions.MidlineLeft;

        // Row template (one button per part, copied at runtime)
        Button template = CreateButton(model.trayContent, "RowTemplate", "Part", RowColor, TextColor);
        ((RectTransform)template.transform).sizeDelta = new Vector2(0, 72);
        TextMeshProUGUI rowText = template.GetComponentInChildren<TextMeshProUGUI>();
        rowText.fontSize = 22;
        rowText.fontStyle = FontStyles.Normal;
        rowText.alignment = TextAlignmentOptions.MidlineLeft;
        rowText.margin = new Vector4(12, 0, 8, 0);
        template.gameObject.AddComponent<LayoutElement>().minHeight = 72;
        template.gameObject.SetActive(false);
        model.rowTemplate = template;

        model.mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (model.mission == null)
            Debug.LogWarning("Rocket model: mission not found at " + MissionPath + ". Run SpaceApps > Import Artemis catalog.");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    [MenuItem("SpaceApps/Rocket model/Add to open scene")]
    static void AddToScene()
    {
        AddPanel();
    }

    static RocketModel AddPanel()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            CreatePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }
        Canvas canvas = OverlayCanvasUtil.GetOrCreate(); // never the PC monitor canvas
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add rocket model panel");
        if (SceneManager.GetActiveScene().name == Scenes.Planning)
        {
            instance.SetActive(false); // the desk station opens it
            Debug.Log("Rocket model panel added INACTIVE to " + Scenes.Planning + ": RocketModelStation opens it.");
        }
        Selection.activeGameObject = instance;
        return instance.GetComponent<RocketModel>();
    }

    // ---------- Desk station ----------

    [MenuItem("SpaceApps/Desk/Add rocket model station")]
    static void AddStation()
    {
        GameObject target = Selection.activeGameObject;
        if (target != null && (target.scene != SceneManager.GetActiveScene() || target.GetComponentInParent<Canvas>() != null))
            target = null;
        if (target == null) target = FindInScene(DeskObjectName);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Rocket model station",
                "No desk rocket found. Select the rocket on the desk (in 01_Planeacion it is called '" + DeskObjectName + "') and run this menu again.",
                "OK");
            return;
        }

        if (target.GetComponentInChildren<Collider>() == null)
        {
            Undo.AddComponent<BoxCollider>(target);
            Debug.Log("Rocket model station: added a BoxCollider to '" + target.name + "' so it can be clicked.");
        }

        RocketModel panel = Object.FindAnyObjectByType<RocketModel>(FindObjectsInactive.Include);
        if (panel == null) panel = AddPanel();
        Undo.RecordObject(panel.gameObject, "Hide rocket model panel");
        panel.gameObject.SetActive(false);

        RocketModelStation station = target.GetComponent<RocketModelStation>();
        if (station == null) station = Undo.AddComponent<RocketModelStation>(target);
        Undo.RecordObject(station, "Link rocket model station");
        station.panel = panel;
        EditorUtility.SetDirty(station);

        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = target;
        Debug.Log("Rocket model station: clicking '" + target.name + "' opens the rocket model. Save the scene.");
    }

    [MenuItem("SpaceApps/Desk/Add rocket model station", true)]
    static bool NotInPlayMode()
    {
        return !Application.isPlaying;
    }

    static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.gameObject.scene == SceneManager.GetActiveScene() && t.name == name) return t.gameObject;
        return null;
    }

    // ---------- Play mode tests ----------

    [MenuItem("SpaceApps/Rocket model/Test: buy and mount a sample rocket")]
    static void TestSampleRocket()
    {
        GameManager gm = GameManager.Instance;
        if (GameData.Instance == null)
        {
            Debug.LogWarning("Rocket model: GameData is missing. Run SpaceApps > Rebuild game data list.");
            return;
        }
        string[] ids = { "ART-SLS-BLOCK1", "ART-CM-ORION", "ART-ESM-ORION", "ART-LAS-ORION", "ART-SCI-LUNAR", "ART-COMMS-DS" };
        foreach (string id in ids)
            foreach (PartData p in GameData.Instance.parts)
                if (p != null && p.catalogId == id && !gm.boughtParts.Contains(p)) gm.BuyPart(p);
        int added = RocketAssembly.MountAll(gm);
        Debug.Log("Rocket model: mounted " + added + " parts. rocketValid = " + gm.rocketValid
            + (gm.rocketValid ? "" : " (" + RocketAssembly.Status(gm) + ")"));
    }

    [MenuItem("SpaceApps/Rocket model/Test: open rocket model")]
    static void TestOpen()
    {
        RocketModelStation station = Object.FindAnyObjectByType<RocketModelStation>();
        if (station != null) { station.Open(); return; }
        RocketModel panel = Object.FindAnyObjectByType<RocketModel>(FindObjectsInactive.Include);
        if (panel != null) panel.Open();
        else Debug.LogWarning("Rocket model: no panel in the scene. Use SpaceApps > Rocket model > Add to open scene.");
    }

    [MenuItem("SpaceApps/Rocket model/Test: reset game")]
    static void TestReset()
    {
        GameManager.Instance.ResetGame();
    }

    [MenuItem("SpaceApps/Rocket model/Test: buy and mount a sample rocket", true)]
    [MenuItem("SpaceApps/Rocket model/Test: open rocket model", true)]
    [MenuItem("SpaceApps/Rocket model/Test: reset game", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
    }

    // ---------- UI helpers ----------

    static GameObject Box(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = NewUI(name, parent, typeof(Image));
        go.GetComponent<Image>().color = PanelColor;
        Stretch((RectTransform)go.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        return go;
    }

    // Vertical scroll list; returns its Content
    static Transform ScrollList(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject scroll = NewUI(name, parent, typeof(Image), typeof(ScrollRect));
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
        Stretch((RectTransform)scroll.transform, Vector2.zero, Vector2.one, offsetMin, offsetMax);

        GameObject viewport = NewUI("Viewport", scroll.transform, typeof(RectMask2D));
        Stretch((RectTransform)viewport.transform, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));

        GameObject content = NewUI("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rt = (RectTransform)content.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = scroll.GetComponent<ScrollRect>();
        sr.viewport = (RectTransform)viewport.transform;
        sr.content = rt;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        return content.transform;
    }

    static GameObject NewUI(string name, Transform parent, params System.Type[] components)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        foreach (System.Type t in components) go.AddComponent(t);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = NewUI(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string text, Color color, Color textColor)
    {
        GameObject go = NewUI(name, parent, typeof(Image), typeof(Button));
        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = color;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, 28, textColor);
        label.fontStyle = FontStyles.Bold;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
