# Wiring minigame: "Reconnect the systems" (design proposal)

By claude_c for Cristina, 2026-11-15. Brief: [WIRING_MINIGAME_PROMPT.md](WIRING_MINIGAME_PROMPT.md).
**Built** (task 43, `ia/claude_c/wiring-minigame`): code in `Assets/_Project/Scripts/Mision/Wiring/` (`WiringBoard` rules,
`WiringMath` formula, `WiringMinigame` UI, `WiringFlightHook` fault + Engineering dispatcher, `WiringSettings` levels/tuning).
Unity step: **SpaceApps → Wiring minigame → Connect to launch scene**, then save `02_Lanzamiento`. Tests: **SpaceApps → Wiring minigame → Tests**.

## 1. Summary

A **wiring fault** on Orion: the power, life support and comms lines came loose. The engineer drags a cable of each color from
its socket to the matching socket, through the free corridors of a 7×7 panel. Cables cannot go through walls or cross each other
(Flow Free rules, Among Us feeling). Instruction: **"Drag each cable to the socket of the same color. Cables can't cross!"**
It takes 30–45 s, and there are 3 hand-made levels that we checked are solvable.

## 2. Role and moment (Cristina's decision: "it appears when there is no minigame, like a fault")

- **Role: `Engineering`.** It is a repair, so it uses the engineer's skill.
- **When:** at about 70 % of a flight phase, **if no minigame has opened in that phase**, a "Wiring fault" happens and this
  minigame opens. Today Departure always has piloting and Lunar orbit always has research, so in practice the fault shows up in
  **Return**, and in any phase where the planned minigame was skipped. Each flight phase then has some hands-on action.
- **Which level** (fixed order, no randomness): the first wiring fault of a flight uses **level 2 (the sketch)**, the second uses
  **level 3 (full rewire)** and any later one uses level 1. Most flights have one fault (in Return), so the player sees the sketch
  level; a flight that also had a telemetry fault in Departure gets the harder level on the way home.
- **Also on one event:** the bad event **EV-DE-02 "Unstable telemetry signal"** opens the wiring panel instead of maintenance
  (it is about a data line, so reconnecting cables fits). That one keeps the event's severity (Serious: a critical result gets a
  second attempt).
- **Fitting `FlightPhases` (one handler per role):** Engineering is already registered by `MaintenanceFlightHook`. We propose
  one **Engineering dispatcher** hook that opens the wiring panel when a wiring fault is pending and the maintenance minigame
  otherwise. A small `WiringFaultHook` watches `PhaseProgress`, sets the "pending" flag and calls `RequestMinigame(Engineering,
  severity, "Wiring fault")`. Both report with `CompleteMinigame(Engineering, raw)`, raw and only once. `FlightPhases` itself does
  not change.
- **Severity: `Minor` (decided by Cristina).** The game invents this fault, so it only costs points and **never aborts**, even
  with a raw result of 0 (`Docs/Task31/MISSION_FAILURE_RULES.md`: Minor incidents always continue).

## 3. Rules (plain words)

1. **Draw:** press on a socket (or on the end of your cable) and drag. The cable follows the mouse cell by cell, up, down, left
   or right, never diagonally.
2. **Blocked:** a cable cannot enter walls, sockets of another color, or cells that already hold a cable of another color.
   Dragging into a blocked cell does nothing: the cable just stops there.
3. **Back up:** dragging back over your own cable shortens it, like Flow Free.
4. **Erase / redraw:** pressing on any socket of a color erases that color's cable and starts it again from there. A right-click
   on a cable erases it. No penalty and no limit.
5. **Connected:** a pair counts once its cable joins its two sockets. Releasing the mouse keeps a half-drawn cable on the board.
6. **Solved:** every pair is connected. Filling every free cell is **not** required: that keeps it easy for kids.
7. **Time:** 45 s. The game ends when the board is solved or time runs out, then shows a summary and a **Continue** button.

## 4. Board and levels

