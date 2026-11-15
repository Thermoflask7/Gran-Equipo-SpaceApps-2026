using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Unity menus for the Results screen (task 16):
//   SpaceApps > Results screen > Add to open scene
//       Puts a panel with title, score, breakdown and a "Back to menu" button on the Canvas
//       of the open scene (03_Resultados or your Sandbox). Then save the scene.
//   SpaceApps > Results screen > Test: sample game
//       Play mode only: fills the GameManager with a complete game (Artemis catalog)
//       and recalculates the screen.
public static class ResultsScreenBuilder
{
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f, 0.95f);
    static readonly Color ButtonColor = new Color(0.2f, 0.45f, 0.85f);

    [MenuItem("SpaceApps/Results screen/Add to open scene")]
    static void AddToScene()
    {
        // Overlay canvas only, never an arbitrary Canvas of the scene (audit F02)
        Canvas canvas = OverlayCanvasUtil.GetOrCreate();
        if (Object.FindAnyObjectByType<ResultsScreen>(FindObjectsInactive.Include) != null
            && !EditorUtility.DisplayDialog("Results screen", "This scene already has a ResultsScreen. Add another one?", "Add", "Cancel"))
            return;

        // Centered panel
        GameObject root = new GameObject("ResultsScreen", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1500, 950);
        root.GetComponent<Image>().color = BoxColor;

        TextMeshProUGUI title = CreateText(root.transform, "Title", "Results", 60);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0, -60), new Vector2(1400, 80));

        TextMeshProUGUI score = CreateText(root.transform, "Score", "0 / 100", 90);
        score.fontStyle = FontStyles.Bold;
        Place(score.rectTransform, new Vector2(0, -160), new Vector2(1400, 110));

        TextMeshProUGUI report = CreateText(root.transform, "Breakdown", "", 26);
        report.alignment = TextAlignmentOptions.TopLeft;
        report.textWrappingMode = TextWrappingModes.Normal;
        report.overflowMode = TextOverflowModes.Truncate;
        report.enableAutoSizing = true; // task 35: long failure reports shrink to fit
        report.fontSizeMin = 14;
        report.fontSizeMax = 26;
        Place(report.rectTransform, new Vector2(0, -520), new Vector2(1400, 560));

        GameObject buttonGo = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(root.transform, false);
        Place((RectTransform)buttonGo.transform, new Vector2(0, -875), new Vector2(420, 90));
        Image img = buttonGo.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
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
        screen.report = report;

        Undo.RegisterCreatedObjectUndo(root, "Add results screen");
        Selection.activeGameObject = root;
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
