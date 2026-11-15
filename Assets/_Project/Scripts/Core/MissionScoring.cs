using System.Collections.Generic;
using UnityEngine;

// Final score from 0 to 100 (task 16): Cristina's V16 system (Drive: CRISTINA/PUNTUACION)
// adapted to the lunar physics (the launcher is a complete service, no stages are assembled).
//
//   Total = Planning (0-45) + Mission (0-55)
//   Planning: engineering 24 + budget 7 + training 8 + weather 6,     +/- 3 from events
//     Engineering: launcher capacity 7 + spacecraft delta-v 7 + generation 3 + batteries 3 + consumables 4
//   Mission:  flight stages 24 + minigames 21 + science 10,           +/- 3 from events
//
// There are two scores: the diagnostic one (total, for learning, shown even if the mission fails)
// and the official one (competitive = total only if the mission succeeded, otherwise 0).
// Winning depends on the mandatory conditions (gates), not on the score.
//
// Use in Results:  ScoreResult r = MissionScoring.SaveFinalScore();
public static class MissionScoring
{
    // Minimum experiments done and data sent to Earth needed to win (0 to 1).
    // Today doing anything (> 0) is enough. Team PROPOSAL, pending approval: 0.5 for both.
    public const float MinExperimentFraction = 0f;
    public const float MinDownlinkFraction = 0f;

    // ---------- Score from the game ----------

    // Calculates the game's score and stores it in gm.planningScore and gm.missionScore.
    // It is recalculated from scratch (it does not add), so calling it several times does not count events twice.
    public static ScoreResult SaveFinalScore()
    {
        GameManager gm = GameManager.Instance;
        ScoreResult r = EvaluateGame(gm);
        gm.planningScore = r.planning;
        gm.missionScore = r.mission;
        return r;
    }

