# Audit of the P5 deliverables

Branch `ia/claude/p5-auditoria`, based on `main` at `e290427` (PR #12). Before starting, we checked that none of this had already been done in `main`.

| Deliverable | Document |
|---|---|
| 1. Audit report and balance results | this file |
| 2. Scientific data table with sources and classification | [REFERENCE_DATA.md](REFERENCE_DATA.md) |
| 3. Δv table by leg and destination | [DELTA_V_BY_DESTINATION.md](DELTA_V_BY_DESTINATION.md) |
| 4. Balance tests | Unity: **SpaceApps → Balance tests**; Python: `python3 Docs/P5/balance.py` |
| 5. Code fixes | section 6 |
| 6. Decisions for the team | section 7 |

---

## 1. Mission balance

P5's enumeration was repeated with the real catalog (`CatalogoArtemis.json`) and the same `RocketPhysics` checks. **P5's numbers are confirmed exactly.**

Combinations: 1 launcher + Orion + up to 1 unit of each of the 11 optional modules = 2 × 2¹¹ = 4,096. Budget: 3,400 credits.

| | 12 days | 15 days |
|---|---:|---:|
| Viable | **78 (1.90 %)** | **5 (0.12 %)** |
| Cheapest viable | 2,940 credits | 3,140 credits |
| Viable with Block 1B | 1 | 0 |
| Fail on budget | 2,401 | 2,401 |
| Fail on mass (SLS capacity) | 1,202 | 1,202 |
| Fail on Orion Δv | 622 | 622 |
| Fail on science (< 40 points) | 1,536 | 1,536 |
| Fail on downlink (antenna) | 1,792 | 1,792 |
| Fail on consumables | 0 | 3,584 |
| **Fail on power generation** | **0** | **0** |
| **Fail on battery** | **0** | **0** |

A combination can fail for several reasons at once.

### 1.1 Is there enough freedom without making it trivial?

**There is a challenge, but little variety of strategies.** The 78 viable 12-day combinations come down to two science approaches:

- 77 use **Block 1 + antenna + SCI-LUNAR + SCI-RAD** (what changes are the extra modules: reserves, navigation, shielding…).
- 1 uses **Block 1B + antenna + SCI-HEAVY**, with only 5 credits left over.

Even so, there are **at least three viable configurations with different strategies**, and the tests protect them:

| Strategy | Configuration | Credits | Days |
|---|---|---:|---:|
| Budget | Block 1 + antenna + SCI-LUNAR + SCI-RAD | 2,940 | 12 |
| Heavy science | Block 1B + antenna + SCI-HEAVY | 3,395 | 12 |
| Long mission | Block 1 + antenna + 2 instruments + water, oxygen and food reserves | 3,140 | 15 |
| Safe | Block 1 + antenna + 2 instruments + lunar navigation + food reserve | 3,115 | 12 |

**Budget sensitivity** (for reference only when deciding; **not changed**):

| Budget | Viable 12 days | Viable 15 days |
|---:|---:|---:|
| 3,400 | 78 | 5 |
| 3,600 | 115 | 10 |
| 3,800 | 257 | 18 |
| 4,000 | 461 | 42 |

Raising the budget mostly opens up Block 1B combinations: with 3,600 credits there are 28 viable Block 1B combinations at 12 days. But the percentage of combinations **does not measure real difficulty**: playtesting is needed (decision D1).

### 1.2 Repeated purchases

Until now nothing in the code prevented them, and SpaceMart (task 8) does not exist yet. With up to 2 units per part:

| | 12 days | 15 days |
|---|---:|---:|
| Combinations | 354,294 | 354,294 |
| Viable | 1,146 | 124 |
| Cheapest | **2,875**: Block 1 + antenna + **2 × SCI-RAD** | **3,075**: same + reserves |

**Unintended advantage:** two SCI-RAD give exactly 40 science points for 230 credits. That is cheaper than SCI-LUNAR + SCI-RAD (295 credits) and makes the lunar instrument useless. Also, the catalog and P5's enumeration assume one unit per part.
**Fix applied (decision D2, reversible):** `RocketPhysics.MaxCopiesPerPart = 1`, and validation rejects two units of the same part. To allow repeats, set it to `0` (and SCI-RAD would then need rebalancing).

### 1.3 Power is too easy (confirmed)

With the current catalog **no combination fails on power**:

- The service module generates 11,000 W, and the maximum possible draw with every module is 3,585 W.
- Its battery holds 18,000 Wh, and the most ever needed is 3,585 W × 1.5 h = 5,378 Wh.
- For the battery to become a limit, the period without sunlight would have to exceed about 5 h with the most loaded spacecraft, or 6.3 h with the basic configuration.

**Effect on scoring:** the 6 power points (generation 3 + batteries 3) are always awarded in full. Nothing was changed, because the formulas are not wrong: it is a data issue. The options are in decision D3.

## 2. Physics model

Formulas reviewed and correct for the educational model: Tsiolkovsky (`DeltaV`), `TliMass`, `DeliveryMargin`, `SpacecraftDeltaV` (ESM propellant only), `PowerBalance`, `BatteryWh`/`BatteryCoversOutage`, consumable margins, `SciencePoints` and `DownlinkSeconds`. SLS stages, `SplitStages`, launcher Δv and TWR were **not reintroduced**.

Edge cases:

| Case | Before | Now |
|---|---|---|
| Null mission | `ValidationMessage(parts, null)` skipped the mission checks and could mark the rocket as **valid** | "No mission assigned." |
| Mission with invalid data (Δv ≤ 0, 0 days, negative values) | Accepted | Rejected (`MissionDataProblem`) |
| Part with negative values | Only mass and fuel were checked; a negative power draw gave free power | All fields are checked (`PartDataProblem`), plus fuel without Isp |
| Science without data | Instruments with points but 0 bits gave 0 s of downlink and **passed** | "The instruments do not generate data to send to Earth (check the catalog)." |
| Repeated parts | Allowed without limit | At most 1 per `catalogId` (see 1.2) |

All these checks pass with the current catalog: no valid configuration changes.

## 3. Parts catalog and provenance

- The **16 parts and their IDs** are kept. No values were changed.
- Documentary table for SLS, ICPS, EUS, RS-25, RL10 and Orion/ESM with unit, source, status and use: [REFERENCE_DATA.md](REFERENCE_DATA.md).
- Prices, science points, power draws and optional modules are marked as **fictional balance**.
- `ART-ESM-ORION`: the 7,667 kg dry mass is a **model allocation** (25,854 − 10,387 − 7,800), not the official dry mass. The 7,800 kg is the **propellant available after TLI** in the model; the real ESM carries about 8.6 t. With 8,618 kg, the Δv of scenario 1 would rise from 1,103 to about 1,246 m/s, so it is **not replaced** without rebalancing (decision D4).
- **Importer:** previously, `SpaceApps → Import Artemis catalog` (and the tests, which call it) **overwrote `MisionArtemis.asset` without warning**. Now:
  - If the mission was edited by hand, it is kept, and the console lists which values differ from V13.
  - To go back to V13 there is a new menu: **SpaceApps → Restore V13 mission**, which asks for confirmation.
  - Parts are still updated from the JSON, as before.

## 4. Δv by destination

Full table in [DELTA_V_BY_DESTINATION.md](DELTA_V_BY_DESTINATION.md):

- Earth→LEO 10,000 m/s; LEO→TLI 3,200 m/s (done by the SLS).
- TLI→NRHO 450 m/s; NRHO→return 450 m/s (done by Orion).
- LLO: 900 + 900 m/s.
- **1,060 m/s = 900 NASA reference + 40 for corrections + 120 reserve (assumptions).** It is not an official NASA requirement.

## 5. Crew (inconsistency, no code changed)

The mission, Orion and the consumables use **4 astronauts** (`mission.crewCount = 4`, `crewCapacity = 4`). But `GameManager` only has a pilot, an engineer and a scientist, and `IsCrewReady()` checks three. **The crew size was not changed.** It is a decision for P3 and P1 (D5). Options:

1. **Assignable fourth astronaut** (for example, a mission specialist or commander). Requires a new field in `GameManager` (contract), another role in the folder, and maybe another minigame or bonus.
2. **Fixed built-in crew member:** the commander comes pre-assigned and the player chooses 3 roles. The UI must show all 4 and explain it ("The commander has already been assigned by NASA").

Either way, the UI must not show 3 people while consumables are calculated for 4.

## 6. Code fixes (only the necessary ones)

| File | Change |
|---|---|
| `Scripts/Fisica/RocketPhysics.cs` | Null or invalid mission, negative part values, science without data, limit on repeated parts (`MaxCopiesPerPart`). New public methods: `DataBits`, `PartDataProblem`, `MissionDataProblem`, `RepeatedPart`. The formulas do not change. |
| `Scripts/Fisica/Editor/CatalogImporter.cs` | Does not overwrite an edited mission; **Restore V13 mission** menu. |
| `Scripts/Fisica/Editor/BalanceTests.cs` (new) | **SpaceApps → Balance tests**: all 4,096 combinations for 12 and 15 days (budget, mass, Δv, consumables, generation, battery, science and communications), that `ValidationMessage` agrees with the separate checks, the 4 viable strategies, and the edge cases. |

**Not touched:** the scoring formulas, the catalog values, the 3,400 credits, scenes or prefabs. `RocketPhysicsTests` (the 9 V13 mission scenarios) and `MissionScoringTests` are unchanged and must keep passing.

**How to test in Unity:** run **SpaceApps → Physics tests**, **Scoring tests** and **Balance tests**; everything should come out OK. This code was not compiled in Unity from here. The expected balance values were obtained with `balance.py`.

## 7. Decisions that need team approval

| # | Decision | Options | Recommendation |
|---|---|---|---|
| D1 | Does the current balance give enough freedom? | Keep 3,400 credits / raise to 3,600 (opens Block 1B) / make SCI-HEAVY or Block 1B cheaper | Keep 3,400 until playtesting; if almost nobody uses Block 1B, try 3,600 |
| D2 | Are repeated parts allowed in SpaceMart? | 1 per part (applied) / allow and rebalance SCI-RAD | 1 per part: it is what the catalog and the enumeration assume. P2 must reflect it in SpaceMart |
| D3 | Power never limits | Accept it and remove or reduce its 6 scoring points / longer period without sunlight, with a source (orbits near the Earth and Moon can have shadows lasting several hours) / lower the ESM battery in the model | Document it in the pitch; if a challenge is wanted, use a sourced period without sunlight, not a made-up number |
| D4 | Real ESM masses (8.6 t of propellant) | Keep the V13 model (7,800 kg) / use the real figure and raise `requiredDeltaV` or the mass | Keep it and present it as a model; changing it forces rebalancing everything |
| D5 | Fourth crew member | Assignable / fixed and explained in the UI | Fixed and explained: simpler given the hackathon time (P3/P1) |
| D6 | Minimum science to win and mandatory minigames (pending from the scoring audit) | See `MissionScoring.MinExperimentFraction` | Still pending |
| D7 | Secondary sources | Replace Wikipedia and Plant Services with official sheets (Lockheed Martin, ESA) before the pitch | Yes, if there is time |
