# Technical annex: reference data and provenance (P5)

Documentary table, **separate from the playable catalog**. The 16 `ART-*` parts and their IDs do not change.
Engines and tanks (RS-25, RL10, ICPS, EUS) **are not playable parts**: the SLS is bought as a complete service.

**Status of each data point**

| Key | Meaning |
|---|---|
| **PUB** | Published by NASA or ESA |
| **PUB-M** | Published by the manufacturer (L3Harris, Lockheed Martin, Airbus) or a partner agency (DLR) |
| **SEC** | Only found in a secondary source (Wikipedia, press). Verify before citing it to the judges |
| **CALC** | Calculated from published data |
| **ASM** | Educational assumption of the model |
| **GAME** | Fictional game balance (not real data) |

Reviewed on 2026-11-14 against `main` at `e290427`.

## 1. SLS launcher (complete service)

| Data | Value | Unit | Source | Status | Use in the game |
|---|---:|---|---|---|---|
| Payload to the Moon, Block 1 | more than 27 (59,500 lb) | t | [1] | PUB | `ART-SLS-BLOCK1.payloadCapacity` = 27,000 kg |
| Payload to the Moon, crewed Block 1B | more than 38 (84,000 lb) | t | [1] | PUB | `ART-SLS-BLOCK1B.payloadCapacity` = 38,000 kg |
| Payload to the Moon, cargo-only Block 1B | more than 42 (93,000 lb) | t | [1] | PUB | Not used |
| Block 2 to deep space | up to 46 | t | [1] | PUB | Not used |
| Maximum thrust, Block 1 | 8.8 million (≈ 39.1 MN) | lbf | [1] | PUB (MN = CALC) | Reference only |
| Height, Block 1 | 322 | ft | [1] | PUB | Reference only |
| Loaded mass, Block 1 | 5.75 million | lb | [1] | PUB | Reference only |
| Core stage | 4 RS-25 engines; 733,000 gallons of LH2/LOX | — | [1] | PUB | Reference only |
| Solid rocket boosters (each) | 5 segments; 3.6 million lbf; 1.6 million lb; 177 ft | — | [1] | PUB | Reference only |
| Price Block 1 / Block 1B | 1,250 / 1,780 | credits | Catalog V8.3 | GAME | SpaceMart price |
| Launcher mass in the model | 0 | kg | Catalog V8.3 | ASM | The SLS only provides capacity |

## 2. Engines (annex, not playable)

| Engine | Data | Value | Unit | Source | Status |
|---|---|---:|---|---|---|
| RS-25 (core stage) | Maximum thrust (SLS operation) | 512,000 | lbf | [1] | PUB |
| RS-25 | Thrust at 109 %: sea level / vacuum | 418,000 / 512,300 | lbf | [6] | PUB-M |
| RS-25 | Vacuum Isp | 452.3 | s | [7] | PUB-M |
| RS-25 | Dry mass | 7,774 | lb | [7] | PUB-M |
| RS-25 ×4 | Combined thrust | ≈ 2 million | lbf | [1] | PUB |
| RL10B-2 (ICPS, Block 1) | Thrust | 24,750 | lbf | [1], [5] | PUB |
| RL10B-2 | Isp | 465.5 | s | [5] | PUB-M |
| RL10B-2 | Mass | 664 | lb | [5] | PUB-M |
| RL10C-3 ×4 (EUS, Block 1B) | Combined thrust | 97,000 | lbf | [1] | PUB |
| RL10C-3 | Thrust / Isp (each) | 24,340 / 460.1 | lbf / s | [8] | PUB-M (read from a column table: verify) |
| OMS-E / AJ10 (ESM) | Thrust | 26.6 | kN | [12] | SEC |
| R4D-11 (ESM, 8 auxiliary) | Combined thrust | 3.92 | kN | [12] | SEC |

**Tanks / stages:** NASA does not publish the propellant masses of the ICPS or the EUS in [1]. They remain pending. **They are not made up**.

## 3. Orion and the European Service Module (ESM)

| Data | Value | Unit | Source | Status | Use in the game |
|---|---:|---|---|---|---|
| Crew / undocked duration | 4 / up to 21 | people / days | [3] | PUB | `crewCapacity` = 4, `maxMissionDays` = 21 |
| Gross liftoff mass (CM + ESM + LAS) | 78,000 | lb | [3] | PUB | Reference only |
| Crew module (CM) mass at liftoff | 10,387 | kg | [10] | PUB-M via SEC | `ART-CM-ORION.dryMass` |
| Mass at translunar injection (TLI) | 26,536 | kg | [10] | PUB-M via SEC | Basis of `tliExtraMass` |
| Mass after TLI | 25,854 | kg | [10] | PUB-M via SEC | Basis of the AM-17/18 model |
| `tliExtraMass` | 682 = 26,536 − 25,854 | kg | — | CALC | `DestinationData.tliExtraMass` |
| Usable ESM propellant | 8,618 (19,000 lb) | kg | [10] | PUB-M via SEC | **Not used** (see ESM note) |
| ESM propellant | 8.6 | t | [9], [11] | PUB-M | Reference |
| ESM mass at liftoff | just over 13 | t | [9], [11] | PUB-M | Reference |
| ESM dry mass (preliminary design review) | 3.5 | t | [13] | PUB (NTRS, 2014) | Reference |
| ESM engines | 33: 1 main, 8 auxiliary R4D-11, 24 reaction control | — | [4] | PUB | Reference only |
| Solar arrays | 4 wings, 15,000 gallium arsenide cells | — | [4] | PUB | Reference only |
| Solar array power | ≈ 11 | kW | [12] | SEC | `ART-ESM-ORION.power` = 11,000 W |
| ESM Isp (model) | 315.1 | s | Catalog V8.3 | ASM (not verified in this audit) | `ART-ESM-ORION.isp` |
| **ESM dry mass in the model** | 7,667 | kg | 25,854 − 10,387 − 7,800 | CALC / ASM | `ART-ESM-ORION.dryMass` |
| **ESM propellant in the model** | 7,800 | kg | Mission V13 | ASM | `ART-ESM-ORION.fuelMass` |
| Battery / base power draw | 18,000 / 2,500 | Wh / W | Catalog V8.3 | ASM | Model power |

