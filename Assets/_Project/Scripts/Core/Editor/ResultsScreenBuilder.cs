using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Unity menus for the Results screen (task 16):
//   SpaceApps > Results screen > Add to open scene
//       Puts a space background and a panel with title, score, a scroll view of collapsible
//       sections and a "Back to menu" button on the overlay Canvas of the open scene
//       (03_Resultados or your Sandbox). If there is one already, it offers to replace it. Then save the scene.
//   SpaceApps > Results screen > Test: sample game
//       Play mode only: fills the GameManager with a complete game (Artemis catalog)
//       and recalculates the screen.
public static class ResultsScreenBuilder
{
    static readonly Color BoxColor = new Color(0.04f, 0.06f, 0.12f, 0.82f);
    static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.85f);

    [MenuItem("SpaceApps/Results screen/Add to open scene")]
    static void AddToScene()
    {
        // Overlay canvas only, never an arbitrary Canvas of the scene (audit F02)
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();
        ResultsScreen old = Object.FindAnyObjectByType<ResultsScreen>(FindObjectsInactive.Include);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Results screen", "This scene already has a ResultsScreen. Replace it with the new layout?", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        // The greybox placeholders ("03_Results" title and "Play Again" button) were drawn on top of the
        // new panel. The panel has its own "Back to menu" button, so they are hidden (Ctrl+Z brings them back).
        HidePlaceholders(canvas);

        // Space background behind everything (only one)
        if (Object.FindAnyObjectByType<SpaceBackground>(FindObjectsInactive.Include) == null)
        {
            GameObject bg = new GameObject("SpaceBackground", typeof(RectTransform), typeof(RawImage), typeof(SpaceBackground));
            bg.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)bg.transform, Vector2.zero, Vector2.zero);
            bg.GetComponent<RawImage>().raycastTarget = false;
            bg.transform.SetAsFirstSibling();
            Undo.RegisterCreatedObjectUndo(bg, "Add space background");
        }

        // Tall panel in the middle, see-through so the stars show at the sides
        GameObject root = new GameObject("ResultsScreen", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400, -80); // 40 px free at the top and bottom
        rt.anchoredPosition = Vector2.zero;
        Image box = root.GetComponent<Image>();
        box.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        box.type = Image.Type.Sliced;
        box.color = BoxColor;

        TextMeshProUGUI title = CreateText(root.transform, "Title", "Results", 64);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.75f, 0.85f, 1f);
        Place(title.rectTransform, new Vector2(0, -70), new Vector2(1300, 80));

        TextMeshProUGUI score = CreateText(root.transform, "Score", "0 / 100", 96);
        score.fontStyle = FontStyles.Bold;
        Place(score.rectTransform, new Vector2(0, -170), new Vector2(1300, 110));

        // Scroll view: Viewport (mask) > Content (sections, created at runtime) + scrollbar on the right
        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(root.transform, false);
        Stretch((RectTransform)scrollGo.transform, new Vector2(40, 150), new Vector2(-30, -240));

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch((RectTransform)viewport.transform, Vector2.zero, new Vector2(-30, 0));
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // invisible, but catches the mouse wheel

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewport.transform, false);
        RectTransform content = (RectTransform)contentGo.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 24);
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        barGo.transform.SetParent(scrollGo.transform, false);
        RectTransform barRt = (RectTransform)barGo.transform;
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(1f, 0.5f);
        barRt.sizeDelta = new Vector2(14, 0);
        Image track = barGo.GetComponent<Image>();
        track.sprite = box.sprite;
        track.type = Image.Type.Sliced;
        track.color = new Color(1f, 1f, 1f, 0.08f);
        GameObject area = new GameObject("Sliding Area", typeof(RectTransform));
        area.transform.SetParent(barGo.transform, false);
        Stretch((RectTransform)area.transform, Vector2.zero, Vector2.zero);
        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(area.transform, false);
        Stretch((RectTransform)handle.transform, Vector2.zero, Vector2.zero);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = box.sprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0.55f, 0.7f, 1f, 0.7f);
        Scrollbar bar = barGo.GetComponent<Scrollbar>();
        bar.handleRect = (RectTransform)handle.transform;
        bar.targetGraphic = handleImage;
        bar.direction = Scrollbar.Direction.BottomToTop;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 10f; // mouse wheel speed
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // "Back to menu" button at the bottom
        GameObject buttonGo = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root.transform, false);
        RectTransform buttonRt = (RectTransform)buttonGo.transform;
        buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = new Vector2(0, 75);
        buttonRt.sizeDelta = new Vector2(420, 90);
        Image img = buttonGo.GetComponent<Image>();
        img.sprite = box.sprite;
        img.type = Image.Type.Sliced;
        img.color = ButtonColor;
        TextMeshProUGUI label = CreateText(buttonGo.transform, "Text", "Back to menu", 36);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        buttonGo.AddComponent<NextSceneButton>(); // after Results it resets and goes to the menu

        ResultsScreen screen = root.AddComponent<ResultsScreen>();
        screen.title = title;
        screen.score = score;
        screen.content = content;

        Undo.RegisterCreatedObjectUndo(root, "Add results screen");
        Selection.activeGameObject = root;
    }

    static void HidePlaceholders(Canvas canvas)
    {
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Titulo" && t.name != "BotonSiguiente") continue;
            Undo.RecordObject(t.gameObject, "Hide greybox placeholder");
            t.gameObject.SetActive(false);
            Debug.Log("Results screen: hid the greybox placeholder \"" + t.name + "\".");
        }
    }

    // Fills the parent, with offsets from its bottom-left (min) and top-right (max) corners
    static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    [MenuItem("SpaceApps/Results screen/Test: sample game")]
    static void FakeGame()
    {
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (mission == null)
        {
            Debug.LogError("The mission is missing: run SpaceApps -> Import Artemis catalog.");
            return;
        }
        MissionScoringTests.FillExampleGame(GameManager.Instance, mission);
        ResultsScreen screen = Object.FindAnyObjectByType<ResultsScreen>();
        if (screen != null) screen.Show();
        Debug.Log("Test: sample game loaded. Score = " + GameManager.Instance.TotalScore().ToString("0.00"));
    }

    [MenuItem("SpaceApps/Results screen/Test: failing sample game")]
    static void FailingGame()
    {
        ResultsSample.Fill(GameManager.Instance, false);
        ResultsScreen screen = Object.FindAnyObjectByType<ResultsScreen>();
        if (screen != null) screen.Show();
        Debug.Log("Test: failing sample game loaded (overloaded spacecraft, no data sent).");
    }

    [MenuItem("SpaceApps/Results screen/Test: failing sample game", true)]
    static bool FailingOnlyInPlayMode()
    {
        return Application.isPlaying;
    }

    [MenuItem("SpaceApps/Results screen/Test: sample game", true)]
    static bool OnlyInPlayMode()
    {
        return Application.isPlaying;
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

    // Position relative to the top edge of the panel
    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
