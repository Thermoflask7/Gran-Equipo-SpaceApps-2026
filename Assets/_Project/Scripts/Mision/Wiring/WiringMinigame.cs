using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Wiring minigame "Reconnect the systems" (task 44). Signed: claude_c.
// Design: Docs/WiringMinigame/WIRING_MINIGAME_DESIGN.md. Among Us wires on a grid: connect each color to its twin;
// cables cannot cross or go through walls (empty squares are fine).
// One round = 3 small panels in a row (3, 4 and 5 colors) against one shared timer (40 s).
// Prefab: Prefabs/WiringMinigame.prefab (SpaceApps > Wiring minigame > Create prefab). The whole UI is built in code
// the first time it is enabled, so the prefab is just this component on a full-screen RectTransform.
// Flow: intro ("Start") -> panel 1 -> panel 2 -> panel 3 -> end panel ("Continue") -> Completed(RawResult) -> hides itself.
// RawResult is the player's performance only (WiringMath.Raw). This component never touches GameManager results:
// WiringFlightHook passes RawResult to FlightPhases.CompleteMinigame(Engineering, raw).
// Testable on its own: SpaceApps > Wiring minigame > Add to open scene, then Play.
public class WiringMinigame : MonoBehaviour
{
    [Tooltip("Empty = WiringSettings.Default")]
    public WiringSettings settings;
    [Tooltip("Round played when it opens (0 = first boards; the flight hook sets it)")]
    public int round;

    // Fired once when the player presses "Continue". Argument: RawResult (0..1, without skill).
    public event Action<float> Completed;

    public bool Playing { get { return stage == Stage.Playing; } }
    public bool Finished { get; private set; }
    public bool Solved { get; private set; }        // every panel of the round fixed
    public float RawResult { get; private set; }
    public int Stars { get; private set; }
    public float Seconds { get; private set; }
    public int BoardsSolved { get; private set; }
    public int BoardsInRound { get { return progress != null ? progress.Length : 0; } }
    public int CurrentBoard { get { return boardNumber; } }
    public int BoardWidth { get { return board != null ? board.Width : 1; } }
    public int BoardHeight { get { return board != null ? board.Height : 1; } }
    public WiringBoard Board { get { return board; } }

    WiringSettings S { get { return settings != null ? settings : WiringSettings.Default; } }

    enum Stage { Intro, Playing, Between, Ending, End }
    Stage stage = Stage.Intro;

    WiringBoard board;
    List<WiringSettings.Level> roundBoards = new List<WiringSettings.Level>();
    float[] progress;
    int boardNumber;
    string context = "";
    float elapsed;
    float delay;
    bool reported;

    // UI
    bool built;
    RectTransform window;
    RectTransform boardRect;
    TMP_Text subtitle;
    TMP_Text timerText;
    TMP_Text emptyText;
    TMP_Text banner;
    Image timerFill;
    RectTransform systemsList;
    GameObject introPanel;
    TMP_Text introBody;
    GameObject endPanel;
    TMP_Text endTitle;
    TMP_Text endBody;
    Image[] starImages;
    AudioSource audioSource;

    // Board visuals
    float cellSize;
    Image[,] cellBg;
    Image[,] dots;
    Image[,] rightLinks;
    Image[,] downLinks;
    readonly Dictionary<char, RectTransform> socketA = new Dictionary<char, RectTransform>();
    readonly Dictionary<char, RectTransform> socketB = new Dictionary<char, RectTransform>();
    readonly Dictionary<char, Image> lamps = new Dictionary<char, Image>();
    readonly Dictionary<char, TMP_Text> lampTexts = new Dictionary<char, TMP_Text>();
    readonly Dictionary<char, float> pulse = new Dictionary<char, float>();
    readonly HashSet<char> wasConnected = new HashSet<char>();
    Vector2Int flashCell = new Vector2Int(-1, -1);
    float flashTimer;

    static Sprite circleSprite;
    static Sprite hatchSprite;

