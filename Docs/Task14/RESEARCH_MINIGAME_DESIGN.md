# Task 14 — Research minigame design: "Moon Photo Safari" + "Signal Catcher"

Author: claude_c (Cristina). **Status: approved by Cristina and implemented in `ia/claude_c/research-minigame`
(`Scripts/Mision/Research/`). Revision 2 after the first playtest: no alien, more targets from many directions, Earth always in view.**
Fits the existing code without changing it: `FlightPhases` (research turn in lunar orbit at 50 %), `CompleteMinigame`,
`MissionScoring` weights and gates, and the task 31 rule that research never aborts the flight.

## 1. The idea in one paragraph

Two short arcade games played back to back, in the same style as the maintenance minigame (hands-on, colourful, mouse only).
**Game 1 — Moon Photo Safari (30 s):** over the far side of the Moon, the player moves a camera viewfinder and snaps the science targets
that the bought instruments can detect: landmarks crossing the surface from both sides, radiation balls flying in from every side and
diagonal, and dark craters with water ice to scan. **Game 2 — Signal Catcher (25 s):** Earth moves around the sky above the Moon's
edge; the player aims the antenna to keep its beam on Earth while the photos fly home as little envelopes. **Album:** every photo taken,
the ones that reached Earth marked "Home", the rest greyed out "On board", plus a short lesson.

Why it is fun: aim-and-shoot with a flash, a busy fast challenge (balls from every direction), a patience challenge (hold to scan ice),
a skill challenge (keep the beam on a moving Earth) and a reward to look at (the album). No sudden failures.

## 2. Game 1 — Moon Photo Safari (30 s)

**Screen:** grey lunar surface with craters at the bottom, starry sky at the top (far side: no Earth in view).
A round **viewfinder** follows the mouse; captured photos slide into a bar under the play area.

**Targets** come only from the science instruments mounted on the rocket (`gm.rocketParts`). Each type has its own action:

| Instrument (catalog) | Target on screen | Action | Count | Data per target |
|---|---|---|---|---|
| `ART-SCI-LUNAR` — lunar observation payload (28 pts, 100 Mbit) | Landmarks crossing the surface, alternately from the right and from the left, in three lanes | **Click** while the target is inside the viewfinder. Centered inside the inner ring = **gold star** | 6 | ~16.7 Mbit |
| `ART-SCI-RAD` — radiation payload (20 pts, 25 Mbit) | Glowing radiation **balls** entering from all 8 directions (sides and diagonals) and crossing the screen | **Click** them | 12 | ~2.1 Mbit |
| `ART-SCI-HEAVY` — heavy multi-sensor package (55 pts, 250 Mbit) | Dark crater with blue water ice crossing the surface | **Hold** the click with the viewfinder on it for 1.5 s; a ring fills up | 3 | ~83 Mbit |

- Targets appear on a **fixed, evenly spaced schedule with fixed directions** (no luck): 18 targets with LUNAR + RAD, 21 with all three.
- Landmarks and ice take 3.5 s to cross; balls take up to 2.2 s (shorter paths are quicker).
- Each capture gives a camera flash and a thumbnail in the photo bar.
- **No instruments on board** (only possible when testing the minigame alone; a valid rocket always has them): nothing appears and a
  banner explains there is nothing to measure. Science results stay at 0.

## 3. Game 2 — Signal Catcher (25 s)

**Screen:** the Moon's edge at the bottom, Orion with a big **antenna dish**, black sky with Earth.

- **Earth moves** left-right and up-down at the same time (two gentle waves, 9 s and 5.5 s), **always above the Moon and always aimable**.
- The player **holds the mouse button and moves** to rotate the dish; a forgiving **beam** (±12°) turns green while it touches Earth.
- While the beam touches Earth, envelopes fly to Earth and the data is sent, oldest photo first. "Data home: X %" is shown live.
- **Sending speed** comes from planning: more data captured = more beam time needed (see 5). With the catalog's only antenna
  (`ART-COMMS-DS`, 180 kbit/s), an attentive player sends everything. It ends early when everything is home.
- **No antenna on board** (only when testing alone): no beam; banner *"No long-range antenna: the photos cannot reach Earth"*.

The real loss of signal behind the Moon is not played (playtest: it felt unfair to lose control of the aim); it stays as a fact in the album.

## 4. Album (end screen, then "Continue")

A grid of the photos: sent ones in colour with "Home", unsent ones greyed out with "On board". Gold-star photos have a small star.
Then two summary lines and the lessons:

- If an ice crater was scanned: *"Water ice! Some craters near the lunar poles never see sunlight and hide water ice: one day it could
  give astronauts water, air and rocket fuel."*
- Normal: *"Behind the Moon there is no contact with Earth: real spacecraft keep their data on board and send it when they see Earth again."*
- Little sent: *"You collected more than you could send. Real missions plan their observations and their contact time with Earth together."*
- No antenna / no instruments: an explanation of what was missing.

## 5. Results it produces (exact)

