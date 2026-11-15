using UnityEditor;
using UnityEngine;

// Wiring minigame tests (task 43). Signed: claude_c.
// Menu: SpaceApps > Wiring minigame > Tests (edit mode). Pure logic: the levels can be solved, the board rules
// (walls, no crossing, back up, erase) and the result formula of the design doc. Dragging with the mouse is checked by playing.
public static class WiringMinigameTests
{
    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Wiring minigame/Tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        Levels();
        Rules();
        Formula();
        Hook();

        string summary = "Wiring minigame tests: " + passed + " passed, " + failed + " failed.";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    static void Levels()
    {
        int[] expectedSolutions = { 16, 4, 2 }; // checked with an independent solver when the levels were designed
        var levels = WiringSettings.DefaultLevels();
        Check("3 default levels", levels.Count == 3);
        for (int i = 0; i < levels.Count; i++)
        {
            string error;
            WiringBoard b = WiringBoard.Parse(levels[i].grid, out error);
            Check("Level " + (i + 1) + " parses (" + error + ")", b != null);
            if (b == null) continue;
            Check("Level " + (i + 1) + " has 3 to 5 pairs", b.PairCount >= 3 && b.PairCount <= 5);
            int n = b.CountSolutions(1000);
            Check("Level " + (i + 1) + " is solvable (" + n + " solutions)", n > 0);
            Check("Level " + (i + 1) + " has " + expectedSolutions[i] + " solutions", n == expectedSolutions[i]);
        }

        // The sketch level: all "1" sockets on the top edge; blue 2 top-right, green 2 bottom-left, yellow 2 bottom-right
        string err;
        WiringBoard sketch = WiringBoard.Parse(levels[0].grid, out err);
        if (sketch != null)
        {
            Check("Sketch: blue top-right corner", Has(sketch, 'B', new Vector2Int(6, 0)));
            Check("Sketch: green bottom-left", Has(sketch, 'G', new Vector2Int(0, 6)));
            Check("Sketch: yellow bottom-right", Has(sketch, 'Y', new Vector2Int(6, 6)));
        }

        Check("Bad grid: one socket only", WiringBoard.Parse("B . .\n. . .", out err) == null);
        Check("Bad grid: uneven rows", WiringBoard.Parse("B . B\n. .", out err) == null);
        Check("Bad grid: unknown cell", WiringBoard.Parse("B x B", out err) == null);
    }

    static void Rules()
    {
        string err;
        // B . # B
        // . . . .
        // G . . G
        WiringBoard b = WiringBoard.Parse("B . # B\n. . . .\nG . . G", out err);
        Check("Small board parses", b != null);
        if (b == null) return;

        Check("Begin on an empty cell does nothing", !b.Begin(new Vector2Int(1, 1)) && b.Active == WiringBoard.None);
        Check("Begin on a socket", b.Begin(new Vector2Int(0, 0)) && b.Active == 'B');
        Check("Wall blocks", b.DragTo(new Vector2Int(2, 0)) && b.LastBlocked == new Vector2Int(2, 0) && b.PathOf('B').Count == 2);
        b.DragTo(new Vector2Int(1, 1));
        b.DragTo(new Vector2Int(2, 1));
        b.DragTo(new Vector2Int(3, 1));
        b.DragTo(new Vector2Int(3, 0));
        Check("Blue connected", b.IsConnected('B') && b.ConnectedCount == 1 && !b.Solved);
        Check("Dragging back over the cable undoes the connection",
            !b.DragTo(new Vector2Int(3, 1)) && !b.IsConnected('B') && b.PathOf('B').Count == 5);
        b.DragTo(new Vector2Int(3, 0));
        Check("Blue connected again", b.IsConnected('B'));
        b.End();

        b.Begin(new Vector2Int(0, 2));
        b.DragTo(new Vector2Int(1, 2));
        Check("Another color's cable blocks (no crossing)", b.DragTo(new Vector2Int(1, 0)) && b.LastBlocked == new Vector2Int(1, 1));
        b.Begin(new Vector2Int(0, 2));
        Check("Another color's socket blocks",
            b.DragTo(new Vector2Int(0, 0)) && b.LastBlocked == new Vector2Int(0, 0) && b.PathOf('G').Count == 2);
        b.Begin(new Vector2Int(0, 2));
        b.DragTo(new Vector2Int(3, 2));
        Check("Green connected along the bottom: solved", b.IsConnected('G') && b.Solved);
        b.End();

        b.Begin(new Vector2Int(2, 1)); // press in the middle of blue: cut there and go on
        Check("Press on a cable cuts it there", b.Active == 'B' && !b.IsConnected('B') && b.PathOf('B')[b.PathOf('B').Count - 1] == new Vector2Int(2, 1));
        b.DragTo(new Vector2Int(1, 1));
        Check("Dragging back shortens the cable", b.PathOf('B').Count == 3);
        b.End();

        Check("Right-click erases a cable", b.EraseAt(new Vector2Int(1, 1)) && b.PathOf('B').Count == 0 && b.ConnectedCount == 1);
        b.Begin(new Vector2Int(3, 2));
        Check("Pressing a socket restarts its cable", b.PathOf('G').Count == 1 && !b.IsConnected('G'));
        b.Clear();
        Check("Clear removes everything", b.ConnectedCount == 0 && b.CableAt(new Vector2Int(1, 2)) == WiringBoard.None);
    }

