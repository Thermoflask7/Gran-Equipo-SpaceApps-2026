# Game design document (GDD)

**Mission:** Artemis-style crewed lunar orbit. Lift off, travel to the Moon, orbit it doing science, and return safely.

The game has **2 stages**: Mission planning and Launch. At the end, the score is calculated from the performance in both.

---

## Stage 1 · Mission planning (`01_Planeacion`)

The player sits in front of a **desk** with several objects. Each object is a **station** (an independent prefab) that opens when clicked.

| Station | What it does | Owner |
|---|---|---|
| **Folder** | Page 1: mission description and updates from random events. Page 2: astronaut profiles and training assignment. Optional extra pages (rocket info, etc.). | P3 |
| **Computer** | Desktop with apps: **SpaceMart** (Amazon-style store to buy parts), game **Settings**, **Shut down** (asks whether you want to quit). Tech tree as an app only if there is time left. | P2 |
| **Rocket model** | Assembly of the final rocket with the purchased parts. Shows mass, Δv and whether it is enough for the mission. | P2 |
| **Globe + calendar** | Choose the launch site on the globe; the calendar shows the site's weather and wind on launch day. | P3 |
| **Launch button** | Locked until every task is complete. Clicking it moves to the Launch stage. | P1 |

### Tasks that unlock the button
1. Mission read (folder opened at least once).
2. Crew assigned, with training chosen.
3. Rocket assembled and valid: Δv ≥ what the mission requires, and mass within the limit.
4. Launch site and date chosen.

The budget is fixed at the start (budget negotiation was left out of this version) and is spent in SpaceMart.

---

## Stage 2 · Launch (`02_Lanzamiento`)

**Split screen:**
- **Left (2D):** mission control. People at their computers and, in the background, a big screen with the flight status and data.
- **Right (3D):** the rocket flying its Earth → Moon → Earth route.

**Flight phases:**

| Phase | Content |
|---|---|
| **Departure** | Liftoff. Piloting and maintenance minigames. Random events can come up and trigger minigames. |
| **Lunar orbit** | Research minigames (data collection) and random events. |
| **Return** | Same as departure but faster. Reentry. |

Each minigame returns a result (0 to 1) that is multiplied by the skill of the astronaut in the matching role (pilot, engineer, researcher).

Random events depend on what was decided during planning (for example, bad weather at the chosen site raises the risk during departure).

---

## Results (`03_Resultados`)

Final score = planning performance (remaining budget, efficient rocket, good date and site, training) + performance in the mission minigames. Screen with the breakdown and an educational explanation of each decision.

---

## Scenes

| Scene | Content |
|---|---|
| `00_Menu` | Title screen |
| `01_Planeacion` | Desk with the 5 stations |
| `02_Lanzamiento` | Split screen, 3 phases, minigames |
| `03_Resultados` | Score and explanation |

Minigames and stations are **prefabs**, not scenes, so everyone works in their own file.
