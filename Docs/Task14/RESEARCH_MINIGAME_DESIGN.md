# Task 14 — Research minigame design: "Moon Photo Safari" + "Signal Catcher"

Author: claude_c (Cristina). **Status: approved by Cristina; main version implemented in `ia/claude_c/research-minigame` (`Scripts/Mision/Research/`). Not yet tested in Unity.**
Fits the existing code without changing it: `FlightPhases` (research turn in lunar orbit at 50 %), `CompleteMinigame`,
`MissionScoring` weights and gates, and the task 31 rule that research never aborts the flight.

## 1. The idea in one paragraph

Two short arcade games played back to back, in the same style as the maintenance minigame (hands-on, colourful, mouse only).
**Game 1 — Moon Photo Safari (30 s):** Orion flies over the far side of the Moon; the player moves a camera viewfinder and snaps the
science targets that the bought instruments can detect (landmarks, radiation sparks, ice in dark craters). Sometimes a cartoon
**little alien** peeks out of a crater and waves: snap it for a bonus. **Game 2 — Signal Catcher (25 s):** Earth rises over the Moon's
edge; the player drags the antenna to keep its beam on Earth while the photos fly home as little envelopes. Halfway, the Moon hides
Earth for a few seconds ("Loss of signal"). **Album:** the player sees every photo taken, the ones that reached Earth marked with a
check, the rest greyed out, and the alien photo with a "surprise photo" stamp that teaches the real fact.

Why it is fun: aim-and-shoot with a flash, a fast challenge (sparks), a patience challenge (hold to scan ice), a rare surprise
(alien), a skill challenge (keep the beam on a moving Earth) and a reward to look at (the album). No sudden failures.

## 2. Game 1 — Moon Photo Safari (30 s)

**Screen:** a strip of grey lunar surface scrolls slowly from right to left under a starry sky (far side: no Earth in view).
A round **viewfinder** follows the mouse. Top bar: timer, "Photos: 3 / 12", small instrument icons.

**Targets** come only from the science instruments mounted on the rocket (`gm.rocketParts`). Each type has its own action:

| Instrument (catalog) | Target on screen | Action | Count | Data per target |
|---|---|---|---|---|
| `ART-SCI-LUNAR` — lunar observation payload (28 pts, 100 Mbit) | Landmarks: big crater, mountain, lava channel, landing-site area, boulder field | **Click** while the target is inside the viewfinder. Centered inside the inner ring = **gold star** photo | 5 | 20 Mbit |
| `ART-SCI-RAD` — radiation payload (20 pts, 25 Mbit) | Glowing radiation **sparks** that zip diagonally across the sky | **Click** on the spark (fast, like catching fireflies) | 6 | ~4.2 Mbit |
| `ART-SCI-HEAVY` — heavy multi-sensor package (55 pts, 250 Mbit) | Dark crater with a blue twinkle (water ice) | **Hold** the click with the viewfinder on it for 1.5 s; a ring fills up | 3 | ~83 Mbit |

- Targets appear on a **fixed, evenly spaced schedule** (no luck): with all three instruments there are 14 targets in 30 s.
  Each one stays on screen ~3–4 s while it scrolls past; sparks ~1.5 s (they are the "fast" challenge).
- Each captured target gives a camera flash, a "click" and a small thumbnail sliding into the photo bar at the bottom.
- **Little alien (bonus, not science):** pops up from behind a crater rim **twice**, at fixed times (12 s and 24 s), waves for 1.5 s and
  hides. Snapping it gives a rainbow flash and the "surprise photo". It never appears as a real science target.
- **No instruments on board** (only possible when testing the minigame alone; a valid rocket always has them): only the alien appears,
  and a banner says *"No science instruments on board: only tourist photos today!"*. Science results stay at 0.

## 3. Game 2 — Signal Catcher (25 s)

**Screen:** the Moon's edge at the bottom, Orion with a big **antenna dish** in the middle, black sky with Earth.
Top bar: timer, "Data home: 45 %". The photo thumbnails taken in game 1 wait in a queue next to Orion.

