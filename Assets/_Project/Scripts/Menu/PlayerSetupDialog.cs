using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// "Who is playing?" popup (task 37b, local leaderboard for 1 to 4 players). Signed: Grok.
// Opened by MissionSelect's "Start mission". Built at runtime on its own overlay canvas, so 00_Menu needs no changes.
// The players pick how many they are (1 to 4) and type their names (empty = "Player N"); "Start mission"
// starts GameManager.session, resets the game and loads planning. The names are remembered for next time.
public class PlayerSetupDialog : MonoBehaviour
{
    public const string CanvasName = "PlayerSetupCanvas";
    const string LastNamesKey = "SpaceApps.LastPlayerNames";

    static readonly Color BoxColor = new Color(0.1f, 0.12f, 0.16f, 0.97f);
    static readonly Color CountOff = new Color(0.22f, 0.26f, 0.32f);
    static readonly Color CountOn = new Color(0.22f, 0.4f, 0.75f);
    static readonly Color StartColor = new Color(0.2f, 0.6f, 0.3f);
    static readonly Color BackColor = new Color(0.3f, 0.34f, 0.4f);

    static PlayerSetupDialog instance;

    GameObject panel;
    DestinationData mission;
    int count = 1;
    readonly List<Image> countButtons = new List<Image>();
    readonly List<GameObject> rows = new List<GameObject>();
    readonly List<TMP_InputField> inputs = new List<TMP_InputField>();

    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

    public static void Open(DestinationData mission)
    {
        if (mission == null || mission.locked) return;
        if (instance == null) instance = Build();
        instance.mission = mission;
        instance.LoadNames();
        instance.panel.SetActive(true);
        if (instance.inputs.Count > 0) instance.inputs[0].Select();
    }

    public static void Close()
    {
        if (instance != null && instance.panel != null) instance.panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void SetCount(int n)
    {
        count = Mathf.Clamp(n, 1, PlayerSession.MaxPlayers);
        for (int i = 0; i < countButtons.Count; i++) countButtons[i].color = i + 1 == count ? CountOn : CountOff;
        for (int i = 0; i < rows.Count; i++) rows[i].SetActive(i < count);
    }

    void StartMission()
    {
        if (mission == null) return;
        List<string> names = new List<string>();
        for (int i = 0; i < count; i++) names.Add(inputs[i].text);
        SaveNames();

        GameManager gm = GameManager.Instance;
        gm.session.Start(names);
        gm.ResetGame();
        gm.mission = mission;
        Close();
        gm.GoToScene(Scenes.Planning);
    }

    // Last names typed (one per line) and how many played, so the same group does not retype them
    void LoadNames()
    {
        string[] saved = PlayerPrefs.GetString(LastNamesKey, "").Split('\n');
        int savedCount = 1;
        if (saved.Length > 0 && int.TryParse(saved[0], out int c)) savedCount = c;
        for (int i = 0; i < inputs.Count; i++) inputs[i].text = i + 1 < saved.Length ? saved[i + 1] : "";
        SetCount(savedCount);
    }

    void SaveNames()
    {
        List<string> parts = new List<string> { count.ToString() };
        foreach (TMP_InputField input in inputs) parts.Add(input.text.Replace("\n", " ").Trim());
        PlayerPrefs.SetString(LastNamesKey, string.Join("\n", parts));
        PlayerPrefs.Save();
    }

    // ---------- UI ----------

    static PlayerSetupDialog Build()
    {
        var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the menu panels
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var dialog = canvasGo.AddComponent<PlayerSetupDialog>();

        // Full-screen dim that also blocks clicks on the menu behind
        RectTransform dim = NewRect("Dim", canvasGo.transform);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        dialog.panel = dim.gameObject;

        RectTransform box = NewRect("Box", dim);
        box.sizeDelta = new Vector2(780f, 660f);
        box.gameObject.AddComponent<Image>().color = BoxColor;

        TextMeshProUGUI title = AddText(box, "Title", "Who is playing?", 44f, new Vector2(0f, 270f), new Vector2(700f, 64f));
        title.fontStyle = FontStyles.Bold;
        AddText(box, "Hint", "Take turns on this computer: each player plays the mission once and the best scores go to the leaderboard.",
            24f, new Vector2(0f, 205f), new Vector2(700f, 70f)).color = new Color(0.75f, 0.82f, 0.92f);

        // Number of players
        AddText(box, "CountLabel", "Players", 30f, new Vector2(-250f, 130f), new Vector2(180f, 60f));
        for (int i = 0; i < PlayerSession.MaxPlayers; i++)
        {
            int n = i + 1;
            Image b = AddButton(box, "Count" + n, n.ToString(), new Vector2(-100f + i * 110f, 130f), new Vector2(90f, 60f), CountOff,
                () => dialog.SetCount(n));
            dialog.countButtons.Add(b);
        }

        // One name row per player
        for (int i = 0; i < PlayerSession.MaxPlayers; i++)
        {
            RectTransform row = NewRect("Row" + (i + 1), box);
            row.anchoredPosition = new Vector2(0f, 45f - i * 75f);
            row.sizeDelta = new Vector2(680f, 60f);
            AddText(row, "Label", "Player " + (i + 1), 28f, new Vector2(-250f, 0f), new Vector2(180f, 60f));
            dialog.inputs.Add(AddInput(row, "Player " + (i + 1), new Vector2(90f, 0f), new Vector2(460f, 56f)));
            dialog.rows.Add(row.gameObject);
        }

        AddButton(box, "BackButton", "Back", new Vector2(-150f, -270f), new Vector2(240f, 64f), BackColor, Close);
        AddButton(box, "StartButton", "Start mission", new Vector2(150f, -270f), new Vector2(240f, 64f), StartColor, dialog.StartMission);

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
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Image AddButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, Color color, UnityAction onClick)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        AddText(rt, "Label", label, 30f, Vector2.zero, size);
        return image;
    }

    static TMP_InputField AddInput(RectTransform parent, string placeholder, Vector2 position, Vector2 size)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = "NameInput";
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.92f, 0.94f, 0.97f);

        TMP_InputField input = go.GetComponent<TMP_InputField>();
        input.characterLimit = PlayerSession.MaxNameLength;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.pointSize = 28f;
        if (input.placeholder is TMP_Text ph) ph.text = placeholder;
        if (input.textComponent != null) input.textComponent.color = new Color(0.08f, 0.1f, 0.14f);
        return input;
    }
}