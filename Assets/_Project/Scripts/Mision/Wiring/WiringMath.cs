using UnityEngine;

// Pure formulas of the wiring minigame (task 43). Signed: claude_c. No UI and no GameManager: easy to test.
public static class WiringMath
{
    // Player result 0..1 (RAW: the engineer skill is applied later, once, by the scoring).
    //   raw = connectedWeight * pairsConnected / pairs  +  (all connected ? (1 - connectedWeight) * speed : 0)
    //   speed = 1 when solved in fastSeconds or less, 0 at timeLimit (linear in between).
    // 0 when nothing is connected, never above 1.
    public static float Raw(int connected, int pairs, bool solved, float seconds, WiringSettings s)
    {
        if (s == null) s = WiringSettings.Default;
        if (pairs <= 0 || connected <= 0) return 0f;
        float share = Mathf.Clamp01((float)connected / pairs);
        float raw = s.connectedWeight * share;
        if (solved && connected >= pairs) raw += (1f - s.connectedWeight) * Speed(seconds, s);
        return Mathf.Clamp01(raw);
    }

    public static float Speed(float seconds, WiringSettings s)
    {
        if (s == null) s = WiringSettings.Default;
        float span = Mathf.Max(0.01f, s.timeLimit - s.fastSeconds);
        return Mathf.Clamp01((s.timeLimit - seconds) / span);
    }

    // 0 stars if not solved; 1 = solved, 2 = solved in twoStarSeconds, 3 = solved in fastSeconds
    public static int Stars(bool solved, float seconds, WiringSettings s)
    {
        if (s == null) s = WiringSettings.Default;
        if (!solved) return 0;
        int stars = 1;
        if (seconds <= s.twoStarSeconds) stars++;
        if (seconds <= s.fastSeconds) stars++;
        return stars;
    }

    // Level for the n-th wiring fault of the flight (0 = first). The last level repeats.
    public static int LevelIndex(int solvedBefore, int levelCount)
    {
        if (levelCount <= 0) return 0;
        return Mathf.Clamp(solvedBefore, 0, levelCount - 1);
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
            case 'R': return new Color(1f, 0.45f, 0.35f);
            default: return Color.white;
        }
    }
}
