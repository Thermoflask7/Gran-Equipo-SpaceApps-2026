using System;
using System.Collections.Generic;
using UnityEngine;

// Local leaderboard (task 37a). Signed: Grok.
// Top 10 saved in PlayerPrefs as JSON (works in WebGL: stored in the browser's IndexedDB).
// Ranking: official score (competitive, 0 if the mission failed) first, then the diagnostic score (total),
// so failed missions are still ordered among themselves. Ties keep the older entry first.
// The numbers come from MissionScoring (ScoreResult): if the scoring formula changes, the leaderboard
// keeps working; old entries just are not comparable (use Clear).
//
// Single player, arcade style: at the end of a game the Results "Leaderboard" button asks the name (LeaderboardDialog).
// Use:  int rank = Leaderboard.Add(Leaderboard.EntryFrom("Ana", result, gm));   // 0-based, -1 = not in the top
//       List<LeaderboardEntry> top = Leaderboard.Load();
public static class Leaderboard
{
    public const int MaxEntries = 10;
    public const int MaxNameLength = 16;
    public const string DefaultKey = "SpaceApps.Leaderboard.v1";

    // Changed only by the editor tests, so they do not touch the real leaderboard
    public static string Key = DefaultKey;

    [Serializable]
    class EntryList
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    public static List<LeaderboardEntry> Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<LeaderboardEntry>();
        try
        {
            EntryList list = JsonUtility.FromJson<EntryList>(json);
            if (list == null || list.entries == null) return new List<LeaderboardEntry>();
            return list.entries;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Leaderboard: saved data could not be read, starting empty. " + e.Message);
            return new List<LeaderboardEntry>();
        }
    }

    // Adds the entry, keeps the best MaxEntries and saves. Returns its 0-based rank, or -1 if it did not make the top.
    public static int Add(LeaderboardEntry entry)
    {
        if (entry == null) return -1;
        List<LeaderboardEntry> entries = Load();
        int index = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            if (Compare(entry, entries[i]) < 0)
            {
                index = i;
                break;
            }
        }
        entries.Insert(index, entry);
        if (entries.Count > MaxEntries) entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        Save(entries);
        return index < MaxEntries ? index : -1;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    // 0-based rank the entry would get, without saving it (-1 = it would not make the top)
    public static int RankFor(LeaderboardEntry entry)
    {
        if (entry == null) return -1;
        List<LeaderboardEntry> entries = Load();
        int index = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            if (Compare(entry, entries[i]) < 0)
            {
                index = i;
                break;
            }
        }
        return index < MaxEntries ? index : -1;
    }

    // Trimmed, no TMP rich text tags, at most MaxNameLength characters; empty = "Player"
    public static string CleanName(string name)
    {
        string n = name == null ? "" : name.Trim();
        n = n.Replace("<", "").Replace(">", "");
        if (n.Length > MaxNameLength) n = n.Substring(0, MaxNameLength).Trim();
        return n.Length == 0 ? "Player" : n;
    }

    // Negative = a goes above b
    public static int Compare(LeaderboardEntry a, LeaderboardEntry b)
    {
        int c = b.competitive.CompareTo(a.competitive);
        if (c != 0) return c;
        return b.diagnostic.CompareTo(a.diagnostic);
    }

    public static LeaderboardEntry EntryFrom(string playerName, ScoreResult r, GameManager gm)
    {
        LeaderboardEntry e = new LeaderboardEntry();
        e.playerName = CleanName(playerName);
        if (r != null)
        {
            e.competitive = Mathf.Round(r.competitive * 10f) / 10f;
            e.diagnostic = Mathf.Round(r.total * 10f) / 10f;
            e.success = r.status == "SUCCESS";
        }
        if (gm != null && gm.mission != null)
            e.mission = string.IsNullOrEmpty(gm.mission.missionName) ? gm.mission.destinationName : gm.mission.missionName;
        e.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        return e;
    }

    static void Save(List<LeaderboardEntry> entries)
    {
        EntryList list = new EntryList();
        list.entries = entries;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(list));
        PlayerPrefs.Save(); // WebGL: flushes to IndexedDB right away
    }
}

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public float competitive;  // official score (0 if the mission failed)
    public float diagnostic;   // diagnostic score, 0 to 100
    public bool success;
    public string mission;
    public string date;        // local time, "yyyy-MM-dd HH:mm"
}