A 7×7 grid (6×6 for the tutorial), about 90 px cells, centered on screen. **Walls** are dark red hatched blocks (hot pipes and
moving parts). **Sockets** are big round plugs in the color of their system, each with an icon and a short label, placed on the
border like connectors on a panel. **Cables** are thick rounded lines.
Legend: `#` wall, `.` free, uppercase = socket, lowercase = solution cable. `B` blue, `G` green, `Y` yellow, `P` purple.
We checked every level with a brute-force solver: it lists the number of valid solutions for each level.

**Level 1: "First connection" (tutorial, 6×6, 3 pairs, 2 solutions).** Blue and yellow are straight, so green teaches turning.
```
Puzzle            Solution
B . . G . Y       B . . G . Y
. # . . # .       b # . g # y
. # # . # .       b # # g # y
. . . . # .       b . g g # y
. # . # # .       b # g # # y
B . . G . Y       B . g G . Y
```

**Level 2: "The sketch" (7×7, 3 pairs, 16 solutions).** It matches the hand sketch: all the "1" sockets are on the top edge,
blue 2 is in the top-right corner, green 2 bottom-left and yellow 2 bottom-right. Walls form long blocks and L shapes. The trap:
if blue takes yellow's corridor (column 4), yellow has no way down.
```
Puzzle              Solution
# G # Y B # B       # G # Y B # B
. . # . . # .       . g # y b # b
. . # . . . .       . g # y b b b
. . # . # # .       . g # y # # .
. . . . . # .       g g . y y # .
. # # # . . .       g # # # y y .
G . . . # . Y       G . . . # y Y
```

**Level 3: "Full rewire" (7×7, 4 pairs, 4 solutions).** The aha moment: blue has to go the long way around the middle wall.
```
Puzzle              Solution
B . . G . . Y       B b b G g g Y
P # . # # . .       P # b # # g y
. # . . # . .       p # b b # g y
. . # . # G .       p . # b # G y
. . # . # # .       p . # b # # y
. . # . . . .       p . # b b b y
P # # . # B Y       P # # . # B Y
```

## 5. Difficulty, result formula and fairness

There are 3 pairs (4 in level 3) and a 45 s limit. The levels are fixed, so none can be impossible.

**Formula:** `connectedShare = pairsConnected / pairsTotal`.
`speed = clamp01((45 − t) / (45 − 20))`, where t is the time it took to solve, so 20 s or less gives full speed.
`raw = 0.8 × connectedShare + (all connected ? 0.2 × speed : 0)`.
The result is 0 when nothing is connected and it is never above 1. It is the player's raw result: the game applies the skill
once, later.

| Player behaviour | raw | Band (task 31) |
|---|---|---|
| Solved in ≤ 20 s | 1.00 | Excellent |
| Solved at 35 s | 0.88 | Good |
| Solved at the last second (45 s) | 0.80 | Good |
| 2 of 3 connected at time-out | 0.53 | Acceptable |
| 3 of 4 connected (level 3) | 0.60 | Acceptable |
| 1 of 3 connected | 0.27 | Poor |
| Nothing connected | 0.00 | Critical, but `Minor`: the flight continues |

## 6. Feedback for kids

- **While dragging:** the cable glows in its color, and each new cell plays a soft "tick".
- **Blocked:** the cable stops, the blocking cell flashes red for 0.2 s, and a dull "bonk" plays. Nothing is lost.
- **Pair connected:** both sockets light up, a pulse of light runs along the cable, a "click + zap" sound plays, and that system's
  status lamp turns green ("POWER ONLINE").
- **Timer:** a bar that goes from green to amber to red, ticking in the last 10 s.
- **End:** "ALL SYSTEMS ONLINE!" with 1–3 stars (connected, + under 35 s, + under 20 s) and the lesson below. If time runs out:
  "Mission Control rerouted the rest. 2 of 3 systems reconnected", never a scary "FAILED".

## 7. Theme and lesson

- **Blue = Power** (from the solar wings to the batteries and systems).
- **Green = Life support** (air and water for the crew).
- **Yellow = Communications** (antenna to Earth).
- **Purple = Data** (computers and sensors, level 3).
- Walls are labeled as hot engine lines and moving solar-wing joints.

**Lesson (end screen):** *"Real spacecraft give every system its own path, and important ones get a backup path, so one broken
wire can't turn everything off."*

## 8. UI sketch (1920×1080)

