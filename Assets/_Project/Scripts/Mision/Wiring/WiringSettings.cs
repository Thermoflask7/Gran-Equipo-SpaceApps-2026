using System;
using System.Collections.Generic;
using UnityEngine;

// Tuning of the wiring minigame (task 43). Signed: claude_c. Design: Docs/WiringMinigame/WIRING_MINIGAME_DESIGN.md.
// Asset: Data/Minigames/WiringSettings.asset (made by SpaceApps > Wiring minigame > Create prefab).
// Levels are text grids: '#' wall, '.' free, an uppercase letter = a socket (two per color).
// B = Power (blue), G = Life support (green), Y = Communications (yellow), P = Data (purple).
// Level order: the first wiring fault of a flight plays levels[0], the next one levels[1], and so on (the last one repeats).
[CreateAssetMenu(fileName = "WiringSettings", menuName = "SpaceApps/Wiring settings")]
public class WiringSettings : ScriptableObject
{
    [Serializable]
    public class Level
    {
        public string levelName = "Level";
        [TextArea(6, 10)] public string grid = "";
    }

    [Header("Time and result")]
    [Tooltip("Seconds to connect every pair")]
    public float timeLimit = 45f;
    [Tooltip("Solved in this time or less = full speed bonus and 3 stars")]
    public float fastSeconds = 20f;
    [Tooltip("Solved in this time or less = 2 stars")]
    public float twoStarSeconds = 35f;
    [Tooltip("Share of the result given by the connected pairs; the rest is the speed bonus (only when all are connected)")]
    [Range(0f, 1f)] public float connectedWeight = 0.8f;

    [Header("Flight")]
    [Tooltip("Point of a phase (0 to 1) where the wiring fault happens if no minigame opened in that phase")]
    [Range(0f, 1f)] public float faultAtProgress = 0.7f;
    [Tooltip("Events (asset name or eventName) whose Engineering minigame is wiring instead of maintenance")]
    public List<string> wiringEvents = new List<string> { "EV-DE-02", "Unstable telemetry signal" };

    [Header("Text")]
    [TextArea] public string instruction = "Drag each cable to the socket of the same color. Cables can't cross!";
    [TextArea] public string lesson = "Real spacecraft give every system its own path, and important ones get a backup path, so one broken wire can't turn everything off.";

    [Header("Sound (optional, the game works silent)")]
    public AudioClip stepClip;
    public AudioClip blockedClip;
    public AudioClip connectClip;
    public AudioClip solvedClip;

    [Header("Levels (in play order)")]
    public List<Level> levels = DefaultLevels();

    static WiringSettings fallback;

    // Used when no asset is assigned (default values)
    public static WiringSettings Default
    {
        get
        {
            if (fallback == null) fallback = CreateInstance<WiringSettings>();
            return fallback;
        }
    }

    // The three hand-made levels of the design doc, checked solvable (tests: SpaceApps > Wiring minigame > Tests)
    public static List<Level> DefaultLevels()
    {
        return new List<Level>
        {
            new Level
            {
                levelName = "The sketch",
                grid =
                    "# G # Y B # B\n" +
                    ". . # . . # .\n" +
                    ". . # . . . .\n" +
                    ". . # . # # .\n" +
                    ". . . . . # .\n" +
                    ". # # # . . .\n" +
                    "G . . . # . Y",
            },
            new Level
            {
                levelName = "Full rewire",
                grid =
                    "B . . G . . Y\n" +
                    "P # . # # . .\n" +
                    ". # . . # . .\n" +
                    ". . # . # G .\n" +
                    ". . # . # # .\n" +
                    ". . # . . . .\n" +
                    "P # # . # B Y",
            },
            new Level
            {
                levelName = "First connection",
                grid =
                    "B . . G . Y\n" +
                    ". # . . # .\n" +
                    ". # # . # .\n" +
                    ". . . . # .\n" +
                    ". # . # # .\n" +
                    "B . . G . Y",
            },
        };
    }
}
