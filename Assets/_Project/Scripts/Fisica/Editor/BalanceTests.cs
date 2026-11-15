using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Balance tests (P5) with the real parts of the Artemis catalog.
// Menu: SpaceApps -> Balance tests. Writes each test to the console with OK or FAIL.
//
// Goes through every combination: 1 launcher (Block 1 or 1B) + Orion (capsule, service module,
// abort tower) + up to one unit of each of the 11 optional modules = 2 x 2^11 = 4,096.
// For each one it separately checks budget, mass, delta-v, consumables, power, battery,
// science and communications, and compares with P5's enumeration (Docs/P5/balance.py).
// The counts change if the catalog, the mission or the 3,400 credits change: if that is intentional,
// the expected values must be updated (and the team told about the effect on the balance).
public static class BalanceTests
{
    const float Budget = 3400f;
    const string PartsFolder = "Assets/_Project/Data/Piezas/";

    static readonly string[] Optional = {
        "ART-CREW-HAB-BACKUP", "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD",
        "ART-PWR-BACKUP", "ART-RAD-SHIELD", "ART-NAV-LUNAR", "ART-COMMS-DS",
        "ART-SCI-LUNAR", "ART-SCI-RAD", "ART-SCI-HEAVY" };
    static readonly string[] Launchers = { "ART-SLS-BLOCK1", "ART-SLS-BLOCK1B" };
    static readonly string[] Categories = { "BUDGET", "MASS", "DELTA_V", "CONSUMABLES", "POWER", "BATTERY", "SCIENCE", "DOWNLINK" };

    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Balance tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        DestinationData baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (baseMission == null || Part("ART-CM-ORION") == null)
        {
            Debug.LogError("The catalog could not be imported: the tests were not run.");
            return;
        }

        // Expected values from P5's enumeration (12 and 15 days, 3,400 credits, no repeated parts)
        Enumerate(Mission(baseMission, 12), 78, 2940f, 1,
            new Dictionary<string, int> { { "BUDGET", 2401 }, { "MASS", 1202 }, { "DELTA_V", 622 }, { "CONSUMABLES", 0 },
                { "POWER", 0 }, { "BATTERY", 0 }, { "SCIENCE", 1536 }, { "DOWNLINK", 1792 } });
        Enumerate(Mission(baseMission, 15), 5, 3140f, 0,
            new Dictionary<string, int> { { "BUDGET", 2401 }, { "MASS", 1202 }, { "DELTA_V", 622 }, { "CONSUMABLES", 3584 },
                { "POWER", 0 }, { "BATTERY", 0 }, { "SCIENCE", 1536 }, { "DOWNLINK", 1792 } });

        StrategyTests(Mission(baseMission, 12), Mission(baseMission, 15));
        EdgeCases(baseMission);

        string summary = "Balance tests: " + passed + " OK, " + failed + " FAIL";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // ---------- Enumeration ----------

    static void Enumerate(DestinationData mission, int expectedViable, float expectedCheapest, int expectedBlock1BViable,
        Dictionary<string, int> expectedFails)
    {
        string tag = mission.missionDays + " days";
        Dictionary<string, int> fails = new Dictionary<string, int>();
        foreach (string c in Categories) fails[c] = 0;
        int total = 0;
        int viable = 0;
        int block1BViable = 0;
        int mismatches = 0;
        float cheapest = float.MaxValue;
        float maxLoad = 0f;

        foreach (string launcher in Launchers)
        {
            for (int mask = 0; mask < (1 << Optional.Length); mask++)
            {
                List<PartData> parts = Rocket(launcher, mask);
                List<string> problems = Problems(parts, mission);
                total++;
                foreach (string p in problems) fails[p]++;
                maxLoad = Mathf.Max(maxLoad, RocketPhysics.PowerConsumption(parts));

                bool ok = problems.Count == 0;
                // ValidationMessage + budget must give the same result as the separate checks
                bool physicsOk = RocketPhysics.IsRocketValid(parts, mission) && RocketPhysics.TotalPrice(parts) <= Budget;
                if (ok != physicsOk) mismatches++;

                if (!ok) continue;
                viable++;
                if (launcher == "ART-SLS-BLOCK1B") block1BViable++;
                cheapest = Mathf.Min(cheapest, RocketPhysics.TotalPrice(parts));
            }
        }

        Check(tag + ": 4,096 combinations", total == 4096);
        Check(tag + ": " + expectedViable + " viable (got " + viable + ")", viable == expectedViable);
        Near(tag + ": cheapest viable", cheapest, expectedCheapest);
        Check(tag + ": " + expectedBlock1BViable + " viable with Block 1B (got " + block1BViable + ")", block1BViable == expectedBlock1BViable);
        Check(tag + ": RocketPhysics.ValidationMessage matches the separate checks (" + mismatches + " different)", mismatches == 0);
        foreach (string c in Categories)
            Check(tag + ": fail on " + c + " = " + expectedFails[c] + " (got " + fails[c] + ")", fails[c] == expectedFails[c]);
        Debug.Log(tag + ": maximum possible spacecraft power draw = " + maxLoad + " W (it generates 11,000 W; power is not a limit with the current catalog).");
    }