    static readonly Color WindowColor = new Color(0.06f, 0.09f, 0.16f, 1f);
    static readonly Color Accent = new Color(1f, 0.62f, 0.2f);
    static readonly Color FreeCell = new Color(0.13f, 0.17f, 0.26f);
    static readonly Color WallColor = new Color(0.78f, 0.22f, 0.2f);
    static readonly Color TextColor = new Color(0.92f, 0.95f, 1f);
    static readonly Color DimText = new Color(0.65f, 0.72f, 0.82f);

    // ---------- Public API ----------

    public void SetRound(int value)
    {
        round = Mathf.Max(0, value);
    }

    // Short text under the title, for example the flight phase ("Return")
    public void SetContext(string text)
    {
        context = text ?? "";
    }

    void OnEnable()
    {
        Build();
        Prepare();
    }

    // Starts the timer (the intro "Start" button)
    public void StartGame()
    {
        if (stage != Stage.Intro) return;
        stage = Stage.Playing;
        elapsed = 0f;
        introPanel.SetActive(false);
    }

    // "Continue" on the end panel: reports the result once and hides the minigame
    public void Continue()
    {
        if (!Finished || reported) return;
        reported = true;
        if (Completed != null) Completed(RawResult);
        gameObject.SetActive(false);
    }

    // ---------- Input (called by WiringBoardInput) ----------

    public void PressCell(Vector2Int cell)
    {
        if (stage != Stage.Playing) return;
        if (board.Begin(cell))
        {
            Play(S.stepClip);
            AfterChange();
        }
    }

    public void DragToCell(Vector2Int cell)
    {
        if (stage != Stage.Playing || board.Active == WiringBoard.None) return;
        if (board.DragTo(cell))
        {
            if (flashTimer <= 0f || flashCell != board.LastBlocked) Play(S.blockedClip);
            flashCell = board.LastBlocked;
            flashTimer = 0.25f;
        }
        AfterChange();
    }

    public void Release()
    {
        if (board != null) board.End();
        if (stage == Stage.Playing) Redraw();
    }

    public void EraseAt(Vector2Int cell)
    {
        if (stage != Stage.Playing) return;
        if (board.EraseAt(cell)) AfterChange();
    }

    // ---------- Game flow ----------

    void Prepare()
    {
        List<WiringSettings.Level> all = S.boards != null && S.boards.Count > 0 ? S.boards : WiringSettings.DefaultBoards();
        int perRound = Mathf.Max(1, S.boardsPerRound);
        roundBoards.Clear();
        for (int i = 0; i < perRound; i++) roundBoards.Add(all[WiringMath.BoardIndex(round, i, perRound, all.Count)]);
        progress = new float[perRound];

        stage = Stage.Intro;
        Finished = false;
        Solved = false;
        RawResult = 0f;
        Stars = 0;
        Seconds = 0f;
        BoardsSolved = 0;
        elapsed = 0f;
        reported = false;

        LoadBoard(0);

        introBody.text = "Some cables on Orion came loose! Fix <b>" + perRound + " panels</b> in <b>" + Mathf.RoundToInt(S.roundSeconds)
            + " seconds</b>.\n\n<b>Connect each color to its twin. Cables can't cross!</b>\n\n"
            + "<size=80%><color=#A6B8D1>Each panel adds one more system.</color></size>";
        introPanel.SetActive(true);
        endPanel.SetActive(false);
        UpdateTimer();
    }

