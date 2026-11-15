# Wiring minigame: "Reconnect the systems"

By claude_c for Cristina, 2026-11-15. Brief: [WIRING_MINIGAME_PROMPT.md](WIRING_MINIGAME_PROMPT.md).
**Built** (task 43, `ia/claude_c/wiring-minigame`). The code is in `Assets/_Project/Scripts/Mision/Wiring/`:
- `WiringBoard`: board rules
- `WiringMath`: result formula
- `WiringMinigame`: UI
- `WiringFlightHook`: wiring fault + Engineering dispatcher
- `WiringSettings`: boards and tuning

Unity step: **SpaceApps → Wiring minigame → Connect to launch scene**, then save `02_Lanzamiento`. Tests: **SpaceApps → Wiring minigame → Tests**.

**Revision 2 (after Cristina's playtest).** A single board was too quick, so one opening is now a **round of 3 small panels in a
row**. Each panel adds one system (3, then 4, then 5 colors), and the player has **40 s for the whole round**. A Flow Free version,
where every square had to be filled, was tried and then dropped at the team's request: **empty squares are fine**, and cables block
each other instead of cutting.

## 1. Summary

A **wiring fault** on Orion: the cables of the ship's systems came loose. The engineer drags a cable from each socket to the
socket of the same color on a 5×5 panel. Cables cannot cross. There are 3 panels in a row, each with one more color, and 40 s in
total. Instruction: **"Connect each color to its twin. Cables can't cross!"**

## 2. Role and moment (Cristina's decision: "it appears when there is no minigame, like a fault")

- **Role: `Engineering`.** It is a repair, so it uses the engineer's skill.
- **When:** at 70 % of a flight phase, **if no minigame has opened in that phase**, a "Wiring fault" happens and this minigame
  opens. Today Departure always has piloting and Lunar orbit always has research, so in practice the fault shows up in **Return**,
  and in any phase whose planned minigame was skipped.
- **Also on one event:** the bad event **EV-DE-02 "Unstable telemetry signal"** opens the wiring panels instead of maintenance.
  That one keeps the event's severity (Serious: a critical result gets a second attempt).
- **Fitting `FlightPhases` (one handler per role):** `WiringFlightHook` takes the Engineering role in `Start`. It opens wiring for
  its own fault and for EV-DE-02, and calls `MaintenanceFlightHook.Open()` for every other Engineering event. It reports with
  `CompleteMinigame(Engineering, raw)`, raw and only once. `FlightPhases` does not change.
- **Severity: `Minor` (Cristina).** The game invents this fault, so it only costs points and **never aborts**, even with raw 0.
- **Which panels:** the first round of a flight plays boards 1–3 of `WiringSettings.boards`, and the next round boards 4–6
  (wrapping around). A second fault therefore gets new panels.

## 3. Rules (plain words)

1. **Draw:** press a socket (or the end of your cable) and drag. The cable follows the mouse square by square (up, down, left or
   right).
2. **Blocked:** walls, the sockets of other colors and other colors' cables stop the cable, and the blocking square flashes red.
   Cables never cross.
3. **Back up:** dragging back over your own cable shortens it.
4. **Erase / redraw:** press a socket to start that cable again, or right-click a cable to erase it. There is no penalty.
5. **Panel fixed:** every color is connected. **Empty squares are fine.** A "PANEL FIXED!" banner shows, and the next panel comes
   in (the timer pauses for the banner).
6. **Round:** 3 panels, one shared **40 s** timer. The round ends when all 3 are fixed or time runs out.

## 4. Boards

The boards are 5×5 squares, about 120 px each, with no walls; the wall code (`#`) is still supported for future boards. Sockets
are big round plugs with a short label (PWR, AIR, COM, DATA, HEAT). The tests check the number of colors on each board and that
every board can be solved; the "ways" are the solutions counted by the solver.

| Round | Panel 1 (3 colors) | Panel 2 (4 colors) | Panel 3 (5 colors) |
|---|---|---|---|
| 1 | "Power on" (43 ways) | "Data line" (4 ways) | "Full panel" (1 way) |
| 2 | "Comms check" (57 ways) | "Backup bus" (2 ways) | "Heater loop" (4 ways) |

```
Power on      Data line     Full panel    Comms check   Backup bus    Heater loop
. G . . .     P . . . G     B . R Y G     . . B . .     . . . . .     P . R . .
. Y . Y .     . . . . B     . R P . .     . Y . G B     . . G B .     Y . P R .
. . . . .     B Y P G .     . P . . .     . . . . .     . . . . .     . . . . .
. B . G B     . . . Y .     . Y . . .     . . . Y .     . G Y P Y     . . . G Y
. . . . .     . . . . .     . B G . .     . . . G .     . . B . P     . G B . B
```

## 5. Result formula

- **Each panel:** progress = pairs connected / pairs, so a fixed panel = 1. Panels not reached count 0.
- **Speed:** `speed = clamp01((40 − t) / (40 − 20))`, so finishing the round in 20 s or less gives full speed.
- **Result:** `raw = 0.85 × average(panel progress) + (all 3 fixed ? 0.15 × speed : 0)`.

The result is 0 when nothing is connected and never above 1. It is raw: the game applies the engineer skill once, later.
Stars = panels fixed (0–3).

| Player behaviour | raw | Band (task 31) |
|---|---|---|
| All 3 panels fixed in ≤ 20 s | 1.00 | Excellent |
| All 3 fixed at 30 s | 0.93 | Excellent |
| All 3 fixed at 40 s | 0.85 | Good |
| 2 fixed + half of the third | 0.71 | Good |
| 1 fixed, nothing else | 0.28 | Poor |
| Nothing connected | 0.00 | Critical, but `Minor`: the flight continues |

## 6. Feedback for kids

- **Drawing:** the cable glows in its color.
- **Blocked:** the blocking square flashes red.
- **Pair connected:** both sockets pulse, and that system's lamp turns green ("ONLINE").
- **Under the board:** "Systems online: 2 / 4".
- **Timer:** a bar that goes from green to amber to red.
- **Panel fixed:** a big "PANEL 1 FIXED!" banner, then the next panel.
- **End:** "ALL SYSTEMS ONLINE!" or "TIME'S UP!", one star per fixed panel, and the lesson. It never says "FAILED".
- **Sound:** the clips are optional fields (task 27), and the game works silent.

## 7. Theme and lesson

- **Blue = Power**
- **Green = Life support**
- **Yellow = Comms**
- **Purple = Data**
- **Red = Heaters**

**Lesson (end screen):** *"Real spacecraft give every system its own path, and important ones get a backup path, so one broken
wire can't turn everything off."*

## 8. UI layout (1920×1080)

```
+----------------------------------------------------------------------------------+
|  WIRING FAULT                                                  ███████░░  32 s   |
|  Return · Panel 2 of 3: Data line · Engineer: Name                               |
|           "Connect each color to its twin. Cables can't cross!"                  |
|   SYSTEMS            +------------------------+     TIPS                         |
|   ● Power   ONLINE   |                        |     • Press a socket and drag    |
|   ● Life    OFFLINE  |     5×5 board, 600 px  |     • Go around other cables     |
|   ● Comms   OFFLINE  |                        |     • Drag back to undo          |
|   ● Data    ONLINE   +------------------------+     • Click socket / right-click |
|                         Systems online: 2 / 4                                    |
+----------------------------------------------------------------------------------+
```
It is a full-screen overlay on the launch canvas, like the other minigames, and the flight is paused while it is open.

## 9. Real vs simplified

**Real (sources):**
- **Power:** Orion's European Service Module makes about **11.2 kW** with 4 solar array wings. The power goes out on **four 120 V
  buses**, which "can be combined or even split for troubleshooting", and the module has two 28 V buses of its own
  ([ESA](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/Orion/European_Service_Module_Power), [ESA Orion blog](https://blogs.esa.int/orion/?p=12610)).
  **Two power conditioning and distribution units** manage it ([NASA NTRS](https://ntrs.nasa.gov/api/citations/20210024750/downloads/AIAA_SciTech_Furin_Orion_Power_Transfer.pdf)).
- **Data:** data runs on a **redundant onboard network (Time-Triggered Ethernet)** between **two vehicle management computers**,
  with backup flight software on different hardware
  ([NASA NTRS](https://ntrs.nasa.gov/api/citations/20100040584/downloads/20100040584.pdf), [Tech Briefs](https://www.techbriefs.com/component/content/article/20298-asics-support-orion-s-onboard-data-network), [TechRadar quoting NASA](https://www.techradar.com/pro/we-can-lose-three-computers-and-still-ride-through-inside-the-8-cpu-brain-of-nasas-artemis-ii-that-votes-on-every-move)).

**Game simplifications:**
- **Re-plugging:** astronauts do not re-plug cables in flight. Real faults are handled by switching buses and resetting electronic
  breakers, so dragging the cables is a metaphor for "rerouting".
- **Redundancy:** there is one cable per system on the board. Redundancy only appears in the lesson.
- **Board rules:** colors, the grid and "no crossing" are game rules.

## 10. Decisions

1. **Severity:** `Minor`, never aborts (Cristina).
2. **Round:** 3 easy panels with 3, 4 and 5 colors, 40 s in total (Cristina, after playtest).
3. **Empty squares allowed, cables block:** the Flow Free version was dropped (Cristina, after playtest).
4. **Phase that already had a minigame:** no fault. At most one extra minigame per phase keeps the flight short.
5. **Electrical event:** EV-DE-02 opens wiring instead of maintenance, so both engineering minigames appear in a game.
6. **When:** at 70 % of the phase, after the phase event (50 %).
7. **Sound:** optional clips in `WiringSettings`, because there is no audio yet (task 27).
