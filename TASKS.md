# Task board

Full design in [GDD.md](GDD.md). Loop: Menu → Planning (desk) → Launch (departure → lunar orbit → return) → Results.

Before taking a task, read [AGENTS.md](AGENTS.md) and [AI_COLLAB.md](AI_COLLAB.md). To claim it, fill in **Assigned AI** and **Branch** in the same PR, and set **Status** to `in progress`.

Valid statuses: `pending`, `in progress`, `in PR`, `done`.

**Human owner** stays as `unassigned` until someone on the team writes their name.

| ID | Task | Role | Human owner | Assigned AI | Branch | Status |
|---|---|---|---|---|---|---|
| 0 | GameManager + scene changes + state | P1 | Elena | Claude | `ia/claude/gamemanager` | done |
| 1 | `00_Menu`: title screen | P1 | Mateo | Grok | `feat/mateo/menu` | done |
| 2 | `01_Planeacion`: base desk, camera and navigation between stations | P1 | unassigned | none | — | pending |
| 3 | Launch button + task checklist | P1 | Elena | Claude | `ia/claude/boton-lanzamiento` | done |
| 4 | Folder: mission + updates from events | P3 | unassigned | none | — | pending |
| 5 | Folder: astronaut profiles + training | P3 | unassigned | none | — | pending |
| 6 | Globe + calendar (site, weather, wind) | P3 | unassigned | none | — | pending |
| 7 | Computer: app desktop, settings and shut down | P2 | unassigned | none | — | pending |
| 8 | Computer: SpaceMart app (buy parts) | P2 | Elena | Claude | `ia/claude/spacemart` (done, PR #18) + `ia/claude/spacemart-filters` (category tabs) | in PR |
| 9 | Rocket model: assembly with purchased parts | P2 | unassigned | none | — | pending |
| 10 | `02_Lanzamiento`: split screen (2D mission control + 3D rocket) | P4 | Mateo | Grok | `feat/mateo/lanzamiento` | in progress |
| 11 | Flight phases: departure, lunar orbit, return | P4 | unassigned | none | — | pending |
| 12 | Piloting minigame | P4 | KvotheGuile | Claude-M | `ia/claude-m/pilotaje` | pending |
| 13 | Maintenance minigame | P4 | unassigned | none | — | pending |
| 14 | Research minigame (data collection) | P4 | unassigned | none | — | pending |
| 15 | Random events (planning and flight) | P4 | Elena | Claude | `ia/claude/eventos` | done |
| 16 | `03_Resultados`: final score and educational explanation | P1 | Elena | Claude | `ia/claude/puntuacion-artemis` (Cristina's V16 scoring, adjusted to Artemis) | done |
| 17 | Physics: Δv, mass and power | P5 | Elena, Cristina | Claude | `ia/claude/catalogo` (fix + Cristina's catalog) | done |
| 18 | Data: parts, astronauts, events, sites and weather, lunar mission | P5 | Cristina | claude_c | `ia/claude_c/task18-data`, `ia/claude_c/game-data`, `ia/claude_c/crew-of-three` | done |
| 19 | WebGL build | P1 | unassigned | none | — | pending |
| 20 | Pitch for the judges | P1–P5 | unassigned | none | — | pending |
| 21 | (Optional) Tech tree as a computer app | P2 | unassigned | none | — | pending |
| 22 | Translate the repo to English: docs, UI, menus, messages and comments | All | Elena | Claude | `ia/claude/english` | done |
| 23 | Rename the Spanish folders, scenes and assets to English (after the open branches are merged) | P1 | unassigned | none | — | pending |
