using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Pruebas del puntaje V16 (tarea 16) con los 9 escenarios y los 9 casos invalidos de Cristina
// (Drive: CRISTINA/PUNTUACION/Pruebas_Puntuacion_V16.json).
// Menu: SpaceApps -> Pruebas de puntuacion. Escribe en la consola cada prueba con OK o FALLA.
public static class MissionScoringTests
{
    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Pruebas de puntuacion")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        // id, entradas (en el orden de Cristina), planeacion, mision, exito, condicion que falla
        Scenario("PERFECT_ALL", Perfect(), 45f, 55f, true, "");
        Scenario("BALANCED_12D", Inputs(13520, 13200, 1103.2742047688298f, 1060, 10000, 9700, 1.18f, 420, 3000, 5400, 1.5f, 3400, 460,
            0.9f, 0.8f, 0.9f, 0.15f, 1, 1, 1, 0.9f, 0.85f, 0.75f, 48, 40, 0.9f, 0.85f, 0, 0), 30.948313f, 48.405f, true, "");
        Scenario("HEAVY_EXPENSIVE", Inputs(13500, 13200, 1080, 1060, 10000, 9930, 1.08f, 175, 3000, 4600, 1.5f, 3400, 5,
            0.7f, 0.9f, 0.95f, 0.35f, 1, 1, 1, 0.85f, 0.8f, 0.95f, 55, 40, 1, 1, 0, 0), 17.062399f, 49.5225f, true, "");

        ScoreInputs noReturn = Perfect();
        noReturn.safeReturnDone = 0;
        noReturn.pilotingRaw = 0.9f;
        Scenario("FAIL_NO_RETURN", noReturn, 45f, 46.3f, false, "RETURN");

        ScoreInputs lowDv = Perfect();
        lowDv.craftDvAvailable = 1055;
        Scenario("FAIL_LOW_DV", lowDv, 38f, 55f, false, "CRAFT_DELTA_V");

        ScoreInputs noDownlink = Perfect();
        noDownlink.downlinkFraction = 0;
        Scenario("FAIL_NO_DOWNLINK", noDownlink, 45f, 52f, false, "NO_DOWNLINK");

        Scenario("FAIL_HIGH_COST", Inputs(13860, 13200, 1145, 1060, 10000, 9500, 1.08f, 750, 3000, 6750, 1.5f, 3400, 0,
            0.6f, 0.5f, 0.7f, 0.9f, 1, 1, 1, 0.55f, 0.5f, 0.7f, 40, 40, 1, 1, 0, 0), 27.2f, 41.49f, true, "");

        ScoreInputs events = Perfect();
        events.planningEventSum = 50;
        events.missionEventSum = -50;
        Scenario("EVENT_POS_NEG", events, 45f, 52f, true, "");

        ScoreInputs usd = Perfect();
        usd.startingMoney = 500;
        usd.moneyRemaining = 75;
        Scenario("BUDGET_500USD", usd, 45f, 55f, true, "");

        // Casos invalidos: se deben rechazar
        ScoreInputs bad;
        bad = Perfect(); bad.pilotingRaw = 1.4f; Rejected("FRACTION_GT_ONE", bad);
        bad = Perfect(); bad.weatherRisk = -0.01f; Rejected("BAD_WEATHER", bad);
        bad = Perfect(); bad.moneyRemaining = -1; Rejected("NEGATIVE_MONEY", bad);
        bad = Perfect(); bad.experimentFraction = 0.4f; bad.downlinkFraction = 0.8f; Rejected("INCONSISTENT_DOWNLINK", bad);
        bad = Perfect(); bad.departureDone = 0; bad.lunarOrbitDone = 1; bad.safeReturnDone = 0; Rejected("UNORDERED_PHASES", bad);
        bad = Perfect(); bad.craftDvRequired = 0; Rejected("ZERO_REQUIRED_DV", bad);
        bad = Perfect(); bad.moneyRemaining = 3401; Rejected("OVERSTART_MONEY", bad);
        bad = Perfect(); bad.safeReturnDone = 0.5f; Rejected("FLAG_NOT_BINARY", bad);
        bad = Perfect(); bad.loadW = float.NaN; Rejected("NAN_INPUT", bad);

