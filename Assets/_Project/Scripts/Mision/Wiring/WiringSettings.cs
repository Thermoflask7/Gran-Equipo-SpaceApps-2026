using System;
using System.Collections.Generic;
using UnityEngine;

// Tuning of the wiring minigame (task 43). Signed: claude_c. Design: Docs/WiringMinigame/WIRING_MINIGAME_DESIGN.md.
// Asset: Data/Minigames/WiringSettings.asset (made by SpaceApps > Wiring minigame > Create prefab).
// One round = boardsPerRound boards in a row (3, 4 and 5 colors) against ONE shared timer.
// A board is fixed when every color is connected (empty squares are fine); cables cannot cross.
// Boards are text grids: '.' free, '#' wall (optional), an uppercase letter = a socket (two per color).
// B = Power (blue), G = Life support (green), Y = Comms (yellow), P = Data (purple), R = Heaters (red).
// Round r plays boards[r * boardsPerRound + i] (wrapping around), so a second wiring fault gets new boards.
[CreateAssetMenu(fileName = "WiringSettings", menuName = "SpaceApps/Wiring settings")]
public class WiringSettings : ScriptableObject
{
    [Serializable]
    public class Level
    {
        public string levelName = "Board";
        [TextArea(5, 9)] public string grid = "";
    }

    [Header("Time and result")]
    [Tooltip("Seconds for the whole round (all the boards)")]
    public float roundSeconds = 40f;
    [Tooltip("Whole round solved in this time or less = full speed bonus")]
    public float fastSeconds = 20f;
    [Tooltip("Boards played in a row in one round")]
    public int boardsPerRound = 3;
    [Tooltip("Share of the result given by the boards' progress; the rest is the speed bonus (only when every board is fixed)")]
    [Range(0f, 1f)] public float progressWeight = 0.85f;

    [Header("Flight")]
    [Tooltip("Point of a phase (0 to 1) where the wiring fault happens if no minigame opened in that phase")]
    [Range(0f, 1f)] public float faultAtProgress = 0.7f;
    [Tooltip("Events (asset name or eventName) whose Engineering minigame is wiring instead of maintenance")]
    public List<string> wiringEvents = new List<string> { "EV-DE-02", "Unstable telemetry signal" };

    [Header("Text")]
    [TextArea] public string howToPlay = "Connect each color to its twin. Cables can't cross!";
    [TextArea] public string lesson = "Real spacecraft give every system its own path, and important ones get a backup path, so one broken wire can't turn everything off.";

    [Header("Sound (optional, the game works silent)")]
    public AudioClip stepClip;
    public AudioClip blockedClip;
    public AudioClip connectClip;
    public AudioClip solvedClip;

    [Header("Boards (in play order: 3, 4, 5 colors per round)")]
    public List<Level> boards = DefaultBoards();

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

    // Six easy 5x5 boards, two rounds of 3 / 4 / 5 colors. Every one can be solved
    // (checked by SpaceApps > Wiring minigame > Tests).
    public static List<Level> DefaultBoards()
    {
        return new List<Level>
        {
            Board("Power on",
                ". G . . .",
                ". Y . Y .",
                ". . . . .",
                ". B . G B",
                ". . . . ."),
            Board("Data line",
                "P . . . G",
                ". . . . B",
                "B Y P G .",
                ". . . Y .",
                ". . . . ."),
            Board("Full panel",
                "B . R Y G",
                ". R P . .",
                ". P . . .",
                ". Y . . .",
                ". B G . ."),
            Board("Comms check",
                ". . B . .",
                ". Y . G B",
                ". . . . .",
                ". . . Y .",
                ". . . G ."),
            Board("Backup bus",
                ". . . . .",
                ". . G B .",
                ". . . . .",
                ". G Y P Y",
                ". . B . P"),
            Board("Heater loop",
                "P . R . .",
                "Y . P R .",
                ". . . . .",
                ". . . G Y",
                ". G B . B"),
        };
    }

    static Level Board(string name, params string[] rows)
    {
        return new Level { levelName = name, grid = string.Join("\n", rows) };
    }
}
