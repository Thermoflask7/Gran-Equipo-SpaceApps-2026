using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// RocketPhysics tests inside Unity (task 17), with the 9 reference scenarios of
// Cristina's mission V13 (SpaceMission_Mision_Artemis_V13.json -> reference_test_scenarios).
// Menu: SpaceApps -> Physics tests. Writes each test to the console with OK or FAIL.
// It first imports the catalog (creates/updates the Data/Piezas assets and the mission) and then
// tests with those same assets, so it also checks that the data is right.
public static class RocketPhysicsTests
{
    const float Budget = 3400f; // starting credits (Cristina's catalog)

    static int passed;
    static int failed;
    static DestinationData baseMission;

    [MenuItem("SpaceApps/Physics tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (baseMission == null || Part("ART-CM-ORION") == null)
        {
            Debug.LogError("The catalog could not be imported: the tests were not run.");
            return;
        }
        DestinationData mission12 = Mission(12);
        DestinationData mission15 = Mission(15);

        // 1. PASS_BLOCK1_OBSERVATION
        List<PartData> p1 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        Near("1 Block 1 observation: mass at translunar injection", RocketPhysics.TliMass(p1, mission12), 26660f);
        Near("1 Block 1 observation: SLS margin", RocketPhysics.DeliveryMargin(p1, mission12), 340f);
        Near("1 Block 1 observation: spacecraft dv", RocketPhysics.SpacecraftDeltaV(p1), 1103.27f);
        Near("1 Block 1 observation: 460 credits left", Budget - RocketPhysics.TotalPrice(p1), 460f);
        Near("1 Block 1 observation: 48 science points", RocketPhysics.SciencePoints(p1), 48f);
        Valid("1 Block 1 observation: valid", p1, mission12);

        // 2. PASS_BLOCK1B_HEAVY_SCIENCE
        List<PartData> p2 = Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY");
        Near("2 Block 1B heavy science: mass at translunar injection", RocketPhysics.TliMass(p2, mission12), 27201f);
        Near("2 Block 1B heavy science: spacecraft dv", RocketPhysics.SpacecraftDeltaV(p2), 1076.34f);
        Near("2 Block 1B heavy science: 5 credits left", Budget - RocketPhysics.TotalPrice(p2), 5f);
        Valid("2 Block 1B heavy science: valid", p2, mission12);

        // 3. PASS_BLOCK1_EXTENDED_15_DAYS
        List<PartData> p3 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD");
        Near("3 Block 1, 15 days with reserves: mass at translunar injection", RocketPhysics.TliMass(p3, mission15), 26756f);
        Near("3 Block 1, 15 days with reserves: spacecraft dv", RocketPhysics.SpacecraftDeltaV(p3), 1098.40f);
        Valid("3 Block 1, 15 days with reserves: valid", p3, mission15);

        // 4. FAIL_LOW_POST_TLI_PROPELLANT (service module with 7,400 kg instead of 7,800)
        PartData lowFuelService = Object.Instantiate(Part("ART-ESM-ORION"));
        lowFuelService.fuelMass = 7400f;
        List<PartData> p4 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        p4[1] = lowFuelService; // index 1 = service module
        Near("4 low propellant: mass at translunar injection", RocketPhysics.TliMass(p4, mission12), 26260f);
        Near("4 low propellant: spacecraft dv", RocketPhysics.SpacecraftDeltaV(p4), 1055.32f);
        Fails("4 low propellant: does not make it", p4, mission12, "lunar orbit");

        // 5. FAIL_BLOCK1_EXCEEDS_TLI_MASS
        List<PartData> p5 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-HEAVY");
        Near("5 Block 1 with heavy science: mass at translunar injection", RocketPhysics.TliMass(p5, mission12), 27201f);
        Fails("5 Block 1 with heavy science: does not fit", p5, mission12, "Too heavy");

        // 6. FAIL_NO_DATA_DOWNLINK
        List<PartData> p6 = Parts("ART-SLS-BLOCK1", "ART-SCI-LUNAR", "ART-SCI-RAD");
        Near("6 no antenna: spacecraft dv", RocketPhysics.SpacecraftDeltaV(p6), 1106.60f);
        Fails("6 no antenna: the data is not downlinked", p6, mission12, "antenna");

        // 7. FAIL_BUDGET_BLOCK1B_INSTRUMENTS (the budget is checked by GameManager when buying)
        List<PartData> p7 = Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY", "ART-CREW-RES-FOOD");
        Near("7 Block 1B with extra food: costs 3,450 credits", RocketPhysics.TotalPrice(p7), 3450f);
        Check("7 Block 1B with extra food: over budget", RocketPhysics.TotalPrice(p7) > Budget);

        // 8. FAIL_LOW_SCIENCE_POINTS
        List<PartData> p8 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR");
        Near("8 low science: spacecraft dv", RocketPhysics.SpacecraftDeltaV(p8), 1103.99f);
        Fails("8 low science: 28 of 40 points", p8, mission12, "Not enough science");

        // 9. FAIL_LONGER_MISSION_WITHOUT_CREW_RESERVES
        Fails("9 15 days without reserves: not enough water", p1, mission15, "water");
        Check("9 15 days without reserves: not enough oxygen and food",
            RocketPhysics.OxygenMargin(p1, mission15) < 0f && RocketPhysics.FoodMargin(p1, mission15) < 0f);

        // Assembly rules
        Fails("10 no launcher", Parts("ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD"), mission12, "Choose a launcher");
        Fails("11 two launchers", Parts("ART-SLS-BLOCK1", "ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD"), mission12, "You can only use");
        List<PartData> noEscape = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        noEscape.Remove(Part("ART-LAS-ORION"));
        Fails("12 no abort tower", noEscape, mission12, "abort tower");

        string summary = "Physics tests: " + passed + " OK, " + failed + " FAIL";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>("Assets/_Project/Data/Piezas/" + id + ".asset");
    }

    // Mandatory Orion (capsule, service module, abort tower) + the parts passed in.
    static List<PartData> Parts(params string[] ids)
    {
        List<PartData> list = new List<PartData> { Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        foreach (string id in ids) list.Add(Part(id));
        return list;
    }

    // Copy of the imported mission with a different duration (does not modify the asset).
    static DestinationData Mission(int days)
    {
        DestinationData m = Object.Instantiate(baseMission);
        m.missionDays = days;
        return m;
    }

    static void Valid(string name, List<PartData> parts, DestinationData mission)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        Report(name, message == "", message);
    }

    static void Fails(string name, List<PartData> parts, DestinationData mission, string expected)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        Report(name, message.Contains(expected), "message: \"" + message + "\"");
    }

    static void Check(string name, bool ok)
    {
        Report(name, ok, "");
    }

    // Tolerance of 0.5 (Unity float against Cristina's values).
    static void Near(string name, float actual, float expected)
    {
        Report(name + ": " + actual.ToString("0.00"), Mathf.Abs(actual - expected) <= 0.5f, "expected " + expected.ToString("0.00"));
    }

    static void Report(string name, bool ok, string detail)
    {
        if (ok) { passed++; Debug.Log("OK     " + name); }
        else { failed++; Debug.LogError("FAIL   " + name + "  " + detail); }
    }
}
