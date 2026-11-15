using System.Collections.Generic;
using UnityEngine;

// Mission physics calculations (task 17), with Cristina's model:
// catalog V8.3, mission V13 and physics models V9.3 (Drive, CRISTINA folder).
// Everything is static: RocketPhysics.SpacecraftDeltaV(parts), etc.
//
// How the rocket is built (the order of the parts no longer matters):
//   - You choose ONE launcher (LaunchService): SLS Block 1 (carries 27,000 kg to translunar injection)
//     or SLS Block 1B (38,000 kg). The launcher already includes its stages: no tanks or engines are assembled.
//   - The spacecraft: Orion capsule + service module + launch abort tower (mandatory)
//     + optional modules (science, antenna, reserves, batteries...).
//
// What is checked:
//   1. Mass at translunar injection (AM-19) <= launcher capacity.
//      Mass at translunar injection = spacecraft mass (without the abort tower) + mission.tliExtraMass.
//   2. Spacecraft delta-v (Tsiolkovsky with the service module, AM-17/18) >= mission.requiredDeltaV.
//   3. Power: in sunlight, what is generated >= what is consumed; in shadow (1.5 h) the battery holds up.
//   4. Crew: the capsule carries everyone, lasts the days, and there is enough water, oxygen and food.
//   5. Science: points >= mission.minSciencePoints and the data is downlinked in <= 1 hour.
//   The budget is checked by GameManager when buying (prices in credits).
public static class RocketPhysics
{
    public const float G0 = 9.80665f;      // m/s2 (standard gravity, same as Cristina's data)
    public const float OutageHours = 1.5f;  // hours without sunlight the battery must cover (mission V13)
    public static readonly int MaxCopiesPerPart = 1;  // units of the same part (catalogId). 0 = no limit. Team decision pending (see Docs/P5)

    // dv of an engine with initial mass m0 and final mass mf (kg). Isp in seconds.
    public static float DeltaV(float isp, float m0, float mf)
    {
        if (isp <= 0f || mf <= 0f || m0 <= mf) return 0f;
        return isp * G0 * Mathf.Log(m0 / mf);
    }

