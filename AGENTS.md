# Project context (for any AI: Claude, Copilot, Cursor, ChatGPT…)

Educational game for the **NASA Space Apps Challenge 2026** (space mission design challenge).
48-hour hackathon: submission on **Sunday, Nov 15, 8:00 pm** (official deadline 11:59 pm).
Team of 5. Unity, low poly, final build in **WebGL** so the judges can play in the browser.
Protocol between the AIs (Claude, Grok and Claude-M): [AI_COLLAB.md](AI_COLLAB.md).
Full game design: [GDD.md](GDD.md).

## The game in one sentence
You run an **Artemis-style crewed lunar orbit mission**: from your planning desk you buy parts,
build the rocket, train the crew and choose the launch site and date; then you live the flight from
mission control. You learn the real trade-offs of mass, Δv, power, weather and budget.

## Chosen mission
Crewed lunar orbit (inspired by Artemis): reach low Earth orbit, translunar injection, enter lunar orbit, collect data and return.
Mars and Europa appear as locked missions ("coming soon"). Lunar landing only if there is time left.

## Loop (a single playable mission, 5–10 min)
Menu → **Planning** (desk) → **Launch** (departure → lunar orbit → return) → Results

## Physics (simplified but real)
- Spacecraft Δv: `Δv = Isp * g0 * ln(m0 / mf)` with `g0 = 9.80665 m/s²`
  - `m0` = total mass with fuel, `mf` = mass without fuel. With stages: add up the Δv of each stage.
- Two minimum Δv values: the **rocket** (built by the player) to lift off and inject toward the Moon (`launchDeltaV`), and the Orion **spacecraft** to enter lunar orbit and return (`requiredDeltaV`). If either falls short, the launch is not allowed. Calculations in `Scripts/Fisica/RocketPhysics.cs`.
- Real data for parts, launch sites and the mission: prepared by P5 (see `Assets/_Project/Data/`).

## Language
- **Everything in the repo is in English**: docs, code comments, UI text, log and error messages, Unity menus and commit messages (for example `Add PC zoom to desk scene`). Space Apps requires the submission and every linked material to be in English.
- New files, folders, scenes, prefabs and assets get **English names**. The existing Spanish folder, scene and asset names (`Fisica`, `Mision`, `Tienda`, `Tripulacion`, `Piezas`, `Destinos`, `Catalogo`, `01_Planeacion`, `02_Lanzamiento`, `03_Resultados`…) will be renamed at the end, once the open branches are merged, so they do not cause merge conflicts. Until then, do not rename them.

## Rules for writing code
- **Simple, readable code.** No complicated patterns, dependency injection or generic systems. It is a hackathon.
- C#, one class per file, English names and English comments.
- Data (parts, astronauts, events, sites, mission) as **ScriptableObjects**, not hardcoded.
- Global state in `GameManager` (singleton with `DontDestroyOnLoad`): money, bought parts, assembled rocket, crew and training, launch site and date, events that happened, score.
- Every scene must be testable on its own: if there is no `GameManager`, one is created with test values.
- Each desk station and each minigame is an **independent prefab** with its own script, so several people can work in the same scene without conflicts.
- WebGL compatible: no threads (`System.Threading`), and no reading/writing local files.
- UI only with uGUI + **TextMeshPro** (`TextMeshProUGUI` / `TMP_Text`). No legacy `UnityEngine.UI.Text`.
- Input only with the **new Input System** (`UnityEngine.InputSystem`: `Keyboard.current`, `Mouse.current` or Input Actions). No `Input.GetKey`, `Input.GetAxis` or `Input.mousePosition`: the old input manager is disabled in the project and crashes. Every EventSystem uses `InputSystemUIInputModule`, not `StandaloneInputModule`.
- No obsolete Unity 6 APIs: zero `CS0618` warnings. For example, `FindAnyObjectByType` / `FindObjectsByType` instead of `FindObjectOfType` or `FindFirstObjectByType`.
- No obsolete import settings in the `.meta` files either (for example `materialLocation: 0`, External).
- Do not modify another person's scenes, prefabs or scripts without telling them (avoids merge conflicts).

## Folder structure
```
Assets/_Project/
  Scenes/          00_Menu, 01_Planeacion (Planning), 02_Lanzamiento (Launch), 03_Resultados (Results)
  Scenes/_Sandbox/ one test scene per person (nobody else touches it)
  Scripts/Core/        GameManager, scene changes, state, launch button                      (P1)
  Scripts/Tienda/      (Shop) computer: SpaceMart app and rocket assembly (model)            (P2)
  Scripts/Tripulacion/ (Crew) folder (mission, astronauts, training), globe and calendar     (P3)
  Scripts/Mision/      (Mission) launch stage: mission control, phases, minigames, events    (P4)
  Scripts/Fisica/      (Physics) Δv, mass, power and weather calculations                    (P5)
  Data/            ScriptableObjects (Piezas = Parts, Astronautas = Astronauts, Eventos = Events, Destinos = Destinations)
  Prefabs/  Art/  Audio/
Assets/ThirdParty/  downloaded assets (Kenney, Quaternius, NASA models)
```

## Who owns what
| Role | Scenes / prefabs / folders |
|---|---|
| P1 · Loop | Core, 00_Menu, scene 01_Planeacion (base desk and navigation between stations), launch button, 03_Resultados, integration and WebGL build |
| P2 · Shop/Rocket | Computer prefab (SpaceMart, settings, shut down) and RocketModel prefab, Scripts/Tienda |
| P3 · Crew/Planning | Folder and GlobeCalendar prefabs, Scripts/Tripulacion |
| P4 · Mission | 02_Lanzamiento, Scripts/Mision, minigame prefabs |
| P5 · Mechatronics | Scripts/Fisica, Data (real values), balancing |

## Out of scope (do not implement)
Online/real-time multiplayer, creative mode / importing spacecraft, real rival AI. Tech tree only if there is time left (as an app on the computer). The local leaderboard for 1 to 4 players is in scope (task 37).
