# Δv needed by leg and destination (P5)

Technical reference document. **It does not add playable destinations**: the only playable mission is still the Artemis-style lunar orbit (NRHO).

## Terms

| Acronym | What it is |
|---|---|
| **LEO** | Low Earth orbit (a few hundred km up). The first step of any mission. |
| **TLI** | Translunar injection: the burn that takes the spacecraft out of LEO toward the Moon. |
| **NRHO** | Near-rectilinear halo orbit around the Moon: very elongated, close to the lunar south pole. It is the orbit of Artemis and the Gateway. Arriving and leaving costs little Δv. |
| **LLO** | Low lunar orbit (about 100 km). Closer to the surface, but entering and leaving costs much more Δv. |
| **TEI** | Trans-Earth injection: the return burn to Earth from lunar orbit. |

## Legs (approximate NASA values)

Main source: NASA Architecture Workshop, June 2023, "The Artemis Orbit" [A]. Rounded architecture values, not the requirements of a specific mission.

| Leg | Δv | Approx. time | Who does it (Artemis, Block 1) | Source | Status |
|---|---:|---|---|---|---|
| Earth → LEO | 10,000 m/s | — | SLS: boosters + core stage | [A] | PUB |
| LEO → TLI | 3,200 m/s | — | SLS: upper stage (ICPS with 1 RL10 on Block 1; EUS on Block 1B) | [A] | PUB |
| TLI → NRHO (arrival) | 450 m/s | ~5 days | Orion (ESM) | [A], [B] | PUB |
| NRHO → return trajectory (TEI) | 450 m/s | — | Orion (ESM) | [A], [B] | PUB |
| TLI → global LLO | 900 m/s | ~4 days | Orion (ESM) | [A] | PUB |
| LLO → return to Earth | 900 m/s | ~4 days | Orion (ESM) | [A] | PUB |
| NRHO total: arrival + departure | 1,000 m/s ("as-built") | — | Orion | [A] | PUB |

[B] (NASA NTRS, 2025) gives a range of 400 to 450 m/s for TLI→NRHO and for NRHO→TEI, which confirms [A].

**Why NRHO and not LLO:** going to LLO and back costs about 1,800 m/s (900 + 900), and NRHO about 900 m/s (450 + 450). Orion **does not carry enough propellant** for LLO with a crew and a return. That is why Artemis uses NRHO, and the landing is done by another vehicle (HLS).

### Watch out for who performs TLI

On Artemis I, TLI was performed by the **SLS upper stage (ICPS)**. NASA's ESM page says the ESM main engine performs "the translunar injection burn" [C]. Our model follows the former: **the SLS delivers the spacecraft already at TLI** (`payloadCapacity` = kg to translunar). Orion only pays for the lunar legs. If the judges ask, this is a simplification consistent with Block 1. The difference in [C] is documented so it is not presented as a contradiction.

## Orion's budget in the game

| Item | Δv | Status |
|---|---:|---|
| Arrival at NRHO | 450 m/s | NASA reference [A] |
| Departure from NRHO toward Earth | 450 m/s | NASA reference [A] |
| **Reference maneuvers** | **900 m/s** | CALC (450 + 450) |
| Trajectory corrections | 40 m/s | **Model assumption** |
| Reserve | 120 m/s | **Model assumption** |
| **Educational total (`requiredDeltaV`)** | **1,060 m/s** | CALC + ASM |

**1,060 m/s is not an official NASA requirement.** It is the sum of 900 m/s of published reference plus 160 m/s of team assumptions. It must be presented that way.

Check with the model: scenario 1 (Block 1 + antenna + 2 instruments) gives **1,103 m/s** with Tsiolkovsky (`Isp × g0 × ln(m0/mf)`, Isp = 315.1 s, 7,800 kg of propellant). That leaves 43 m/s above the required 1,060.

## What each vehicle does (summary for the pitch)

- **SLS:** Earth → LEO → TLI (≈ 13,200 m/s in total). The game does not calculate it: the player chooses Block 1 (27 t to TLI) or Block 1B (38 t). That is why there is no launcher Δv or TWR.
- **Orion/ESM:** TLI → NRHO → return (900 m/s + corrections + reserve). This is what the game calculates with Tsiolkovsky.

## Sources

- [A] NASA Architecture Workshop (June 2023), "The Artemis Orbit / Cis-Lunar Architecture". https://www.nasa.gov/wp-content/uploads/2023/10/nrho-artemis-orbit.pdf. See also NASA's technical paper "Why NRHO: the Artemis Orbit": https://nasa.gov/wp-content/uploads/2023/10/acr22-wp-why-nrho-the-artemis-orbit.pdf
- [B] NASA NTRS 20250000718, "Enhancing Fast Transfer Launch Availability". https://ntrs.nasa.gov/api/citations/20250000718/downloads/Enhancing%20Fast%20Transfer%20Launch%20Availability.pdf
- [C] NASA — European Service Module. https://www.nasa.gov/reference/european-service-module/