    // Same as SaveFinalScore but without touching GameManager (to show progress before the end).
    public static ScoreResult EvaluateGame(GameManager gm)
    {
        if (gm.mission == null)
        {
            ScoreResult empty = new ScoreResult();
            empty.status = "INVALID";
            empty.problem = "No mission loaded. To test: Play and SpaceApps -> Results screen -> Test: sample game.";
            return empty;
        }
        ScoreResult r = Evaluate(InputsFromGame(gm));
        if (r.problem != "") return r;

        MissionResults f = gm.flight;
        if (!f.HasFailed && (!f.launched || !f.flightFinished))
        {
            // Not launched yet or the flight has not finished: neither won nor lost
            r.status = "PENDING";
            r.failedGates.Clear();
        }
        else
        {
            // Conditions that only exist in the game (in addition to those in Evaluate)
            List<string> gameGates = new List<string>();
            // Task 31: an in-flight failure or abort goes first; the other failed gates are kept
            if (f.aborted) gameGates.Add(MissionFailureRules.AbortGate);
            if (f.HasFailed) gameGates.Add(f.failureCode);
            if (!RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission)) gameGates.Add("ROCKET_INVALID");
            if (!gm.IsCrewReady()) gameGates.Add("CREW_NOT_READY");
            for (int g = gameGates.Count - 1; g >= 0; g--)
            {
                if (r.failedGates.Contains(gameGates[g])) gameGates.RemoveAt(g); // no duplicate reasons
            }
            if (gameGates.Count > 0) r.failedGates.InsertRange(0, gameGates);
            if (r.failedGates.Count > 0) r.status = "FAILURE"; // a terminal failure is never PENDING or SUCCESS
            r.aborted = f.aborted;
            r.failureReason = f.failureReason;
        }
        r.competitive = r.status == "SUCCESS" ? r.total : 0f;
        return r;
    }

    // Fills in the inputs with the state of GameManager and RocketPhysics (lunar catalog).
    public static ScoreInputs InputsFromGame(GameManager gm)
    {
        ScoreInputs i = new ScoreInputs();
        List<PartData> parts = gm.rocketParts;
        DestinationData mission = gm.mission;
        PartData launcherPart = RocketPhysics.LaunchService(parts);

        i.deliveryCapacity = launcherPart != null ? launcherPart.payloadCapacity : 1f; // no launcher: 0 points (and the rocket is not valid)
        i.tliMass = RocketPhysics.TliMass(parts, mission);
        i.craftDvAvailable = RocketPhysics.SpacecraftDeltaV(parts);
        i.craftDvRequired = mission.requiredDeltaV;
        i.powerNetW = RocketPhysics.PowerBalance(parts);
        i.loadW = RocketPhysics.PowerConsumption(parts);
        i.batteryWh = RocketPhysics.BatteryWh(parts);

        float personDays = mission.crewCount * mission.missionDays;
        i.waterMargin = RocketPhysics.WaterMargin(parts, mission);
        i.waterRequired = personDays * mission.waterPerPersonDay;
        i.oxygenMargin = RocketPhysics.OxygenMargin(parts, mission);
        i.oxygenRequired = personDays * mission.oxygenPerPersonDay;
        i.foodMargin = RocketPhysics.FoodMargin(parts, mission);
        i.foodRequired = personDays * mission.foodPerPersonDay;

        // Savings are graded with the money there was at launch: flight events
        // change gm.money but do not change a planning decision already made.
        float money = gm.flight.launched ? gm.flight.moneyAtLaunch : gm.money;
        i.startingMoney = gm.startingMoney;
        i.moneyAtLaunch = Mathf.Clamp(money, 0f, gm.startingMoney);

        i.pilotSkill = gm.SkillMultiplier(gm.pilot, TrainingFocus.Piloting);
        i.engineerSkill = gm.SkillMultiplier(gm.engineer, TrainingFocus.Engineering);
        i.scientistSkill = gm.SkillMultiplier(gm.scientist, TrainingFocus.Research);
        i.weatherRisk = Mathf.Clamp01(gm.weatherRisk);

        MissionResults f = gm.flight;
        i.departureDone = f.departureDone ? 1f : 0f;
        i.lunarOrbitDone = f.departureDone && f.lunarOrbitDone ? 1f : 0f;
        i.safeReturnDone = f.departureDone && f.lunarOrbitDone && f.safeReturnDone ? 1f : 0f;
        i.pilotingRaw = Played(f.pilotingResult);
        i.repairRaw = Played(f.repairResult);
        i.researchRaw = Played(f.researchResult);
        i.experimentFraction = Played(f.experimentFraction);
        i.downlinkFraction = Mathf.Min(Played(f.downlinkFraction), i.experimentFraction);

        i.sciencePoints = RocketPhysics.SciencePoints(parts);
        i.requiredSciencePoints = mission.minSciencePoints;

        foreach (EventData e in gm.eventsHappened)
        {
            if (e == null) continue;
            if (e.phase == EventPhase.Planning) i.planningEventSum += e.scoreChange;
            else i.missionEventSum += e.scoreChange;
        }
        return i;
    }

    // -1 (not played) counts as 0
    static float Played(float value)
    {
        if (value < 0f) return 0f;
        return Mathf.Clamp01(value);
    }

    // ---------- Formula ----------

    public static ScoreResult Evaluate(ScoreInputs i)
    {
        ScoreResult r = new ScoreResult();
        r.problem = Problem(i);
        if (r.problem != "")
        {
            r.status = "INVALID";
            return r;
        }

        // Engineering (24). Points grow with the margin and reach the maximum with some extra margin.
        // launcher (7): full with 5% of its capacity free
        r.deliveryPoints = 7f * Mathf.Clamp01((i.deliveryCapacity - i.tliMass) / (0.05f * i.deliveryCapacity));
        // spacecraft (7): full with 8% extra delta-v
        r.craftDvPoints = 7f * Mathf.Clamp01((i.craftDvAvailable - i.craftDvRequired) / (0.08f * i.craftDvRequired));
        // Generation (3): full if 25% of the draw is left over
        r.powerPoints = 3f * Mathf.Clamp01(i.powerNetW / (0.25f * Mathf.Max(i.loadW, 1f)));
        // Batteries (3): full if they last 50% longer than the time without sunlight
        float outageWh = i.loadW * i.outageHours;
        r.batteryPoints = 3f * Mathf.Clamp01((i.batteryWh - outageWh) / (0.5f * Mathf.Max(outageWh, 1f)));
        // Consumables (4): each one full with a 20% reserve over what is consumed
        r.consumablePoints = 4f * (Reserve(i.waterMargin, i.waterRequired)
                                 + Reserve(i.oxygenMargin, i.oxygenRequired)
                                 + Reserve(i.foodMargin, i.foodRequired)) / 3f;
        r.engineering = r.deliveryPoints + r.craftDvPoints + r.powerPoints + r.batteryPoints + r.consumablePoints;

        // Budget (7): full if at least 15% is left over
        r.budgetPoints = 7f * Mathf.Clamp01(i.moneyAtLaunch / (0.15f * i.startingMoney));
        // Training (8) and weather (6)
        r.trainingPoints = 8f * (i.pilotSkill + i.engineerSkill + i.scientistSkill) / 3f;
        r.weatherPoints = 6f * (1f - i.weatherRisk);

        r.planningBase = r.engineering + r.budgetPoints + r.trainingPoints + r.weatherPoints;

        // Mission: stages (24), minigames (21) and science (10)
        r.milestonePoints = 8f * (i.departureDone + i.lunarOrbitDone + i.safeReturnDone);
        r.minigamePoints = 7f * (i.pilotingRaw * i.pilotSkill + i.repairRaw * i.engineerSkill + i.researchRaw * i.scientistSkill);
        r.sciencePoints = 4f * Mathf.Clamp01(i.sciencePoints / i.requiredSciencePoints)
                        + 3f * i.experimentFraction
                        + 3f * i.downlinkFraction;
        r.missionBase = r.milestonePoints + r.minigamePoints + r.sciencePoints;

        // Events: at most +/- 3 per stage
        r.planningEvents = Mathf.Clamp(i.planningEventSum, -3f, 3f);
        r.missionEvents = Mathf.Clamp(i.missionEventSum, -3f, 3f);
        r.planning = Mathf.Clamp(r.planningBase + r.planningEvents, 0f, 45f);
        r.mission = Mathf.Clamp(r.missionBase + r.missionEvents, 0f, 55f);
        r.total = r.planning + r.mission;

        // Mandatory conditions to win (they do not depend on the score)
        if (i.tliMass > i.deliveryCapacity) r.failedGates.Add("DELIVERY");
        if (i.craftDvAvailable < i.craftDvRequired) r.failedGates.Add("CRAFT_DELTA_V");
        if (i.powerNetW < 0f) r.failedGates.Add("POWER");
        if (i.batteryWh < outageWh) r.failedGates.Add("BATTERY");
        if (i.waterMargin < 0f || i.oxygenMargin < 0f || i.foodMargin < 0f) r.failedGates.Add("CONSUMABLES");
        if (i.departureDone < 1f) r.failedGates.Add("DEPARTURE");
        if (i.lunarOrbitDone < 1f) r.failedGates.Add("LUNAR_ORBIT");
        if (i.safeReturnDone < 1f) r.failedGates.Add("RETURN");
        if (i.sciencePoints < i.requiredSciencePoints || i.experimentFraction <= 0f || i.experimentFraction < MinExperimentFraction)
            r.failedGates.Add("SCIENCE");
        if (i.downlinkFraction <= 0f || i.downlinkFraction < MinDownlinkFraction) r.failedGates.Add("NO_DOWNLINK");

        r.status = r.failedGates.Count == 0 ? "SUCCESS" : "FAILURE";
        r.competitive = r.status == "SUCCESS" ? r.total : 0f;
        return r;
    }

    // Reserve of a consumable from 0 to 1: 0 if nothing is left over (or it falls short), 1 if 20% or more is left over.
    static float Reserve(float margin, float required)
    {
        return Mathf.Clamp01(margin / (0.2f * required));
    }

    // Checks that the inputs make sense. "" = all good.
    public static string Problem(ScoreInputs i)
    {
        float[] all = { i.deliveryCapacity, i.tliMass, i.craftDvAvailable, i.craftDvRequired,
            i.powerNetW, i.loadW, i.batteryWh, i.outageHours,
            i.waterMargin, i.waterRequired, i.oxygenMargin, i.oxygenRequired, i.foodMargin, i.foodRequired,
            i.startingMoney, i.moneyAtLaunch, i.pilotSkill, i.engineerSkill, i.scientistSkill, i.weatherRisk,
            i.departureDone, i.lunarOrbitDone, i.safeReturnDone, i.pilotingRaw, i.repairRaw, i.researchRaw,
            i.sciencePoints, i.requiredSciencePoints, i.experimentFraction, i.downlinkFraction,
            i.planningEventSum, i.missionEventSum };
        foreach (float v in all)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return "A value is not a number.";
        }

        if (i.craftDvRequired <= 0f) return "The required delta-v must be greater than 0.";
        if (i.deliveryCapacity <= 0f) return "The launcher capacity must be greater than 0.";
        if (i.waterRequired <= 0f || i.oxygenRequired <= 0f || i.foodRequired <= 0f) return "The crew consumption must be greater than 0.";
        if (i.requiredSciencePoints <= 0f) return "The required science points must be greater than 0.";
        if (i.startingMoney <= 0f) return "The starting budget must be greater than 0.";
        if (i.moneyAtLaunch < 0f || i.moneyAtLaunch > i.startingMoney) return "The money at launch must be between 0 and the starting budget.";
        if (i.loadW < 0f || i.batteryWh < 0f || i.tliMass < 0f || i.sciencePoints < 0f) return "A mass, power draw, battery or science value is negative.";

        float[] fractions = { i.pilotSkill, i.engineerSkill, i.scientistSkill, i.weatherRisk,
            i.pilotingRaw, i.repairRaw, i.researchRaw, i.experimentFraction, i.downlinkFraction };
        foreach (float v in fractions)
        {
            if (v < 0f || v > 1f) return "A skill, the weather, a minigame or the science is not between 0 and 1.";
        }

        if (!IsFlag(i.departureDone) || !IsFlag(i.lunarOrbitDone) || !IsFlag(i.safeReturnDone)) return "The flight stages must be 0 or 1.";
        if (i.lunarOrbitDone > i.departureDone || i.safeReturnDone > i.lunarOrbitDone) return "The flight stages must be completed in order.";
        if (i.downlinkFraction > i.experimentFraction) return "You cannot send more data than the experiments done.";
        return "";
    }

    static bool IsFlag(float v)
    {
        return v == 0f || v == 1f;
    }
}