- **Earth moves** slowly along an arc across the sky (a gentle back-and-forth, predictable).
- The player **holds the mouse button and moves** to rotate the dish; a wide, forgiving **beam** (about ±12°) comes out of it.
- While the beam touches Earth, the queue sends data: envelopes fly along the beam to Earth, oldest photo first. When a photo's data is
  complete, a check appears on its thumbnail.
- **Loss of signal:** from second 10 to 16, Earth slides behind the Moon's edge. The beam hits rock, sending stops and a banner says
  *"Moon blocking the signal — data stays safe on board"* with a countdown. Then Earth comes back and the player re-aims.
- **Sending speed** comes from planning: more data captured = more beam time needed (see 5). With the catalog's only antenna
  (`ART-COMMS-DS`, 180 kbit/s), an attentive player sends everything.
- **No antenna on board** (only when testing alone): there is no beam; banner *"No long-range antenna: the photos cannot reach Earth"*.
  Downlink stays at 0.

## 4. Album (end screen, about 5 s, then "Continue")

A grid of the photos taken: sent ones in colour with a green check, unsent ones greyed out with "stayed on board". Gold-star photos
have a small star. The alien photo has a big stamp:

> **Surprise photo!** No aliens have ever been found on the Moon. The real treasure lunar missions look for is **water ice** hidden in
> craters that never see sunlight: one day it could give astronauts water, air and rocket fuel.

Then two summary lines and one lesson, chosen by what happened:

- Normal: *"Experiments: 11 of 14 (82 %). Data home: 78 %. Real spacecraft keep their data on board behind the Moon and send it when
  they see Earth again."*
- Little sent: *"You collected a lot, but there was not enough antenna time to send it all. Real missions plan observations and contact
  windows together."*

**Continue** closes the minigame and resumes the flight.

## 5. Results it produces (exact)

Let `B` = planned data = sum of `experimentDataBits` of the science instruments mounted (`RocketPhysics.DataBits`).
Each target carries `instrument bits / instrument target count`.

- `experimentFraction` = captured data bits / `B` (data-weighted, so the big ice scans matter more). 0 if `B` = 0.
- `downlinkFraction` = data bits that reached Earth / `B`, **capped at `experimentFraction`**. 0 without antenna.
- `raw` = clamp01( 0.45 × experimentFraction + 0.45 × downlinkFraction + 0.05 × goldStarShare + 0.05 × alienShare )
  - `goldStarShare` = gold-star landmark photos / landmark photos taken (0 if none taken)
  - `alienShare` = alien photos / 2
  - Doing nothing = 0. Perfect play = 1. The alien and stars are a small bonus: science is 90 % of the result.
- **Beam time needed** to send everything captured:
  `needed = (minBeamSeconds + extraBeamSeconds × DownlinkSeconds / maxDownlinkSeconds) × capturedBits / B`
  with defaults 8 s + 10 s. Examples with everything captured: LUNAR + RAD (694 s real) → 9.9 s of beam; HEAVY alone (1,389 s) →
  11.9 s; all three (2,083 s) → 13.8 s. There are 19 s of visible Earth, so heavier science is harder but possible.
- The hook writes `flight.experimentFraction` and `flight.downlinkFraction`, then calls
  `FlightPhases.CompleteMinigame(TrainingFocus.Research, raw)` **once**. `raw` never includes the scientist skill
  (the scoring multiplies once: 7 × raw × skill). A bad result never aborts the flight (task 31).

## 6. Fairness (valid rocket with LUNAR + RAD + antenna, 11 targets, beam needed 9.9 s)

| Player | Captures | Data home | experimentFraction | downlinkFraction | raw | Science gates |
|---|---|---|---|---|---|---|
| Perfect | all, all gold, 2 aliens | all | 1.00 | 1.00 | 1.00 | pass |
| Attentive | 4/5 landmarks (2 gold), 5/6 sparks, 1 alien | 85 % of captured | 0.81 | 0.69 | 0.72 | pass |
| Careless | 2/5 landmarks (no gold), 2/6 sparks | aims late, 40 % of captured | 0.39 | 0.15 | 0.24 | pass (low score) |
| Does nothing | 0 | 0 | 0 | 0 | 0 | fail SCIENCE + NO_DOWNLINK |
| Photographs, never aims | all | 0 | 1.00 | 0 | 0.45+ | fail NO_DOWNLINK |
| No instruments (test only) | alien only | — | 0 | 0 | ≤ 0.05 | fail SCIENCE |

