using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Unity menus for the standalone SpaceMart prefab (task 8, tests only).
// The live in-game front-end is SpaceMartApp on the desk PC (SpaceApps > Build desk PC).
// Do not add this prefab to 01_Planeacion: that would be a second shop UI.
//   SpaceApps > SpaceMart > Create prefab
//       Creates Assets/_Project/Prefabs/SpaceMart.prefab with every part in Data/Piezas
//       and the lunar mission, with category tabs on top of the list.
//       It can be run again (for example after importing the catalog).
//   SpaceApps > SpaceMart > Add to open scene
//       Puts the prefab inside the Canvas of the open scene (creates a Canvas and an EventSystem if missing).
//       Blocked in 01_Planeacion and in any scene that already has SpaceMartApp.
//   SpaceApps > SpaceMart > Test: reset game
//       Play mode only: gives back the starting credits and empties the cart.
public static class SpaceMartBuilder
{
    const string PrefabPath = "Assets/_Project/Prefabs/SpaceMart.prefab";
    const string PartsFolder = "Assets/_Project/Data/Piezas";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static readonly Color BackColor = new Color(0.92f, 0.93f, 0.95f, 1f);
    static readonly Color HeaderColor = new Color(0.07f, 0.1f, 0.13f, 1f);
    static readonly Color OrangeColor = new Color(1f, 0.6f, 0f, 1f);
    static readonly Color RowColor = new Color(0.12f, 0.15f, 0.2f, 1f);
    static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.16f, 1f);
    static readonly Color BuyColor = new Color(1f, 0.85f, 0.08f, 1f);
    static readonly Color GrayText = new Color(0.75f, 0.78f, 0.82f, 1f);

    [MenuItem("SpaceApps/SpaceMart/Create prefab")]
    static void CreatePrefab()
    {
        // Root: fills its parent (the computer screen or the Canvas)
        GameObject root = NewUI("SpaceMart", null, typeof(Image));
        Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.GetComponent<Image>().color = BackColor;
        SpaceMart shop = root.AddComponent<SpaceMart>();

        // Header: title, credits and close button
        GameObject header = NewUI("Header", root.transform, typeof(Image));
        Stretch((RectTransform)header.transform, new Vector2(0, 1), Vector2.one, new Vector2(0, -90), Vector2.zero);
        header.GetComponent<Image>().color = HeaderColor;

        TextMeshProUGUI title = CreateText(header.transform, "Title", "SpaceMart", 48, OrangeColor);
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        Stretch(title.rectTransform, Vector2.zero, new Vector2(0.5f, 1), new Vector2(30, 0), Vector2.zero);

        TextMeshProUGUI money = CreateText(header.transform, "Money", "Credits: 0 cr", 34, Color.white);
        money.alignment = TextAlignmentOptions.Right;
        Stretch(money.rectTransform, new Vector2(0.5f, 0), Vector2.one, Vector2.zero, new Vector2(-110, 0));
        shop.moneyText = money;

        Button close = CreateButton(header.transform, "Close", "X", new Color(0.8f, 0.2f, 0.15f, 1f), Color.white);
        RectTransform closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1, 0.5f);
        closeRt.anchoredPosition = new Vector2(-15, 0);
        closeRt.sizeDelta = new Vector2(70, 70);
        UnityEventTools.AddPersistentListener(close.onClick, shop.Close);

        // Category tabs above the list (each one calls SpaceMart.SetFilter with its name)
        GameObject tabs = NewUI("CategoryTabs", root.transform, typeof(HorizontalLayoutGroup));
        Stretch((RectTransform)tabs.transform, new Vector2(0, 1), new Vector2(0.62f, 1), new Vector2(20, -165), new Vector2(-10, -105));
        HorizontalLayoutGroup tabLayout = tabs.GetComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 8;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;
        foreach (string filter in SpaceMart.Filters)
        {
            Button tab = CreateButton(tabs.transform, "Tab " + filter, filter, Color.white, Color.white);
            tab.GetComponentInChildren<TextMeshProUGUI>().fontSize = 22;
            UnityEventTools.AddStringPersistentListener(tab.onClick, shop.SetFilter, filter);
            shop.filterTabs.Add(tab.GetComponent<Image>()); // SpaceMart colors the selected one
        }

        // Left: scroll list with the parts
        GameObject scroll = NewUI("PartList", root.transform, typeof(Image), typeof(ScrollRect));
        Stretch((RectTransform)scroll.transform, Vector2.zero, new Vector2(0.62f, 1), new Vector2(20, 20), new Vector2(-10, -175));
        scroll.GetComponent<Image>().color = new Color(0, 0, 0, 0.08f);

        GameObject viewport = NewUI("Viewport", scroll.transform, typeof(Image), typeof(RectMask2D));
        Stretch((RectTransform)viewport.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // almost invisible, but catches the mouse wheel

        GameObject content = NewUI("Content", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRt = (RectTransform)content.transform;
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = Vector2.one;
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = (RectTransform)viewport.transform;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30;
        shop.listContent = content.transform;
        shop.rowTemplate = CreateRowTemplate(content.transform);

        // Right: cart summary with RocketPhysics, and the last message
        GameObject panel = NewUI("Cart", root.transform, typeof(Image));
        Stretch((RectTransform)panel.transform, new Vector2(0.62f, 0), Vector2.one, new Vector2(10, 20), new Vector2(-20, -110));
        panel.GetComponent<Image>().color = PanelColor;

        TextMeshProUGUI stats = CreateText(panel.transform, "Stats", "", 26, Color.white);
        stats.alignment = TextAlignmentOptions.TopLeft;
        Stretch(stats.rectTransform, new Vector2(0, 0.2f), Vector2.one, new Vector2(25, 0), new Vector2(-25, -25));
        shop.statsText = stats;

        TextMeshProUGUI feedback = CreateText(panel.transform, "Feedback", "", 24, BuyColor);
        feedback.alignment = TextAlignmentOptions.BottomLeft;
        Stretch(feedback.rectTransform, Vector2.zero, new Vector2(1, 0.2f), new Vector2(25, 25), new Vector2(-25, 0));
        shop.feedbackText = feedback;

        // Data
        shop.catalog = LoadCatalog();
        shop.mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (shop.catalog.Count == 0)
            Debug.LogWarning("SpaceMart: no parts in " + PartsFolder + ". Run SpaceApps > Import mission catalog first.");
        if (shop.mission == null)
            Debug.LogWarning("SpaceMart: mission not found at " + MissionPath + ".");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Prefab created: " + PrefabPath + " (" + shop.catalog.Count + " parts)");
    }

    // Inactive row that SpaceMart clones for each part
    static SpaceMartItem CreateRowTemplate(Transform parent)
    {
        GameObject row = NewUI("RowTemplate", parent, typeof(Image));
        ((RectTransform)row.transform).sizeDelta = new Vector2(0, 130);
        row.GetComponent<Image>().color = RowColor;
        SpaceMartItem item = row.AddComponent<SpaceMartItem>();

        TextMeshProUGUI name = CreateText(row.transform, "Name", "Part", 28, Color.white);
        name.fontStyle = FontStyles.Bold;
        name.alignment = TextAlignmentOptions.TopLeft;
        name.overflowMode = TextOverflowModes.Truncate;
        Stretch(name.rectTransform, new Vector2(0, 0.62f), Vector2.one, new Vector2(18, 0), new Vector2(-270, -10));
        item.nameText = name;

        TextMeshProUGUI info = CreateText(row.transform, "Info", "", 20, GrayText);
        info.alignment = TextAlignmentOptions.TopLeft;
        info.overflowMode = TextOverflowModes.Truncate;
        Stretch(info.rectTransform, Vector2.zero, new Vector2(1, 0.62f), new Vector2(18, 8), new Vector2(-270, 0));
        item.infoText = info;

        TextMeshProUGUI price = CreateText(row.transform, "Price", "0 cr", 26, OrangeColor);
        price.fontStyle = FontStyles.Bold;
        price.alignment = TextAlignmentOptions.Right;
        Stretch(price.rectTransform, new Vector2(1, 0), Vector2.one, new Vector2(-260, 0), new Vector2(-150, 0));
        item.priceText = price;

        Button button = CreateButton(row.transform, "Button", "Buy", BuyColor, Color.black);
        RectTransform buttonRt = (RectTransform)button.transform;
        buttonRt.anchorMin = buttonRt.anchorMax = buttonRt.pivot = new Vector2(1, 0.5f);
        buttonRt.anchoredPosition = new Vector2(-15, 0);
        buttonRt.sizeDelta = new Vector2(120, 60);
        item.button = button;
        item.buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>();

        row.SetActive(false);
        return item;
    }

    static List<PartData> LoadCatalog()
    {
        List<PartData> parts = new List<PartData>();
        string[] guids = AssetDatabase.FindAssets("t:PartData", new[] { PartsFolder });
        foreach (string guid in guids)
        {
            PartData part = AssetDatabase.LoadAssetAtPath<PartData>(AssetDatabase.GUIDToAssetPath(guid));
            if (part != null) parts.Add(part);
        }
        return parts;
    }

    [MenuItem("SpaceApps/SpaceMart/Add to open scene")]
    static void AddToScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.path.Replace("\\", "/").EndsWith("01_Planeacion.unity"))
        {
            EditorUtility.DisplayDialog("SpaceMart",
                "The in-monitor SpaceOS window (SpaceMartApp) is the shop front-end in 01_Planeacion. The standalone prefab is only for tests in other scenes.",
                "OK");
            return;
        }
        if (Object.FindAnyObjectByType<SpaceMartApp>() != null)
        {
            EditorUtility.DisplayDialog("SpaceMart",
                "This scene already has SpaceMartApp, the in-monitor SpaceOS front-end. The standalone prefab was not added.",
                "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            CreatePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // Overlay canvas only, never an arbitrary Canvas of the scene (audit F02)
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add SpaceMart");
        Selection.activeGameObject = instance;
    }

    [MenuItem("SpaceApps/SpaceMart/Test: reset game")]
    static void ResetGame()
    {
        GameManager.Instance.ResetGame();
        SpaceMart shop = Object.FindAnyObjectByType<SpaceMart>();
        if (shop != null) shop.Refresh();
    }

    [MenuItem("SpaceApps/SpaceMart/Test: reset game", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
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
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 1f); // gray when you cannot afford it
        button.colors = colors;

        TextMeshProUGUI label = CreateText(go.transform, "Text", text, 28, textColor);
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
}
