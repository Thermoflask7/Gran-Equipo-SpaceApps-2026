using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// "Abandon mission?" popup opened by the Shut down app on the desk PC.
/// Built at runtime on its own overlay canvas, so it needs no scene setup.
/// "Quit to menu" resets the game state and loads the main menu; "Stay" just closes it.
/// </summary>
public class QuitToMenuDialog : MonoBehaviour
{
    public const string CanvasName = "QuitDialogCanvas";

    static QuitToMenuDialog instance;
    GameObject panel;

    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

    public static void Open()
    {
        if (instance == null) instance = Build();
        instance.panel.SetActive(true);
    }

    public static void Close()
    {
        if (instance != null && instance.panel != null) instance.panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void QuitToMenu()
    {
        Close();
        GameManager gm = GameManager.Instance;
        gm.ResetGame();
        gm.GoToScene(Scenes.Menu);
    }

    static QuitToMenuDialog Build()
    {
        var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the launch panel (50) and everything else
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var dialog = canvasGo.AddComponent<QuitToMenuDialog>();

        // Full-screen dim that also blocks clicks on whatever is behind
        RectTransform dim = NewRect("Dim", canvasGo.transform);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        dialog.panel = dim.gameObject;

        RectTransform box = NewRect("Box", dim);
        box.sizeDelta = new Vector2(680f, 340f);
        box.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.97f);

        TextMeshProUGUI title = AddText(box, "Title", "Abandon mission?", 44f, new Vector2(0f, 105f), new Vector2(620f, 70f));
        title.fontStyle = FontStyles.Bold;
        AddText(box, "Body", "Your planning progress will be lost and you will return to the main menu.",
            28f, new Vector2(0f, 15f), new Vector2(600f, 100f));

        AddButton(box, "StayButton", "Stay", new Vector2(-140f, -110f), new Color(0.3f, 0.34f, 0.4f), Close);
        AddButton(box, "QuitButton", "Quit to menu", new Vector2(140f, -110f), new Color(0.8f, 0.2f, 0.18f), dialog.QuitToMenu);

        dialog.panel.SetActive(false);
        return dialog;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI AddText(RectTransform parent, string name, string text, float size, Vector2 position, Vector2 boxSize)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchoredPosition = position;
        rt.sizeDelta = boxSize;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void AddButton(RectTransform parent, string name, string label, Vector2 position, Color color, UnityAction onClick)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(240f, 64f);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        AddText(rt, "Label", label, 30f, Vector2.zero, rt.sizeDelta);
    }
}
