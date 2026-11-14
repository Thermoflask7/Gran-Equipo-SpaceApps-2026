using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu: SpaceApps > Crear escenas greybox
// Crea las 7 escenas del loop (si no existen) con un titulo, el estado y un boton "Siguiente",
// y las agrega en orden a la lista de escenas del build. No sobrescribe escenas que ya existan.
public static class GreyboxSceneCreator
{
    const string Folder = "Assets/_Project/Scenes";

    [MenuItem("SpaceApps/Crear escenas greybox")]
    static void CreateScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < Scenes.Order.Length; i++)
        {
            string sceneName = Scenes.Order[i];
            string path = Folder + "/" + sceneName + ".unity";
            if (!File.Exists(path))
            {
                bool isLast = i == Scenes.Order.Length - 1;
                CreateScene(sceneName, path, isLast);
            }
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();

        EditorSceneManager.OpenScene(Folder + "/" + Scenes.Menu + ".unity");
        Debug.Log("Escenas greybox listas y agregadas al build.");
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

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateText(canvasGo.transform, "Titulo", sceneName, font, 72, new Vector2(0.5f, 0.8f), new Vector2(1400, 150));
        Text state = CreateText(canvasGo.transform, "Estado", "", font, 36, new Vector2(0.5f, 0.5f), new Vector2(1400, 300));
        state.gameObject.AddComponent<StateDisplay>();

        GameObject button = new GameObject("BotonSiguiente", typeof(RectTransform), typeof(Image), typeof(Button), typeof(NextSceneButton));
        button.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(420, 110);
        button.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);
        Text label = CreateText(button.transform, "Texto", isLast ? "Jugar de nuevo" : "Siguiente", font, 44, new Vector2(0.5f, 0.5f), new Vector2(420, 110));
        label.color = Color.white;

        EditorSceneManager.SaveScene(scene, path);
    }

    static Text CreateText(Transform parent, string name, string content, Font font, int size, Vector2 anchor, Vector2 boxSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = boxSize;
        Text t = go.GetComponent<Text>();
        t.text = content;
        t.font = font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }
}
