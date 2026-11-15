using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Unity menu: SpaceApps > Maintenance minigame > Connect to launch scene  (tasks 13 + 31). Signed: claude_c.
// In 02_Lanzamiento (opened by the menu if needed):
//   1. Creates Prefabs/MaintenanceMinigame.prefab if it does not exist (Claude-M's "Create prefab" menu).
//   2. Puts one instance of it as the last child of CanvasLanzamiento (on top of the mission control UI), INACTIVE.
//   3. Adds MaintenanceFlightHook to the Lanzamiento object (next to LaunchScreen and FlightPhases) and links both.
// Then save the scene. It can be run again: it reuses the instance and the hook that are already there.
// In Play: a bad event with minigameRole = Engineering opens the minigame; "Continue" sends RawResult to
// FlightPhases.CompleteMinigame (task 31 rules: resolved, one recovery attempt, or abort).
public static class MaintenanceFlightHookBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/MaintenanceMinigame.prefab";
    const string ScenePath = "Assets/_Project/Scenes/" + Scenes.Launch + ".unity";

    [MenuItem("SpaceApps/Maintenance minigame/Connect to launch scene")]
    static void Connect()
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
            EditorUtility.DisplayDialog("Maintenance minigame",
                Scenes.Launch + " has no FlightPhases. Run SpaceApps > Flight phases > Add to open scene first.", "OK");
            return;
        }
        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Maintenance minigame",
                Scenes.Launch + " has no CanvasLanzamiento (Screen Space Overlay). Run SpaceApps > Build launch scene first.", "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            EditorApplication.ExecuteMenuItem("SpaceApps/Maintenance minigame/Create prefab");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("Maintenance minigame: " + PrefabPath + " could not be created.");
            return;
        }

        // One instance, inactive, on top of the mission control UI
        MaintenanceMinigame minigame = Object.FindAnyObjectByType<MaintenanceMinigame>(FindObjectsInactive.Include);
        if (minigame == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Add maintenance minigame");
            minigame = instance.GetComponent<MaintenanceMinigame>();
        }
        Undo.RecordObject(minigame.transform, "Move maintenance minigame");
        minigame.transform.SetAsLastSibling();
        Undo.RecordObject(minigame.gameObject, "Hide maintenance minigame");
        minigame.gameObject.SetActive(false); // the hook opens it when the flight asks for maintenance

        // Hook on the always-active Lanzamiento object
        MaintenanceFlightHook hook = Object.FindAnyObjectByType<MaintenanceFlightHook>(FindObjectsInactive.Include);
        if (hook == null) hook = Undo.AddComponent<MaintenanceFlightHook>(phases.gameObject);
        Undo.RecordObject(hook, "Link maintenance hook");
        hook.minigame = minigame;
        hook.phases = phases;
        EditorUtility.SetDirty(hook);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = hook.gameObject;
        Debug.Log("Maintenance minigame connected in " + Scenes.Launch + " (hook on '" + hook.gameObject.name
            + "', minigame under '" + canvas.name + "', inactive). Save the scene.");
    }

    [MenuItem("SpaceApps/Maintenance minigame/Connect to launch scene", true)]
    static bool ConnectValid()
    {
        return !Application.isPlaying;
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
