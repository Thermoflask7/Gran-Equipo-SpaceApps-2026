using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Arcade-style leaderboard popup (task 37). Signed: Grok.
// Opened by the "Leaderboard" button on 03_Resultados. If the game's score makes the top 10, it first asks
// the player's name (remembered for next time) and saves the score; then it shows the top 10 with the new
// entry highlighted. The main menu opens only the top 10 (OpenBoard, with a Clear button).
// Built at runtime on its own overlay canvas, so no scene needs changes.
public class LeaderboardDialog : MonoBehaviour
{
    const string LastNameKey = "SpaceApps.LastPlayerName";

    static readonly Color BoxColor = new Color(0.1f, 0.12f, 0.16f, 0.97f);
    static readonly Color SaveColor = new Color(0.2f, 0.6f, 0.3f);
    static readonly Color GrayColor = new Color(0.3f, 0.34f, 0.4f);
    static readonly Color NoteColor = new Color(0.75f, 0.82f, 0.92f);

    static LeaderboardDialog instance;

    GameObject panel;
    GameObject nameStep;
    GameObject boardStep;
    TMP_Text nameInfo;
    TMP_InputField input;
    TMP_Text boardNote;
    TMP_Text boardList;
    GameObject clearButton;
    TMP_Text clearLabel;
    bool clearArmed;

    LeaderboardEntry pending;
    Action<int> onSaved;

    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

    // pending: this game's score not saved yet (null = only show the board).
    // highlightRank: 0-based rank to highlight on the board (-1 = none).
    // onSaved: called with the saved rank (-1 if the score did not make the top 10), once the score is handled.
    public static void Open(LeaderboardEntry pending, int highlightRank, Action<int> onSaved)
    {
        if (instance == null) instance = Build();
        instance.pending = pending;
        instance.onSaved = onSaved;
        instance.panel.SetActive(true);
        instance.clearButton.SetActive(false);

        if (pending == null)
        {
            instance.ShowBoard(highlightRank, highlightRank >= 0 ? "You are #" + (highlightRank + 1) + "!" : "");
            return;
        }

        int rank = Leaderboard.RankFor(pending);
        if (rank < 0)
        {
            instance.pending = null;
            onSaved?.Invoke(-1);
            instance.ShowBoard(-1, "Your score (" + pending.competitive.ToString("0.0") + ") did not make the top "
                                   + Leaderboard.MaxEntries + ". Try again!");
            return;
        }
        instance.AskName(rank);
    }

    // Main menu (task 37d): only the top 10, with a "Clear" button (asks to confirm with a second click)
    public static void OpenBoard()
    {
        Open(null, -1, null);
        instance.clearButton.SetActive(true);
    }

    void ClearClicked()
    {
        if (!clearArmed)
        {
            clearArmed = true;
            clearLabel.text = "Sure? Click again";
            return;
        }
        Leaderboard.Clear();
        ShowBoard(-1, "Leaderboard cleared.");
    }