    static void Formula()
    {
        WiringSettings s = WiringSettings.Default;
        Near("Solved in 20 s = 1.00", WiringMath.Raw(3, 3, true, 20f, s), 1f);
        Near("Solved in 10 s = 1.00 (capped)", WiringMath.Raw(3, 3, true, 10f, s), 1f);
        Near("Solved at 35 s = 0.88", WiringMath.Raw(3, 3, true, 35f, s), 0.88f);
        Near("Solved at 45 s = 0.80", WiringMath.Raw(3, 3, true, 45f, s), 0.8f);
        Near("2 of 3 = 0.53", WiringMath.Raw(2, 3, false, 45f, s), 0.8f * 2f / 3f);
        Near("3 of 4 = 0.60", WiringMath.Raw(3, 4, false, 45f, s), 0.6f);
        Near("1 of 3 = 0.27", WiringMath.Raw(1, 3, false, 45f, s), 0.8f / 3f);
        Near("Nothing = 0", WiringMath.Raw(0, 3, false, 45f, s), 0f);
        Near("No pairs = 0", WiringMath.Raw(0, 0, false, 0f, s), 0f);
        Check("Never above 1", WiringMath.Raw(9, 3, true, -5f, s) <= 1f);

        Check("Stars: unsolved 0", WiringMath.Stars(false, 5f, s) == 0);
        Check("Stars: 18 s = 3", WiringMath.Stars(true, 18f, s) == 3);
        Check("Stars: 30 s = 2", WiringMath.Stars(true, 30f, s) == 2);
        Check("Stars: 44 s = 1", WiringMath.Stars(true, 44f, s) == 1);

        Check("Level order: first = 0", WiringMath.LevelIndex(0, 3) == 0);
        Check("Level order: after 1 solved = 1", WiringMath.LevelIndex(1, 3) == 1);
        Check("Level order: last repeats", WiringMath.LevelIndex(7, 3) == 2);
        Check("Level order: no levels = 0", WiringMath.LevelIndex(2, 0) == 0);

        // Task 31: a Minor wiring fault never aborts, even with raw 0 and no skill
        MissionFailureRules.Outcome o = MissionFailureRules.Judge(TrainingFocus.Engineering, EventPhase.Return, 0f, 0f,
            EventSeverity.Minor, 0, MissionFailureSettings.Default);
        Check("Minor wiring fault with raw 0 continues", o.decision == MissionFailureRules.Decision.Continue);
    }

    static void Hook()
    {
        WiringSettings s = WiringSettings.Default;
        EventData e = ScriptableObject.CreateInstance<EventData>();
        e.name = "EV-DE-02";
        e.eventName = "Unstable telemetry signal";
        Check("EV-DE-02 opens wiring", WiringFlightHook.IsWiringEvent(e, s));
        e.name = "EV-RE-02";
        e.eventName = "Thermal reading outside the expected range";
        Check("Other events open maintenance", !WiringFlightHook.IsWiringEvent(e, s));
        Check("No event = not a wiring event", !WiringFlightHook.IsWiringEvent(null, s));
        Object.DestroyImmediate(e);
    }

    static bool Has(WiringBoard b, char color, Vector2Int cell)
    {
        foreach (Vector2Int c in b.SocketsOf(color)) if (c == cell) return true;
        return false;
    }

    static void Near(string name, float actual, float expected)
    {
        Check(name + " (got " + actual.ToString("0.000") + ")", Mathf.Abs(actual - expected) < 0.005f);
    }

    static void Check(string name, bool ok)
    {
        if (ok) passed++;
        else
        {
            failed++;
            Debug.LogError("FAIL: " + name);
        }
    }
}
