using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu: SpaceApps > Crear escenas greybox
//   Crea las escenas del loop que falten con un titulo, el estado y un boton "Siguiente",
//   y las agrega en orden a la lista de escenas del build.
// Menu: SpaceApps > Regenerar escenas greybox (sobrescribe)
//   Igual, pero vuelve a crear las 4 escenas aunque ya existan. Solo para el greybox inicial:
//   no lo uses cuando alguien ya haya trabajado en una escena.
// UI con TextMeshPro y EventSystem del Input System nuevo (reglas del equipo en AGENTS.md).
public static class GreyboxSceneCreator
{
    const string Folder = "Assets/_Project/Scenes";

    [MenuItem("SpaceApps/Crear escenas greybox")]
    static void CreateMissingScenes()
    {
        Build(false);
    }

    [MenuItem("SpaceApps/Regenerar escenas greybox (sobrescribe)")]
    static void RegenerateScenes()
    {
        if (!EditorUtility.DisplayDialog("Regenerar escenas greybox",
            "Esto sobrescribe 00_Menu, 01_Planeacion, 02_Lanzamiento y 03_Resultados. Se pierde lo que tengan. ¿Continuar?",
            "Sí, regenerar", "Cancelar")) return;
        Build(true);
    }

    static void Build(bool overwrite)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Primero importa los recursos de TextMeshPro: Window > TextMeshPro > Import TMP Essential Resources. Luego vuelve a correr este menú.",
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

        CreateText(canvasGo.transform, "Titulo", sceneName, 72, new Vector2(0.5f, 0.8f), new Vector2(1400, 150));
        TextMeshProUGUI state = CreateText(canvasGo.transform, "Estado", "", 36, new Vector2(0.5f, 0.5f), new Vector2(1400, 300));
        state.gameObject.AddComponent<StateDisplay>();

        GameObject button = new GameObject("BotonSiguiente", typeof(RectTransform), typeof(Image), typeof(Button), typeof(NextSceneButton));
        button.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.2f);
        rt.sizeDelta = new Vector2(420, 110);
        button.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);
        CreateText(button.transform, "Texto", isLast ? "Jugar de nuevo" : "Siguiente", 44, new Vector2(0.5f, 0.5f), new Vector2(420, 110));

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