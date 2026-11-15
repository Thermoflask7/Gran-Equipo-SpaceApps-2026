using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Unity menus for the mission folder (task 4):
//   SpaceApps > Mission folder > Create prefab
//       Creates Assets/_Project/Prefabs/MissionFolder.prefab: a manila folder with page 1
//       (briefing, launch checklist and mission updates) and buttons to change page.
//   SpaceApps > Mission folder > Add to open scene
//       Puts the prefab inside the Canvas of the open scene (creates a Canvas and an EventSystem if missing).
//       It is added ACTIVE so it can be tested; on the desk the station must leave it inactive
//       and call Open() when the folder is clicked (opening it marks "Mission read").
//   SpaceApps > Mission folder > Test: open folder / Test: roll planning event / Test: reset game  (Play mode only)
public static class MissionFolderBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/MissionFolder.prefab";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color FolderColor = new Color(0.82f, 0.66f, 0.4f, 1f);
    static readonly Color TabColor = new Color(0.74f, 0.58f, 0.34f, 1f);
    static readonly Color PaperColor = new Color(0.98f, 0.96f, 0.9f, 1f);
    static readonly Color InkColor = new Color(0.12f, 0.12f, 0.16f, 1f);
    static readonly Color HeadingColor = new Color(0.1f, 0.25f, 0.5f, 1f);
    static readonly Color ChecklistBack = new Color(0.1f, 0.12f, 0.16f, 1f);

    [MenuItem("SpaceApps/Mission folder/Create prefab")]
    static void CreatePrefab()
    {
        // Root: fills its parent and darkens what is behind (also blocks clicks on the desk)
        GameObject root = NewUI("MissionFolder", null, typeof(Image));
        Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.GetComponent<Image>().color = BackdropColor;
        MissionFolder folder = root.AddComponent<MissionFolder>();

        // The folder (manila)
        GameObject body = NewUI("Folder", root.transform, typeof(Image));
        Stretch((RectTransform)body.transform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);
        body.GetComponent<Image>().color = FolderColor;

        // Tab on top of the folder, with the title
        GameObject tab = NewUI("Tab", body.transform, typeof(Image));
        Stretch((RectTransform)tab.transform, new Vector2(0, 1), new Vector2(0.35f, 1), new Vector2(0, 0), new Vector2(0, 70));
        tab.GetComponent<Image>().color = TabColor;
        TextMeshProUGUI title = CreateText(tab.transform, "Title", "MISSION FOLDER", 38, InkColor);
        title.fontStyle = FontStyles.Bold;
        Stretch(title.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Button close = CreateButton(body.transform, "Close", "X", new Color(0.8f, 0.2f, 0.15f, 1f), Color.white);
        Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-15, -15), new Vector2(70, 70));
        UnityEventTools.AddPersistentListener(close.onClick, folder.Close);

        // Footer: previous page, page number, next page
        Button previous = CreateButton(body.transform, "Previous", "<", TabColor, InkColor);
        Place((RectTransform)previous.transform, new Vector2(0.4f, 0), new Vector2(0, 15), new Vector2(80, 60));
        UnityEventTools.AddPersistentListener(previous.onClick, folder.PreviousPage);
        folder.previousButton = previous;

        TextMeshProUGUI pageLabel = CreateText(body.transform, "PageLabel", "Page 1 / 1", 28, InkColor);
        Place(pageLabel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(220, 60));
        folder.pageLabel = pageLabel;

        Button next = CreateButton(body.transform, "Next", ">", TabColor, InkColor);
        Place((RectTransform)next.transform, new Vector2(0.6f, 0), new Vector2(0, 15), new Vector2(80, 60));
        UnityEventTools.AddPersistentListener(next.onClick, folder.NextPage);
        folder.nextButton = next;

        // The paper sheet. Every page goes inside "Pages" (task 5 adds its own page here)
        GameObject paper = NewUI("Pages", body.transform, typeof(Image));
        Stretch((RectTransform)paper.transform, Vector2.zero, Vector2.one, new Vector2(30, 90), new Vector2(-100, -30));
        paper.GetComponent<Image>().color = PaperColor;

        GameObject page1 = NewUI("Page 1 - Mission", paper.transform);
        Stretch((RectTransform)page1.transform, Vector2.zero, Vector2.one, new Vector2(30, 25), new Vector2(-30, -25));
        folder.pages.Add(page1);

        // Left column: briefing and checklist
        TextMeshProUGUI briefingHeading = CreateHeading(page1.transform, "BriefingHeading", "Mission briefing");
        Stretch(briefingHeading.rectTransform, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(0, -50), Vector2.zero);

        TextMeshProUGUI briefing = CreateText(page1.transform, "Briefing", "", 24, InkColor);
        briefing.alignment = TextAlignmentOptions.TopLeft;
        Stretch(briefing.rectTransform, new Vector2(0, 0.36f), new Vector2(0.55f, 1), new Vector2(0, 0), new Vector2(0, -60));
        folder.briefingText = briefing;

        GameObject checklistBox = NewUI("ChecklistBox", page1.transform, typeof(Image));
        Stretch((RectTransform)checklistBox.transform, Vector2.zero, new Vector2(0.55f, 0.34f), Vector2.zero, Vector2.zero);
        checklistBox.GetComponent<Image>().color = ChecklistBack;

        TextMeshProUGUI checklistHeading = CreateText(checklistBox.transform, "ChecklistHeading", "Before launch", 26, new Color(1f, 0.6f, 0f, 1f));
        checklistHeading.fontStyle = FontStyles.Bold;
        checklistHeading.alignment = TextAlignmentOptions.TopLeft;
        Stretch(checklistHeading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(18, -45), new Vector2(-18, -10));

        TextMeshProUGUI checklist = CreateText(checklistBox.transform, "Checklist", "", 22, Color.white);
        checklist.alignment = TextAlignmentOptions.TopLeft;
        Stretch(checklist.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 10), new Vector2(-18, -50));
        folder.checklistText = checklist;

        // Right column: mission updates (random events), in a scroll list
        TextMeshProUGUI updatesHeading = CreateHeading(page1.transform, "UpdatesHeading", "Mission updates");
        Stretch(updatesHeading.rectTransform, new Vector2(0.58f, 1), Vector2.one, new Vector2(0, -50), Vector2.zero);

        GameObject scroll = NewUI("Updates", page1.transform, typeof(Image), typeof(ScrollRect));
        Stretch((RectTransform)scroll.transform, new Vector2(0.58f, 0), Vector2.one, Vector2.zero, new Vector2(0, -60));
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);

        GameObject viewport = NewUI("Viewport", scroll.transform, typeof(Image), typeof(RectMask2D));
        Stretch((RectTransform)viewport.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f); // almost invisible, but catches the mouse wheel

        TextMeshProUGUI updates = CreateText(viewport.transform, "Content", "", 22, InkColor);
        updates.alignment = TextAlignmentOptions.TopLeft;
        updates.margin = new Vector4(15, 10, 15, 10);
        RectTransform updatesRt = updates.rectTransform;
        updatesRt.anchorMin = new Vector2(0, 1);
        updatesRt.anchorMax = Vector2.one;
        updatesRt.pivot = new Vector2(0.5f, 1);
        updatesRt.sizeDelta = Vector2.zero;
        updates.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        folder.updatesText = updates;

        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.content = updatesRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30;

        // Data
        folder.mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (folder.mission == null)
            Debug.LogWarning("Mission folder: mission not found at " + MissionPath + ". Run SpaceApps > Import Artemis catalog.");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath);
    }

    [MenuItem("SpaceApps/Mission folder/Add to open scene")]
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
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        }
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add mission folder");
        Selection.activeGameObject = instance;
    }

    // ---------- Play mode tests ----------

    [MenuItem("SpaceApps/Mission folder/Test: open folder")]
    static void TestOpen()
    {
        MissionFolder folder = FindFolder();
        if (folder != null) folder.Open();
    }

    [MenuItem("SpaceApps/Mission folder/Test: roll planning event")]
    static void TestRoll()
    {
        MissionFolder folder = FindFolder();
        if (folder == null) return;
        EventData e = folder.RollPlanningUpdate();
        Debug.Log(e != null ? "Planning event: " + e.eventName : "No planning event this time (or none left).");
    }

    [MenuItem("SpaceApps/Mission folder/Test: reset game")]
    static void TestReset()
    {
        GameManager.Instance.ResetGame();
        MissionFolder folder = FindFolder();
        if (folder != null) folder.Refresh();
    }

    [MenuItem("SpaceApps/Mission folder/Test: open folder", true)]
    [MenuItem("SpaceApps/Mission folder/Test: roll planning event", true)]
    [MenuItem("SpaceApps/Mission folder/Test: reset game", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
    }

    // Also finds the folder when it is closed (inactive)
    static MissionFolder FindFolder()
    {
        MissionFolder folder = Object.FindAnyObjectByType<MissionFolder>(FindObjectsInactive.Include);
        if (folder == null) Debug.LogWarning("Mission folder: there is no MissionFolder in the scene. Use SpaceApps > Mission folder > Add to open scene.");
        return folder;
    }

    // ---------- UI helpers ----------

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
        t.raycastTarget = false; // so texts do not block clicks or the mouse wheel
        return t;
    }

    static TextMeshProUGUI CreateHeading(Transform parent, string name, string text)
    {
        TextMeshProUGUI t = CreateText(parent, name, text, 32, HeadingColor);
        t.fontStyle = FontStyles.Bold | FontStyles.Underline;
        t.alignment = TextAlignmentOptions.TopLeft;
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
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); // faded on the first and last page
        button.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, 32, textColor);
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

    // Fixed-size element anchored (and pivoted) at one point of its parent
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
