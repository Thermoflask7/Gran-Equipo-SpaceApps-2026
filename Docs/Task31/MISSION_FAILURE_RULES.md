# Task 31 — Mission failure, abort and recovery rules

Author: claude_c (Cristina). Branch `ia/claude_c/mission-failure-rules`, built on `main` at `ae33d5d`.

The thresholds below are **gameplay values for an educational game**, chosen for playability.
They are **not** NASA flight-certification limits. P4/P5 should confirm them after playtesting.

## 1. Outcomes

| Outcome | When | `ScoreResult.status` | Extra |
|---|---|---|---|
| SUCCESS | Flight finished, every gate passed | `SUCCESS` | `competitive = total` |
| MISSION FAILURE | Flight finished, a mandatory objective missed (science, downlink, planning gates) or the return failed | `FAILURE` | `competitive = 0`, `total` kept |
| MISSION ABORT | A critical in-flight problem ended the mission during departure or lunar orbit | `FAILURE` | `aborted = true`, gate `MISSION_ABORT` + phase code, `failureReason` |
| PENDING | Not launched or flight not finished, and no failure recorded | `PENDING` | — |
| INVALID | Mission data or scoring inputs are invalid | `INVALID` | — |

A recorded failure is never `PENDING` and can never become `SUCCESS` later.

## 2. Three levels

| Level | Source | What happens |
|---|---|---|
| A — Minor | Good events, bad events without a minigame, planned minigames that are not safety-critical, any research result | Flight continues. Existing points, money and lesson apply. A negative `scoreChange` or `moneyChange` alone **never** fails the mission. |
| B — Serious (recoverable) | A bad event with a `minigameRole` (default for every existing P5 event that requests a minigame) | Its minigame decides. Non-critical result = resolved (poor = resolved with difficulty, fewer points). Critical result = **one more recovery attempt** (`seriousRetries = 1`). Only if that attempt is also critical the mission aborts. |
| C — Critical | Departure piloting (and return piloting, if a return piloting minigame is planned), or an event explicitly marked `severity = Critical` | A critical result ends the mission: departure / lunar orbit = **abort**, return = **failed return**. `criticalRetries = 0`. |

## 3. Performance bands

Judged on `effective = raw x SkillMultiplier` (both clamped to 0..1, NaN = 0). The effective value is **only** used for the
threshold: `MissionResults` stores the raw result and `MissionScoring` applies the skill once for points.

| Effective | Band | Safety-critical action | Other actions |
|---|---|---|---|
| 0.00–0.19 | Critical | Fail (or retry for Serious incidents) | Continue (log note) |
| 0.20–0.39 | Poor | Continue, fewer points | Continue, fewer points |
| 0.40–0.69 | Acceptable | Continue | Continue |
| 0.70–0.89 | Good | Continue | Continue |
| 0.90–1.00 | Excellent | Continue | Continue |

**Rationale (proposal for P4/P5 approval):**
- A trained pilot (skill 0.8–1.0) only fails the departure with a raw result below ~0.20–0.25: an attentive player will not fail.
- An untrained, weak pilot (skill 0.3) needs a raw result of ~0.67: crew choice and training clearly matter.
- A missing crew member counts as skill 0.5 (existing `SkillMultiplier` rule): fail below raw 0.40.
- No random instant failures: every failure needs a critical result in a minigame the player actually played, or an invalid spacecraft.
- Values live in `MissionFailureSettings` (Create > SpaceApps > Mission failure settings; drag it to `FlightPhases.failureSettings`). Without an asset the defaults above are used.

## 4. Rules by phase

**Departure.** At the start of the phase, in a real launched game (`flight.launched`), an invalid spacecraft
(`RocketPhysics.IsRocketValid` false or no mission) aborts with `DEPARTURE_FAILURE`; training cannot compensate. A scene tested on its
own (not launched) is not judged. A critical piloting result aborts with `DEPARTURE_FAILURE`; `departureDone` stays false and the flight
never reaches lunar orbit. No launch Δv or TWR is used (the SLS is a complete launch service).

