using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Pruebas del puntaje (tarea 16) con la fisica Artemis y las piezas reales del catalogo
// (Data/Piezas, mision Data/Destinos/MisionArtemis). Los valores esperados se calcularon aparte
// con las mismas formulas y los datos de CatalogoArtemis.json.
// Menu: SpaceApps -> Pruebas de puntuacion. Escribe en la consola cada prueba con OK o FALLA.
public static class MissionScoringTests
{
    const string PartsFolder = "Assets/_Project/Data/Piezas/";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static int passed;
    static int failed;
    static DestinationData baseMission;
    static GameManager gm;

    [MenuItem("SpaceApps/Pruebas de puntuacion")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (baseMission == null || Part("ART-CM-ORION") == null)
        {
            Debug.LogError("No se pudo importar el catalogo: no se corrieron las pruebas.");
            return;
        }

        GameObject go = new GameObject("GameManager de prueba");
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

        string summary = "Pruebas de puntuacion: " + passed + " OK, " + failed + " FALLA";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // ---------- 1. Configuraciones viables (Block 1 y Block 1B) ----------

    static void ViableConfigurations()
    {
        // Escenario 1 de la mision V13: Block 1, antena, 2 instrumentos, 12 dias
        FillGame(gm, Mission(12), Block1Observation());
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Near("Block 1 observacion: capacidad del SLS (340 kg libres)", r.deliveryPoints, 7f * 340f / 1350f);
        Near("Block 1 observacion: ingenieria", r.engineering, 14.606290f);
        Near("Block 1 observacion: planeacion", r.planning, 33.753349f);
        Near("Block 1 observacion: mision", r.mission, 50.155f);
        Near("Block 1 observacion: total", r.total, 83.908349f);
        Success("Block 1 observacion", r);

        // Escenario 2: Block 1B con ciencia pesada (sobran 5 creditos)
        FillGame(gm, Mission(12), Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY"));
        r = MissionScoring.EvaluateGame(gm);
        Near("Block 1B ciencia pesada: capacidad del SLS (completa)", r.deliveryPoints, 7f);
        Near("Block 1B ciencia pesada: ingenieria", r.engineering, 17.620192f);
        Near("Block 1B ciencia pesada: total", r.total, 80.677152f);
        Success("Block 1B ciencia pesada", r);

        // Escenario 3: Block 1, 15 dias con reservas de agua, oxigeno y comida
        FillGame(gm, Mission(15), Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD"));
        r = MissionScoring.EvaluateGame(gm);
        Near("Block 1 de 15 dias con reservas: ingenieria", r.engineering, 14.016697f);
        Near("Block 1 de 15 dias con reservas: total", r.total, 80.573658f);
        Success("Block 1 de 15 dias con reservas", r);
    }

    // ---------- 2. Propelente, masa, energia y consumibles ----------

    static void PhysicsFailures()
    {
        // Falta de propelente: modulo de servicio con 7,400 kg (escenario 4 de la mision V13)
        List<PartData> lowFuel = Block1Observation();
        PartData service = Object.Instantiate(Part("ART-ESM-ORION"));
        service.fuelMass = 7400f;
        lowFuel[1] = service;
        FillGame(gm, Mission(12), lowFuel);
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Near("Poco propelente: 0 puntos de delta-v", r.craftDvPoints, 0f);
        Near("Poco propelente: total", r.total, 82.410260f);
        Fails("Poco propelente", r, "CRAFT_DELTA_V");

        // Exceso de masa: Block 1 con ciencia pesada (escenario 5)
        FillGame(gm, Mission(12), Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-HEAVY"));
        r = MissionScoring.EvaluateGame(gm);
        Near("Exceso de masa: 0 puntos de capacidad", r.deliveryPoints, 0f);
        Near("Exceso de masa: total", r.total, 80.608525f);
        Fails("Exceso de masa", r, "DELIVERY");

        // Energia insuficiente: paneles que generan solo 2,500 W
        List<PartData> lowPower = Block1Observation();
        PartData weakPanels = Object.Instantiate(Part("ART-ESM-ORION"));
        weakPanels.power = 2500f;
        lowPower[1] = weakPanels;
        FillGame(gm, Mission(12), lowPower);
        r = MissionScoring.EvaluateGame(gm);
        Near("Poca generacion: 0 puntos de generacion", r.powerPoints, 0f);
        Near("Poca generacion: total", r.total, 80.908349f);
        Fails("Poca generacion", r, "POWER");

        // Bateria que no aguanta 1.5 h sin sol (1,000 Wh)
        List<PartData> lowBattery = Block1Observation();
        PartData weakBattery = Object.Instantiate(Part("ART-ESM-ORION"));
        weakBattery.batteryWh = 1000f;
        lowBattery[1] = weakBattery;
        FillGame(gm, Mission(12), lowBattery);
        r = MissionScoring.EvaluateGame(gm);
        Near("Bateria chica: 0 puntos de baterias", r.batteryPoints, 0f);
        Fails("Bateria chica", r, "BATTERY");

        // Falta de consumibles: 15 dias sin reservas
        FillGame(gm, Mission(15), Block1Observation());
        r = MissionScoring.EvaluateGame(gm);
        Near("Sin reservas en 15 dias: 0 puntos de consumibles", r.consumablePoints, 0f);
        Near("Sin reservas en 15 dias: total", r.total, 80.637185f);
        Fails("Sin reservas en 15 dias", r, "CONSUMABLES");
    }

    // ---------- 3. Eventos que cambian el dinero despues del lanzamiento ----------

    static void MoneyAfterLaunch()
    {
        FillGame(gm, Mission(12), Block1Observation());
        float before = MissionScoring.EvaluateGame(gm).budgetPoints;

        gm.AddMoney(500f); // evento bueno del vuelo
        Near("Evento +500 creditos en el vuelo: el ahorro no cambia", MissionScoring.EvaluateGame(gm).budgetPoints, before);
        gm.money = 0f;     // evento malo del vuelo que se lleva todo
        Near("Evento que deja 0 creditos en el vuelo: el ahorro no cambia", MissionScoring.EvaluateGame(gm).budgetPoints, before);

        gm.flight.launched = false; // antes de lanzar si cuenta el dinero actual
        Near("Antes de lanzar: el ahorro usa el dinero actual (0)", MissionScoring.EvaluateGame(gm).budgetPoints, 0f);
    }

    // ---------- 4. Limites de experimentos y transmision ----------

    static void ScienceLimits()
    {
        Debug.Log("Minimo actual para ganar: experimentos > " + MissionScoring.MinExperimentFraction
            + ", datos > " + MissionScoring.MinDownlinkFraction + " (propuesta pendiente: 0.5).");

        FillGame(gm, Mission(12), Block1Observation());
        gm.flight.experimentFraction = 1f;
        gm.flight.downlinkFraction = 1f;
        Near("Ciencia completa: 10 puntos", MissionScoring.EvaluateGame(gm).sciencePoints, 10f);

        gm.flight.experimentFraction = 0.01f;
        gm.flight.downlinkFraction = 0.01f;
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Near("1% de experimentos y datos: 4.06 puntos de ciencia", r.sciencePoints, 4.06f);
        Check("1% de experimentos y datos: " + (MissionScoring.MinExperimentFraction > 0.01f ? "falla" : "gana con la regla actual"),
            (r.status == "SUCCESS") == (MissionScoring.MinExperimentFraction <= 0.01f && MissionScoring.MinDownlinkFraction <= 0.01f));

        gm.flight.experimentFraction = 0.8f;
        gm.flight.downlinkFraction = 0f;
        Fails("Sin datos mandados", MissionScoring.EvaluateGame(gm), "NO_DOWNLINK");

        gm.flight.experimentFraction = 0f;
        gm.flight.downlinkFraction = 0f;
        r = MissionScoring.EvaluateGame(gm);
        Check("Sin experimentos: falla SCIENCE", r.status == "FAILURE" && r.failedGates.Contains("SCIENCE"));

        gm.flight.experimentFraction = 0.5f;
        gm.flight.downlinkFraction = 0.9f; // el juego no deja mandar mas de lo que se midio
        Near("Datos > experimentos: se limita a los experimentos", MissionScoring.InputsFromGame(gm).downlinkFraction, 0.5f);
    }

    // ---------- 5. Estados, rango 0-100 y recalcular sin duplicar eventos ----------

    static void StatesAndRecalculation()
    {
        FillGame(gm, Mission(12), Block1Observation());
        gm.flight.launched = false;
        Check("Sin lanzar: pendiente", MissionScoring.EvaluateGame(gm).status == "PENDING");
        gm.flight.launched = true;
        gm.flight.flightFinished = false;
        ScoreResult r = MissionScoring.EvaluateGame(gm);
        Check("Vuelo sin terminar: pendiente y oficial 0", r.status == "PENDING" && r.competitive == 0f);

        gm.flight.flightFinished = true;
        gm.flight.safeReturnDone = false;
        r = MissionScoring.EvaluateGame(gm);
        Fails("Sin regreso seguro", r, "RETURN");
        Check("Sin regreso seguro: diagnostico > 0 y oficial 0", r.total > 0f && r.competitive == 0f);

        // Mejor caso posible: nunca pasa de 100 aunque los eventos sumen mucho
        FillGame(gm, Mission(12), Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD"));
        gm.flight.pilotingResult = gm.flight.repairResult = gm.flight.researchResult = 1f;
        gm.flight.experimentFraction = gm.flight.downlinkFraction = 1f;
        AddEvent(EventPhase.Planning, 50);
        AddEvent(EventPhase.Departure, 50);
        r = MissionScoring.EvaluateGame(gm);
        Check("Mejor caso con eventos +50: total entre 0 y 100 (" + r.total.ToString("0.00") + ")", r.total >= 0f && r.total <= 100f);

        // Peor caso: todo mal y eventos -50
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
        Check("Peor caso con eventos -50: total entre 0 y 100 (" + r.total.ToString("0.00") + ")", r.total >= 0f && r.total <= 100f);

        // Recalcular no duplica eventos (RandomEvents.Apply ya sumo a planningScore)
        FillGame(gm, Mission(12), Block1Observation());
        float before = MissionScoring.EvaluateGame(gm).planning;
        AddEvent(EventPhase.Planning, 2);
        gm.planningScore += 2f; // como lo hace RandomEvents.Apply
        float first = MissionScoring.EvaluateGame(gm).planning;
        float second = MissionScoring.EvaluateGame(gm).planning;
        Near("Evento de +2: suma una vez", first, before + 2f);
        Near("Recalcular da lo mismo", second, first);
    }

    // ---------- 6. Entradas invalidas ----------

    static void InvalidInputs()
    {
        FillGame(gm, Mission(12), Block1Observation());
        ScoreInputs bad;
        bad = Base(); bad.pilotingRaw = 1.4f; Rejected("Minijuego mayor que 1", bad);
        bad = Base(); bad.weatherRisk = -0.01f; Rejected("Clima negativo", bad);
        bad = Base(); bad.moneyAtLaunch = -1f; Rejected("Dinero negativo", bad);
        bad = Base(); bad.moneyAtLaunch = bad.startingMoney + 1f; Rejected("Dinero mayor que el presupuesto", bad);
        bad = Base(); bad.experimentFraction = 0.4f; bad.downlinkFraction = 0.8f; Rejected("Mas datos que experimentos", bad);
        bad = Base(); bad.departureDone = 0f; bad.lunarOrbitDone = 1f; bad.safeReturnDone = 0f; Rejected("Etapas fuera de orden", bad);
        bad = Base(); bad.safeReturnDone = 0.5f; Rejected("Etapa que no es 0 o 1", bad);
        bad = Base(); bad.craftDvRequired = 0f; Rejected("Delta-v requerido 0", bad);
        bad = Base(); bad.waterRequired = 0f; Rejected("Consumo de agua 0", bad);
        bad = Base(); bad.loadW = float.NaN; Rejected("Valor NaN", bad);
        Check("La partida base es valida", MissionScoring.Problem(Base()) == "");
    }

    static ScoreInputs Base()
    {
        return MissionScoring.InputsFromGame(gm);
    }

    // ---------- Partida de ejemplo ----------

    // Llena un GameManager con una partida completa del escenario 1 (la usa tambien el menu de Resultados).
    public static void FillExampleGame(GameManager target, DestinationData mission)
    {
        FillGame(target, mission, Block1Observation());
    }

    // Partida completa: tripulacion entrenada, clima 0.15, vuelo terminado con buenos minijuegos.
    public static void FillGame(GameManager target, DestinationData mission, List<PartData> parts)
    {
        target.ResetGame();
        target.mission = mission;
        target.missionRead = true;
        target.rocketParts.Clear();
        target.rocketParts.AddRange(parts);
        target.money = Mathf.Max(0f, target.startingMoney - RocketPhysics.TotalPrice(parts));
        target.rocketValid = RocketPhysics.IsRocketValid(parts, mission);

        target.pilot = Astronaut("Piloto de prueba", 1, 8, 4);
        target.engineer = Astronaut("Ingeniera de prueba", 3, 4, 7);
        target.scientist = Astronaut("Cientifica de prueba", 9, 2, 3);
        target.pilotTraining = TrainingFocus.Piloting;
        target.engineerTraining = TrainingFocus.Engineering;
        target.scientistTraining = TrainingFocus.Research;

        LaunchSiteData site = ScriptableObject.CreateInstance<LaunchSiteData>();
        site.siteName = "Sitio de prueba";
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

    // Orion (capsula, modulo de servicio, torre de escape) + las piezas que se pidan
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

    // Copia de la mision con otra duracion (no modifica el asset)
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
        e.eventName = "Evento de prueba";
        e.phase = phase;
        e.scoreChange = score;
        gm.eventsHappened.Add(e);
    }

    // ---------- Revisiones ----------

    static void Success(string name, ScoreResult r)
    {
        Check(name + ": exito (" + string.Join(",", r.failedGates) + ")", r.status == "SUCCESS");
        Near(name + ": oficial = diagnostico", r.competitive, r.total);
    }

    static void Fails(string name, ScoreResult r, string gate)
    {
        Check(name + ": falla " + gate + " (" + string.Join(",", r.failedGates) + ")",
            r.status == "FAILURE" && r.failedGates.Contains(gate));
        Near(name + ": oficial 0", r.competitive, 0f);
    }

    static void Rejected(string name, ScoreInputs inputs)
    {
        string problem = MissionScoring.Problem(inputs);
        Report(name + ": se rechaza", problem != "", problem == "" ? "se acepto" : problem);
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