```
+----------------------------------------------------------------------------------+
|  ⚡ WIRING FAULT – Return            [Engineer: Name]          ⏱ ███████░░ 32 s  |
|  "Drag each cable to the socket of the same color. Cables can't cross!"          |
|                                                                                  |
|   SYSTEMS            +--------------------------------+                          |
|   ● Power      OFF   |                                |   TIP (fades after 5 s): |
|   ● Life sup.  OFF   |     7×7 board, ~640×640 px     |   "Stuck? Click a socket |
|   ● Comms      ON ✓  |     (centered)                 |    to redraw its cable." |
|   (lamps turn green) |                                |                          |
|                      +--------------------------------+                          |
|                                    [ Continue ]  (only at the end)               |
+----------------------------------------------------------------------------------+
```
The panel is a full-screen overlay on the launch canvas, like the maintenance and research minigames, and the flight is paused
while it is open.

## 9. Real vs simplified

**Real (sources):**
- Orion's European Service Module makes power with **4 solar array wings, about 11.2 kW** in total. The power is sent out on
  **four 120 V buses**, which "can be combined or even split for troubleshooting". The module also has two 28 V buses of its own
  ([ESA: European Service Module power](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/Orion/European_Service_Module_Power), [ESA Orion blog](https://blogs.esa.int/orion/?p=12610)).
- **Two power conditioning and distribution units** manage that power ([NASA NTRS, Orion power transfer](https://ntrs.nasa.gov/api/citations/20210024750/downloads/AIAA_SciTech_Furin_Orion_Power_Transfer.pdf)).
- Data runs on a **redundant onboard network (Time-Triggered Ethernet)** between **two vehicle management computers**. There is
  also a backup flight software system on different hardware ([NASA NTRS](https://ntrs.nasa.gov/api/citations/20100040584/downloads/20100040584.pdf), [Tech Briefs](https://www.techbriefs.com/component/content/article/20298-asics-support-orion-s-onboard-data-network), [TechRadar quoting NASA](https://www.techradar.com/pro/we-can-lose-three-computers-and-still-ride-through-inside-the-8-cpu-brain-of-nasas-artemis-ii-that-votes-on-every-move)).
- **For a child:** "Orion has several separate power and data lines, so if one breaks, another one keeps the lights and the
  radio working."

**Game simplifications:**
- Astronauts do not re-plug cables in flight. Real faults are handled by switching buses and resetting electronic breakers. The
  dragging is a metaphor for "rerouting".
- One cable per system and no backups on the board. The redundancy only appears in the lesson text, to keep it at 3 pairs.
- The colors, walls and "no crossing" rule are game rules. In reality cables are routed to stay away from heat and moving parts,
  which inspired the walls.

**Why it is fun** (reviews, not developer data): the rules are simple but the boards get harder, the no-crossing rule makes you
plan, and you can restart freely with no penalty
([Common Sense Media](https://www.commonsensemedia.org/app-reviews/flow-free), [iD Tech](https://wwwapi.idtech.com/blog/the-bad-and-good-of-flow-free-and-bridges), [App Store page](https://apps.apple.com/ca/app/flow-free/id526641427?uo=4)).
Among Us wires add the instant "click" when a wire is connected.

## 10. Decisions (the open questions, answered by claude_c for Cristina)

1. **Severity:** `Minor`, never aborts (Cristina).
2. **Level order:** first fault = level 2 (the sketch, the most fun to solve), second = level 3, later ones = level 1.
3. **Phase that already had a minigame:** no fault. At most one "extra" minigame per phase keeps the flight short.
4. **Electrical events:** yes, EV-DE-02 "Unstable telemetry signal" opens wiring instead of maintenance, so both engineering
   minigames show up in a game.
5. **When:** at 70 % of the phase, after the phase event (50 %), so a bad event's minigame always comes first.
6. **Built** by claude_c in `ia/claude_c/wiring-minigame` (task 43). A human with Unity runs
   **SpaceApps → Wiring minigame → Connect to launch scene** and saves `02_Lanzamiento`.
7. **Sound:** the project has no audio yet (task 27), so the clips are optional fields in `WiringSettings`; the minigame works silent.