        GameTest();

        string summary = "Pruebas de puntuacion: " + passed + " OK, " + failed + " FALLA";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // Partida completa con el catalogo Artemis (escenario 1 de la mision V13 + vuelo perfecto).
    // No hay valores de Cristina para esta parte: revisa que el puente con el juego funcione.
    static void GameTest()
    {
        CatalogImporter.Import();
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (mission == null)
        {
            Report("Partida de ejemplo", false, "no se pudo importar el catalogo");
            return;
        }

        GameObject go = new GameObject("GameManager de prueba");
        GameManager gm = go.AddComponent<GameManager>();
        FillExampleGame(gm, mission);

        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Check("Partida de ejemplo: exito (" + string.Join(",", r.failedGates) + ")", r.status == "SUCCESS");
        Check("Partida de ejemplo: puntaje entre 0 y 100 (" + r.total.ToString("0.00") + ")", r.total > 0f && r.total <= 100f);
        Check("Partida de ejemplo: masa con 340 kg de margen da ~1 punto (" + r.massPoints.ToString("0.000") + ")",
            Mathf.Abs(r.massPoints - 4f * 340f / 1350f) < 0.01f);

        gm.flight.flightFinished = false;
        Check("Partida de ejemplo: sin terminar el vuelo queda pendiente", MissionScoring.EvaluateGame(gm).status == "PENDING");

        gm.flight.flightFinished = true;
        gm.flight.safeReturnDone = false;
        r = MissionScoring.EvaluateGame(gm);
        Check("Partida de ejemplo: sin regreso falla", r.status == "FAILURE" && r.failedGates.Contains("RETURN"));

        // El puntaje se recalcula desde cero: un evento de +2 suma 2 una sola vez
        gm.flight.safeReturnDone = true;
        float before = MissionScoring.EvaluateGame(gm).planning;
        EventData bonus = ScriptableObject.CreateInstance<EventData>();
        bonus.phase = EventPhase.Planning;
        bonus.scoreChange = 2;
        gm.eventsHappened.Add(bonus);
        gm.planningScore += 2; // como lo hace RandomEvents.Apply
        float first = MissionScoring.EvaluateGame(gm).planning;
        float second = MissionScoring.EvaluateGame(gm).planning;
        Check("Partida de ejemplo: evento de +2 cuenta una vez", Mathf.Abs(first - Mathf.Min(before + 2f, 45f)) < 0.001f && Mathf.Abs(first - second) < 0.001f);

        Object.DestroyImmediate(go);
    }