    // Total mass (kg): dry + fuel of all parts (the launcher weighs 0 in this model).
    public static float TotalMass(List<PartData> parts)
    {
        float total = 0f;
        if (parts == null) return total;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.dryMass + p.fuelMass;
        }
        return total;
    }

    // Sum of prices (credits).
    public static float TotalPrice(List<PartData> parts)
    {
        float total = 0f;
        if (parts == null) return total;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.price;
        }
        return total;
    }

    static int CountType(List<PartData> parts, PartType type)
    {
        int count = 0;
        if (parts == null) return count;
        foreach (PartData p in parts)
        {
            if (p != null && p.type == type) count++;
        }
        return count;
    }

    static PartData FirstOfType(List<PartData> parts, PartType type)
    {
        if (parts == null) return null;
        foreach (PartData p in parts)
        {
            if (p != null && p.type == type) return p;
        }
        return null;
    }

    // ---------- 1. Launcher and mass ----------

    // The chosen SLS (null if there is none).
    public static PartData LaunchService(List<PartData> parts)
    {
        return FirstOfType(parts, PartType.LaunchService);
    }

    // The spacecraft that travels to the Moon: everything except the launcher and the abort tower.
    public static List<PartData> Spacecraft(List<PartData> parts)
    {
        List<PartData> result = new List<PartData>();
        if (parts == null) return result;
        foreach (PartData p in parts)
        {
            if (p == null) continue;
            if (p.type == PartType.LaunchService || p.type == PartType.AbortSystem) continue;
            result.Add(p);
        }
        return result;
    }

    // Spacecraft mass right after translunar injection, with fuel (kg). AM-18.
    public static float SpacecraftMass(List<PartData> parts)
    {
        return TotalMass(Spacecraft(parts));
    }

    // Mass the launcher has to carry to translunar injection (kg). AM-19.
    public static float TliMass(List<PartData> parts, DestinationData mission)
    {
        float extra = mission != null ? mission.tliExtraMass : 0f;
        return SpacecraftMass(parts) + extra;
    }

    // Launcher capacity minus the mass at translunar injection (kg). Negative = does not fit. AM-07.
    public static float DeliveryMargin(List<PartData> parts, DestinationData mission)
    {
        PartData sls = LaunchService(parts);
        if (sls == null) return 0f;
        return sls.payloadCapacity - TliMass(parts, mission);
    }

    // ---------- 2. Spacecraft delta-v ----------

    // Spacecraft delta-v with the service module (m/s). Only uses its propellant. AM-17/18 + MOD-16.
    public static float SpacecraftDeltaV(List<PartData> parts)
    {
        List<PartData> craft = Spacecraft(parts);
        float isp = 0f;
        float fuel = 0f;
        int serviceModules = 0;
        foreach (PartData p in craft)
        {
            if (p.type == PartType.ServiceModule)
            {
                serviceModules++;
                fuel += p.fuelMass;
                isp = p.isp;
            }
        }
        if (serviceModules != 1) return 0f; // ValidationMessage reports it
        float m0 = TotalMass(craft);
        return DeltaV(isp, m0, m0 - fuel);
    }

    // Fuel that would be needed to reach a given delta-v (kg). GM-02. Useful for giving hints.
    public static float FuelNeeded(float isp, float dryMass, float targetDeltaV)
    {
        if (isp <= 0f) return 0f;
        return dryMass * (Mathf.Exp(targetDeltaV / (isp * G0)) - 1f);
    }

    // ---------- 3. Power ----------

    // Generation minus draw (W) in sunlight. Negative = not enough power. AM-13.
    public static float PowerBalance(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in Spacecraft(parts)) total += p.power - p.powerDraw;
        return total;
    }

    // Total spacecraft power draw (W).
    public static float PowerConsumption(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in Spacecraft(parts)) total += p.powerDraw;
        return total;
    }

    public static float BatteryWh(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in Spacecraft(parts)) total += p.batteryWh;
        return total;
    }

    // In shadow nothing is generated: the battery must cover the whole draw for OutageHours. AM-11/12.
    public static bool BatteryCoversOutage(List<PartData> parts)
    {
        return BatteryWh(parts) >= PowerConsumption(parts) * OutageHours;
    }

    // ---------- 4. Crew ----------

    // Water, oxygen or food left over (kg) for the whole mission. Negative = shortfall. AM-02..05.
    public static float WaterMargin(List<PartData> parts, DestinationData mission)
    {
        float stock = mission.baseWaterKg;
        foreach (PartData p in Spacecraft(parts)) stock += p.waterKg;
        return stock - mission.crewCount * mission.missionDays * mission.waterPerPersonDay;
    }

    public static float OxygenMargin(List<PartData> parts, DestinationData mission)
    {
        float stock = mission.baseOxygenKg;
        foreach (PartData p in Spacecraft(parts)) stock += p.oxygenKg;
        return stock - mission.crewCount * mission.missionDays * mission.oxygenPerPersonDay;
    }

    public static float FoodMargin(List<PartData> parts, DestinationData mission)
    {
        float stock = mission.baseFoodKg;
        foreach (PartData p in Spacecraft(parts)) stock += p.foodKg;
        return stock - mission.crewCount * mission.missionDays * mission.foodPerPersonDay;
    }

    // ---------- 5. Science ----------

    public static float SciencePoints(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in Spacecraft(parts)) total += p.sciencePoints;
        return total;
    }

    // Seconds to downlink all the instrument data to Earth. No antenna = infinity.
    public static float DownlinkSeconds(List<PartData> parts)
    {
        float bits = 0f;
        float rate = 0f;
        foreach (PartData p in Spacecraft(parts))
        {
            bits += p.experimentDataBits;
            rate += p.dataRateBps;
        }
        if (bits <= 0f) return 0f;
        if (rate <= 0f) return float.PositiveInfinity;
        return bits / rate;
    }

    // ---------- Validation ----------

    // Checks whether the spacecraft works for the mission. Returns "" if everything is fine,
    // or a message with the first problem (to show it in the rocket model).
    public static string ValidationMessage(List<PartData> parts, DestinationData mission)
    {
        if (mission == null) return "No mission assigned.";
        string missionProblem = MissionDataProblem(mission);
        if (missionProblem != "") return missionProblem;
        if (parts == null || parts.Count == 0) return "The rocket has no parts.";
        foreach (PartData p in parts)
        {
            if (p == null) return "There is an empty part in the rocket.";
            string partProblem = PartDataProblem(p);
            if (partProblem != "") return partProblem;
        }
        string repeated = RepeatedPart(parts);
        if (repeated != "") return "You can only take one unit of \"" + repeated + "\".";

        int launchers = CountType(parts, PartType.LaunchService);
        if (launchers == 0) return "Choose a launcher: SLS Block 1 or SLS Block 1B.";
        if (launchers > 1) return "You can only use one launcher.";
        if (CountType(parts, PartType.Capsule) != 1) return "The spacecraft needs an Orion capsule.";
        if (CountType(parts, PartType.ServiceModule) != 1) return "The spacecraft needs exactly one service module.";
        if (CountType(parts, PartType.AbortSystem) != 1) return "The launch abort tower that protects the crew is missing.";

        if (mission != null)
        {
            float margin = DeliveryMargin(parts, mission);
            if (margin < 0f)
                return "Too heavy for " + LaunchService(parts).partName + ": " + (-margin).ToString("N0") + " kg over. Remove modules or use a bigger launcher.";

            float dv = SpacecraftDeltaV(parts);
            if (dv < mission.requiredDeltaV)
                return "The spacecraft cannot reach lunar orbit and return: " + dv.ToString("N0") + " m/s of the " + mission.requiredDeltaV.ToString("N0") + " m/s needed.";

            PartData capsule = FirstOfType(parts, PartType.Capsule);
            if (capsule.crewCapacity < mission.crewCount)
                return "The capsule does not have room for " + mission.crewCount + " astronauts.";
            if (capsule.maxMissionDays < mission.missionDays)
                return "The capsule lasts " + capsule.maxMissionDays + " days and the mission lasts " + mission.missionDays + ".";
            if (WaterMargin(parts, mission) < 0f) return "Not enough water for the mission: add water reserves.";
            if (OxygenMargin(parts, mission) < 0f) return "Not enough oxygen for the mission: add oxygen reserves.";
            if (FoodMargin(parts, mission) < 0f) return "Not enough food for the mission: add food reserves.";
        }

        if (PowerBalance(parts) < 0f) return "Not enough power: the modules draw more than the solar arrays generate.";
        if (!BatteryCoversOutage(parts)) return "The battery does not last " + OutageHours + " hours without sunlight: add batteries.";

        if (mission != null)
        {
            float science = SciencePoints(parts);
            if (science < mission.minSciencePoints)
                return "Not enough science: " + science.ToString("0") + " of " + mission.minSciencePoints.ToString("0") + " points. Add instruments.";
            if (SciencePoints(parts) > 0f && DataBits(parts) <= 0f)
                return "The instruments do not generate data to send to Earth (check the catalog).";
            if (DownlinkSeconds(parts) > mission.maxDownlinkSeconds)
                return "The science data cannot be sent to Earth in time: add a long-range antenna.";
        }

        return "";
    }

    // Data bits generated by the spacecraft's instruments.
    public static float DataBits(List<PartData> parts)
    {
        float bits = 0f;
        foreach (PartData p in Spacecraft(parts)) bits += p.experimentDataBits;
        return bits;
    }

    // Part data that makes no physical sense (negative). "" = fine.
    public static string PartDataProblem(PartData p)
    {
        if (p.dryMass < 0f || p.fuelMass < 0f) return "\"" + p.partName + "\" has negative mass or fuel.";
        if (p.price < 0f || p.isp < 0f || p.thrust < 0f || p.power < 0f || p.powerDraw < 0f || p.batteryWh < 0f
            || p.payloadCapacity < 0f || p.crewCapacity < 0 || p.maxMissionDays < 0
            || p.waterKg < 0f || p.oxygenKg < 0f || p.foodKg < 0f
            || p.sciencePoints < 0f || p.dataRateBps < 0f || p.experimentDataBits < 0f)
            return "\"" + p.partName + "\" has a negative value in the catalog.";
        if (p.fuelMass > 0f && p.isp <= 0f) return "\"" + p.partName + "\" has fuel but no Isp.";
        return "";
    }

    // Mission data that makes no sense. "" = fine.
    public static string MissionDataProblem(DestinationData m)
    {
        if (m.requiredDeltaV <= 0f) return "The mission has no required delta-v.";
        if (m.crewCount <= 0 || m.missionDays <= 0) return "The mission needs a crew and a duration in days.";
        if (m.tliExtraMass < 0f || m.waterPerPersonDay < 0f || m.oxygenPerPersonDay < 0f || m.foodPerPersonDay < 0f
            || m.baseWaterKg < 0f || m.baseOxygenKg < 0f || m.baseFoodKg < 0f
            || m.minSciencePoints < 0f || m.maxDownlinkSeconds < 0f)
            return "The mission has a negative value.";
        return "";
    }

    // Name of the first part that appears more times than allowed ("" = none).
    public static string RepeatedPart(List<PartData> parts)
    {
        if (MaxCopiesPerPart <= 0) return "";
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (PartData p in parts)
        {
            if (p == null) continue;
            string id = string.IsNullOrEmpty(p.catalogId) ? p.partName : p.catalogId;
            if (!counts.ContainsKey(id)) counts[id] = 0;
            counts[id]++;
            if (counts[id] > MaxCopiesPerPart) return p.partName;
        }
        return "";
    }

    public static bool IsRocketValid(List<PartData> parts, DestinationData mission)
    {
        return ValidationMessage(parts, mission) == "";
    }
}
