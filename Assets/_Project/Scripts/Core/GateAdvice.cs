using System.Collections.Generic;
using UnityEngine;

// Task 35: one explanation per failed gate on the Results screen:
// what went wrong (with the real RocketPhysics numbers), the lesson and what would have fixed it
// (a part from the catalog, or the mass / module to remove).
public static class GateAdvice
{
    public static string Explain(string gate, GameManager gm)
    {
        string problem = ResultsScreen.GateText(gate);
        string lesson = "";
        string fix = "";

        if (gm != null && gm.mission != null)
        {
            List<PartData> parts = gm.rocketParts;
            DestinationData m = gm.mission;

            switch (gate)
            {
                case "DELIVERY":
                {
                    PartData sls = RocketPhysics.LaunchService(parts);
                    float tli = RocketPhysics.TliMass(parts, m);
                    float capacity = sls != null ? sls.payloadCapacity : 0f;
                    float over = tli - capacity;
                    if (sls != null)
                        problem = "Your spacecraft weighed " + Kg(tli) + " at translunar injection, but the " + sls.partName + " carries only " + Kg(capacity) + ".";
                    lesson = "The launcher sends a fixed mass to the Moon: every module you add uses part of that capacity.";
                    PartData bigger = null;
                    foreach (PartData p in Catalog())
                    {
                        if (p.type == PartType.LaunchService && p.payloadCapacity >= tli && !Has(parts, p)) bigger = p;
                    }
                    PartData remove = LightestToRemove(parts, over);
                    if (bigger != null) fix = "the " + bigger.partName + " (" + Kg(bigger.payloadCapacity) + ") would have passed";
                    if (remove != null) fix += (fix == "" ? "" : ", or ") + "removing the " + remove.partName + " (" + Kg(Mass(remove)) + ") would have passed";
                    if (fix == "") fix = "a spacecraft " + Kg(over) + " lighter would have passed";
                    fix += ".";
                    break;
                }
                case "CRAFT_DELTA_V":
                {
                    float dv = RocketPhysics.SpacecraftDeltaV(parts);
                    problem = "Orion had " + dv.ToString("N0") + " m/s of delta-v, but lunar orbit and the return need " + m.requiredDeltaV.ToString("N0") + " m/s.";
                    lesson = "dv = Isp x g0 x ln(m0/mf): with the same fuel, every extra kilogram means less delta-v.";
                    PartData esm = FirstOfType(parts, PartType.ServiceModule);
                    if (esm == null || esm.isp <= 0f || esm.fuelMass <= 0f)
                    {
                        fix = "a service module (it carries Orion's fuel) is required.";
                        break;
                    }
                    // Heaviest spacecraft that still reaches the required delta-v: m0 / (m0 - fuel) = e^(dv / (Isp g0))
                    float ratio = Mathf.Exp(m.requiredDeltaV / (esm.isp * RocketPhysics.G0));
                    float maxMass = esm.fuelMass * ratio / (ratio - 1f);
                    float over = RocketPhysics.SpacecraftMass(parts) - maxMass;
                    PartData remove = LightestToRemove(parts, over);
                    if (remove != null) fix = "removing the " + remove.partName + " (" + Kg(Mass(remove)) + ") would have passed.";
                    else fix = "a spacecraft " + Kg(over) + " lighter would have passed.";
                    break;
                }
                case "POWER":
                {
                    float draw = RocketPhysics.PowerConsumption(parts);
                    float net = RocketPhysics.PowerBalance(parts);
                    problem = "Your modules drew " + Watts(draw) + ", but the solar arrays generated only " + Watts(draw + net) + ".";
                    lesson = "Every instrument and module needs power: generation must be bigger than the total draw.";
                    PartData add = null;
                    foreach (PartData p in Catalog())
                    {
                        if (p.power - p.powerDraw >= -net && !Has(parts, p)) add = p;
                    }
                    PartData remove = null;
                    foreach (PartData p in parts)
                    {
                        if (p != null && IsOptional(p) && p.powerDraw >= -net && (remove == null || p.powerDraw < remove.powerDraw)) remove = p;
                    }
                    if (add != null) fix = "adding the " + add.partName + " would have passed.";
                    else if (remove != null) fix = "removing the " + remove.partName + " (" + Watts(remove.powerDraw) + ") would have passed.";
                    else fix = "using " + Watts(-net) + " less would have passed.";
                    break;
                }
                case "BATTERY":
                {
                    float draw = RocketPhysics.PowerConsumption(parts);
                    float need = draw * RocketPhysics.OutageHours;
                    float have = RocketPhysics.BatteryWh(parts);
                    problem = "Your battery stores " + have.ToString("N0") + " Wh, but " + RocketPhysics.OutageHours + " h in shadow at " + Watts(draw) + " needs " + need.ToString("N0") + " Wh.";
                    lesson = "Behind the Moon or in Earth's shadow there is no sunlight: only the batteries power the spacecraft.";
                    PartData add = null;
                    foreach (PartData p in Catalog())
                    {
                        // The new module also draws power during the outage
                        float gain = p.batteryWh - p.powerDraw * RocketPhysics.OutageHours;
                        if (p.batteryWh > 0f && gain >= need - have && !Has(parts, p)) add = p;
                    }
                    if (add != null) fix = "a battery module (" + add.partName + ", +" + add.batteryWh.ToString("N0") + " Wh) would have passed.";
                    else fix = "using less power or carrying " + (need - have).ToString("N0") + " Wh more would have passed.";
                    break;
                }
                case "CONSUMABLES":
                {
                    float water = RocketPhysics.WaterMargin(parts, m);
                    float oxygen = RocketPhysics.OxygenMargin(parts, m);
                    float food = RocketPhysics.FoodMargin(parts, m);
                    problem = "";
                    fix = "";
                    if (water < 0f) { problem += "water " + Kg(-water) + " short; "; fix += ReserveFor("water", -water, parts); }
                    if (oxygen < 0f) { problem += "oxygen " + Kg(-oxygen) + " short; "; fix += ReserveFor("oxygen", -oxygen, parts); }
                    if (food < 0f) { problem += "food " + Kg(-food) + " short; "; fix += ReserveFor("food", -food, parts); }
                    problem = "Not enough supplies for " + m.crewCount + " astronauts x " + m.missionDays + " days: " + problem.TrimEnd(' ', ';') + ".";
                    lesson = "The crew uses water, oxygen and food every day; real missions carry a reserve in case the trip takes longer.";
                    fix = fix.Trim() + " would have passed.";
                    break;
                }
                case "SCIENCE":
                {
                    float science = RocketPhysics.SciencePoints(parts);
                    lesson = "Science is the reason for the trip: you need the right instruments and you have to run the experiments.";
                    if (science < m.minSciencePoints)
                    {
                        float missing = m.minSciencePoints - science;
                        problem = "Your instruments give " + science.ToString("0") + " science points; the mission needs " + m.minSciencePoints.ToString("0") + ".";
                        PartData add = null;
                        foreach (PartData p in Catalog())
                        {
                            if (p.sciencePoints >= missing && !Has(parts, p) && (add == null || Mass(p) < Mass(add))) add = p;
                        }
                        if (add != null) fix = "adding the " + add.partName + " (+" + add.sciencePoints.ToString("0") + " points) would have passed.";
                        else fix = "more instruments would have passed.";
                    }
                    else
                    {
                        problem = "You carried the instruments, but the experiments were not run.";
                        fix = "running the experiments in lunar orbit (research minigame) would have passed.";
                    }
                    break;
                }
                case "NO_DOWNLINK":
                {
                    problem = "The science data never reached Earth.";
                    lesson = "Data only counts when it is sent home: the antenna transmits it during the flight.";
                    PartData antenna = null;
                    foreach (PartData p in Catalog())
                    {
                        if (p.type == PartType.Antenna && !Has(parts, p)) antenna = p;
                    }
                    if (FirstOfType(parts, PartType.Antenna) == null && antenna != null)
                        fix = "adding the " + antenna.partName + " and sending the data would have passed.";
                    else fix = "sending the data to Earth before the return would have passed.";
                    break;
                }
                case "ROCKET_INVALID":
                    problem = "The rocket was not valid: " + RocketPhysics.ValidationMessage(parts, m);
                    lesson = "A real rocket must pass every check before it is allowed to launch.";
                    break;
                case "CREW_NOT_READY":
                    lesson = "Every role needs an astronaut: pilot, engineer and scientist.";
                    fix = "choosing and training the 3 astronauts in the mission folder would have passed.";
                    break;
                case "DEPARTURE":
                case "LUNAR_ORBIT":
                case "RETURN":
                    lesson = "The flight stages happen in order: if one is not completed, the next ones cannot happen.";
                    break;
            }
        }

        string text = "- " + problem;
        if (lesson != "") text += "\n    Lesson: " + lesson;
        if (fix != "") text += "\n    Fix: " + Capital(fix);
        return text;
    }