    void LoadBoard(int number)
    {
        boardNumber = number;
        WiringSettings.Level level = roundBoards[number];
        string error;
        board = WiringBoard.Parse(level.grid, out error);
        if (board == null)
        {
            Debug.LogError("WiringMinigame: board '" + level.levelName + "' is not valid (" + error + "). Using a default board.");
            board = WiringBoard.Parse(WiringSettings.DefaultBoards()[0].grid, out error);
        }
        flashTimer = 0f;
        pulse.Clear();
        wasConnected.Clear();
        banner.gameObject.SetActive(false);

        BuildBoard();
        BuildSystems();
        Redraw();

        string engineer = "";
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.engineer != null && !string.IsNullOrEmpty(gm.engineer.astronautName))
            engineer = "Engineer: " + gm.engineer.astronautName;
        subtitle.text = (context != "" ? context + "  ·  " : "") + "Panel " + (number + 1) + " of " + roundBoards.Count + ": " + level.levelName
            + (engineer != "" ? "  ·  " + engineer : "");
    }

    void Update()
    {
        if (!built) return;
        float dt = Time.deltaTime;

        if (stage == Stage.Playing)
        {
            elapsed += dt;
            UpdateTimer();
            if (elapsed >= S.roundSeconds) Finish();
        }
        else if (stage == Stage.Between)
        {
            delay -= dt; // the timer waits while the "panel fixed" banner shows
            if (delay <= 0f)
            {
                LoadBoard(boardNumber + 1);
                stage = Stage.Playing;
            }
        }
        else if (stage == Stage.Ending)
        {
            delay -= dt;
            if (delay <= 0f) ShowEnd();
        }

        // Blocked cell flash
        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            if (board != null && board.InBounds(flashCell) && cellBg != null)
                PaintCell(flashCell, flashTimer > 0f);
        }

        // Socket pulse when a pair connects
        if (pulse.Count > 0)
        {
            List<char> keys = new List<char>(pulse.Keys);
            foreach (char c in keys)
            {
                float t = pulse[c] - dt;
                float scale = t > 0f ? 1f + 0.35f * Mathf.Sin((1f - t / 0.6f) * Mathf.PI) : 1f;
                if (socketA.ContainsKey(c)) socketA[c].localScale = Vector3.one * scale;
                if (socketB.ContainsKey(c)) socketB[c].localScale = Vector3.one * scale;
                if (t <= 0f) pulse.Remove(c);
                else pulse[c] = t;
            }
        }
    }

    void AfterChange()
    {
        Redraw();
        foreach (char c in board.Colors)
        {
            bool now = board.IsConnected(c);
            if (now && !wasConnected.Contains(c))
            {
                wasConnected.Add(c);
                pulse[c] = 0.6f;
                Play(S.connectClip);
            }
            else if (!now) wasConnected.Remove(c);
        }
        if (board.Solved) BoardDone();
    }

    void BoardDone()
    {
        if (stage != Stage.Playing) return;
        board.End();
        progress[boardNumber] = 1f;
        BoardsSolved++;
        Play(S.solvedClip);
        if (boardNumber >= roundBoards.Count - 1)
        {
            Finish();
            return;
        }
        stage = Stage.Between;
        delay = 0.9f;
        banner.text = "PANEL " + (boardNumber + 1) + " FIXED!";
        banner.gameObject.SetActive(true);
    }

    void Finish()
    {
        if (stage != Stage.Playing) return;
        board.End();
        if (!board.Solved)
            progress[boardNumber] = WiringMath.BoardProgress(board.ConnectedCount, board.PairCount);
        Seconds = Mathf.Min(elapsed, S.roundSeconds);
        Solved = BoardsSolved >= roundBoards.Count;
        RawResult = WiringMath.Raw(progress, Solved, Seconds, S);
        Stars = WiringMath.Stars(BoardsSolved, roundBoards.Count);
        Finished = true;
        stage = Stage.Ending;
        delay = Solved ? 0.8f : 0.3f; // a moment to see the last cable glow
        Redraw();
    }

    void ShowEnd()
    {
        stage = Stage.End;
        banner.gameObject.SetActive(false);
        endPanel.SetActive(true);
        if (Solved)
        {
            endTitle.text = "ALL SYSTEMS ONLINE!";
            endTitle.color = new Color(0.4f, 1f, 0.5f);
            endBody.text = "You fixed all " + roundBoards.Count + " panels in " + Mathf.CeilToInt(Seconds) + " seconds.";
        }
        else
        {
            endTitle.text = "TIME'S UP!";
            endTitle.color = Accent;
            endBody.text = BoardsSolved > 0
                ? "You fixed " + BoardsSolved + " of " + roundBoards.Count + " panels. Mission Control rerouted the rest from Earth."
                : "Mission Control rerouted the cables from Earth. Tip: start with the cables that hug the edges!";
        }
        endBody.text += "\n\n<color=#A6B8D1><b>Did you know?</b> " + S.lesson + "</color>";
        for (int i = 0; i < starImages.Length; i++)
            starImages[i].color = i < Stars ? new Color(1f, 0.82f, 0.2f) : new Color(0.25f, 0.3f, 0.4f);
    }

    void UpdateTimer()
    {
        if (timerFill == null) return;
        float left = Mathf.Max(0f, S.roundSeconds - elapsed);
        float share = S.roundSeconds > 0f ? left / S.roundSeconds : 0f;
        timerFill.rectTransform.anchorMax = new Vector2(share, 1f);
        timerFill.color = share > 0.5f ? new Color(0.3f, 0.85f, 0.4f) : share > 0.22f ? new Color(1f, 0.7f, 0.2f) : new Color(1f, 0.3f, 0.25f);
        timerText.text = Mathf.CeilToInt(left) + " s";
    }

    void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, SpaceOsSettings.SfxVolume);
    }

    // ---------- Drawing the board ----------

    void Redraw()
    {
        if (board == null || dots == null) return;
        for (int x = 0; x < board.Width; x++)
            for (int y = 0; y < board.Height; y++)
            {
                dots[x, y].enabled = false;
                rightLinks[x, y].enabled = false;
                downLinks[x, y].enabled = false;
                PaintCell(new Vector2Int(x, y), false);
            }

        foreach (char c in board.Colors)
        {
            bool connected = board.IsConnected(c);
            Color col = WiringMath.SystemColor(c);
            if (!connected) col = Color.Lerp(col, Color.black, board.Active == c ? 0.1f : 0.3f);
            IList<Vector2Int> p = board.PathOf(c);
            for (int i = 0; i < p.Count; i++)
            {
                Vector2Int a = p[i];
                dots[a.x, a.y].enabled = true;
                dots[a.x, a.y].color = col;
                if (i == p.Count - 1) continue;
                Vector2Int b = p[i + 1];
                Image link = null;
                if (b.y == a.y) link = rightLinks[Mathf.Min(a.x, b.x), a.y];
                else if (b.x == a.x) link = downLinks[a.x, Mathf.Min(a.y, b.y)];
                if (link != null)
                {
                    link.enabled = true;
                    link.color = col;
                }
            }

            if (lamps.ContainsKey(c))
            {
                lamps[c].color = connected ? new Color(0.3f, 1f, 0.45f) : new Color(0.45f, 0.12f, 0.12f);
                lampTexts[c].text = "<color=#" + ColorUtility.ToHtmlStringRGB(WiringMath.SystemColor(c)) + ">" + WiringMath.SystemName(c)
                    + "</color>\n<size=75%>" + (connected ? "<color=#5CFF7A>ONLINE</color>" : "<color=#FF7A6B>OFFLINE</color>") + "</size>";
            }
        }

        emptyText.text = board.Solved
            ? "<color=#5CFF7A>All systems connected!</color>"
            : "Systems online: " + board.ConnectedCount + " / " + board.PairCount;
    }

    void PaintCell(Vector2Int c, bool flashing)
    {
        Image bg = cellBg[c.x, c.y];
        if (board.IsWall(c)) bg.color = WallColor;
        else bg.color = flashing ? new Color(0.9f, 0.25f, 0.25f) : FreeCell;
    }

    void BuildBoard()
    {
        foreach (Transform child in boardRect) Destroy(child.gameObject);
        socketA.Clear();
        socketB.Clear();

        const float maxSize = 600f;
        cellSize = Mathf.Floor(maxSize / Mathf.Max(board.Width, board.Height));
        boardRect.sizeDelta = new Vector2(cellSize * board.Width, cellSize * board.Height);

        RectTransform cellsLayer = Layer("Cells");
        RectTransform cablesLayer = Layer("Cables");
        RectTransform socketsLayer = Layer("Sockets");

        int w = board.Width;
        int h = board.Height;
        cellBg = new Image[w, h];
        dots = new Image[w, h];
        rightLinks = new Image[w, h];
        downLinks = new Image[w, h];
        float thick = cellSize * 0.4f;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2Int c = new Vector2Int(x, y);
                Image bg = MakeImage("Cell " + x + "," + y, cellsLayer, FreeCell, board.IsWall(c) ? HatchSprite() : null);
                PlaceCell(bg.rectTransform, (x + 0.5f) * cellSize, (y + 0.5f) * cellSize, cellSize - 4f, cellSize - 4f);
                cellBg[x, y] = bg;

                Image right = MakeImage("Link R", cablesLayer, Color.white, null);
                PlaceCell(right.rectTransform, (x + 1f) * cellSize, (y + 0.5f) * cellSize, cellSize, thick);
                rightLinks[x, y] = right;
                Image down = MakeImage("Link D", cablesLayer, Color.white, null);
                PlaceCell(down.rectTransform, (x + 0.5f) * cellSize, (y + 1f) * cellSize, thick, cellSize);
                downLinks[x, y] = down;
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Image dot = MakeImage("Dot", cablesLayer, Color.white, CircleSprite());
                PlaceCell(dot.rectTransform, (x + 0.5f) * cellSize, (y + 0.5f) * cellSize, thick * 1.25f, thick * 1.25f);
                dots[x, y] = dot;
            }

        foreach (char color in board.Colors)
        {
            Vector2Int[] s = board.SocketsOf(color);
            socketA[color] = MakeSocket(socketsLayer, color, s[0]);
            socketB[color] = MakeSocket(socketsLayer, color, s[1]);
        }
    }

    RectTransform MakeSocket(RectTransform parent, char color, Vector2Int cell)
    {
        Image ring = MakeImage("Socket " + color, parent, Color.Lerp(WiringMath.SystemColor(color), Color.white, 0.55f), CircleSprite());
        RectTransform rt = ring.rectTransform;
        PlaceCell(rt, (cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, cellSize * 0.8f, cellSize * 0.8f);
        Image core = MakeImage("Core", rt, WiringMath.SystemColor(color), CircleSprite());
        Stretch(core.rectTransform, 5f);
        TMP_Text label = MakeText("Label", core.rectTransform, WiringMath.SocketLabel(color), cellSize * 0.18f, new Color(0.05f, 0.07f, 0.12f), TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        Stretch(label.rectTransform, 0f);
        return rt;
    }

    void BuildSystems()
    {
        foreach (Transform child in systemsList) Destroy(child.gameObject);
        lamps.Clear();
        lampTexts.Clear();
        float y = 0f;
        foreach (char c in board.Colors)
        {
            RectTransform row = MakeRect("System " + c, systemsList);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -y);
            row.sizeDelta = new Vector2(0f, 74f);

            Image lamp = MakeImage("Lamp", row, Color.red, CircleSprite());
            lamp.rectTransform.anchorMin = lamp.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            lamp.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            lamp.rectTransform.sizeDelta = new Vector2(30f, 30f);
            lamps[c] = lamp;

            TMP_Text t = MakeText("Name", row, "", 27f, TextColor, TextAlignmentOptions.MidlineLeft);
            Stretch(t.rectTransform, 0f);
            t.rectTransform.offsetMin = new Vector2(52f, 0f);
            lampTexts[c] = t;
            y += 80f;
        }
    }

    RectTransform Layer(string name)
    {
        RectTransform rt = MakeRect(name, boardRect);
        Stretch(rt, 0f);
        return rt;
    }

    // Places a child of the board by its center, measured from the board's top-left corner
    static void PlaceCell(RectTransform rt, float fromLeft, float fromTop, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(fromLeft, -fromTop);
        rt.sizeDelta = new Vector2(width, height);
    }

    // ---------- Building the UI (once) ----------

    void Build()
    {
        if (built) return;
        built = true;

        RectTransform root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Dark backdrop that blocks clicks on the mission control behind
        Image backdrop = MakeImage("Backdrop", root, new Color(0f, 0f, 0f, 0.82f), null);
        backdrop.raycastTarget = true;
        Stretch(backdrop.rectTransform, 0f);

        Image border = MakeImage("Border", root, Accent, null); // behind the window: a thin orange frame
        Center(border.rectTransform, 0f, 0f, 1588f, 968f);
        Image win = MakeImage("Window", root, WindowColor, null);
        window = win.rectTransform;
        Center(window, 0f, 0f, 1580f, 960f);

        TMP_Text title = MakeText("Title", window, "WIRING FAULT", 50f, Accent, TextAlignmentOptions.TopLeft);
        title.fontStyle = FontStyles.Bold;
        TopStrip(title.rectTransform, 24f, 64f, 40f, 700f);
        subtitle = MakeText("Subtitle", window, "", 26f, DimText, TextAlignmentOptions.TopLeft);
        TopStrip(subtitle.rectTransform, 88f, 36f, 40f, 1000f);

        // Timer (top right)
        Image timerBack = MakeImage("Timer", window, new Color(0.15f, 0.18f, 0.25f), null);
        RectTransform tb = timerBack.rectTransform;
        tb.anchorMin = tb.anchorMax = new Vector2(1f, 1f);
        tb.pivot = new Vector2(1f, 1f);
        tb.anchoredPosition = new Vector2(-40f, -40f);
        tb.sizeDelta = new Vector2(380f, 30f);
        timerFill = MakeImage("Fill", tb, Color.green, null);
        timerFill.rectTransform.anchorMin = Vector2.zero;
        timerFill.rectTransform.anchorMax = Vector2.one;
        timerFill.rectTransform.offsetMin = timerFill.rectTransform.offsetMax = Vector2.zero;
        timerText = MakeText("Seconds", window, "", 30f, TextColor, TextAlignmentOptions.TopRight);
        RectTransform tt = timerText.rectTransform;
        tt.anchorMin = tt.anchorMax = new Vector2(1f, 1f);
        tt.pivot = new Vector2(1f, 1f);
        tt.anchoredPosition = new Vector2(-440f, -36f);
        tt.sizeDelta = new Vector2(140f, 40f);

        TMP_Text instruction = MakeText("Instruction", window, S.howToPlay, 34f, TextColor, TextAlignmentOptions.Center);
        instruction.fontStyle = FontStyles.Bold;
        Center(instruction.rectTransform, 0f, 340f, 1400f, 50f);

        // Board (center)
        Image boardBack = MakeImage("Board", window, new Color(0.03f, 0.05f, 0.1f), null);
        boardRect = boardBack.rectTransform;
        Center(boardRect, 0f, -20f, 600f, 600f);
        boardBack.raycastTarget = true;
        WiringBoardInput input = boardBack.gameObject.AddComponent<WiringBoardInput>();
        input.owner = this;

        emptyText = MakeText("Empty", window, "", 28f, DimText, TextAlignmentOptions.Center);
        Center(emptyText.rectTransform, 0f, -370f, 900f, 44f);

        banner = MakeText("Banner", window, "", 72f, new Color(0.4f, 1f, 0.5f), TextAlignmentOptions.Center);
        banner.fontStyle = FontStyles.Bold;
        banner.outlineWidth = 0.25f;
        banner.outlineColor = new Color32(0, 0, 0, 255);
        Center(banner.rectTransform, 0f, -20f, 1000f, 120f);
        banner.gameObject.SetActive(false);

        // Systems (left)
        TMP_Text systemsTitle = MakeText("SystemsTitle", window, "SYSTEMS", 30f, Accent, TextAlignmentOptions.TopLeft);
        systemsTitle.fontStyle = FontStyles.Bold;
        Center(systemsTitle.rectTransform, -590f, 250f, 300f, 44f);
        systemsList = MakeRect("Systems", window);
        Center(systemsList, -590f, -40f, 300f, 520f);

        // Tips (right)
        TMP_Text tips = MakeText("Tips", window,
            "<b><color=#FF9E33>TIPS</color></b>\n\n• Press a socket and drag to its twin.\n\n• A cable in the way? Go around it.\n\n• Drag back to undo.\n\n• Click a socket to start again. Right-click erases.",
            25f, DimText, TextAlignmentOptions.TopLeft);
        Center(tips.rectTransform, 590f, -10f, 300f, 600f);

        // Intro panel
        introPanel = Overlay("Intro");
        TMP_Text introTitle = MakeText("Title", introPanel.transform, "WIRING FAULT!", 64f, Accent, TextAlignmentOptions.Center);
        introTitle.fontStyle = FontStyles.Bold;
        Center(introTitle.rectTransform, 0f, 250f, 1200f, 90f);
        introBody = MakeText("Body", introPanel.transform, "", 34f, TextColor, TextAlignmentOptions.Center);
        Center(introBody.rectTransform, 0f, 30f, 1150f, 320f);
        MakeButton(introPanel.transform, "Start", new Color(0.25f, 0.7f, 0.35f), StartGame, -260f);

        // End panel
        endPanel = Overlay("End");
        endTitle = MakeText("Title", endPanel.transform, "", 64f, Accent, TextAlignmentOptions.Center);
        endTitle.fontStyle = FontStyles.Bold;
        Center(endTitle.rectTransform, 0f, 290f, 1200f, 90f);
        starImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            starImages[i] = MakeImage("Star " + (i + 1), endPanel.transform, Color.gray, CircleSprite());
            Center(starImages[i].rectTransform, (i - 1) * 120f, 170f, 90f, 90f);
        }
        endBody = MakeText("Body", endPanel.transform, "", 32f, TextColor, TextAlignmentOptions.Center);
        Center(endBody.rectTransform, 0f, -20f, 1150f, 260f);
        MakeButton(endPanel.transform, "Continue", Accent, Continue, -260f);
    }

    GameObject Overlay(string name)
    {
        Image img = MakeImage(name, window, new Color(0.04f, 0.06f, 0.11f, 0.97f), null);
        img.raycastTarget = true;
        Stretch(img.rectTransform, 0f);
        return img.gameObject;
    }

    void MakeButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action, float y)
    {
        Image img = MakeImage(label + "Button", parent, color, null);
        img.raycastTarget = true;
        Center(img.rectTransform, 0f, y, 320f, 86f);
        Button b = img.gameObject.AddComponent<Button>();
        Navigation nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
        b.onClick.AddListener(action);
        TMP_Text t = MakeText("Label", img.rectTransform, label, 38f, new Color(0.05f, 0.07f, 0.12f), TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
        Stretch(t.rectTransform, 0f);
    }

    // ---------- Small UI helpers ----------

    static RectTransform MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static Image MakeImage(string name, Transform parent, Color color, Sprite sprite)
    {
        RectTransform rt = MakeRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = MakeRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static void Center(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    static void TopStrip(RectTransform rt, float fromTop, float height, float left, float width)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(left, -fromTop);
        rt.sizeDelta = new Vector2(width, height);
    }

    // Round sprite made in code (no art asset needed)
    static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    // Diagonal stripes for the walls (tinted red by the Image color)
    static Sprite HatchSprite()
    {
        if (hatchSprite != null) return hatchSprite;
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool stripe = ((x + y) / 8) % 2 == 0;
                tex.SetPixel(x, y, stripe ? Color.white : new Color(0.55f, 0.55f, 0.55f));
            }
        tex.Apply();
        hatchSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return hatchSprite;
    }
}
