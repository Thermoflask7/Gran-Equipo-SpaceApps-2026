using System.Collections.Generic;
using UnityEngine;

// Puntaje final de 0 a 100 (tarea 16): sistema V16 de Cristina (Drive: CRISTINA/PUNTUACION)
// adaptado a la fisica Artemis (el SLS es un servicio completo, no se arman etapas).
//
//   Total = Planeacion (0-45) + Mision (0-55)
//   Planeacion: ingenieria 24 + presupuesto 7 + entrenamiento 8 + clima 6, +/- 3 por eventos
//     Ingenieria: capacidad del SLS 7 + delta-v de Orion 7 + generacion 3 + baterias 3 + consumibles 4
//   Mision:     etapas del vuelo 24 + minijuegos 21 + ciencia 10,           +/- 3 por eventos
//
// Hay dos puntajes: el de diagnostico (total, para aprender, se muestra aunque la mision falle)
// y el oficial (competitive = total solo si la mision fue exitosa, si no 0).
// Ganar depende de las condiciones obligatorias (gates), no del puntaje.
//
// Uso en Resultados:  ScoreResult r = MissionScoring.SaveFinalScore();
public static class MissionScoring
{
    // Minimo de experimentos hechos y de datos mandados a la Tierra para ganar (0 a 1).
    // Hoy basta con hacer algo (> 0). PROPUESTA del equipo, pendiente de aprobar: 0.5 en ambos.
    public const float MinExperimentFraction = 0f;
    public const float MinDownlinkFraction = 0f;

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

        if (!gm.flight.launched || !gm.flight.flightFinished)
        {
            // Todavia no se lanza o el vuelo no ha terminado: ni ganada ni perdida
            r.status = "PENDING";
            r.failedGates.Clear();
        }
        else
        {
            // Condiciones que solo existen en el juego (ademas de las de Evaluate)
            List<string> gameGates = new List<string>();
            if (!RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission)) gameGates.Add("ROCKET_INVALID");
            if (!gm.IsCrewReady()) gameGates.Add("CREW_NOT_READY");
            if (gameGates.Count > 0)
            {
                r.failedGates.InsertRange(0, gameGates);
                r.status = "FAILURE";
            }
        }
        r.competitive = r.status == "SUCCESS" ? r.total : 0f;
        return r;
    }

    // Llena las entradas con el estado de GameManager y RocketPhysics (catalogo Artemis).
    public static ScoreInputs InputsFromGame(GameManager gm)
    {
        ScoreInputs i = new ScoreInputs();
        List<PartData> parts = gm.rocketParts;
        DestinationData mission = gm.mission;
        PartData sls = RocketPhysics.LaunchService(parts);

        i.deliveryCapacity = sls != null ? sls.payloadCapacity : 1f; // sin SLS: 0 puntos (y el cohete no es valido)
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

        // El ahorro se califica con el dinero que habia al lanzar: los eventos del vuelo
        // cambian gm.money pero no cambian una decision de la planeacion ya tomada.
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

    // -1 (no jugado) cuenta como 0
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

        // Ingenieria (24). Los puntos crecen con el margen y llegan al maximo con cierto margen extra.
        // SLS (7): completo con 5% de su capacidad libre
        r.deliveryPoints = 7f * Mathf.Clamp01((i.deliveryCapacity - i.tliMass) / (0.05f * i.deliveryCapacity));
        // Orion (7): completo con 8% de delta-v extra
        r.craftDvPoints = 7f * Mathf.Clamp01((i.craftDvAvailable - i.craftDvRequired) / (0.08f * i.craftDvRequired));
        // Generacion (3): completo si sobra 25% del consumo
        r.powerPoints = 3f * Mathf.Clamp01(i.powerNetW / (0.25f * Mathf.Max(i.loadW, 1f)));
        // Baterias (3): completo si aguantan 50% mas que el tiempo sin sol
        float outageWh = i.loadW * i.outageHours;
        r.batteryPoints = 3f * Mathf.Clamp01((i.batteryWh - outageWh) / (0.5f * Mathf.Max(outageWh, 1f)));
        // Consumibles (4): cada uno completo con 20% de reserva sobre lo que se consume
        r.consumablePoints = 4f * (Reserve(i.waterMargin, i.waterRequired)
                                 + Reserve(i.oxygenMargin, i.oxygenRequired)
                                 + Reserve(i.foodMargin, i.foodRequired)) / 3f;
        r.engineering = r.deliveryPoints + r.craftDvPoints + r.powerPoints + r.batteryPoints + r.consumablePoints;

        // Presupuesto (7): completo si sobra al menos el 15%
        r.budgetPoints = 7f * Mathf.Clamp01(i.moneyAtLaunch / (0.15f * i.startingMoney));
        // Entrenamiento (8) y clima (6)
        r.trainingPoints = 8f * (i.pilotSkill + i.engineerSkill + i.scientistSkill) / 3f;
        r.weatherPoints = 6f * (1f - i.weatherRisk);

        r.planningBase = r.engineering + r.budgetPoints + r.trainingPoints + r.weatherPoints;

        // Mision: etapas (24), minijuegos (21) y ciencia (10)
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

    // Reserva de un consumible de 0 a 1: 0 si no sobra nada (o falta), 1 si sobra 20% o mas.
    static float Reserve(float margin, float required)
    {
        return Mathf.Clamp01(margin / (0.2f * required));
    }

    // Revisa que las entradas tengan sentido. "" = todo bien.
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
            if (float.IsNaN(v) || float.IsInfinity(v)) return "Hay un valor que no es un numero.";
        }

        if (i.craftDvRequired <= 0f) return "El delta-v requerido debe ser mayor que 0.";
        if (i.deliveryCapacity <= 0f) return "La capacidad del lanzador debe ser mayor que 0.";
        if (i.waterRequired <= 0f || i.oxygenRequired <= 0f || i.foodRequired <= 0f) return "El consumo de la tripulacion debe ser mayor que 0.";
        if (i.requiredSciencePoints <= 0f) return "Los puntos de ciencia requeridos deben ser mayores que 0.";
        if (i.startingMoney <= 0f) return "El presupuesto inicial debe ser mayor que 0.";
        if (i.moneyAtLaunch < 0f || i.moneyAtLaunch > i.startingMoney) return "El dinero al lanzar debe estar entre 0 y el presupuesto inicial.";
        if (i.loadW < 0f || i.batteryWh < 0f || i.tliMass < 0f || i.sciencePoints < 0f) return "Hay una masa, consumo, bateria o ciencia negativa.";

        float[] fractions = { i.pilotSkill, i.engineerSkill, i.scientistSkill, i.weatherRisk,
            i.pilotingRaw, i.repairRaw, i.researchRaw, i.experimentFraction, i.downlinkFraction };
        foreach (float v in fractions)
        {
            if (v < 0f || v > 1f) return "Una habilidad, el clima, un minijuego o la ciencia no esta entre 0 y 1.";
        }

        if (!IsFlag(i.departureDone) || !IsFlag(i.lunarOrbitDone) || !IsFlag(i.safeReturnDone)) return "Las etapas del vuelo deben ser 0 o 1.";
        if (i.lunarOrbitDone > i.departureDone || i.safeReturnDone > i.lunarOrbitDone) return "Las etapas del vuelo deben cumplirse en orden.";
        if (i.downlinkFraction > i.experimentFraction) return "No se pueden mandar mas datos que los experimentos hechos.";
        return "";
    }

    static bool IsFlag(float v)
    {
        return v == 0f || v == 1f;
    }
}
