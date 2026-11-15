using System.Collections.Generic;
using UnityEngine;

// Task 35: one explanation per failed gate on the Results screen:
// what went wrong (with the real RocketPhysics numbers), the lesson and what would have fixed it
// (a part from the catalog, or the mass / module to remove).
public static class GateAdvice
{
    // Colors of the three parts of each entry (TextMeshPro rich text)
    const string TitleColor = "#FF8A7A";
    const string LessonColor = "#9FB0C3";
    const string FixColor = "#8BE39B";

    // A short title, the real numbers, the lesson and the fix.
    // Only uses data from RocketPhysics, the mission and the catalog (GameData.parts).
    public static string Explain(string gate, GameManager gm)
    {
        string title = ResultsScreen.GateText(gate);
        string numbers = "";
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
                    title = "Too heavy for the launcher";
                    numbers = Kg(tli) + " to send, the launcher carries " + Kg(capacity);
                    lesson = "Every module you add uses part of the launcher capacity.";
                    PartData bigger = null;
                    foreach (PartData p in Catalog())
                    {
                        if (p.type == PartType.LaunchService && p.payloadCapacity >= tli && !Has(parts, p)) bigger = p;
                    }
                    PartData remove = LightestToRemove(parts, tli - capacity);
                    if (bigger != null) fix = "use the " + ShortName(bigger) + " (" + Kg(bigger.payloadCapacity) + ")";
                    if (remove != null) fix += (fix == "" ? "" : ", or ") + "remove " + ShortName(remove) + " (" + Kg(Mass(remove)) + ")";
                    if (fix == "") fix = "remove " + Kg(tli - capacity) + " of modules";
                    break;
                }
                case "CRAFT_DELTA_V":
                {
                    float dv = RocketPhysics.SpacecraftDeltaV(parts);
                    title = "Not enough delta-v";
                    numbers = dv.ToString("N0") + " of " + m.requiredDeltaV.ToString("N0") + " m/s";
                    lesson = "dv = Isp x g0 x ln(m0/mf): same fuel + more mass = less delta-v.";
                    PartData esm = FirstOfType(parts, PartType.ServiceModule);
                    if (esm == null || esm.isp <= 0f || esm.fuelMass <= 0f)
                    {
                        fix = "add a service module (it carries the fuel)";
                        break;
                    }
                    // Heaviest spacecraft that still reaches the required delta-v: m0 / (m0 - fuel) = e^(dv / (Isp g0))
                    float ratio = Mathf.Exp(m.requiredDeltaV / (esm.isp * RocketPhysics.G0));
                    float maxMass = esm.fuelMass * ratio / (ratio - 1f);
                    float over = RocketPhysics.SpacecraftMass(parts) - maxMass;
                    PartData remove = LightestToRemove(parts, over);
                    if (remove != null) fix = "remove " + ShortName(remove) + " (" + Kg(Mass(remove)) + ")";
                    else fix = "remove " + Kg(over) + " of modules";
                    break;
                }
                case "POWER":
                {
                    float draw = RocketPhysics.PowerConsumption(parts);
                    float net = RocketPhysics.PowerBalance(parts);
                    title = "Not enough power";
                    numbers = Watts(draw) + " used, " + Watts(draw + net) + " generated";
                    lesson = "Generation must be bigger than the total draw.";
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
                    if (add != null) fix = "add " + ShortName(add);
                    else if (remove != null) fix = "remove " + ShortName(remove) + " (" + Watts(remove.powerDraw) + ")";
                    else fix = "use " + Watts(-net) + " less";
                    break;
                }
                case "BATTERY":
                {
                    float draw = RocketPhysics.PowerConsumption(parts);
                    float need = draw * RocketPhysics.OutageHours;
                    float have = RocketPhysics.BatteryWh(parts);
                    title = "Battery too small";
                    numbers = have.ToString("N0") + " of " + need.ToString("N0") + " Wh (" + RocketPhysics.OutageHours + " h in shadow)";
                    lesson = "Without sunlight, only the batteries power the spacecraft.";
                    PartData add = null;
                    foreach (PartData p in Catalog())
                    {
                        // The new module also draws power during the outage
                        float gain = p.batteryWh - p.powerDraw * RocketPhysics.OutageHours;
                        if (p.batteryWh > 0f && gain >= need - have && !Has(parts, p)) add = p;
                    }
                    if (add != null) fix = "add " + ShortName(add) + " (+" + add.batteryWh.ToString("N0") + " Wh)";
                    else fix = "add " + (need - have).ToString("N0") + " Wh of battery";
                    break;
                }
                case "CONSUMABLES":
                {
                    float water = RocketPhysics.WaterMargin(parts, m);
                    float oxygen = RocketPhysics.OxygenMargin(parts, m);
                    float food = RocketPhysics.FoodMargin(parts, m);
                    title = "Not enough supplies";
                    if (water < 0f) { numbers += "water -" + Kg(-water) + "   "; fix += ReserveFor("water", -water, parts) + "  "; }
                    if (oxygen < 0f) { numbers += "oxygen -" + Kg(-oxygen) + "   "; fix += ReserveFor("oxygen", -oxygen, parts) + "  "; }
                    if (food < 0f) { numbers += "food -" + Kg(-food) + "   "; fix += ReserveFor("food", -food, parts) + "  "; }
                    numbers = numbers.Trim() + "  (" + m.crewCount + " crew x " + m.missionDays + " days)";
                    fix = fix.Trim();
                    lesson = "The crew uses water, oxygen and food every day of the mission.";
                    break;
                }
                case "SCIENCE":
                {
                    float science = RocketPhysics.SciencePoints(parts);
                    title = "Science goal not met";
                    lesson = "You need the right instruments and you have to run the experiments.";
                    if (science < m.minSciencePoints)
                    {
                        numbers = science.ToString("0") + " of " + m.minSciencePoints.ToString("0") + " science points";
                        PartData add = null;
                        foreach (PartData p in Catalog())
                        {
                            if (p.sciencePoints >= m.minSciencePoints - science && !Has(parts, p) && (add == null || Mass(p) < Mass(add))) add = p;
                        }
                        if (add != null) fix = "add " + ShortName(add) + " (+" + add.sciencePoints.ToString("0") + " points)";
                        else fix = "add more instruments";
                    }
                    else
                    {
                        numbers = "the experiments were not run";
                        fix = "run the experiments in lunar orbit";
                    }
                    break;
                }
                case "NO_DOWNLINK":
                {
                    title = "No data sent to Earth";
                    lesson = "Science only counts when the data reaches home.";
                    PartData antenna = null;
                    foreach (PartData p in Catalog())
                    {
                        if (p.type == PartType.Antenna && !Has(parts, p)) antenna = p;
                    }
                    if (FirstOfType(parts, PartType.Antenna) == null && antenna != null) fix = "add " + ShortName(antenna) + " and send the data";
                    else fix = "send the data before the return";
                    break;
                }
                case "ROCKET_INVALID":
                    title = "Rocket not valid";
                    numbers = RocketPhysics.ValidationMessage(parts, m);
                    break;
                case "CREW_NOT_READY":
                    title = "Crew not ready";
                    fix = "choose and train the 3 astronauts in the mission folder";
                    break;
                case "DEPARTURE":
                case "LUNAR_ORBIT":
                case "RETURN":
                    lesson = "The flight stages happen in order.";
                    break;
            }
        }

        string text = "<b><color=" + TitleColor + ">" + title + "</color></b>";
        if (numbers != "") text += "   " + numbers;
        if (lesson != "") text += "\n<size=85%><color=" + LessonColor + ">" + lesson + "</color></size>";
        if (fix != "") text += "\n<size=85%><color=" + FixColor + "><b>Fix:</b> " + fix + "</color></size>";
        return text + "\n";
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
            if (kg >= shortKg) return "add " + ShortName(p) + " (+" + Kg(kg) + ")";
        }
        return "add " + Kg(shortKg) + " of " + resource;
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

    // Catalog names are long: "SLS Block 1B launch service - future-architecture reference" -> "SLS Block 1B launch service"
    static string ShortName(PartData p)
    {
        string name = p.partName;
        int cut = name.IndexOf(" \u2014 ");
        if (cut > 0) name = name.Substring(0, cut);
        cut = name.IndexOf(" (");
        if (cut > 0) name = name.Substring(0, cut);
        return name;
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

}
