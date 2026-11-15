using UnityEditor;
using UnityEngine;

// Wiring minigame tests (task 44). Signed: claude_c.
// Menu: SpaceApps > Wiring minigame > Tests (edit mode). Pure logic: every board can be solved, the board rules
// (walls, no crossing, back up, erase, empty squares allowed) and the result formula of the design doc.
// Dragging with the mouse is checked by playing.
public static class WiringMinigameTests
{
    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Wiring minigame/Tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        Boards();
        Rules();
        Formula();
        Hook();

        string summary = "Wiring minigame tests: " + passed + " passed, " + failed + " failed.";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    static void Boards()
    {
        var boards = WiringSettings.DefaultBoards();
        WiringSettings s = WiringSettings.Default;
        Check("6 default boards (two rounds of 3)", boards.Count == 6 && s.boardsPerRound == 3);
        Check("Round time is 40 s or less", s.roundSeconds <= 40f);
        for (int i = 0; i < boards.Count; i++)
        {
            string error;
            WiringBoard b = WiringBoard.Parse(boards[i].grid, out error);
            string name = "Board " + (i + 1) + " '" + boards[i].levelName + "'";
            Check(name + " parses (" + error + ")", b != null);
            if (b == null) continue;
            int colors = 3 + i % 3; // each round: 3, then 4, then 5 colors
            Check(name + " has " + colors + " colors", b.PairCount == colors);
            Check(name + " can be solved", b.CountSolutions(1) > 0);
            Check(name + " starts unsolved", !b.Solved && b.ConnectedCount == 0);
        }

        string err;
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
        Check("All connected = solved, even with an empty square (0,1)", b.Solved && b.CableAt(new Vector2Int(0, 1)) == WiringBoard.None);
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
        Near("Board all connected = 1", WiringMath.BoardProgress(4, 4), 1f);
        Near("Board 2 of 4 connected = 0.5", WiringMath.BoardProgress(2, 4), 0.5f);
        Near("Board nothing connected = 0", WiringMath.BoardProgress(0, 3), 0f);

        Near("All 3 fixed in 20 s = 1.00", WiringMath.Raw(new[] { 1f, 1f, 1f }, true, 20f, s), 1f);
        Near("All 3 fixed at 30 s = 0.925", WiringMath.Raw(new[] { 1f, 1f, 1f }, true, 30f, s), 0.925f);
        Near("All 3 fixed at 40 s = 0.85", WiringMath.Raw(new[] { 1f, 1f, 1f }, true, 40f, s), 0.85f);
        Near("2 fixed + half of the third = 0.708", WiringMath.Raw(new[] { 1f, 1f, 0.5f }, false, 40f, s), 0.85f * 2.5f / 3f);
        Near("1 fixed, nothing else = 0.283", WiringMath.Raw(new[] { 1f, 0f, 0f }, false, 40f, s), 0.85f / 3f);
        Near("Nothing = 0", WiringMath.Raw(new[] { 0f, 0f, 0f }, false, 40f, s), 0f);
        Check("Never above 1", WiringMath.Raw(new[] { 1f, 1f, 1f }, true, -5f, s) <= 1f);

        Check("Stars = panels fixed", WiringMath.Stars(2, 3) == 2 && WiringMath.Stars(0, 3) == 0 && WiringMath.Stars(5, 3) == 3);

        Check("Round 0 = boards 0, 1, 2", WiringMath.BoardIndex(0, 0, 3, 6) == 0 && WiringMath.BoardIndex(0, 2, 3, 6) == 2);
        Check("Round 1 = boards 3, 4, 5", WiringMath.BoardIndex(1, 0, 3, 6) == 3 && WiringMath.BoardIndex(1, 2, 3, 6) == 5);
        Check("Round 2 wraps to board 0", WiringMath.BoardIndex(2, 0, 3, 6) == 0);
        Check("No boards = 0", WiringMath.BoardIndex(2, 1, 3, 0) == 0);

        // Task 31: a Minor wiring fault never aborts, even with raw 0 and no skill
        MissionFailureRules.Outcome o = MissionFailureRules.Judge(TrainingFocus.Engineering, EventPhase.Return, 0f, 0f,
            EventSeverity.Minor, 0, MissionFailureSettings.Default);
        Check("Minor wiring fault with raw 0 continues", o.decision == MissionFailureRules.Decision.Continue);
    }

    static void Hook()
    {
        // Task 45 flight plan: when does the wiring fault happen? (phase, guaranteed phase, fault when empty, engineer played, opened, max)
        EventPhase R = EventPhase.Return, D = EventPhase.Departure;
        Check("Return: guaranteed wiring for the engineer", WiringFlightHook.ShouldFault(R, R, true, false, 0, 2));
        Check("Return after a research incident: wiring still comes (2nd minigame)", WiringFlightHook.ShouldFault(R, R, true, false, 1, 2));
        Check("Return after maintenance: no wiring (engineer already played)", !WiringFlightHook.ShouldFault(R, R, true, true, 1, 2));
        Check("Return already full: no wiring", !WiringFlightHook.ShouldFault(R, R, true, false, 2, 2));
        Check("Departure with piloting played: no wiring", !WiringFlightHook.ShouldFault(D, R, true, false, 1, 2));
        Check("Departure with no minigame at all: wiring fault", WiringFlightHook.ShouldFault(D, R, true, false, 0, 2));
        Check("Departure with no minigame, option off: no fault", !WiringFlightHook.ShouldFault(D, R, false, false, 0, 2));

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