Let `B` = planned data = sum of `experimentDataBits` of the science instruments mounted (`RocketPhysics.DataBits`).
Each target carries `instrument bits / instrument target count`.

- `experimentFraction` = captured data bits / `B` (data-weighted, so the big ice scans matter more). 0 if `B` = 0.
- `downlinkFraction` = data bits that reached Earth / `B`, **capped at `experimentFraction`**. 0 without antenna.
- `raw` = clamp01( 0.45 × experimentFraction + 0.45 × downlinkFraction + 0.10 × goldStarShare )
  - `goldStarShare` = gold-star landmark photos / landmark photos taken (0 if none taken). Doing nothing = 0; perfect play = 1.
- **Beam time needed** to send everything: `(8 s + 10 s × DownlinkSeconds / maxDownlinkSeconds) × capturedBits / B`.
  LUNAR + RAD (694 s real) → 9.9 s; HEAVY alone (1,389 s) → 11.9 s; all three (2,083 s) → 13.8 s, out of 25 s.
- The hook writes `flight.experimentFraction` and `flight.downlinkFraction`, then calls
  `FlightPhases.CompleteMinigame(TrainingFocus.Research, raw)` **once**. `raw` never includes the scientist skill
  (the scoring multiplies once: 7 × raw × skill). A bad result never aborts the flight (task 31).

## 6. Fairness (valid rocket with LUNAR + RAD + antenna, 18 targets, beam needed 9.9 s)

| Player | Captures | Data home | experimentFraction | downlinkFraction | raw | Science gates |
|---|---|---|---|---|---|---|
| Perfect | all, all gold | all | 1.00 | 1.00 | 1.00 | pass |
| Attentive | 5/6 landmarks (2 gold), 10/12 balls | 85 % of captured | 0.83 | 0.71 | 0.73 | pass |
| Careless | 2/6 landmarks (no gold), 4/12 balls | 40 % of captured | 0.33 | 0.13 | 0.21 | pass (low score) |
| Does nothing | 0 | 0 | 0 | 0 | 0 | fail SCIENCE + NO_DOWNLINK |
| Photographs, never aims | all | 0 | 1.00 | 0 | 0.45–0.55 | fail NO_DOWNLINK |

(Any value above 0 passes the gates today; the 0.5 thresholds are a separate pending team decision and are not changed here.)

## 7. How it plugs in (no changes to existing systems)

- **One research turn** already exists (lunar orbit, 50 %). `ResearchFlightHook` registers with
  `RegisterMinigame(TrainingFocus.Research, Open)` and runs Photo Safari → Signal Catcher → Album as one sequence, then reports once.
- `Scripts/Mision/Research/`: `ResearchMinigame` (sequence + album), `PhotoSafari`, `SignalCatcher`, `ResearchSettings`, `ResearchMath`
  (pure formulas), `ResearchUI`, `ResearchFlightHook`. Editor: **SpaceApps → Research minigame → Create prefab / Add to open scene /
  Connect to launch scene / Tests**.
- uGUI + TextMeshPro, new Input System (mouse position and button), works in WebGL, no new scene, no art needed (plain shapes).

## 8. Settings (`ResearchSettings`, defaults)

| Setting | Default |
|---|---|
| Photo Safari duration | 30 s |
| Targets per instrument (LUNAR / RAD / HEAVY) | 6 / 12 / 3 |
| Crossing time (landmark, ice / ball across the full width) | 3.5 s / 2.2 s |
| Ice scan hold time | 1.5 s |
| Viewfinder radius / gold-star inner radius | 90 px / 35 px |
| Signal Catcher duration | 25 s |
| Earth motion (left-right / up-down period) | 9 s / 5.5 s |
| Beam half-width | 12° |
| Beam time needed (min + extra × downlink ratio) | 8 s + 10 s |
| raw weights (experiments / downlink / gold) | 0.45 / 0.45 / 0.10 |

## 9. Real vs simplified

**Real (sources below):** orbiters like NASA's Lunar Reconnaissance Orbiter photograph the surface (LROC camera), measure radiation
(CRaTER) and search for water ice in permanently shadowed polar craters (LAMP, LEND, Diviner); NASA's LCROSS impact (2009) confirmed
water there. When a spacecraft is behind the Moon it loses direct radio contact with Earth (Artemis I had a planned loss of signal
during its lunar flyby). Deep space data reaches Earth through the Deep Space Network (Goldstone, Madrid, Canberra), and the antenna
data rate limits how much can be sent. Spacecraft store data on board and send it later.

**Simplified for play:** an orbit of about two hours becomes 55 seconds; the beam time is a scaled abstraction of the real downlink time;
targets appear on a schedule and radiation is shown as balls you can see; one click = one observation; Earth's motion in the sky is
exaggerated; the game's instruments are fictional modules inspired by real ones, not Orion's actual payload.

Sources (verify links before publishing): science.nasa.gov/mission/lro · nasa.gov/mission/lcross ·
nasa.gov/communicating-with-missions/dsn · nasa.gov/mission/artemis-i