**Lunar orbit.** Needs a successful departure (`MarkPhaseDone` refuses `lunarOrbitDone` without `departureDone`). Research never aborts:
scientific success is graded only by the existing `SCIENCE` / `NO_DOWNLINK` gates. An unresolved Serious/Critical maintenance incident
aborts with `LUNAR_ORBIT_FAILURE`; `lunarOrbitDone` stays false.

**Return.** `safeReturnDone` needs `departureDone` and `lunarOrbitDone` and no recorded failure. A critical return action (future return
piloting minigame, or `FlightPhases.FailMission(EventPhase.Return, ...)`) records `RETURN_FAILURE`: `flightFinished = true`,
`safeReturnDone = false`, not an abort. The normal ending never marks a safe return after a failure.

## 5. Implemented changes

| File | Change |
|---|---|
| `Core/MissionResults.cs` | `failureCode`, `failureReason`, `failurePhase`, `aborted`, `recoveryAttempts`, `recoveryCompleted`, `HasFailed`; all reset in `Reset()` (so `ResetGame()` clears them). |
| `Core/EventSeverity.cs` (new), `Core/EventData.cs` | Optional `severity` field (`Auto` default). Existing assets: bad + `minigameRole` = Serious, else Minor. No existing event becomes Critical. |
| `Mision/MissionFailureSettings.cs` (new) | Thresholds and retries (ScriptableObject, optional). |
| `Mision/MissionFailureRules.cs` (new) | Pure rules: `Effective`, `BandOf`, `SeverityOf`, `Judge`, `PreflightProblem`, `RecordFailure` (once only), messages and codes. |
| `Mision/FlightPhases.cs` | `CompleteMinigame(role, raw)`, `FailMission(phase, code, reason)`, `RequestMinigame(role, severity, source)`, `Terminated`, `MissionTerminated` event, `failureSettings`. `MarkPhaseDone` refuses milestones after a failure and out of order. Update stops after a failure. Missing handlers are still skipped safely. |
| `Mision/LaunchScreen.cs` | `Finish()` does not log a safe splashdown after a failure, and the end panel shows "Mission aborted"/"Mission failed" with the reason. |
| `Mision/Maintenance/MaintenanceFlightHook.cs` | Calls `CompleteMinigame(Engineering, RawResult)` (raw, once). A retry reopens the minigame on the next frame. |
| `Core/MissionScoring.cs`, `Core/ScoreResult.cs` | A recorded failure forces `FAILURE` (never PENDING), adds `MISSION_ABORT` and the phase code first, keeps the other gates, no duplicates. `ScoreResult.aborted` and `failureReason`. **Point weights unchanged (45/55).** `MinExperimentFraction`/`MinDownlinkFraction` unchanged (0). |
| `Core/ResultsScreen.cs` | "Mission aborted" title, the failure reason, "returned safely, but the scientific objectives were incomplete", texts for the new gates and an educational lesson per failure code. |

## 6. Scenario table

Milestone points are `8 x` each milestone reached (`MissionScoring`). "Official" is `competitive`.

| # | Inputs | Threshold hit | Result state | Milestones | Gates added | Official |
|---|---|---|---|---|---|---|
| S1 | Full valid game, good minigames, science done | none | SUCCESS | 24 | — | = total |
| S2 | Departure piloting raw 0.6, pilot skill 0.5 (eff. 0.30) | Poor | continues | as flown | — | = total if the rest passes |
| S3 | Departure piloting raw 0.1, skill 0.8 (eff. 0.08) | Critical, safety-critical | ABORT in departure | 0 | `MISSION_ABORT`, `DEPARTURE_FAILURE` (+ `DEPARTURE`, `LUNAR_ORBIT`, `RETURN`) | 0 |
| S4 | Bad event (Auto) asks maintenance, raw 0.8, skill 0.7 | none | resolved, continues | as flown | — | = total if the rest passes |
| S5 | Same event, raw 0.05 then raw 0.05 again, in lunar orbit | Critical twice | ABORT in lunar orbit | 8 | `MISSION_ABORT`, `LUNAR_ORBIT_FAILURE` (+ `LUNAR_ORBIT`, `RETURN`) | 0 |
| S6 | Same event, raw 0.05 then raw 0.7 | Critical, then resolved | continues | as flown | — | = total if the rest passes |
| S7 | Event explicitly `Critical`, repair raw 0.05 | Critical, no retry | ABORT in that phase | depends on phase | `MISSION_ABORT` + phase code | 0 |
| S8 | Bad event with -3 score / -500 credits, no minigame | none | continues | as flown | — (event points apply, max ±3) | = total if the rest passes |
| S9 | No piloting/research minigame in the scene (tasks 12/14 missing) | none | flight completes | 24 | `SCIENCE`, `NO_DOWNLINK` (no data was produced) | 0 |
| S10 | Return piloting planned, raw 0.05 | Critical | FAILED RETURN (not abort) | 16 | `RETURN_FAILURE` (+ `RETURN`) | 0 |
| S11 | Launched with an invalid rocket | Preflight | ABORT in departure | 0 | `MISSION_ABORT`, `DEPARTURE_FAILURE`, `ROCKET_INVALID`… | 0 |
| S12 | Flight safe, `experimentFraction`/`downlinkFraction` not set | — | FAILURE (science) | 24 | `SCIENCE`, `NO_DOWNLINK` | 0 |

