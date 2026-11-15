# Design proposal for a new minigame: "connect the systems" (Among Us wires + Flow Free)

**Your job is to research and design only. Do not write code, do not create branches, prefabs or scenes, and do not open a PR.**
The team will review your proposal and implement it later. A hand sketch of the idea is attached (described in "The sketch" below).

## Project context

Repository: `Thermoflask7/Gran-Equipo-SpaceApps-2026` (Unity 6000.6.9f1, final build in **WebGL**).
Educational game for the NASA Space Apps Challenge 2026: an Artemis-style crewed lunar orbit mission, played in 5–10 minutes,
for a young audience (it must be **fun for kids**, like the maintenance minigame we already have). The player plans the mission at a desk
(buys parts, chooses and trains a crew of 3, picks the launch day), then lives the flight from mission control in three phases:
Departure, Lunar orbit, Return. Minigames happen during the flight.
Deadline: submission **Sunday Nov 15, 8:00 pm**, so the minigame must be **small and buildable in a few hours**.

Minigames that already exist (read them if you can, as the style reference):
- **Maintenance** (`Scripts/Mision/Maintenance/`): 4 ship systems wear down; each is repaired in its own hands-on panel
  (replace fuses, pump oxygen, tune the radio dial, match switches to LEDs). 60 s.
- **Piloting** (`Scripts/Mision/Piloting/`): timed commands (separations, burns, parachutes).
- **Research** (`Scripts/Mision/Research/`): photograph targets on the Moon, then aim the antenna at Earth to send the data.

## The sketch (attached image)

A square board seen from above, like a circuit or a maze:
- **Walls / blocked zones** (red, hatched or filled with rocks/hexagons) form corridors. Some are long straight blocks, some are L-shaped.
- **Three pairs of colored endpoints** sit on the border: **blue 1 → blue 2**, **green 1 → green 2**, **yellow 1 → yellow 2**.
  Each "1" is on the top edge; each "2" is on another edge (top-right corner, bottom-left, bottom-right).
- The player draws a **path** for each color from its 1 to its 2 through the free corridors. In the sketch the three paths run along
  different corridors, turn at right angles, and **never cross or overlap** each other (like Flow Free), and they cannot go through walls.
- The feeling should be like **connecting the wires in Among Us**, but with Flow Free's puzzle: you must find a route for every color
  where all fit at the same time.

## Constraints the design must fit

- It must be a **2D UI minigame** (uGUI + TextMeshPro), **mouse only** (click and drag to draw a path; no keyboard), works in a web browser,
  about **30–60 seconds**, playable by kids on the first try with one sentence of instructions.
- It plugs into the existing flight system: a minigame is opened by `FlightPhases` for one of the three crew roles
  (`TrainingFocus.Piloting`, `Engineering` or `Research`) and returns **one raw result from 0 to 1** (the player's performance only, never
  multiplied by the crew skill; the game applies the skill once). Engineering already has the maintenance minigame and research already has
  its own, so say clearly **which role and which moment** this one should use (options: a second engineering minigame used by some
  events, a power-routing step during departure, a data-routing step before the downlink, or something better you propose).
- A bad result must never cause a sudden mission abort by itself (serious incidents give a second attempt; see `Docs/Task31/MISSION_FAILURE_RULES.md`).
- **No randomness that can make a level impossible.** Levels must always be solvable; prefer a small set of hand-made levels.
- It should teach one small real idea (for example: spacecraft route power and data through separate, redundant lines; cables must not
  cross hot or moving parts; each system needs its own connection).

## What to research (short, with sources)

1. What is really connected inside a spacecraft like Orion (power buses, data buses, redundancy, avoiding single points of failure) and
   how to say it in one or two sentences a child understands. NASA sources preferred.
2. Why Flow Free / Among Us wiring puzzles are fun and easy to learn (one clear goal, instant feedback, no penalty for trying).

## What to propose

1. **Rules** in plain words: how a path is drawn, erased and redrawn; what blocks it; when a pair counts as connected; when the board is solved.
2. **The board:** grid size (for example 6×6 to 8×8), how walls are shown, how endpoints are shown, and **3 example levels** written as
   small text grids (`#` wall, `.` free, letters for endpoints, e.g. `B` blue, `G` green, `Y` yellow), each with its solution.
   Include one level that matches the attached sketch as closely as possible.
3. **Difficulty and fairness:** how many pairs (3 to 5), time limit, and a table "player behaviour → result":
   solved fast, solved slow, 2 of 3 connected, nothing connected.
4. **The exact result formula** for `raw` (0 to 1): for example share of pairs connected, plus a small bonus for speed or for filling
   every free cell (Flow Free style), never above 1, 0 if nothing is connected.
5. **Feedback for kids:** what happens visually when a pair connects (glow, sound), when paths would cross, and at the end.
6. **Theme:** what the colors mean in the story (for example blue = power, green = life support, yellow = communications) and the
   one-line lesson shown at the end.
7. **A rough UI sketch** (ASCII or bullet layout) for a 1920×1080 screen.
8. **Real vs simplified:** separate real facts (with sources) from game simplifications.
9. **Open questions** for the team.

## Format

- English, one Markdown document, about **2 pages**. Clear headings. Level grids as code blocks; no other code.
- If you assume something, say so and continue; do not stop to ask unless it blocks the whole design.
