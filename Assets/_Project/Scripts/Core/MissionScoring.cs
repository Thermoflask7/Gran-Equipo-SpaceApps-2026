using System.Collections.Generic;
using UnityEngine;

// Puntaje final de 0 a 100 (tarea 16), con el sistema V16 de Cristina
// (Drive: CRISTINA/PUNTUACION, README_PUNTUACION_V16.md).
//
//   Total = Planeacion (0-45) + Mision (0-55)
//   Planeacion: ingenieria 24 + presupuesto 7 + entrenamiento 8 + clima 6, +/- 3 por eventos
//   Mision:     hitos del vuelo 24 + minijuegos 21 + ciencia 10,          +/- 3 por eventos
//
// El puntaje se muestra aunque la mision falle (para aprender). Si falla, el puntaje
// "competitivo" es 0. Ganar depende de las condiciones obligatorias (gates), no del puntaje.
//
// Uso en Resultados:  ScoreResult r = MissionScoring.SaveFinalScore();
public static class MissionScoring
{
    // ---------- Puntaje desde la partida ----------

    // Calcula el puntaje de la partida y lo guarda en gm.planningScore y gm.missionScore.
    // Se recalcula desde cero (no suma), asi que llamarlo varias veces no cuenta doble los eventos.
    public static ScoreResult SaveFinalScore()
    {
        GameManager gm = GameManager.Instance;
        ScoreResult r = EvaluateGame(gm);
        gm.planningScore = r.planning;
        gm.missionScore = r.mission;
        return r;
    }

    // Igual que SaveFinalScore pero sin tocar GameManager (para mostrar un avance antes del final).
    public static ScoreResult EvaluateGame(GameManager gm)
    {
        if (gm.mission == null)
        {
            ScoreResult empty = new ScoreResult();
            empty.status = "INVALID";
            empty.problem = "No hay mision cargada. Para probar: Play y SpaceApps -> Pantalla de resultados -> Prueba: partida de ejemplo.";
            return empty;
        }
        ScoreResult r = Evaluate(InputsFromGame(gm));
        if (r.problem != "") return r;

        // Condiciones que solo existen en el juego (ademas de las de Evaluate)
        List<string> gameGates = new List<string>();
        if (!gm.flight.launched) gameGates.Add("NO_LAUNCH");
        if (!RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission)) gameGates.Add("ROCKET_INVALID");
        if (!gm.IsCrewReady()) gameGates.Add("CREW_NOT_READY");