## 7. Tests

Menu **SpaceApps → Mission failure → Tests** (`Mision/Editor/MissionFailureTests.cs`, edit mode). It covers the 22 requested cases:
pure rules (bands, severity, judge, record once, milestones, raw storage, NaN), scoring (abort/return/science-only states, no duplicates,
official 0 with diagnostic kept, no PENDING after failure, reset) and a real `FlightPhases` + `LaunchScreen` pair driven by reflection
(skipped handlers, recovery, retry then abort, duplicate/wrong/late callbacks, queued minigames, preflight abort, normal ending).
At the end it runs **Scoring tests** and **Flight phases tests** again (case 21). Also run **SpaceApps → Physics tests** and
**Balance tests**: no physics code changed.

**Status: not run yet.** The author had no Unity editor available; the results must be checked in Unity before merging.

**Manual Play mode check (02_Lanzamiento):** add the maintenance prefab + `MaintenanceFlightHook`; temporarily set one P5 event to
`probability = 1` and `minigameRole = Engineering`; play: a good repair resumes; a very bad repair asks for a second attempt; two very bad
repairs show "Mission aborted" with the reason; Results shows official 0, the learning score and the lesson.

## 8. Handoff for P4 (tasks 12, 13, 14)

```csharp
flightPhases.RegisterMinigame(TrainingFocus.Piloting, OpenPiloting); // in Awake
// when the player finishes (RAW 0..1, never x skill):
flightPhases.CompleteMinigame(TrainingFocus.Piloting, raw);
```
- `CompleteMinigame` replaces `SetResult` + `MinigameDone` (both still work but skip the failure rules). It ignores duplicate, late or wrong-role calls.
- After a `Retry` the same handler is called again on the next frame: the minigame must be able to reopen.
- Research (task 14) must also write `flight.experimentFraction` and `flight.downlinkFraction` (0..1, downlink ≤ experiments). A weak result never aborts. Do not write fake values.
- A critical action not covered by a minigame can call `FailMission(phase, code, reason)` once.

## 9. Decisions that need team approval

1. Default thresholds (0.20 / 0.40 / 0.70 / 0.90) and `seriousRetries = 1`, `criticalRetries = 0`.
2. Departure piloting is safety-critical by default (`departurePilotingIsCritical = true`).
3. No P5 event is marked `Critical`. P5 may mark some (for example a major propulsion failure) after playtesting.
4. A recovery attempt averages its repair result with the first attempt (existing `SetResult` rule).
5. The 0.5 science thresholds (`MinExperimentFraction`/`MinDownlinkFraction`) remain a separate proposal, unchanged here.

## 10. Outstanding dependencies

- Tasks 12 (piloting) and 14 (research) are not implemented: piloting is skipped (no departure failure possible from it) and science stays at 0, so the mission ends in FAILURE by the `SCIENCE`/`NO_DOWNLINK` gates, as before.
- The maintenance prefab and `MaintenanceFlightHook` are not yet in `02_Lanzamiento` (scene work, P4).
- The Results panel (task 25) must be in `03_Resultados` to show the new texts.