    // ---------- Helpers ----------

    static List<PartData> Catalog()
    {
        List<PartData> result = new List<PartData>();
        GameData data = GameData.Instance;
        if (data == null) return result;
        foreach (PartData p in data.parts)
        {
            if (p != null) result.Add(p);
        }
        return result;
    }

    static string ReserveFor(string resource, float shortKg, List<PartData> parts)
    {
        foreach (PartData p in Catalog())
        {
            if (Has(parts, p)) continue;
            float kg = resource == "water" ? p.waterKg : resource == "oxygen" ? p.oxygenKg : p.foodKg;
            if (kg >= shortKg) return "the " + p.partName + " (+" + Kg(kg) + ")";
        }
        return "about " + Kg(shortKg) + " more " + resource;
    }

    // Optional module on the rocket whose removal saves at least minMass kg (the lightest one that is enough)
    static PartData LightestToRemove(List<PartData> parts, float minMass)
    {
        PartData best = null;
        foreach (PartData p in parts)
        {
            if (p == null || !IsOptional(p) || Mass(p) < minMass) continue;
            if (best == null || Mass(p) < Mass(best)) best = p;
        }
        return best;
    }

    // Parts the mission can fly without (not the launcher, abort tower, capsule or service module)
    static bool IsOptional(PartData p)
    {
        return p.type != PartType.LaunchService && p.type != PartType.AbortSystem
            && p.type != PartType.Capsule && p.type != PartType.ServiceModule;
    }

    static bool Has(List<PartData> parts, PartData part)
    {
        foreach (PartData p in parts)
        {
            if (p != null && p.catalogId == part.catalogId) return true;
        }
        return false;
    }

    static PartData FirstOfType(List<PartData> parts, PartType type)
    {
        foreach (PartData p in parts)
        {
            if (p != null && p.type == type) return p;
        }
        return null;
    }

    static float Mass(PartData p)
    {
        return p.dryMass + p.fuelMass;
    }

    static string Kg(float kg)
    {
        return kg.ToString("N0") + " kg";
    }

    static string Watts(float w)
    {
        return w.ToString("N0") + " W";
    }

    static string Capital(string s)
    {
        if (s.Length == 0) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