    // Llena un GameManager con una partida completa (tambien lo usa el menu de prueba de Resultados).
    public static void FillExampleGame(GameManager gm, DestinationData mission)
    {
        gm.ResetGame();
        gm.mission = mission;
        gm.missionRead = true;
        gm.rocketParts.Clear();
        foreach (string id in new[] { "ART-SLS-BLOCK1", "ART-CM-ORION", "ART-ESM-ORION", "ART-LAS-ORION",
            "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD" })
        {
            PartData p = AssetDatabase.LoadAssetAtPath<PartData>("Assets/_Project/Data/Piezas/" + id + ".asset");
            if (p != null) gm.rocketParts.Add(p);
        }
        gm.money = gm.startingMoney - RocketPhysics.TotalPrice(gm.rocketParts);
        gm.rocketValid = RocketPhysics.IsRocketValid(gm.rocketParts, mission);

        gm.pilot = Astronaut("Piloto de prueba", 1, 8, 4);
        gm.engineer = Astronaut("Ingeniera de prueba", 3, 4, 7);
        gm.scientist = Astronaut("Cientifica de prueba", 9, 2, 3);
        gm.pilotTraining = TrainingFocus.Piloting;
        gm.engineerTraining = TrainingFocus.Engineering;
        gm.scientistTraining = TrainingFocus.Research;

        LaunchSiteData site = ScriptableObject.CreateInstance<LaunchSiteData>();
        site.siteName = "Sitio de prueba";
        gm.launchSite = site;
        gm.launchDay = 0;
        gm.weatherRisk = 0.15f;

        MissionResults f = gm.flight;
        f.launched = true;
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

    static AstronautData Astronaut(string name, int research, int piloting, int engineering)
    {
        AstronautData a = ScriptableObject.CreateInstance<AstronautData>();
        a.astronautName = name;
        a.research = research;
        a.piloting = piloting;
        a.engineering = engineering;
        return a;
    }

    // ---------- Ayudantes ----------

    static ScoreInputs Perfect()
    {
        return Inputs(13860, 13200, 1145, 1060, 10000, 9500, 1.3f, 750, 3000, 6750, 1.5f, 3400, 510,
            1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 40, 40, 1, 1, 0, 0);
    }

    static ScoreInputs Inputs(float launchDvAvailable, float launchDvRequired, float craftDvAvailable, float craftDvRequired,
        float massLimit, float massForLimit, float liftoffTwr, float powerNetW, float loadW, float batteryWh, float outageHours,
        float startingMoney, float moneyRemaining, float pilotSkill, float engineerSkill, float scientistSkill, float weatherRisk,
        float departureDone, float lunarOrbitDone, float safeReturnDone, float pilotingRaw, float repairRaw, float researchRaw,
        float sciencePoints, float requiredSciencePoints, float experimentFraction, float downlinkFraction,
        float planningEventSum, float missionEventSum)
    {
        ScoreInputs i = new ScoreInputs();
        i.launchDvAvailable = launchDvAvailable;
        i.launchDvRequired = launchDvRequired;
        i.craftDvAvailable = craftDvAvailable;
        i.craftDvRequired = craftDvRequired;
        i.massLimit = massLimit;
        i.massForLimit = massForLimit;
        i.liftoffTwr = liftoffTwr;
        i.powerNetW = powerNetW;
        i.loadW = loadW;
        i.batteryWh = batteryWh;
        i.outageHours = outageHours;
        i.startingMoney = startingMoney;
        i.moneyRemaining = moneyRemaining;
        i.pilotSkill = pilotSkill;
        i.engineerSkill = engineerSkill;
        i.scientistSkill = scientistSkill;
        i.weatherRisk = weatherRisk;
        i.departureDone = departureDone;
        i.lunarOrbitDone = lunarOrbitDone;
        i.safeReturnDone = safeReturnDone;
        i.pilotingRaw = pilotingRaw;
        i.repairRaw = repairRaw;
        i.researchRaw = researchRaw;
        i.sciencePoints = sciencePoints;
        i.requiredSciencePoints = requiredSciencePoints;
        i.experimentFraction = experimentFraction;
        i.downlinkFraction = downlinkFraction;
        i.planningEventSum = planningEventSum;
        i.missionEventSum = missionEventSum;
        return i;
    }

    static void Scenario(string id, ScoreInputs inputs, float planning, float mission, bool success, string failedGate)
    {
        ScoreResult r = MissionScoring.Evaluate(inputs);
        Near(id + ": planeacion", r.planning, planning);
        Near(id + ": mision", r.mission, mission);
        Near(id + ": total", r.total, planning + mission);
        Near(id + ": competitivo", r.competitive, success ? planning + mission : 0f);
        Check(id + ": " + (success ? "exito" : "falla") + " (" + string.Join(",", r.failedGates) + ")",
            (r.status == "SUCCESS") == success);
        if (failedGate != "") Check(id + ": falla " + failedGate, r.failedGates.Contains(failedGate) && r.failedGates.Count == 1);
    }

    static void Rejected(string id, ScoreInputs inputs)
    {
        string problem = MissionScoring.Problem(inputs);
        Report(id + ": se rechaza", problem != "", problem == "" ? "se acepto" : problem);
    }

    static void Check(string name, bool ok)
    {
        Report(name, ok, "");
    }

    static void Near(string name, float actual, float expected)
    {
        bool ok = Mathf.Abs(actual - expected) < 0.001f;
        Report(name, ok, "esperado " + expected.ToString("0.000000") + ", salio " + actual.ToString("0.000000"));
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
            Debug.LogError("FALLA  " + name + "  " + detail);
        }
    }
}
