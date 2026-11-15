using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Unity menus for the research minigame (task 14). Signed: claude_c.
//   SpaceApps > Research minigame > Create prefab
//       Creates Assets/_Project/Prefabs/ResearchMinigame.prefab (and Data/Minigames/ResearchSettings.asset if missing).
//   SpaceApps > Research minigame > Add to open scene
//       Puts the prefab on an overlay canvas of the open scene (to test it alone in a Sandbox scene: press Play, then Start).
//   SpaceApps > Research minigame > Connect to launch scene
//       In 02_Lanzamiento: one instance, inactive, last child of CanvasLanzamiento + ResearchFlightHook on Lanzamiento. Save the scene.
// The games draw their targets at runtime with plain shapes, so the prefab only holds the frame, texts and buttons.
public static class ResearchMinigameBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/ResearchMinigame.prefab";
    const string SettingsFolder = "Assets/_Project/Data/Minigames";
    const string SettingsPath = SettingsFolder + "/ResearchSettings.asset";
    const string ScenePath = "Assets/_Project/Scenes/" + Scenes.Launch + ".unity";

    static readonly Color Backdrop = new Color(0.02f, 0.03f, 0.06f, 0.92f);
    static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.15f, 0.98f);
    static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.8f);
    static readonly Color Accent = new Color(1f, 0.82f, 0.3f);

    [MenuItem("SpaceApps/Research minigame/Create prefab")]
    public static void CreatePrefab()
    {
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject root = new GameObject("ResearchMinigame", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = Backdrop; // blocks clicks to the flight behind

        ResearchMinigame game = root.AddComponent<ResearchMinigame>();
        PhotoSafari safari = root.AddComponent<PhotoSafari>();
        SignalCatcher catcher = root.AddComponent<SignalCatcher>();
        game.safari = safari;
        game.catcher = catcher;
        game.settings = GetOrCreateSettings();
        game.circle = circle;

        // Header
        TMP_Text title = Text(root.transform, "Title", "Science in lunar orbit", 48, new Vector2(0f, 480f), new Vector2(1400f, 70f), Accent);
        title.fontStyle = FontStyles.Bold;
        game.titleLabel = title;
        TMP_Text timer = Text(root.transform, "Timer", "", 44, new Vector2(760f, 480f), new Vector2(240f, 70f), Color.white);
        TMP_Text info = Text(root.transform, "Info", "", 28, new Vector2(0f, 415f), new Vector2(1600f, 50f), new Color(0.8f, 0.85f, 0.95f));

        // Shared play area (masked) and photo bar
        GameObject areaGo = new GameObject("PlayArea", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        areaGo.transform.SetParent(root.transform, false);
        RectTransform area = (RectTransform)areaGo.transform;
        Place(area, new Vector2(0f, 10f), new Vector2(1600f, 740f));
        areaGo.GetComponent<Image>().color = new Color(0.01f, 0.01f, 0.04f, 1f);

        GameObject barGo = new GameObject("PhotoBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        barGo.transform.SetParent(root.transform, false);
        RectTransform bar = (RectTransform)barGo.transform;
        Place(bar, new Vector2(0f, -430f), new Vector2(1600f, 64f));
        HorizontalLayoutGroup layout = barGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        safari.area = area;
        safari.photoBar = bar;
        safari.timerLabel = timer;
        safari.infoLabel = info;
        safari.circle = circle;
        catcher.area = area;
        catcher.timerLabel = timer;
        catcher.infoLabel = info;
        catcher.circle = circle;

        // Intro panel
        RectTransform intro = Panel(root.transform, "Intro", new Vector2(0f, 10f), new Vector2(1400f, 700f));
        TMP_Text introText = Text(intro, "Text", "", 30, new Vector2(0f, 60f), new Vector2(1250f, 480f), Color.white);
        introText.alignment = TextAlignmentOptions.TopLeft;
        Button start = MakeButton(intro, "Start", "Start", new Vector2(0f, -270f), new Vector2(360f, 90f), rounded);
        game.introPanel = intro.gameObject;
        game.introText = introText;
        game.startButton = start;

        // Album panel
        RectTransform album = Panel(root.transform, "Album", new Vector2(0f, 10f), new Vector2(1400f, 700f));
        GameObject gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.transform.SetParent(album, false);
        RectTransform grid = (RectTransform)gridGo.transform;
        Place(grid, new Vector2(0f, 175f), new Vector2(1300f, 300f));
        GridLayoutGroup gl = gridGo.GetComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(80f, 90f);
        gl.spacing = new Vector2(12f, 12f);
        gl.childAlignment = TextAnchor.UpperCenter;
        TMP_Text albumText = Text(album, "Text", "", 26, new Vector2(0f, -140f), new Vector2(1250f, 250f), Color.white);
        albumText.alignment = TextAlignmentOptions.TopLeft;
        Button cont = MakeButton(album, "Continue", "Continue", new Vector2(0f, -295f), new Vector2(360f, 80f), rounded);
        game.albumPanel = album.gameObject;
        game.albumGrid = grid;
        game.albumText = albumText;
        game.continueButton = cont;
        album.gameObject.SetActive(false);

        EnsureFolder("Assets/_Project/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    [MenuItem("SpaceApps/Research minigame/Add to open scene")]
    static void AddToScene()
    {
        GameObject prefab = LoadOrCreatePrefab();
        if (prefab == null) return;
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add research minigame");
        Selection.activeGameObject = instance;
    }

    [MenuItem("SpaceApps/Research minigame/Connect to launch scene")]
    static void ConnectToLaunchScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        FlightPhases phases = Object.FindAnyObjectByType<FlightPhases>(FindObjectsInactive.Include);
        if (phases == null)
        {
            EditorUtility.DisplayDialog("Research minigame", Scenes.Launch + " has no FlightPhases. Run SpaceApps > Flight phases > Add to open scene first.", "OK");
            return;
        }
        Canvas canvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas c = root.GetComponent<Canvas>();
            if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (canvas == null || root.name == "CanvasLanzamiento") canvas = c;
        }
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Research minigame", Scenes.Launch + " has no overlay canvas (CanvasLanzamiento). Run SpaceApps > Build launch scene first.", "OK");
            return;
        }
        GameObject prefab = LoadOrCreatePrefab();
        if (prefab == null) return;

        ResearchMinigame minigame = Object.FindAnyObjectByType<ResearchMinigame>(FindObjectsInactive.Include);
        if (minigame == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Add research minigame");
            minigame = instance.GetComponent<ResearchMinigame>();
        }
        Undo.RecordObject(minigame.transform, "Move research minigame");
        minigame.transform.SetAsLastSibling();
        Undo.RecordObject(minigame.gameObject, "Hide research minigame");
        minigame.gameObject.SetActive(false);

        ResearchFlightHook hook = Object.FindAnyObjectByType<ResearchFlightHook>(FindObjectsInactive.Include);
        if (hook == null) hook = Undo.AddComponent<ResearchFlightHook>(phases.gameObject);
        Undo.RecordObject(hook, "Link research hook");
        hook.minigame = minigame;
        hook.phases = phases;
        EditorUtility.SetDirty(hook);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = hook.gameObject;
        Debug.Log("Research minigame connected in " + Scenes.Launch + " (hook on '" + hook.gameObject.name
            + "', minigame under '" + canvas.name + "', inactive). Save the scene.");
    }

    [MenuItem("SpaceApps/Research minigame/Add to open scene", true)]
    [MenuItem("SpaceApps/Research minigame/Connect to launch scene", true)]
    static bool EditModeOnly()
    {
        return !Application.isPlaying;
    }

    // ---------- Helpers ----------

    static GameObject LoadOrCreatePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) Debug.LogError("Research minigame: " + PrefabPath + " could not be created.");
        return prefab;
    }

    static ResearchSettings GetOrCreateSettings()
    {
        ResearchSettings s = AssetDatabase.LoadAssetAtPath<ResearchSettings>(SettingsPath);
        if (s != null) return s;
        EnsureFolder(SettingsFolder);
        s = ScriptableObject.CreateInstance<ResearchSettings>();
        AssetDatabase.CreateAsset(s, SettingsPath);
        AssetDatabase.SaveAssets();
        return s;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        string parent = folder.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
    }

    static void Place(RectTransform r, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }

    static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)go.transform;
        Place(r, position, size);
        go.GetComponent<Image>().color = PanelColor;
        return r;
    }

    static TMP_Text Text(Transform parent, string name, string content, float size, Vector2 position, Vector2 box, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Truncate;
        t.raycastTarget = false;
        Place(t.rectTransform, position, box);
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, position, size);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = ButtonColor;
        TMP_Text t = Text(go.transform, "Text", label, 36, Vector2.zero, size, Color.white);
        t.fontStyle = FontStyles.Bold;
        return go.GetComponent<Button>();
    }
}
