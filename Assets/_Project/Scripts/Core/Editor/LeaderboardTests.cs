using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Leaderboard and player session tests (task 37a). Signed: Grok.
// Menu: SpaceApps -> Leaderboard -> Run tests. Uses a separate PlayerPrefs key, the real leaderboard is not touched.
//       SpaceApps -> Leaderboard -> Log saved / Clear saved: look at or wipe the real one.
public static class LeaderboardTests
{
    const string TestKey = "SpaceApps.Leaderboard.Tests";
    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Leaderboard/Run tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;
        string realKey = Leaderboard.Key;
        Leaderboard.Key = TestKey;
        try
        {
            Leaderboard.Clear();
            Ordering();
            Leaderboard.Clear();
            TopTen();
            Leaderboard.Clear();
            Session();
        }
        finally
        {
            Leaderboard.Clear();
            Leaderboard.Key = realKey;
        }
        string summary = "Leaderboard tests: " + passed + " OK, " + failed + " FAIL";
        if (failed == 0) Debug.Log(summary); else Debug.LogError(summary);
    }

    [MenuItem("SpaceApps/Leaderboard/Log saved")]
    static void LogSaved()
    {
        List<LeaderboardEntry> top = Leaderboard.Load();
        if (top.Count == 0) Debug.Log("Leaderboard: empty");
        for (int i = 0; i < top.Count; i++)
        {
            LeaderboardEntry e = top[i];
            Debug.Log((i + 1) + ". " + e.playerName + "  " + e.competitive + " (diagnostic " + e.diagnostic + ")  "
                      + (e.success ? "SUCCESS" : "FAILURE") + "  " + e.mission + "  " + e.date);
        }
    }

    [MenuItem("SpaceApps/Leaderboard/Clear saved")]
    static void ClearSaved()
    {
        if (EditorUtility.DisplayDialog("Clear leaderboard", "Delete every saved leaderboard entry?", "Delete", "Cancel"))
        {
            Leaderboard.Clear();
            Debug.Log("Leaderboard: cleared");
        }
    }

    // Play mode in 03_Resultados: plays a sample turn for a 2-player session ("Tester A", "Tester B") and SAVES it
    // in the real leaderboard, to see the Leaderboard sections and the "Next player" button. Clear saved afterwards.
    [MenuItem("SpaceApps/Leaderboard/Test: 2-player results turn")]
    static void TestResultsTurn()
    {
        ResultsScreen screen = Object.FindAnyObjectByType<ResultsScreen>();
        if (screen == null)
        {
            Debug.LogWarning("Leaderboard test: open 03_Resultados and press Play first.");
            return;
        }
        GameManager gm = GameManager.Instance;
        if (!gm.session.IsStarted || !gm.session.HasNextPlayer) gm.session.Start(new List<string> { "Tester A", "Tester B" });
        ResultsSample.Fill(gm, Random.value > 0.5f);
        screen.useTestValues = false;
        screen.Show();
    }

    [MenuItem("SpaceApps/Leaderboard/Test: 2-player results turn", true)]
    static bool PlayModeOnly()
    {
        return Application.isPlaying;
    }

    static void Ordering()
    {
        Check("first entry is rank 0", Leaderboard.Add(Entry("A", 0f, 40f)) == 0);
        Check("success beats any failure", Leaderboard.Add(Entry("B", 55f, 55f)) == 0);
        Check("failure ordered by diagnostic", Leaderboard.Add(Entry("C", 0f, 60f)) == 1);
        Check("tie goes after the older entry", Leaderboard.Add(Entry("D", 55f, 55f)) == 1);
        List<LeaderboardEntry> top = Leaderboard.Load();
        Check("order is B D C A", top.Count == 4 && top[0].playerName == "B" && top[1].playerName == "D"
                                  && top[2].playerName == "C" && top[3].playerName == "A");
    }

    static void TopTen()
    {
        for (int i = 0; i < 10; i++) Leaderboard.Add(Entry("P" + i, 50f + i, 50f + i));
        Check("worse than all 10 is not in the top", Leaderboard.Add(Entry("Low", 10f, 10f)) == -1);
        Check("best goes first", Leaderboard.Add(Entry("High", 99f, 99f)) == 0);
        List<LeaderboardEntry> top = Leaderboard.Load();
        Check("keeps 10 entries", top.Count == Leaderboard.MaxEntries);
        Check("worst one dropped", top[top.Count - 1].playerName == "P1");
    }

    static void Session()
    {
        PlayerSession s = new PlayerSession();
        Check("no session: Player 1", s.CurrentName == "Player 1" && !s.IsStarted && s.PlayerCount == 1);

        s.Start(new List<string> { " Ana ", "", "<b>Bob</b>", "Dana", "Extra" });
        Check("max 4 players", s.players.Count == 4);
        Check("names cleaned", s.players[0] == "Ana" && s.players[1] == "Player 2" && s.players[2] == "bBob/b");

        ScoreResult win = new ScoreResult { total = 80f, competitive = 80f, status = "SUCCESS" };
        ScoreResult lose = new ScoreResult { total = 70f, competitive = 0f, status = "FAILURE" };
        int rank = s.RecordTurn(lose, null);
        Check("turn saved once", s.RecordTurn(win, null) == rank && Leaderboard.Load().Count == 1);
        Check("next player", s.NextPlayer() && s.CurrentName == "Player 2" && !s.turnRecorded);
        s.RecordTurn(win, null);
        List<LeaderboardEntry> round = s.RoundRanking();
        Check("round ranking best first", round.Count == 2 && round[0].playerName == "Player 2" && round[0].success);
        s.NextPlayer();
        s.NextPlayer();
        Check("no player after the 4th", !s.NextPlayer() && s.CurrentName == "Dana");
        Check("long name cut", PlayerSession.CleanName("ABCDEFGHIJKLMNOPQRST", 1).Length == PlayerSession.MaxNameLength);
    }

    static LeaderboardEntry Entry(string name, float competitive, float diagnostic)
    {
        return new LeaderboardEntry { playerName = name, competitive = competitive, diagnostic = diagnostic, success = competitive > 0f };
    }

    static void Check(string name, bool ok)
    {
        if (ok) { passed++; Debug.Log("OK   " + name); }
        else { failed++; Debug.LogError("FAIL " + name); }
    }
}