    // Checks each requirement separately (several can fail at once).
    static List<string> Problems(List<PartData> parts, DestinationData mission)
    {
        List<string> list = new List<string>();
        if (RocketPhysics.TotalPrice(parts) > Budget) list.Add("BUDGET");
        if (RocketPhysics.DeliveryMargin(parts, mission) < 0f) list.Add("MASS");
        if (RocketPhysics.SpacecraftDeltaV(parts) < mission.requiredDeltaV) list.Add("DELTA_V");
        if (RocketPhysics.WaterMargin(parts, mission) < 0f || RocketPhysics.OxygenMargin(parts, mission) < 0f
            || RocketPhysics.FoodMargin(parts, mission) < 0f) list.Add("CONSUMABLES");
        if (RocketPhysics.PowerBalance(parts) < 0f) list.Add("POWER");
        if (!RocketPhysics.BatteryCoversOutage(parts)) list.Add("BATTERY");
        if (RocketPhysics.SciencePoints(parts) < mission.minSciencePoints) list.Add("SCIENCE");
        if (RocketPhysics.DownlinkSeconds(parts) > mission.maxDownlinkSeconds) list.Add("DOWNLINK");
        return list;
    }

    static List<PartData> Rocket(string launcher, int mask)
    {
        List<PartData> parts = new List<PartData> { Part(launcher), Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        for (int i = 0; i < Optional.Length; i++)
        {
            if ((mask & (1 << i)) != 0) parts.Add(Part(Optional[i]));
        }
        return parts;
    }

    // ---------- Different strategies that must stay viable ----------

    static void StrategyTests(DestinationData mission12, DestinationData mission15)
    {
        Viable("Budget strategy: Block 1 + antenna + 2 small instruments (12 days)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD"), mission12);
        Viable("Heavy science strategy: Block 1B + antenna + heavy instrument (12 days)",
            Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY"), mission12);
        Viable("Long mission strategy: Block 1 + water, oxygen and food reserves (15 days)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
                "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD"), mission15);
        Viable("Safe strategy: Block 1 + lunar navigation + food reserve (12 days)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD", "ART-NAV-LUNAR", "ART-CREW-RES-FOOD"), mission12);
    }

    // ---------- Edge cases ----------

    static void EdgeCases(DestinationData baseMission)
    {
        DestinationData mission12 = Mission(baseMission, 12);
        List<PartData> good = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");

        Rejected("No mission assigned", good, null, "No mission");

        DestinationData badMission = Mission(baseMission, 12);
        badMission.requiredDeltaV = 0f;
        Rejected("Mission without required delta-v", good, badMission, "required delta-v");
        badMission = Mission(baseMission, 12);
        badMission.baseWaterKg = -10f;
        Rejected("Mission with negative water", good, badMission, "negative value");

        List<PartData> negativeMass = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        PartData antenna = Object.Instantiate(Part("ART-COMMS-DS"));
        antenna.dryMass = -65f;
        negativeMass[4] = antenna;
        Rejected("Part with negative mass", negativeMass, mission12, "negative");

        List<PartData> negativePower = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        PartData antenna2 = Object.Instantiate(Part("ART-COMMS-DS"));
        antenna2.powerDraw = -5000f; // negative draw = free power
        negativePower[4] = antenna2;
        Rejected("Part with negative power draw", negativePower, mission12, "negative");

        List<PartData> noData = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        PartData lunar = Object.Instantiate(Part("ART-SCI-LUNAR"));
        PartData rad = Object.Instantiate(Part("ART-SCI-RAD"));
        lunar.experimentDataBits = 0f;
        rad.experimentDataBits = 0f;
        noData[5] = lunar;
        noData[6] = rad;
        Rejected("Instruments with science but no data (0 s of downlink does not count as success)", noData, mission12, "do not generate data");

        Rejected("Two units of SCI-RAD (with repeats it would be the cheapest: 2,875 credits)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-RAD", "ART-SCI-RAD"), mission12, "one unit");
        Rejected("Two water reserves", Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-WATER"), mission12, "one unit");
    }

    // ---------- Helpers ----------

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>(PartsFolder + id + ".asset");
    }

    // Orion (capsule, service module, abort tower) after the launcher + the requested parts
    static List<PartData> Parts(string launcher, params string[] ids)
    {
        List<PartData> list = new List<PartData> { Part(launcher), Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        foreach (string id in ids) list.Add(Part(id));
        return list;
    }

    static DestinationData Mission(DestinationData baseMission, int days)
    {
        DestinationData m = Object.Instantiate(baseMission);
        m.missionDays = days;
        return m;
    }

    static void Viable(string name, List<PartData> parts, DestinationData mission)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        float price = RocketPhysics.TotalPrice(parts);
        Report(name + " (" + price + " credits)", message == "" && price <= Budget, message);
    }

    static void Rejected(string name, List<PartData> parts, DestinationData mission, string expected)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        Report(name + ": rejected", message.Contains(expected), "message: \"" + message + "\"");
    }

    static void Check(string name, bool ok)
    {
        Report(name, ok, "");
    }

    static void Near(string name, float actual, float expected)
    {
        Report(name + " = " + expected, Mathf.Abs(actual - expected) < 0.5f, "got " + actual);
    }

    static void Report(string name, bool ok, string detail)
    {
        if (ok)
        {
            passed++;
            Debug.Log("OK     " + name);
        }
        else
        {
            failed++;
            Debug.LogError("FAIL   " + name + "  " + detail);
        }
    }
}
