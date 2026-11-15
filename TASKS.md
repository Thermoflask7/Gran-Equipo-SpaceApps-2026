# Task board

Full design in [GDD.md](GDD.md). Loop: Menu → Planning (desk) → Launch (departure → lunar orbit → return) → Results.

Before taking a task, read [AGENTS.md](AGENTS.md) and [AI_COLLAB.md](AI_COLLAB.md). To claim it, fill in **Assigned AI** and **Branch** in the same PR, and set **Status** to `in progress`.

Valid statuses: `pending`, `in progress`, `in PR`, `done`.

**Human owner** stays as `unassigned` until someone on the team writes their name.

| ID | Task | Role | Human owner | Assigned AI | Branch | Status |
|---|---|---|---|---|---|---|
| 0 | GameManager + scene changes + state | P1 | Elena | Claude | `ia/claude/gamemanager` | done |
| 1 | `00_Menu`: title screen | P1 | Mateo | Grok | `feat/mateo/menu` | done |
| 2 | `01_Planeacion`: base desk, camera and navigation between stations | P1 | asdidkf/Mateo | Grok | `pantalla-escritorio` (desk camera and PC zoom already in `main`, PR #24; navigation to the other stations pending) | in progress |
| 3 | Launch button + task checklist | P1 | Elena | Claude | `ia/claude/boton-lanzamiento` | done |
| 4 | Folder: mission + updates from events | P3 | Elena | Claude | `ia/claude/folder-mission` (PR #26) | done |
| 5 | Folder: astronaut profiles + training | P3 | Elena | Claude | `ia/claude/folder-crew` (PR #29) | done |
| 6 | Globe + calendar (site, weather, wind) | P3 | Mateo | Grok | `feat/mateo/globo-calendario` (planning wall with world map, calendar and globe zoom; not in `main` yet) | in progress |
| 7 | Computer: app desktop, settings and shut down | P2 | Mateo | Grok | `pantalla-escritorio` (PR #24: SpaceOS desktop, Settings and Shut down) | done |
| 8 | Computer: SpaceMart app (buy parts) | P2 | Elena (logic), Mateo (SpaceOS window) | Claude, Grok | `ia/claude/spacemart` (PR #18) + `ia/claude/spacemart-filters` (PR #22) + SpaceOS front-end `SpaceMartApp` (PR #24) | done |
| 9a | Rocket model logic: `RocketAssembly` (mount bought parts, `rocketValid`) | P2 | Cristina | claude_c | `ia/claude_c/rocket-assembly` (PR #23, 25/25 tests OK in Unity) | done |
| 9b | Rocket model: prefab, UI and 3D model (calls `RocketAssembly`) | P2 | Cristina | claude_c | branch not pushed yet | in progress |
| 10 | `02_Lanzamiento`: split screen (2D mission control + 3D rocket) | P4 | Mateo | Grok | merged into `main` with task 11 (PR #32). Code and scene in `main`; Play Mode check pending (task 29) | done |
| 11 | Flight phases: departure, lunar orbit, return | P4 | Cristina | claude_c | `ia/claude_c/flight-phases` (PR #32). Minigames register with `FlightPhases.RegisterMinigame(role, open)`; unhandled roles are skipped safely (`ia/claude_c/audit-fixes`) | done |
| 12 | Piloting minigame (register with `FlightPhases.RegisterMinigame(TrainingFocus.Piloting, open)`, write the RAW result with `FlightPhases.SetResult`, then `MinigameDone()`) | P4 | KvotheGuile | Claude-M | `ia/claude-m/pilotaje` | in progress |
| 13 | Maintenance minigame (same `FlightPhases` hooks as 12) | P4 | KvotheGuile | Claude-M | `ia/claude-m/mantenimiento` (PR #28 + #30). Code done. Flight hook `MaintenanceFlightHook` (uses `RawResult`, task 31 `CompleteMinigame`). Scene hookup: menu **SpaceApps → Maintenance minigame → Connect to launch scene** in `ia/claude_c/maintenance-hookup` (claude_c); the scene + prefab are saved from Unity | done (code), scene hookup in progress |
| 14 | Research minigame (data collection; register with `FlightPhases.RegisterMinigame(TrainingFocus.Research, open)` like 12, and it must set `flight.experimentFraction` and `flight.downlinkFraction`, which nobody sets today) | P4 | unassigned | none | — | pending |
| 15 | Random events (planning and flight) | P4 | Elena | Claude | `ia/claude/eventos` | done |
| 16 | `03_Resultados`: final score and educational explanation | P1 | Elena | Claude | `ia/claude/puntuacion-artemis` (Cristina's V16 scoring, adjusted to Artemis) | done |
| 17 | Physics: Δv, mass and power | P5 | Elena, Cristina | Claude | `ia/claude/catalogo` (fix + Cristina's catalog) | done |
| 18 | Data: parts, astronauts, events, sites and weather, lunar mission | P5 | Cristina | claude_c | `ia/claude_c/task18-data`, `ia/claude_c/game-data`, `ia/claude_c/crew-of-three` | done |
| 19 | WebGL build | P1 | unassigned | none | — | pending |
| 20 | Pitch for the judges | P1–P5 | unassigned | none | — | pending |
| 21 | (Optional) Tech tree as a computer app | P2 | unassigned | none | — | pending |
| 22 | Translate the repo to English: docs, UI, menus, messages and comments | All | Elena | Claude | `ia/claude/english` | done |
| 23 | Rename the Spanish folders, scenes and assets to English (after the open branches are merged) | P1 | unassigned | none | — | pending |
| 24 | `01_Planeacion`: put the stations on the desk (folder, rocket model, globe + calendar, launch button + checklist) and open each one from its 3D object | P1 (folder part: Cristina) | claude_c (folder) | `ia/claude_c/folder-station` (folder: `FolderStation` on `notepad`, menu **SpaceApps → Desk → Add folder station**; rocket model, globe and launch button still open) | in progress |
| 25 | `03_Resultados`: add the Results panel (**SpaceApps → Results screen**) to the scene and test it at the end of the loop | P1 | unassigned | none | — | pending |
| 26 | Locked missions (Mars, Europa) shown as "coming soon" (`DestinationData.locked`) | P1 | unassigned | none | — | pending |
| 27 | Audio: music and sound effects, using the volumes in `SpaceOsSettings` | All | unassigned | none | — | pending |
| 28 | How to play: short tutorial or hints for first-time players (the judges) | P1 | unassigned | none | — | pending |
| 29 | Full-loop playtest (Menu → Planning → Launch → Results) and bug fixes before the final build | All | unassigned | none | — | pending |
| 30 | Space Apps submission: project page text, demo video, link to the WebGL build, and credits for NASA data and third-party assets | P1–P5 | unassigned | none | — | pending |
| 31 | Mission failure, abort and recovery rules (a phase can fail; departure/lunar orbit abort, failed return, recoverable incidents); docs in `Docs/Task31/MISSION_FAILURE_RULES.md` | P4, P5 | Cristina | claude_c | `ia/claude_c/mission-failure-rules` | in PR |
| 32 | Code fixes from the GitHub audit of 2026-11-14 (F02 overlay canvas in builders, F06 safe minigame routing + dev-only Skip, F08 maintenance raw-result hook, F09 non-destructive launch builder, F11 12-day HUD + simulated label, F12 English launch UI). Scene/prefab work (F01, F03, F05, F07) stays in tasks 24, 25, 9b, 12 and 14 | P4, P1 | Cristina | claude_c | `ia/claude_c/audit-fixes` (PR #33) | done |
| 33 | Orbit choice (NASA challenge goal 3, "select the launch vehicle and orbit"): 2–3 lunar orbit options chosen in the folder, for example low lunar orbit, NRHO (Artemis/Gateway) and DRO (Artemis I). Each one changes `requiredDeltaV`, science and communication/downlink time, with a short plain-language explanation | P5, P3 | unassigned | none | — | pending |
| 34 | Interactive crew training: skill bars (Piloting, Engineering, Research; approximate stars before training, exact value after), a +2 preview bar when choosing training, the effect on the minigame (`SkillMultiplier` as %, e.g. "pilot effectiveness 60 % → 80 %") and a training cost in credits so it competes with the budget. Changes the training part of the scoring: tell claude_c and Claude-M | P3 | unassigned | none | — | pending |
| 35 | Results: one line per failed gate with what went wrong, the lesson and what would have fixed it (e.g. "Your battery does not cover 1.5 h in shadow: a battery module would have passed") | P1 | unassigned | none | — | pending |
| 36 | SpaceMart: highlight the 2–3 critical resources of the current mission (for SLS Block 1: mass to the Moon and Orion Δv), as the challenge asks | P2 | unassigned | none | — | pending |
| 37 | Local leaderboard for 1 to 4 players (how we cover the challenge's "1 to 4 players"): each player enters a name before planning, plays the mission in turns on the same computer, and the best scores are saved in PlayerPrefs (WebGL-safe) and shown in Results and the Menu | P1 | unassigned | none | — | pending |
