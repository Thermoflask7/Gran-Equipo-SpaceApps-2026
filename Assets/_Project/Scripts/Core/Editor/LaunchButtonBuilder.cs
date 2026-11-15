using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Unity menus for the launch button (task 3):
//   SpaceApps > Launch button > Create prefab
//       Creates Assets/_Project/Prefabs/BotonLanzamiento.prefab (checklist + button). It can be run again.
//       (The prefab file keeps its Spanish name until the renaming at the end, task 23.)
//   SpaceApps > Launch button > Add to open scene
//       Puts the prefab inside the Canvas of the open scene (to test in your Sandbox or in 01_Planeacion).
//   SpaceApps > Launch button > Test: complete the 4 tasks / Test: reset tasks
//       Play mode only: fills the GameManager with fake data to test the button without the other stations.
public static class LaunchButtonBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/BotonLanzamiento.prefab";
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f, 0.92f);
    static readonly Color RedColor = new Color(0.85f, 0.2f, 0.15f);

    [MenuItem("SpaceApps/Launch button/Create prefab")]
    static void CreatePrefab()
    {
        // Background box, anchored to the bottom right
        GameObject root = new GameObject("LaunchButton", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40, 40);
        rt.sizeDelta = new Vector2(760, 470);
        root.GetComponent<Image>().color = BoxColor;

        TextMeshProUGUI title = CreateText(root.transform, "Title", "Before launch", 40);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0, -45), new Vector2(700, 60));

        TextMeshProUGUI list = CreateText(root.transform, "Checklist", "", 26);
        list.alignment = TextAlignmentOptions.TopLeft;
        Place(list.rectTransform, new Vector2(0, -205), new Vector2(700, 230));
        list.gameObject.AddComponent<LaunchChecklist>();

        // Red button
        GameObject buttonGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root.transform, false);
        Place((RectTransform)buttonGo.transform, new Vector2(0, -395), new Vector2(420, 100));
        Image img = buttonGo.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = RedColor;
        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f); // gray when locked
        button.colors = colors;

        TextMeshProUGUI label = CreateText(buttonGo.transform, "Text", "Locked (0/4)", 42);
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

        LaunchButton launch = buttonGo.AddComponent<LaunchButton>();
        launch.label = label;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    [MenuItem("SpaceApps/Launch button/Add to open scene")]
    static void AddToScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            CreatePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }
        // Overlay canvas only: never the PC monitor canvas (PC_Desktop) of 01_Planeacion (audit F02)
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add launch button");
        Selection.activeGameObject = instance;
    }

    [MenuItem("SpaceApps/Launch button/Test: complete the 4 tasks")]
    static void FakeAllTasks()
    {
        GameManager gm = GameManager.Instance;
        gm.missionRead = true;

        gm.pilot = FakeAstronaut("Test pilot");
        gm.engineer = FakeAstronaut("Test engineer");
        gm.scientist = FakeAstronaut("Test scientist");
        gm.pilotTraining = TrainingFocus.Piloting;
        gm.engineerTraining = TrainingFocus.Engineering;
        gm.scientistTraining = TrainingFocus.Research;

        gm.rocketValid = true;

        LaunchSiteData site = ScriptableObject.CreateInstance<LaunchSiteData>();
        site.siteName = "Test site";
        gm.launchSite = site;
        gm.launchDay = 0;
        gm.weatherRisk = 0.2f;
        Debug.Log("Test: all 4 tasks complete, the button should become active.");
    }

    [MenuItem("SpaceApps/Launch button/Test: reset tasks")]
    static void ResetTasks()
    {
        GameManager.Instance.ResetGame();
    }

    [MenuItem("SpaceApps/Launch button/Test: complete the 4 tasks", true)]
    [MenuItem("SpaceApps/Launch button/Test: reset tasks", true)]
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

    // Position relative to the top edge of the box
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