### Note on `ART-ESM-ORION` (audit items 5 to 7)

- **7,667 kg is not the official ESM dry mass.** It is a **model allocation**: what remains of the post-TLI mass (25,854 kg) after subtracting the capsule (10,387 kg) and the model propellant (7,800 kg). In practice it therefore includes residual propellant, consumables and the adapter. Published dry-mass references: 3.5 t in the preliminary design review [13]; about 6.2 t in a secondary source [12].
- **7,800 kg is the propellant available in the model after translunar injection**, not the full load of the real ESM: 8.6 t published [9], [11], or 8,618 kg usable [10]. It is an assumption of mission V13 that leaves a reserve for maneuvers the model does not simulate.
- **Impact of changing it:** with 8,618 kg and the same total mass, Orion's Δv in scenario 1 would rise from 1,103 to about 1,246 m/s. Far fewer of the 4,096 combinations would fail on Δv (622 fail today). **It must not be replaced** without recalculating the whole mission and the balance (decision D4).

## 4. Mission (`MisionArtemis.asset`, V13)

| Data | Value | Unit | Source | Status |
|---|---:|---|---|---|
| Required Orion Δv | 1,060 | m/s | See `DELTA_V_BY_DESTINATION.md` | CALC + ASM (900 NASA reference + 40 + 120) |
| Crew | 4 | people | [3] | PUB (Orion capacity) |
| Duration | 12 (and 15 in the tests) | days | Mission V13 | ASM / GAME |
| Water / oxygen / food per person per day | 2.5 / 0.84 / 0.75 | kg | Mission V13 | ASM (typical planning values; not verified in this audit) |
| Baseline water / oxygen / food in Orion | 135 / 47 / 44 | kg | Mission V13 | ASM |
| Hours without sunlight covered by the battery | 1.5 | h | Mission V13 | ASM |
| Minimum science / maximum downlink time | 40 points / 3,600 s | — | Mission V13 | GAME |

## 5. Optional modules (all GAME)

Prices, optional module masses, power draws, science points, data rates and experiment bits of `ART-CREW-*`, `ART-PWR-BACKUP`, `ART-RAD-SHIELD`, `ART-NAV-LUNAR`, `ART-COMMS-DS` and `ART-SCI-*` are **fictional game balance**. They are inspired by real systems, but **must not be presented as NASA values**.

## Sources

1. NASA — Space Launch System (reference). https://www.nasa.gov/reference/space-launch-system/
2. NASA — SLS Block 1B (reference). https://www.nasa.gov/reference/sls-space-launch-system-block-1b/ (could not be opened during this audit; the Block 1B data comes from [1])
3. NASA — Orion overview. https://www.nasa.gov/humans-in-space/orion-spacecraft/orion-overview/
4. NASA — European Service Module (reference). https://www.nasa.gov/reference/european-service-module/
5. L3Harris — RL10 engine. https://www.l3harris.com/all-capabilities/rl10-engine
6. L3Harris — RS-25 spec sheet (2024). https://l3harris.com/sites/default/files/2024-07/l3harris-ar-rs-25-spec-sheet.pdf
7. L3Harris — RS-25 spec sheet (2023). https://www.L3Harris.com/sites/default/files/2023-07/AJRD-LHX_RS-25_SpecSheet_CMYK_061423_APPROVED.pdf
8. L3Harris — RL10 spec sheet (2024). https://l3harris.com/sites/default/files/2024-07/l3harris-ar-rl10-spec-sheet.pdf and https://www.l3harris.com/sites/default/files/2024-01/AJRD-LHX_RL10_SpecSheet_CMYK_APPROVED_011024.pdf
9. Airbus — Orion European Service Module. https://airbus.com/en/space/exploration/moon/orion-european-service-module
10. Lockheed Martin, figures quoted in Plant Services, "Inside Lockheed Martin's Orion crew capsule". https://www.plantservices.com/industrial-science-report/article/55378102/inside-lockheed-martins-orion-crew-capsule (find the official Lockheed sheet to cite to the judges)
11. DLR — The European Service Module. https://www.dlr.de/en/research-and-transfer/featured-topics/mission-moon/artemis-programme/the-european-service-module-esm
12. Wikipedia — European Service Module (secondary). https://en.wikipedia.org/wiki/European_Service_Module
13. NASA NTRS 20140010527 — The MPCV European Service Module (PDR). https://ntrs.nasa.gov/citations/20140010527

The ESA pages ([ESM](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/Orion/European_Service_Module), [ESM-4](https://www.esa.int/ESA_Multimedia/Images/2022/10/European_Service_Module-4_in_production)) could not be opened during this audit. The ESM data was taken from NASA, Airbus, DLR and NTRS. They should be checked by hand.
