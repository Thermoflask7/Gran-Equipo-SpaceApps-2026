# SpaceMission — Task #18 (P5) · Unity data package

> Integrated by claude_c (for Cristina) in `ia/claude_c/task18-data`. The package files are already at their paths under `Assets/_Project/`; the "copy from the ZIP" step below is done.

**Version:** P5-T18-1.0-EN, November 14, 2026  
**Repository baseline:** `Thermoflask7/Gran-Equipo-SpaceApps-2026`, `main` commit `e290427ba958770bfacb43f2f49d1e005f89866c`.  
**Scope:** new data and an importer. No changes to existing rocket parts, mission, scoring, GameManager, scenes, or prefabs.

## Deliverables

| Task #18 component | Current state | Included here |
|---|---|---|
| Artemis V8.3 parts | Already on GitHub (16 ScriptableObjects) | Left unchanged; not duplicated |
| Artemis V13 mission | Already on GitHub (one ScriptableObject) | Left unchanged; not duplicated |
| Astronaut candidates | New | `AstronautsP5.json`: 10 fictional characters with biographies and hidden skills |
| Events | New | `EventsP5.json`: 16 fictional events; four per phase, eight positive and eight negative |
| Launch sites and weather | New | `LaunchSitesWeatherP5.json`: playable LC-39B, 14 simulated days, two non-playable reference sites |
| Unity importer | Ready for testing | `Task18DataImporter.cs` creates/updates `.asset` files |
| Tests | Independent Python validation complete | 135/135 checks; `Task18DataTests.cs` still requires execution inside Unity |

## Integration in Unity

1. Copy the `Assets/_Project/` directory from this ZIP into the root of the Unity project, retaining its internal paths. **Do not delete or overwrite** `CatalogoArtemis.json`, `MisionArtemis.asset`, or the existing 16 part `.asset` files.
2. Let Unity import the `.cs` and `.json` files. Resolve any pre-existing compilation errors first.
3. Run **SpaceApps → Import P5 data (astronauts, events, weather)**. Validation runs before importing and creates or updates:
   - `Assets/_Project/Data/Astronautas/AST-*.asset` (10 candidates).
   - `Assets/_Project/Data/Eventos/EV-*.asset` (16 events).
   - `Assets/_Project/Data/LaunchSites/SITE-KSC-LC39B.asset` (one launch site).
4. Run **SpaceApps → Test P5 data** and check the Console for `OK` / `FAILED`. Also run the existing physics and scoring test menus, which may still have Spanish labels until the owners migrate them.
5. Commit the generated `.asset` and `.meta` files **from Unity**. Do not hand-edit scene/prefab YAML or GUIDs.
6. P3: connect the new ScriptableObjects to the Folder, Globe, and Calendar prefabs. P4: supply the imported event list to the code calling `RandomEvents.RollAndApply()` for each phase. **Importing data does not automatically wire prefabs.**
7. Work in a separate branch (suggestion: `feat/cristina/p5-data`, subject to team coordination) and open a PR. Set Task #18 to `done` only after integration tests pass and the two decisions below are resolved.

### Existing Unity contracts preserved

- `AstronautData`: `astronautName`, `bio`, `research`, `piloting`, `engineering`; existing `portrait` values are **not changed**. JSON `id` and `primaryRole` are identifiers/documentation, not new public fields.
- `EventData`: `eventName`, `description`, `probability`, `isGood`, `moneyChange`, `scoreChange`, `phase`, `weatherSensitive`, `minigameRole`, `lesson`.
- `LaunchSiteData`: `siteName`, `description`, `latitude`, `longitude`, `weatherRiskByDay[14]`.
- `GameManager`: initial budget remains **3,400 fictional game credits**. P3 must assign `gm.weatherRisk = gm.launchSite.weatherRiskByDay[gm.launchDay]` when the site/date changes, with a valid calendar index.
- After import, the game uses ScriptableObjects. It does not have to parse JSON at runtime, including in WebGL builds.

### Two decisions for P1/P3

1. **Fourth crew member:** `MisionArtemis.asset` requires four people; `GameManager.IsCrewReady()` currently checks only `pilot`, `engineer`, `scientist`, and their training. The dataset includes ten candidates, but the package does **not** add a fourth public field or alter readiness checks. P1/P3 should decide whether to use `reserves[0]` as a mission specialist or add a dedicated field. The selected crew must contain four unique candidates; do not modify the candidate ScriptableObjects at runtime.
2. **Globe site choices:** NASA uses LC-39B for Artemis SLS launches. Only LC-39B is imported as a playable site. LC-39A and Vandenberg are **non-playable informational references**. Do not depict them as certified SLS launch locations. Additional playable sites would require explicit agreement on a fictional simulation mode or a compatible launch vehicle.

### Wind, cloud cover, and precipitation

The JSON provides 14 illustrative days with wind in **knots**, precipitation likelihood in **percent**, cloud cover in **percent**, and dimensionless risk values between **0 and 1**. The existing `LaunchSiteData` ScriptableObject only stores `weatherRiskByDay`. The importer **deliberately does not add public fields**, preserving P3's current contract. P3 may display risk alone, or coordinate a contract extension to display other weather metadata.

**These are synthetic educational examples, not forecasts or real launch authorization criteria.** Actual NASA launch decisions involve multiple operational constraints, including winds, lightning, precipitation, and temperature.

### Events

`RandomEvents.Roll(events, phase)` independently rolls each event in that phase, using climate/crew modifiers where applicable. If several succeed, one is chosen at random. Therefore a value stored in `probability` is a **base roll probability**, not the effective observed frequency. Events have small score changes (each between -2 and +2). V16 scoring already caps event impact at ±3 per major stage.

`moneyChange` is denominated in **fictional game credits, not USD**. The existing `EventData.cs` currently has an outdated Spanish comment referring to millions of dollars; its owner should update that **comment only**, without changing field names, units, or behavior. Other pre-existing GitHub paths and comments in Spanish must be migrated separately with the relevant owners.

Events currently alter money, score, history, and may trigger a minigame based on `minigameRole`. They do **not** directly change physical resources or scientific completion fractions. Any additional effects need design approval and coordination with P4.

### Data provenance

- LC-39B: a **real** launch pad documented by NASA.
- LC-39B coordinates: approximate historical NASA reference for the in-game globe.
- Orion's capacity for four crew members is based on NASA material; all **candidate names, bios, and skills are fictional**.
- The V13 educational NRHO mission (12 days, four crew) is **not** the actual Artemis II flight plan.
- The 14-day weather, event rates, and event bonuses are **fictional game parameters**, not NASA observations or predictions.

Source links and classifications are in `QA/Sources_and_Classification.json`.

## Validation and remaining work

- Independent local checks: `python QA/validate_task18.py` → **135 passed, 0 failed**.
- Still required: compile scripts in Unity, run the Editor menu commands, verify GUIDs, connect prefabs, run physics/scoring regression tests, and complete a 5–10 minute playthrough.
- Reimporting **preserves asset GUIDs and portraits** for existing assets at the same path, but overwrites other supported fields from JSON. Coordinate with P3 before reimporting assets that someone has edited manually.
- This package did not modify GitHub.

## Language policy

Every **new string, comment, description, documentation section, QA message, and Unity menu label in this package is English**. Existing repository-owned folder names `Data/Astronautas` and `Data/Eventos` remain unchanged to avoid breaking links and editor references. The new folder uses `Data/LaunchSites`. If the team wants to rename existing folders and previously committed data to English, perform that separately in Unity and update all references with the owners' approval. The preserved `CatalogoArtemis.json` and `MisionArtemis.asset` are pre-existing files outside this package.
