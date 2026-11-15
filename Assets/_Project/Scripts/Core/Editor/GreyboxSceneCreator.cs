using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu: SpaceApps > Create greybox scenes
//   Creates the missing loop scenes with a title, the state and a "Next" button,
//   and adds them in order to the build's scene list.
// Menu: SpaceApps > Regenerate greybox scenes (overwrites)
//   Same, but recreates all 4 scenes even if they already exist. Only for the initial greybox:
//   do not use it once someone has worked on a scene.
// UI with TextMeshPro and an EventSystem for the new Input System (team rules in AGENTS.md).
public static class GreyboxSceneCreator
{
    const string Folder = "Assets/_Project/Scenes";

    [MenuItem("SpaceApps/Create greybox scenes")]
    static void CreateMissingScenes()
    {
        Build(false);
    }

    [MenuItem("SpaceApps/Regenerate greybox scenes (overwrites)")]
    static void RegenerateScenes()
    {
        if (!EditorUtility.DisplayDialog("Regenerate greybox scenes",
            "This overwrites 00_Menu, 01_Planeacion, 02_Lanzamiento and 03_Resultados. Whatever they contain is lost. Continue?",
            "Yes, regenerate", "Cancel")) return;
        Build(true);
    }

    static void Build(bool overwrite)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("TextMeshPro is missing",
                "First import the TextMeshPro resources: Window > TextMeshPro > Import TMP Essential Resources. Then run this menu again.",
                "OK");
            return;
        }

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < Scenes.Order.Length; i++)
        {
            string sceneName = Scenes.Order[i];
            string path = Folder + "/" + sceneName + ".unity";
            if (overwrite || !File.Exists(path))
            {
                bool isLast = i == Scenes.Order.Length - 1;
                CreateScene(sceneName, path, isLast);
            }
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();

        EditorSceneManager.OpenScene(Folder + "/" + Scenes.Menu + ".unity");
        Debug.Log("Greybox scenes ready and added to the build.");
    }

    static void CreateScene(string sceneName, string path, bool isLast)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        CreateText(canvasGo.transform, "Title", sceneName, 72, new Vector2(0.5f, 0.8f), new Vector2(1400, 150));
        TextMeshProUGUI state = CreateText(canvasGo.transform, "State", "", 36, new Vector2(0.5f, 0.5f), new Vector2(1400, 300));
        state.gameObject.AddComponent<StateDisplay>();

        GameObject button = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(NextSceneButton));
        button.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(420, 110);
        button.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);
        CreateText(button.transform, "Text", isLast ? "Play again" : "Next", 44, new Vector2(0.5f, 0.5f), new Vector2(420, 110));

        EditorSceneManager.SaveScene(scene, path);
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 boxSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = boxSize;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return t;
    }
}