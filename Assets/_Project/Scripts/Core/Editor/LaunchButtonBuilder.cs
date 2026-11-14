using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Menus de Unity para el boton de lanzamiento (tarea 3):
//   SpaceApps > Boton de lanzamiento > Crear prefab
//       Crea Assets/_Project/Prefabs/BotonLanzamiento.prefab (checklist + boton). Se puede volver a correr.
//   SpaceApps > Boton de lanzamiento > Agregar a la escena abierta
//       Pone el prefab dentro del Canvas de la escena abierta (para probar en tu Sandbox o en 01_Planeacion).
//   SpaceApps > Boton de lanzamiento > Prueba: completar las 4 tareas / Reiniciar
//       Solo en Play: llena el GameManager con datos falsos para probar el boton sin las otras estaciones.
public static class LaunchButtonBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/BotonLanzamiento.prefab";
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f, 0.92f);
    static readonly Color RedColor = new Color(0.85f, 0.2f, 0.15f);

    [MenuItem("SpaceApps/Boton de lanzamiento/Crear prefab")]
    static void CreatePrefab()
    {
        // Caja de fondo, anclada abajo a la derecha
        GameObject root = new GameObject("BotonLanzamiento", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40, 40);
        rt.sizeDelta = new Vector2(760, 470);
        root.GetComponent<Image>().color = BoxColor;

        TextMeshProUGUI title = CreateText(root.transform, "Titulo", "Antes de lanzar", 40);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0, -45), new Vector2(700, 60));

        TextMeshProUGUI list = CreateText(root.transform, "Checklist", "", 26);
        list.alignment = TextAlignmentOptions.TopLeft;
        Place(list.rectTransform, new Vector2(0, -205), new Vector2(700, 230));
        list.gameObject.AddComponent<LaunchChecklist>();

        // Boton rojo
        GameObject buttonGo = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root.transform, false);
        Place((RectTransform)buttonGo.transform, new Vector2(0, -395), new Vector2(420, 100));
        Image img = buttonGo.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = RedColor;
        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f); // gris cuando esta bloqueado
        button.colors = colors;

        TextMeshProUGUI label = CreateText(buttonGo.transform, "Texto", "Bloqueado (0/4)", 42);
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

        LaunchButton launch = buttonGo.AddComponent<LaunchButton>();
        launch.label = label;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab creado: " + PrefabPath);
    }

    [MenuItem("SpaceApps/Boton de lanzamiento/Agregar a la escena abierta")]
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
            EditorUtility.DisplayDialog("Boton de lanzamiento", "La escena abierta no tiene Canvas.", "OK");
            return;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Agregar boton de lanzamiento");
        Selection.activeGameObject = instance;
    }

    [MenuItem("SpaceApps/Boton de lanzamiento/Prueba: completar las 4 tareas")]
    static void FakeAllTasks()
    {
        GameManager gm = GameManager.Instance;
        gm.missionRead = true;

        gm.pilot = FakeAstronaut("Piloto de prueba");
        gm.engineer = FakeAstronaut("Ingeniera de prueba");
        gm.scientist = FakeAstronaut("Científica de prueba");
        gm.pilotTraining = TrainingFocus.Piloting;
        gm.engineerTraining = TrainingFocus.Engineering;
        gm.scientistTraining = TrainingFocus.Research;

        gm.rocketValid = true;

        LaunchSiteData site = ScriptableObject.CreateInstance<LaunchSiteData>();
        site.siteName = "Sitio de prueba";
        gm.launchSite = site;
        gm.launchDay = 0;
        gm.weatherRisk = 0.2f;
        Debug.Log("Prueba: las 4 tareas completas, el boton debe activarse.");
    }

    [MenuItem("SpaceApps/Boton de lanzamiento/Prueba: reiniciar tareas")]
    static void ResetTasks()
    {
        GameManager.Instance.ResetGame();
    }

    [MenuItem("SpaceApps/Boton de lanzamiento/Prueba: completar las 4 tareas", true)]
    [MenuItem("SpaceApps/Boton de lanzamiento/Prueba: reiniciar tareas", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
    }

    static AstronautData FakeAstronaut(string name)
    {
        AstronautData a = ScriptableObject.CreateInstance<AstronautData>();
        a.astronautName = name;
        return a;
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
        return t;
    }

    // Posicion relativa al borde de arriba de la caja
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
