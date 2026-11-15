using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Leaderboard tests (task 37). Signed: Grok.
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
            RankPreview();
            Names();
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

    static void Names()
    {
        Check("name trimmed", Leaderboard.CleanName("  Ana ") == "Ana");
        Check("empty name is Player", Leaderboard.CleanName("") == "Player" && Leaderboard.CleanName(null) == "Player");
        Check("no rich text tags", Leaderboard.CleanName("<b>Bob</b>") == "bBob/b");
        Check("long name cut", Leaderboard.CleanName("ABCDEFGHIJKLMNOPQRST").Length == Leaderboard.MaxNameLength);
    }

    static void RankPreview()
    {
        Check("empty board: rank 0", Leaderboard.RankFor(Entry("X", 10f, 10f)) == 0);
        for (int i = 0; i < 10; i++) Leaderboard.Add(Entry("P" + i, 50f + i, 50f + i));
        Check("preview does not save", Leaderboard.Load().Count == Leaderboard.MaxEntries);
        Check("too low: -1", Leaderboard.RankFor(Entry("Low", 10f, 10f)) == -1);
        Check("preview matches Add", Leaderboard.RankFor(Entry("Mid", 55.5f, 55.5f)) == Leaderboard.Add(Entry("Mid", 55.5f, 55.5f)));
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