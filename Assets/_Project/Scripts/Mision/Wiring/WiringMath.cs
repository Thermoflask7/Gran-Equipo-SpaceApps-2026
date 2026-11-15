using UnityEngine;

// Pure formulas of the wiring minigame (task 44). Signed: claude_c. No UI and no GameManager: easy to test.
public static class WiringMath
{
    // Progress on ONE board, 0..1: share of its pairs that are connected (all connected = fixed = 1)
    public static float BoardProgress(int connected, int pairs)
    {
        return pairs > 0 ? Mathf.Clamp01((float)connected / pairs) : 0f;
    }

    // Player result 0..1 for a whole round (RAW: the engineer skill is applied later, once, by the scoring).
    //   raw = progressWeight * average(board progress, boards not reached = 0)
    //       + (every board fixed ? (1 - progressWeight) * speed : 0)
    //   speed = 1 when the round is done in fastSeconds or less, 0 at roundSeconds (linear in between).
    // 0 when nothing was done, never above 1.
    public static float Raw(float[] progress, bool allSolved, float seconds, WiringSettings s)
    {
        if (s == null) s = WiringSettings.Default;
        if (progress == null || progress.Length == 0) return 0f;
        float sum = 0f;
        foreach (float p in progress) sum += Mathf.Clamp01(p);
        if (sum <= 0f) return 0f;
        float raw = s.progressWeight * sum / progress.Length;
        if (allSolved) raw += (1f - s.progressWeight) * Speed(seconds, s);
        return Mathf.Clamp01(raw);
    }

    public static float Speed(float seconds, WiringSettings s)
    {
        if (s == null) s = WiringSettings.Default;
        float span = Mathf.Max(0.01f, s.roundSeconds - s.fastSeconds);
        return Mathf.Clamp01((s.roundSeconds - seconds) / span);
    }

    // One star per fixed board (0 to 3)
    public static int Stars(int boardsSolved, int boards)
    {
        return Mathf.Clamp(boardsSolved, 0, Mathf.Max(0, boards));
    }

    // Board played as number i (0-based) of round number `round` (0 = first wiring fault). Wraps around the list.
    public static int BoardIndex(int round, int i, int perRound, int boardCount)
    {
        if (boardCount <= 0) return 0;
        int n = Mathf.Max(0, round) * Mathf.Max(1, perRound) + Mathf.Max(0, i);
        return n % boardCount;
    }

    // ---------- Systems (colors) ----------

    public static string SystemName(char color)
    {
        switch (color)
        {
            case 'B': return "Power";
            case 'G': return "Life support";
            case 'Y': return "Comms";
            case 'P': return "Data";
            case 'R': return "Heaters";
            default: return "System " + color;
        }
    }

    // Short label shown on the socket
    public static string SocketLabel(char color)
    {
        switch (color)
        {
            case 'B': return "PWR";
            case 'G': return "AIR";
            case 'Y': return "COM";
            case 'P': return "DATA";
            case 'R': return "HEAT";
            default: return color.ToString();
        }
    }

    public static Color SystemColor(char color)
    {
        switch (color)
        {
            case 'B': return new Color(0.25f, 0.6f, 1f);
            case 'G': return new Color(0.3f, 0.9f, 0.4f);
            case 'Y': return new Color(1f, 0.85f, 0.2f);
            case 'P': return new Color(0.75f, 0.45f, 1f);
            case 'R': return new Color(1f, 0.4f, 0.35f);
            default: return Color.white;
        }
    }
}