(Any value above 0 passes the gates today; the 0.5 thresholds are a separate pending team decision and are not changed here.)

## 7. How it plugs in (no changes to existing systems)

- **One research turn** already exists (lunar orbit, 50 %). `ResearchFlightHook` registers with
  `RegisterMinigame(TrainingFocus.Research, Open)` and runs Photo Safari → Signal Catcher → Album as one sequence, then reports once.
- Same structure as maintenance, so the team recognises it:
  `Scripts/Mision/Research/` — `ResearchMinigame` (sequence + album), `PhotoSafari`, `SignalCatcher`, `ResearchSettings`
  (ScriptableObject, every number below), `ResearchFlightHook`; static helpers for the formulas.
  Editor: **SpaceApps → Research minigame → Create prefab / Add to open scene / Connect to launch scene / Tests**.
- uGUI + TextMeshPro, new Input System (mouse position and button), works in WebGL, no new scene.
- **Art:** works with simple shapes (UI circles for craters, Earth and sparks; a tiled grey strip for the surface). Nice to have: an alien
  sprite (2 frames: peek and wave), a dish sprite, an envelope icon. Kenney packs in `ThirdParty` may already have usable icons.

## 8. Settings (`ResearchSettings`, defaults)

| Setting | Default |
|---|---|
| Photo Safari duration | 30 s |
| Targets per instrument (LUNAR / RAD / HEAVY) | 5 / 6 / 3 |
| Target time on screen (landmark, ice / spark) | 3.5 s / 1.5 s |
| Ice scan hold time | 1.5 s |
| Viewfinder radius / gold-star inner radius | 90 px / 35 px |
| Alien appearances (times) / visible time | 12 s and 24 s / 1.5 s |
| Signal Catcher duration | 25 s |
| Loss of signal | 10 s → 16 s |
| Beam half-width | 12° |
| Beam time needed (min + extra × downlink ratio) | 8 s + 10 s |
| raw weights (experiments / downlink / gold / alien) | 0.45 / 0.45 / 0.05 / 0.05 |

## 9. Real vs simplified

**Real (sources below):** orbiters like NASA's Lunar Reconnaissance Orbiter photograph the surface (LROC camera), measure radiation
(CRaTER) and search for water ice in permanently shadowed polar craters (LAMP, LEND, Diviner); NASA's LCROSS impact (2009) confirmed
water there. When a spacecraft is behind the Moon it loses direct radio contact with Earth (Artemis I had a planned loss of signal
during its lunar flyby). Deep space data reaches Earth through the Deep Space Network (Goldstone, Madrid, Canberra), and the antenna
data rate limits how much can be sent. Spacecraft store data on board and send it later.

**Simplified for play:** an orbit of about two hours becomes 55 seconds; the beam time is a scaled abstraction of the real downlink time;
targets appear on a schedule; one click = one observation; the game's instruments are fictional modules inspired by real ones, not
Orion's actual payload. **The alien is a joke, clearly labelled: there is no life on the Moon.**

Sources (verify links before publishing): science.nasa.gov/mission/lro · nasa.gov/mission/lcross ·
nasa.gov/communicating-with-missions/dsn · nasa.gov/mission/artemis-i

## 10. Fallback (if time runs out)

Same two games, simpler input: in Photo Safari the targets appear at fixed spots and are clicked directly (no moving viewfinder, no
hold); in Signal Catcher the dish is a **slider** that must stay in a moving green zone (like the oxygen pump). Same formulas and album.

## 11. Decisions for the team

1. Approve the alien as a labelled bonus (5 % of raw at most) — or drop it for a "rare Earthrise photo" bonus.
2. Approve the defaults above (durations, counts, 8 s + 10 s beam time).
3. Main version or fallback first. **Recommendation:** build the fallback input first (a few hours), then upgrade to the moving
   viewfinder and the dish if time allows; the formulas, hook and album are the same.