    public static void Close()
    {
        if (instance != null && instance.panel != null) instance.panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void AskName(int rank)
    {
        nameStep.SetActive(true);
        boardStep.SetActive(false);
        string result = pending.success ? "<color=#6FCF97>Mission accomplished</color>" : "<color=#EB5757>Mission failed</color>";
        nameInfo.text = "You made the top " + Leaderboard.MaxEntries + " at <b>#" + (rank + 1) + "</b>\n"
                        + result + "   Score <b>" + pending.competitive.ToString("0.0") + "</b>"
                        + "   <size=80%>(learning " + pending.diagnostic.ToString("0") + ")</size>";
        input.text = PlayerPrefs.GetString(LastNameKey, "");
        input.Select();
        input.ActivateInputField();
    }

    void SaveName()
    {
        if (pending == null) return;
        string name = Leaderboard.CleanName(input.text);
        PlayerPrefs.SetString(LastNameKey, name);
        pending.playerName = name;
        int rank = Leaderboard.Add(pending); // also flushes PlayerPrefs to disk
        pending = null;
        onSaved?.Invoke(rank);
        ShowBoard(rank, rank >= 0 ? name + ", you are #" + (rank + 1) + "!" : "");
    }

    // The score is not saved, the player can come back with the Leaderboard button
    void Skip()
    {
        ShowBoard(-1, "Your score was not saved.");
    }

    void ShowBoard(int highlightRank, string note)
    {
        nameStep.SetActive(false);
        boardStep.SetActive(true);
        boardNote.text = note;
        clearArmed = false;
        clearLabel.text = "Clear";
        List<LeaderboardEntry> top = Leaderboard.Load();
        boardList.text = top.Count == 0 ? "No scores yet. Finish a mission to be the first!" : RankingText(top, highlightRank);
    }

    public static string RankingText(List<LeaderboardEntry> list, int highlightRank)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            LeaderboardEntry e = list[i];
            string result = e.success ? "<color=#6FCF97>Success</color>"
                                      : "<color=#EB5757>Failed</color>  <size=80%>(learning " + e.diagnostic.ToString("0") + ")</size>";
            string line = (i + 1) + ".<pos=8%>" + e.playerName + "<pos=48%>" + e.competitive.ToString("0.0") + "<pos=64%>" + result;
            if (i == highlightRank) line = "<color=#FFD54F><b>" + line + "</b></color>";
            if (i > 0) sb.Append('\n');
            sb.Append(line);
        }
        return sb.ToString();
    }

    // ---------- UI ----------

    static LeaderboardDialog Build()
    {
        GameObject canvasGo = NewCanvas("LeaderboardCanvas", 100);
        var dialog = canvasGo.AddComponent<LeaderboardDialog>();

        // Full-screen dim that also blocks clicks on the screen behind
        RectTransform dim = NewRect("Dim", canvasGo.transform);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        dialog.panel = dim.gameObject;

        // Step 1: name entry
        RectTransform nameBox = NewRect("NameStep", dim);
        nameBox.sizeDelta = new Vector2(820f, 520f);
        nameBox.gameObject.AddComponent<Image>().color = BoxColor;
        dialog.nameStep = nameBox.gameObject;
        AddText(nameBox, "Title", "New high score!", 50f, new Vector2(0f, 190f), new Vector2(760f, 70f)).fontStyle = FontStyles.Bold;
        dialog.nameInfo = AddText(nameBox, "Info", "", 30f, new Vector2(0f, 90f), new Vector2(760f, 100f));
        AddText(nameBox, "Prompt", "Enter your name", 28f, new Vector2(0f, 5f), new Vector2(760f, 44f)).color = NoteColor;
        dialog.input = AddInput(nameBox, "Your name", new Vector2(0f, -60f), new Vector2(520f, 64f));
        dialog.input.onSubmit.AddListener(_ => dialog.SaveName()); // Enter key
        AddButton(nameBox, "SkipButton", "Skip", new Vector2(-150f, -185f), new Vector2(240f, 64f), GrayColor, dialog.Skip);
        AddButton(nameBox, "SaveButton", "Save", new Vector2(150f, -185f), new Vector2(240f, 64f), SaveColor, dialog.SaveName);

        // Step 2: top 10
        RectTransform board = NewRect("BoardStep", dim);
        board.sizeDelta = new Vector2(1100f, 820f);
        board.gameObject.AddComponent<Image>().color = BoxColor;
        dialog.boardStep = board.gameObject;
        AddText(board, "Title", "Leaderboard", 50f, new Vector2(0f, 345f), new Vector2(1000f, 70f)).fontStyle = FontStyles.Bold;
        dialog.boardNote = AddText(board, "Note", "", 30f, new Vector2(0f, 285f), new Vector2(1000f, 50f));
        dialog.boardNote.color = new Color(1f, 0.84f, 0.31f);
        TMP_Text header = AddText(board, "Columns", "#<pos=8%>Name<pos=48%>Score<pos=64%>Result", 26f, new Vector2(0f, 230f), new Vector2(940f, 40f));
        header.alignment = TextAlignmentOptions.Left;
        header.color = NoteColor;
        dialog.boardList = AddText(board, "List", "", 32f, new Vector2(0f, -55f), new Vector2(940f, 520f));
        dialog.boardList.alignment = TextAlignmentOptions.TopLeft;
        dialog.boardList.lineSpacing = 18f;
        AddButton(board, "CloseButton", "Close", new Vector2(0f, -350f), new Vector2(260f, 64f), GrayColor, Close);
        Image clear = AddButton(board, "ClearButton", "Clear", new Vector2(-400f, -350f), new Vector2(240f, 64f),
            new Color(0.6f, 0.2f, 0.2f), dialog.ClearClicked);
        dialog.clearButton = clear.gameObject;
        dialog.clearLabel = clear.GetComponentInChildren<TMP_Text>();

        dialog.panel.SetActive(false);
        return dialog;
    }

    // Fallback when the scene has no menu button to copy: a "Leaderboard" button in the bottom left corner
    public static Button CreateCornerButton()
    {
        GameObject canvasGo = NewCanvas("LeaderboardButtonCanvas", 50);
        Image image = AddButton((RectTransform)canvasGo.transform, "LeaderboardButton", "Leaderboard", Vector2.zero,
            new Vector2(280f, 70f), new Color(0.22f, 0.4f, 0.75f), () => { });
        RectTransform rt = (RectTransform)image.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(40f, 40f);
        return image.GetComponent<Button>();
    }

    static GameObject NewCanvas(string name, int sortingOrder)
    {
        var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGo;
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

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        field.characterLimit = Leaderboard.MaxNameLength;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.contentType = TMP_InputField.ContentType.Standard;
        field.pointSize = 32f;
        if (field.placeholder is TMP_Text ph) ph.text = placeholder;
        if (field.textComponent != null)
        {
            field.textComponent.color = new Color(0.08f, 0.1f, 0.14f);
            field.textComponent.alignment = TextAlignmentOptions.Center;
        }
        return field;
    }
}