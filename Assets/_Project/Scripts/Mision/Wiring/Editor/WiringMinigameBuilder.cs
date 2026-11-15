using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Unity menus of the wiring minigame (task 43). Signed: claude_c.
//   SpaceApps > Wiring minigame > Create prefab           settings asset + Prefabs/WiringMinigame.prefab (UI is built in code)
//   SpaceApps > Wiring minigame > Add to open scene       one instance, active, on the overlay canvas (to try it on its own)
//   SpaceApps > Wiring minigame > Connect to launch scene 02_Lanzamiento: instance (inactive) under CanvasLanzamiento
//                                                         + WiringFlightHook on Lanzamiento, linked. Then save the scene.
//   SpaceApps > Wiring minigame > Test: open next round   (Play) opens the minigame with round 1, 2, 3... (3 panels each)
public static class WiringMinigameBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/WiringMinigame.prefab";
    const string SettingsFolder = "Assets/_Project/Data/Minigames";
    const string SettingsPath = SettingsFolder + "/WiringSettings.asset";
    const string ScenePath = "Assets/_Project/Scenes/" + Scenes.Launch + ".unity";

    static int testLevel;

    [MenuItem("SpaceApps/Wiring minigame/Create prefab")]
    public static GameObject CreatePrefab()
    {
        WiringSettings settings = GetOrCreateSettings();

        GameObject go = new GameObject("WiringMinigame", typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        WiringMinigame minigame = go.AddComponent<WiringMinigame>();
        minigame.settings = settings;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        Debug.Log("Wiring minigame: prefab saved in " + PrefabPath + " (settings: " + SettingsPath + ").");
        return prefab;
    }

    [MenuItem("SpaceApps/Wiring minigame/Add to open scene")]
    static void AddToOpenScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) prefab = CreatePrefab();
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add wiring minigame");
        instance.transform.SetAsLastSibling();
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("Wiring minigame added to '" + canvas.name + "'. Press Play to try it.");
    }

    [MenuItem("SpaceApps/Wiring minigame/Connect to launch scene")]
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
            EditorUtility.DisplayDialog("Wiring minigame",
                Scenes.Launch + " has no FlightPhases. Run SpaceApps > Flight phases > Add to open scene first.", "OK");
            return;
        }
        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Wiring minigame",
                Scenes.Launch + " has no CanvasLanzamiento (Screen Space Overlay). Run SpaceApps > Build launch scene first.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) prefab = CreatePrefab();

        WiringMinigame minigame = Object.FindAnyObjectByType<WiringMinigame>(FindObjectsInactive.Include);
        if (minigame == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Add wiring minigame");
            minigame = instance.GetComponent<WiringMinigame>();
        }
        Undo.RecordObject(minigame.transform, "Move wiring minigame");
        minigame.transform.SetAsLastSibling();
        Undo.RecordObject(minigame.gameObject, "Hide wiring minigame");
        minigame.gameObject.SetActive(false); // the hook opens it when the flight asks for it

        WiringFlightHook hook = Object.FindAnyObjectByType<WiringFlightHook>(FindObjectsInactive.Include);
        if (hook == null) hook = Undo.AddComponent<WiringFlightHook>(phases.gameObject);
        Undo.RecordObject(hook, "Link wiring hook");
        hook.minigame = minigame;
        hook.phases = phases;
        hook.screen = phases.GetComponent<LaunchScreen>() != null ? phases.GetComponent<LaunchScreen>() : Object.FindAnyObjectByType<LaunchScreen>(FindObjectsInactive.Include);
        hook.maintenance = Object.FindAnyObjectByType<MaintenanceFlightHook>(FindObjectsInactive.Include);
        EditorUtility.SetDirty(hook);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = hook.gameObject;
        Debug.Log("Wiring minigame connected in " + Scenes.Launch + " (hook on '" + hook.gameObject.name + "', minigame under '"
            + canvas.name + "', inactive" + (hook.maintenance == null ? "; no maintenance hook found, every Engineering minigame will be wiring" : "")
            + "). Save the scene.");
    }

    [MenuItem("SpaceApps/Wiring minigame/Add to open scene", true)]
    [MenuItem("SpaceApps/Wiring minigame/Connect to launch scene", true)]
    [MenuItem("SpaceApps/Wiring minigame/Create prefab", true)]
    static bool EditModeOnly()
    {
        return !Application.isPlaying;
    }

    [MenuItem("SpaceApps/Wiring minigame/Test: open next round")]
    static void TestOpen()
    {
        WiringMinigame minigame = Object.FindAnyObjectByType<WiringMinigame>(FindObjectsInactive.Include);
        if (minigame == null)
        {
            Debug.LogWarning("Wiring minigame: none in the scene. Use SpaceApps > Wiring minigame > Add to open scene, then Play.");
            return;
        }
        minigame.gameObject.SetActive(false);
        minigame.SetRound(testLevel);
        minigame.SetContext("Test");
        minigame.Completed -= LogResult;
        minigame.Completed += LogResult;
        minigame.gameObject.SetActive(true);
        testLevel++;
    }

    [MenuItem("SpaceApps/Wiring minigame/Test: open next round", true)]
    static bool PlayModeOnly()
    {
        return Application.isPlaying;
    }

    static void LogResult(float raw)
    {
        Debug.Log("Wiring minigame finished: raw " + raw.ToString("0.00"));
    }

    static WiringSettings GetOrCreateSettings()
    {
        WiringSettings s = AssetDatabase.LoadAssetAtPath<WiringSettings>(SettingsPath);
        if (s != null) return s;
        if (!AssetDatabase.IsValidFolder(SettingsFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Minigames");
        s = ScriptableObject.CreateInstance<WiringSettings>();
        AssetDatabase.CreateAsset(s, SettingsPath);
        AssetDatabase.SaveAssets();
        return s;
    }

    // CanvasLanzamiento, or any root Screen Space Overlay canvas of the scene
    static Canvas FindCanvas(Scene scene)
    {
        Canvas any = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas c = root.GetComponent<Canvas>();
            if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (root.name == "CanvasLanzamiento") return c;
            if (any == null) any = c;
        }
        return any;
    }
}