        if (gameGates.Count > 0)
        {
            r.failedGates.InsertRange(0, gameGates);
            r.status = "FAILURE";
        }
        else if (!gm.flight.flightFinished)
        {
            // El vuelo no ha terminado: no se da por ganado ni por perdido
            r.status = "PENDING";
            r.failedGates.Clear();
        }
        r.competitive = r.status == "SUCCESS" ? r.total : 0f;
        return r;
    }

    // Llena las entradas de V16 con el estado de GameManager y RocketPhysics (catalogo Artemis).
    public static ScoreInputs InputsFromGame(GameManager gm)
    {
        ScoreInputs i = new ScoreInputs();
        List<PartData> parts = gm.rocketParts;
        DestinationData mission = gm.mission;
        PartData sls = RocketPhysics.LaunchService(parts);

        // El SLS es un servicio completo: el jugador no arma sus etapas, asi que no hay delta-v
        // ni empuje/peso del lanzador que calcular. Esos 9 puntos (6 + 3) se dan completos si
        // el SLS elegido puede llevar la nave a la translunar, y 0 si no.
        bool slsCanCarry = sls != null && mission != null && RocketPhysics.DeliveryMargin(parts, mission) >= 0f;
        i.launchDvRequired = 1f;
        i.launchDvAvailable = slsCanCarry ? 1.05f : 0f;
        i.liftoffTwr = slsCanCarry ? 1.3f : 0f;

        // Masa: el limite es la capacidad del SLS a la translunar (V16 dice "crear calculo por fase")
        i.massLimit = sls != null ? sls.payloadCapacity : 1f; // sin SLS: 0 puntos de masa (y el cohete no es valido)
        i.massForLimit = RocketPhysics.TliMass(parts, mission);

        i.craftDvAvailable = RocketPhysics.SpacecraftDeltaV(parts);
        i.craftDvRequired = mission != null ? mission.requiredDeltaV : 0f;
        i.powerNetW = RocketPhysics.PowerBalance(parts);
        i.loadW = RocketPhysics.PowerConsumption(parts);
        i.batteryWh = RocketPhysics.BatteryWh(parts);

        // El dinero ganado con eventos no cuenta como ahorro: se limita al presupuesto inicial
        i.startingMoney = gm.startingMoney;
        i.moneyRemaining = Mathf.Clamp(gm.money, 0f, gm.startingMoney);

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
        i.requiredSciencePoints = mission != null ? mission.minSciencePoints : 40f;

        foreach (EventData e in gm.eventsHappened)
        {
            if (e == null) continue;
            if (e.phase == EventPhase.Planning) i.planningEventSum += e.scoreChange;
            else i.missionEventSum += e.scoreChange;
        }
        return i;
    }

    // -1 (no jugado) cuenta como 0
    static float Played(float value)
    {
        if (value < 0f) return 0f;
        return Mathf.Clamp01(value);
    }

    // ---------- Formula V16 ----------

    public static ScoreResult Evaluate(ScoreInputs i)
    {
        ScoreResult r = new ScoreResult();
        r.problem = Problem(i);
        if (r.problem != "")
        {
            r.status = "INVALID";
            return r;
        }

        // Planeacion: ingenieria (24). Puntos completos con 5% de margen (8% en la nave).
        r.launchDvPoints = 6f * Mathf.Clamp01((i.launchDvAvailable - i.launchDvRequired) / (0.05f * i.launchDvRequired));
        r.craftDvPoints = 7f * Mathf.Clamp01((i.craftDvAvailable - i.craftDvRequired) / (0.08f * i.craftDvRequired));
        r.massPoints = 4f * Mathf.Clamp01((i.massLimit - i.massForLimit) / (0.05f * i.massLimit));
        r.twrPoints = 3f * Mathf.Clamp01((i.liftoffTwr - 1f) / 0.3f);
        float load = Mathf.Max(i.loadW, 1f);
        float outageWh = i.loadW * i.outageHours;
        r.energyPoints = 2f * Mathf.Clamp01(i.powerNetW / (0.25f * load))
                       + 2f * Mathf.Clamp01((i.batteryWh - outageWh) / (0.5f * Mathf.Max(outageWh, 1f)));
        r.engineering = r.launchDvPoints + r.craftDvPoints + r.massPoints + r.twrPoints + r.energyPoints;

        // Presupuesto (7): completo si sobra al menos el 15%
        r.budgetPoints = 7f * Mathf.Clamp01(i.moneyRemaining / (0.15f * i.startingMoney));
        // Entrenamiento (8) y clima (6)
        r.trainingPoints = 8f * (i.pilotSkill + i.engineerSkill + i.scientistSkill) / 3f;
        r.weatherPoints = 6f * (1f - i.weatherRisk);

        r.planningBase = r.engineering + r.budgetPoints + r.trainingPoints + r.weatherPoints;

        // Mision: hitos (24), minijuegos (21) y ciencia (10)
        r.milestonePoints = 8f * (i.departureDone + i.lunarOrbitDone + i.safeReturnDone);
        r.minigamePoints = 7f * (i.pilotingRaw * i.pilotSkill + i.repairRaw * i.engineerSkill + i.researchRaw * i.scientistSkill);
        r.sciencePoints = 4f * Mathf.Clamp01(i.sciencePoints / i.requiredSciencePoints)
                        + 3f * i.experimentFraction
                        + 3f * i.downlinkFraction;
        r.missionBase = r.milestonePoints + r.minigamePoints + r.sciencePoints;

        // Eventos: maximo +/- 3 por etapa
        r.planningEvents = Mathf.Clamp(i.planningEventSum, -3f, 3f);
        r.missionEvents = Mathf.Clamp(i.missionEventSum, -3f, 3f);
        r.planning = Mathf.Clamp(r.planningBase + r.planningEvents, 0f, 45f);
        r.mission = Mathf.Clamp(r.missionBase + r.missionEvents, 0f, 55f);
        r.total = r.planning + r.mission;

        // Condiciones obligatorias para ganar (no dependen del puntaje)
        if (i.launchDvAvailable < i.launchDvRequired) r.failedGates.Add("LAUNCH_DELTA_V");
        if (i.craftDvAvailable < i.craftDvRequired) r.failedGates.Add("CRAFT_DELTA_V");
        if (i.massForLimit > i.massLimit) r.failedGates.Add("MASS");
        if (i.liftoffTwr <= 1f) r.failedGates.Add("TWR");
        if (i.powerNetW < 0f || i.batteryWh < outageWh) r.failedGates.Add("POWER");
        if (i.departureDone < 1f) r.failedGates.Add("DEPARTURE");
        if (i.lunarOrbitDone < 1f) r.failedGates.Add("LUNAR_ORBIT");
        if (i.safeReturnDone < 1f) r.failedGates.Add("RETURN");
        if (i.sciencePoints < i.requiredSciencePoints || i.experimentFraction <= 0f) r.failedGates.Add("SCIENCE");
        if (i.downlinkFraction <= 0f) r.failedGates.Add("NO_DOWNLINK");

        r.status = r.failedGates.Count == 0 ? "SUCCESS" : "FAILURE";
        r.competitive = r.status == "SUCCESS" ? r.total : 0f;
        return r;
    }

    // Revisa que las entradas tengan sentido (casos invalidos de Cristina). "" = todo bien.
    public static string Problem(ScoreInputs i)
    {
        float[] all = { i.launchDvAvailable, i.launchDvRequired, i.craftDvAvailable, i.craftDvRequired,
            i.massLimit, i.massForLimit, i.liftoffTwr, i.powerNetW, i.loadW, i.batteryWh, i.outageHours,
            i.startingMoney, i.moneyRemaining, i.pilotSkill, i.engineerSkill, i.scientistSkill, i.weatherRisk,
            i.departureDone, i.lunarOrbitDone, i.safeReturnDone, i.pilotingRaw, i.repairRaw, i.researchRaw,
            i.sciencePoints, i.requiredSciencePoints, i.experimentFraction, i.downlinkFraction,
            i.planningEventSum, i.missionEventSum };
        foreach (float v in all)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return "Hay un valor que no es un numero.";
        }

        if (i.launchDvRequired <= 0f || i.craftDvRequired <= 0f) return "El delta-v requerido debe ser mayor que 0.";
        if (i.massLimit <= 0f) return "El limite de masa debe ser mayor que 0.";
        if (i.requiredSciencePoints <= 0f) return "Los puntos de ciencia requeridos deben ser mayores que 0.";
        if (i.startingMoney <= 0f) return "El presupuesto inicial debe ser mayor que 0.";
        if (i.moneyRemaining < 0f || i.moneyRemaining > i.startingMoney) return "El dinero restante debe estar entre 0 y el presupuesto inicial.";
        if (i.loadW < 0f || i.batteryWh < 0f || i.massForLimit < 0f || i.sciencePoints < 0f) return "Hay una masa, consumo, bateria o ciencia negativa.";

        float[] fractions = { i.pilotSkill, i.engineerSkill, i.scientistSkill, i.weatherRisk,
            i.pilotingRaw, i.repairRaw, i.researchRaw, i.experimentFraction, i.downlinkFraction };
        foreach (float v in fractions)
        {
            if (v < 0f || v > 1f) return "Una habilidad, el clima, un minijuego o la ciencia no esta entre 0 y 1.";
        }

        if (!IsFlag(i.departureDone) || !IsFlag(i.lunarOrbitDone) || !IsFlag(i.safeReturnDone)) return "Los hitos del vuelo deben ser 0 o 1.";
        if (i.lunarOrbitDone > i.departureDone || i.safeReturnDone > i.lunarOrbitDone) return "Los hitos del vuelo deben cumplirse en orden.";
        if (i.downlinkFraction > i.experimentFraction) return "No se pueden mandar mas datos que los experimentos hechos.";
        return "";
    }

    static bool IsFlag(float v)
    {
        return v == 0f || v == 1f;
    }
}
