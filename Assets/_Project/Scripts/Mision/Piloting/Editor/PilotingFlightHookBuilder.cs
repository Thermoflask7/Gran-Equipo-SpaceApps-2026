using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Unity menu: SpaceApps > Piloting minigame > Connect to launch scene  (task 12). By Claude-M, same pattern as
// claude_c's MaintenanceFlightHookBuilder. In 02_Lanzamiento (opened by the menu if needed):
//   1. Creates Prefabs/PilotingMinigame.prefab if it does not exist ("Create prefab" menu).
//   2. Puts one instance of it as the last child of CanvasLanzamiento (on top of the mission control UI), INACTIVE.
//   3. Adds PilotingFlightHook to the Lanzamiento object (next to LaunchScreen and FlightPhases) and links it.
// Then save the scene. It can be run again: it reuses the instance and the hook that are already there.
// In Play: piloting opens during the departure liftoff (FlightPhases.plannedMinigames) and when a bad event asks
// for it; "Continue" sends RawResult to FlightPhases.CompleteMinigame (task 31 rules).
public static class PilotingFlightHookBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/PilotingMinigame.prefab";
    const string ScenePath = "Assets/_Project/Scenes/" + Scenes.Launch + ".unity";

    [MenuItem("SpaceApps/Piloting minigame/Connect to launch scene")]
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
            EditorUtility.DisplayDialog("Piloting minigame",
                Scenes.Launch + " has no FlightPhases. Run SpaceApps > Flight phases > Add to open scene first.", "OK");
            return;
        }
        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Piloting minigame",
                Scenes.Launch + " has no CanvasLanzamiento (Screen Space Overlay). Run SpaceApps > Build launch scene first.", "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            EditorApplication.ExecuteMenuItem("SpaceApps/Piloting minigame/Create prefab");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("Piloting minigame: " + PrefabPath + " could not be created.");
            return;
        }

        // One instance, inactive, on top of the mission control UI
        PilotingMinigame minigame = Object.FindAnyObjectByType<PilotingMinigame>(FindObjectsInactive.Include);
        if (minigame == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Add piloting minigame");
            minigame = instance.GetComponent<PilotingMinigame>();
        }
        Undo.RecordObject(minigame.transform, "Move piloting minigame");
        minigame.transform.SetAsLastSibling();
        Undo.RecordObject(minigame.gameObject, "Hide piloting minigame");
        minigame.gameObject.SetActive(false); // the hook opens it when the flight asks for piloting

        // Hook on the always-active Lanzamiento object
        PilotingFlightHook hook = Object.FindAnyObjectByType<PilotingFlightHook>(FindObjectsInactive.Include);
        if (hook == null) hook = Undo.AddComponent<PilotingFlightHook>(phases.gameObject);
        Undo.RecordObject(hook, "Link piloting hook");
        hook.minigame = minigame;
        hook.phases = phases;
        if (hook.screen == null) hook.screen = phases.GetComponent<LaunchScreen>();
        EditorUtility.SetDirty(hook);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = hook.gameObject;
        Debug.Log("Piloting minigame connected in " + Scenes.Launch + " (hook on '" + hook.gameObject.name
            + "', minigame under '" + canvas.name + "', inactive). Save the scene.");
    }

    [MenuItem("SpaceApps/Piloting minigame/Connect to launch scene", true)]
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
