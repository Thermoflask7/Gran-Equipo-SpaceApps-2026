using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Score tests (task 16) with the lunar physics and the real catalog parts
// (Data/Piezas, mission Data/Destinos/MisionArtemis). The expected values were calculated separately
// with the same formulas and the data in CatalogoArtemis.json.
// Menu: SpaceApps -> Scoring tests. Writes each test to the console with OK or FAIL.
public static class MissionScoringTests
{
    const string PartsFolder = "Assets/_Project/Data/Piezas/";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static int passed;
    static int failed;
    static DestinationData baseMission;
    static GameManager gm;

    [MenuItem("SpaceApps/Scoring tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (baseMission == null || Part("ART-CM-ORION") == null)
        {
            Debug.LogError("The catalog could not be imported: the tests were not run.");
            return;
        }

        GameObject go = new GameObject("Test GameManager");
        gm = go.AddComponent<GameManager>();
        try
        {
            ViableConfigurations();
            PhysicsFailures();
            MoneyAfterLaunch();
            ScienceLimits();
            StatesAndRecalculation();
            InvalidInputs();
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        string summary = "Scoring tests: " + passed + " OK, " + failed + " FAIL";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // ---------- 1. Viable configurations (Block 1 and Block 1B) ----------

    static void ViableConfigurations()
    {
        // Scenario 1 of mission V13: Block 1, antenna, 2 instruments, 12 days
        FillGame(gm, Mission(12), Block1Observation());
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Near("Block 1 observation: launcher capacity (340 kg free)", r.deliveryPoints, 7f * 340f / 1350f);
        Near("Block 1 observation: engineering", r.engineering, 14.606290f);
        Near("Block 1 observation: planning", r.planning, 33.753349f);
        Near("Block 1 observation: mission", r.mission, 50.155f);
        Near("Block 1 observation: total", r.total, 83.908349f);
        Success("Block 1 observation", r);

        // Scenario 2: Block 1B with heavy science (5 credits left)
        FillGame(gm, Mission(12), Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY"));
        r = MissionScoring.EvaluateGame(gm);
        Near("Block 1B heavy science: launcher capacity (full)", r.deliveryPoints, 7f);
        Near("Block 1B heavy science: engineering", r.engineering, 17.620192f);
        Near("Block 1B heavy science: total", r.total, 80.677152f);
        Success("Block 1B heavy science", r);

        // Scenario 3: Block 1, 15 days with water, oxygen and food reserves
        FillGame(gm, Mission(15), Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD"));
        r = MissionScoring.EvaluateGame(gm);
        Near("Block 1, 15 days with reserves: engineering", r.engineering, 14.016697f);
        Near("Block 1, 15 days with reserves: total", r.total, 80.573658f);
        Success("Block 1, 15 days with reserves", r);
    }

    // ---------- 2. Propellant, mass, power and consumables ----------

    static void PhysicsFailures()
    {
        // Not enough propellant: service module with 7,400 kg (scenario 4 of mission V13)
        List<PartData> lowFuel = Block1Observation();
        PartData service = Object.Instantiate(Part("ART-ESM-ORION"));
        service.fuelMass = 7400f;
        lowFuel[1] = service;
        FillGame(gm, Mission(12), lowFuel);
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Near("Low propellant: 0 delta-v points", r.craftDvPoints, 0f);
        Near("Low propellant: total", r.total, 82.410260f);
        Fails("Low propellant", r, "CRAFT_DELTA_V");

        // Excess mass: Block 1 with heavy science (scenario 5)
        FillGame(gm, Mission(12), Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-HEAVY"));
        r = MissionScoring.EvaluateGame(gm);
        Near("Excess mass: 0 capacity points", r.deliveryPoints, 0f);
        Near("Excess mass: total", r.total, 80.608525f);
        Fails("Excess mass", r, "DELIVERY");

        // Not enough power: solar arrays that generate only 2,500 W
        List<PartData> lowPower = Block1Observation();
        PartData weakPanels = Object.Instantiate(Part("ART-ESM-ORION"));
        weakPanels.power = 2500f;
        lowPower[1] = weakPanels;
        FillGame(gm, Mission(12), lowPower);
        r = MissionScoring.EvaluateGame(gm);
        Near("Low generation: 0 generation points", r.powerPoints, 0f);
        Near("Low generation: total", r.total, 80.908349f);
        Fails("Low generation", r, "POWER");

        // Battery that does not last 1.5 h without sunlight (1,000 Wh)
        List<PartData> lowBattery = Block1Observation();
        PartData weakBattery = Object.Instantiate(Part("ART-ESM-ORION"));
        weakBattery.batteryWh = 1000f;
        lowBattery[1] = weakBattery;
        FillGame(gm, Mission(12), lowBattery);
        r = MissionScoring.EvaluateGame(gm);
        Near("Small battery: 0 battery points", r.batteryPoints, 0f);
        Fails("Small battery", r, "BATTERY");

        // Not enough consumables: 15 days without reserves
        FillGame(gm, Mission(15), Block1Observation());
        r = MissionScoring.EvaluateGame(gm);
        Near("No reserves for 15 days: 0 consumable points", r.consumablePoints, 0f);
        Near("No reserves for 15 days: total", r.total, 80.637185f);
        Fails("No reserves for 15 days", r, "CONSUMABLES");
    }

    // ---------- 3. Events that change the money after launch ----------

    static void MoneyAfterLaunch()
    {
        FillGame(gm, Mission(12), Block1Observation());
        float before = MissionScoring.EvaluateGame(gm).budgetPoints;

        gm.AddMoney(500f); // good flight event
        Near("Event +500 credits during the flight: savings do not change", MissionScoring.EvaluateGame(gm).budgetPoints, before);
        gm.money = 0f;     // bad flight event that takes everything
        Near("Event that leaves 0 credits during the flight: savings do not change", MissionScoring.EvaluateGame(gm).budgetPoints, before);

        gm.flight.launched = false; // before launch the current money does count
        Near("Before launch: savings use the current money (0)", MissionScoring.EvaluateGame(gm).budgetPoints, 0f);
    }

    // ---------- 4. Experiment and downlink limits ----------

    static void ScienceLimits()
    {
        Debug.Log("Current minimum to win: experiments > " + MissionScoring.MinExperimentFraction
            + ", data > " + MissionScoring.MinDownlinkFraction + " (pending proposal: 0.5).");

        FillGame(gm, Mission(12), Block1Observation());
        gm.flight.experimentFraction = 1f;
        gm.flight.downlinkFraction = 1f;
        Near("Full science: 10 points", MissionScoring.EvaluateGame(gm).sciencePoints, 10f);

        gm.flight.experimentFraction = 0.01f;
        gm.flight.downlinkFraction = 0.01f;
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Near("1% of experiments and data: 4.06 science points", r.sciencePoints, 4.06f);
        Check("1% of experiments and data: " + (MissionScoring.MinExperimentFraction > 0.01f ? "fails" : "wins with the current rule"),
            (r.status == "SUCCESS") == (MissionScoring.MinExperimentFraction <= 0.01f && MissionScoring.MinDownlinkFraction <= 0.01f));

        gm.flight.experimentFraction = 0.8f;
        gm.flight.downlinkFraction = 0f;
        Fails("No data sent", MissionScoring.EvaluateGame(gm), "NO_DOWNLINK");

        gm.flight.experimentFraction = 0f;
        gm.flight.downlinkFraction = 0f;
        r = MissionScoring.EvaluateGame(gm);
        Check("No experiments: fails SCIENCE", r.status == "FAILURE" && r.failedGates.Contains("SCIENCE"));

        gm.flight.experimentFraction = 0.5f;
        gm.flight.downlinkFraction = 0.9f; // the game does not allow sending more than what was measured
        Near("Data > experiments: limited to the experiments", MissionScoring.InputsFromGame(gm).downlinkFraction, 0.5f);
    }

    // ---------- 5. States, 0-100 range and recalculating without duplicating events ----------

    static void StatesAndRecalculation()
    {
        FillGame(gm, Mission(12), Block1Observation());
        gm.flight.launched = false;
        Check("Not launched: pending", MissionScoring.EvaluateGame(gm).status == "PENDING");
        gm.flight.launched = true;
        gm.flight.flightFinished = false;
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Check("Flight not finished: pending and official 0", r.status == "PENDING" && r.competitive == 0f);

        gm.flight.flightFinished = true;
        gm.flight.safeReturnDone = false;
        r = MissionScoring.EvaluateGame(gm);
        Fails("No safe return", r, "RETURN");
        Check("No safe return: diagnostic > 0 and official 0", r.total > 0f && r.competitive == 0f);

        // Best possible case: never goes over 100 even if the events add a lot
        FillGame(gm, Mission(12), Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD"));
        gm.flight.pilotingResult = gm.flight.repairResult = gm.flight.researchResult = 1f;
        gm.flight.experimentFraction = gm.flight.downlinkFraction = 1f;
        AddEvent(EventPhase.Planning, 50);
        AddEvent(EventPhase.Departure, 50);
        r = MissionScoring.EvaluateGame(gm);
        Check("Best case with +50 events: total between 0 and 100 (" + r.total.ToString("0.00") + ")", r.total >= 0f && r.total <= 100f);

        // Worst case: everything wrong and -50 events
        FillGame(gm, Mission(15), Parts("ART-SLS-BLOCK1", "ART-SCI-HEAVY"));
        gm.flight.departureDone = gm.flight.lunarOrbitDone = gm.flight.safeReturnDone = false;
        gm.flight.pilotingResult = gm.flight.repairResult = gm.flight.researchResult = -1f;
        gm.flight.experimentFraction = gm.flight.downlinkFraction = -1f;
        gm.weatherRisk = 1f;
        gm.money = 0f;
        gm.flight.moneyAtLaunch = 0f;
        AddEvent(EventPhase.Planning, -50);
        AddEvent(EventPhase.Return, -50);
        r = MissionScoring.EvaluateGame(gm);
        Check("Worst case with -50 events: total between 0 and 100 (" + r.total.ToString("0.00") + ")", r.total >= 0f && r.total <= 100f);

        // Recalculating does not duplicate events (RandomEvents.Apply already added to planningScore)
        FillGame(gm, Mission(12), Block1Observation());
        float before = MissionScoring.EvaluateGame(gm).planning;
        AddEvent(EventPhase.Planning, 2);
        gm.planningScore += 2f; // as RandomEvents.Apply does
        float first = MissionScoring.EvaluateGame(gm).planning;
        float second = MissionScoring.EvaluateGame(gm).planning;
        Near("+2 event: added once", first, before + 2f);
        Near("Recalculating gives the same result", second, first);
    }

    // ---------- 6. Invalid inputs ----------

    static void InvalidInputs()
    {
        FillGame(gm, Mission(12), Block1Observation());
        ScoreInputs bad;
        bad = Base(); bad.pilotingRaw = 1.4f; Rejected("Minigame greater than 1", bad);
        bad = Base(); bad.weatherRisk = -0.01f; Rejected("Negative weather", bad);
        bad = Base(); bad.moneyAtLaunch = -1f; Rejected("Negative money", bad);
        bad = Base(); bad.moneyAtLaunch = bad.startingMoney + 1f; Rejected("Money greater than the budget", bad);
        bad = Base(); bad.experimentFraction = 0.4f; bad.downlinkFraction = 0.8f; Rejected("More data than experiments", bad);
        bad = Base(); bad.departureDone = 0f; bad.lunarOrbitDone = 1f; bad.safeReturnDone = 0f; Rejected("Stages out of order", bad);
        bad = Base(); bad.safeReturnDone = 0.5f; Rejected("Stage that is not 0 or 1", bad);
        bad = Base(); bad.craftDvRequired = 0f; Rejected("Required delta-v 0", bad);
        bad = Base(); bad.waterRequired = 0f; Rejected("Water consumption 0", bad);
        bad = Base(); bad.loadW = float.NaN; Rejected("NaN value", bad);
        Check("The base game is valid", MissionScoring.Problem(Base()) == "");
    }

    static ScoreInputs Base()
    {
        return MissionScoring.InputsFromGame(gm);
    }

    // ---------- Sample game ----------

    // Fills a GameManager with a complete game of scenario 1 (also used by the Results menu).
    public static void FillExampleGame(GameManager target, DestinationData mission)
    {
        FillGame(target, mission, Block1Observation());
    }

    // Complete game: trained crew, weather 0.15, flight finished with good minigames.
    public static void FillGame(GameManager target, DestinationData mission, List<PartData> parts)
    {
        target.ResetGame();
        target.mission = mission;
        target.missionRead = true;
        target.rocketParts.Clear();
        target.rocketParts.AddRange(parts);
        target.money = Mathf.Max(0f, target.startingMoney - RocketPhysics.TotalPrice(parts));
        target.rocketValid = RocketPhysics.IsRocketValid(parts, mission);

        target.pilot = Astronaut("Test pilot", 1, 8, 4);
        target.engineer = Astronaut("Test engineer", 3, 4, 7);
        target.scientist = Astronaut("Test scientist", 9, 2, 3);
        target.pilotTraining = TrainingFocus.Piloting;
        target.engineerTraining = TrainingFocus.Engineering;
        target.scientistTraining = TrainingFocus.Research;

        LaunchSiteData site = ScriptableObject.CreateInstance<LaunchSiteData>();
        site.siteName = "Test site";
        target.launchSite = site;
        target.launchDay = 0;
        target.weatherRisk = 0.15f;

        MissionResults f = target.flight;
        f.launched = true;
        f.moneyAtLaunch = target.money;
        f.departureDone = true;
        f.lunarOrbitDone = true;
        f.safeReturnDone = true;
        f.pilotingResult = 0.9f;
        f.repairResult = 0.85f;
        f.researchResult = 0.75f;
        f.experimentFraction = 0.9f;
        f.downlinkFraction = 0.85f;
        f.flightFinished = true;
    }

    static List<PartData> Block1Observation()
    {
        return Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
    }

    // spacecraft (capsule, service module, abort tower) + the requested parts
    static List<PartData> Parts(params string[] ids)
    {
        List<PartData> list = new List<PartData> { Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        foreach (string id in ids) list.Add(Part(id));
        return list;
    }

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>(PartsFolder + id + ".asset");
    }

    // Copy of the mission with a different duration (does not modify the asset)
    static DestinationData Mission(int days)
    {
        DestinationData m = Object.Instantiate(baseMission);
        m.missionDays = days;
        return m;
    }

    static AstronautData Astronaut(string name, int research, int piloting, int engineering)
    {
        AstronautData a = ScriptableObject.CreateInstance<AstronautData>();
        a.astronautName = name;
        a.research = research;
        a.piloting = piloting;
        a.engineering = engineering;
        return a;
    }

    static void AddEvent(EventPhase phase, int score)
    {
        EventData e = ScriptableObject.CreateInstance<EventData>();
        e.eventName = "Test event";
        e.phase = phase;
        e.scoreChange = score;
        gm.eventsHappened.Add(e);
    }

    // ---------- Checks ----------

    static void Success(string name, ScoreResult r)
    {
        Check(name + ": success (" + string.Join(",", r.failedGates) + ")", r.status == "SUCCESS");
        Near(name + ": official = diagnostic", r.competitive, r.total);
    }

    static void Fails(string name, ScoreResult r, string gate)
    {
        Check(name + ": fails " + gate + " (" + string.Join(",", r.failedGates) + ")",
            r.status == "FAILURE" && r.failedGates.Contains(gate));
        Near(name + ": official 0", r.competitive, 0f);
    }

    static void Rejected(string name, ScoreInputs inputs)
    {
        string problem = MissionScoring.Problem(inputs);
        Report(name + ": rejected", problem != "", problem == "" ? "accepted" : problem);
    }

    static void Check(string name, bool ok)
    {
        Report(name, ok, "");
    }

    static void Near(string name, float actual, float expected)
    {
        bool ok = Mathf.Abs(actual - expected) < 0.001f;
        Report(name, ok, "expected " + expected.ToString("0.000000") + ", got " + actual.ToString("0.000000"));
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
