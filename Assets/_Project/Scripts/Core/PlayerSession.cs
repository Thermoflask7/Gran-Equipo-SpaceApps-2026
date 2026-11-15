using System;
using System.Collections.Generic;

// Players of the current session (task 37a, "1 to 4 players"). Signed: Grok.
// Lives in GameManager.session and is NOT cleared by ResetGame: every player plays the same mission in turns
// on the same computer. Results calls RecordTurn once per turn (37c), then NextPlayer() + ResetGame()
// for the next one. With no session started (scene tested on its own) there is one player, "Player 1".
[Serializable]
public class PlayerSession
{
    public const int MaxPlayers = 4;
    public const int MaxNameLength = 16;

    public List<string> players = new List<string>();
    public int current;
    public bool turnRecorded;  // the current player's score is already in the leaderboard
    public List<LeaderboardEntry> roundEntries = new List<LeaderboardEntry>(); // this session's results, in play order
    public List<int> roundRanks = new List<int>();  // leaderboard rank of each round entry when it was saved (-1 = not in top)

    public bool IsStarted { get { return players.Count > 0; } }
    public int PlayerCount { get { return Math.Max(1, players.Count); } }
    public bool HasNextPlayer { get { return current + 1 < players.Count; } }

    public string CurrentName
    {
        get { return current >= 0 && current < players.Count ? players[current] : CleanName("", current + 1); }
    }

    // Starts a new session with 1 to 4 names (empty names become "Player N")
    public void Start(IList<string> names)
    {
        players.Clear();
        int count = names == null ? 1 : Math.Max(1, Math.Min(MaxPlayers, names.Count));
        for (int i = 0; i < count; i++)
            players.Add(CleanName(names != null && i < names.Count ? names[i] : "", i + 1));
        current = 0;
        turnRecorded = false;
        roundEntries.Clear();
        roundRanks.Clear();
    }

    public void Clear()
    {
        players.Clear();
        current = 0;
        turnRecorded = false;
        roundEntries.Clear();
        roundRanks.Clear();
    }

    // Saves the current player's result in the leaderboard once per turn. Returns the rank (0-based, -1 = not in top),
    // or the rank saved before if it was already recorded (Results opened twice).
    public int RecordTurn(ScoreResult r, GameManager gm)
    {
        if (turnRecorded) return roundRanks.Count > 0 ? roundRanks[roundRanks.Count - 1] : -1;
        LeaderboardEntry entry = Leaderboard.EntryFrom(CurrentName, r, gm);
        int rank = Leaderboard.Add(entry);
        roundEntries.Add(entry);
        roundRanks.Add(rank);
        turnRecorded = true;
        return rank;
    }

    // Moves to the next player. Returns false if this was the last one.
    public bool NextPlayer()
    {
        if (!HasNextPlayer) return false;
        current++;
        turnRecorded = false;
        return true;
    }

    // This session's results ordered like the leaderboard (best first)
    public List<LeaderboardEntry> RoundRanking()
    {
        List<LeaderboardEntry> list = new List<LeaderboardEntry>(roundEntries);
        list.Sort(Leaderboard.Compare); // ties: List.Sort is not stable, acceptable for 4 entries
        return list;
    }

    public static string CleanName(string name, int playerNumber)
    {
        string n = name == null ? "" : name.Trim();
        n = n.Replace("<", "").Replace(">", ""); // no TMP rich text tags
        if (n.Length > MaxNameLength) n = n.Substring(0, MaxNameLength).Trim();
        return n.Length == 0 ? "Player " + Math.Max(1, playerNumber) : n;
    }
}