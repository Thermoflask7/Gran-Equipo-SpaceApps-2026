using System.Collections.Generic;
using UnityEngine;

// Calculos de fisica de la mision (tarea 17), con el modelo de Cristina:
// catalogo V8.3, mision V13 y modelos fisicos V9.3 (Drive, carpeta CRISTINA).
// Todo es estatico: RocketPhysics.SpacecraftDeltaV(partes), etc.
//
// Como se arma el cohete (el orden de las piezas ya no importa):
//   - Se elige UN lanzador (LaunchService): SLS Block 1 (lleva 27,000 kg a la translunar)
//     o SLS Block 1B (38,000 kg). El lanzador ya trae sus etapas: no se arman tanques ni motores.
//   - La nave: capsula Orion + modulo de servicio + torre de escape (obligatorias)
//     + modulos opcionales (ciencia, antena, reservas, baterias...).
//
// Lo que se revisa:
//   1. Masa en la translunar (AM-19) <= capacidad del lanzador.
//      Masa en la translunar = masa de la nave (sin torre de escape) + mission.tliExtraMass.
//   2. Delta-v de la nave (Tsiolkovsky con el modulo de servicio, AM-17/18) >= mission.requiredDeltaV.
//   3. Energia: con sol, lo que se genera >= lo que se consume; en sombra (1.5 h) la bateria aguanta.
//   4. Tripulacion: la capsula lleva a todos, aguanta los dias y alcanzan agua, oxigeno y comida.
//   5. Ciencia: puntos >= mission.minSciencePoints y los datos se bajan en <= 1 hora.
//   El presupuesto lo revisa GameManager al comprar (precios en creditos).
public static class RocketPhysics
{
    public const float G0 = 9.80665f;      // m/s2 (gravedad estandar, igual que los datos de Cristina)
    public const float OutageHours = 1.5f;  // horas sin sol que la bateria debe aguantar (mision V13)

    // dv de un motor con masa inicial m0 y final mf (kg). Isp en segundos.
    public static float DeltaV(float isp, float m0, float mf)
    {
        if (isp <= 0f || mf <= 0f || m0 <= mf) return 0f;
        return isp * G0 * Mathf.Log(m0 / mf);
    }

    // Masa total (kg): seca + combustible de todas las piezas (el lanzador pesa 0 en este modelo).
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

    // Suma de precios (creditos).
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

    // ---------- 1. Lanzador y masa ----------

    // El SLS elegido (null si no hay).
    public static PartData LaunchService(List<PartData> parts)
    {
        return FirstOfType(parts, PartType.LaunchService);
    }

    // La nave que viaja a la Luna: todo menos el lanzador y la torre de escape.
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

    // Masa de la nave justo despues de la translunar, con combustible (kg). AM-18.
    public static float SpacecraftMass(List<PartData> parts)
    {
        return TotalMass(Spacecraft(parts));
    }

    // Masa que el lanzador tiene que llevar a la translunar (kg). AM-19.
    public static float TliMass(List<PartData> parts, DestinationData mission)
    {
        float extra = mission != null ? mission.tliExtraMass : 0f;
        return SpacecraftMass(parts) + extra;
    }

    // Capacidad del lanzador menos la masa en la translunar (kg). Negativo = no cabe. AM-07.
    public static float DeliveryMargin(List<PartData> parts, DestinationData mission)
    {
        PartData sls = LaunchService(parts);
        if (sls == null) return 0f;
        return sls.payloadCapacity - TliMass(parts, mission);
    }

    // ---------- 2. Delta-v de la nave ----------

    // Delta-v de la nave con el modulo de servicio (m/s). Solo usa su propelente. AM-17/18 + MOD-16.
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
        if (serviceModules != 1) return 0f; // ValidationMessage lo avisa
        float m0 = TotalMass(craft);
        return DeltaV(isp, m0, m0 - fuel);
    }

    // Combustible que se necesitaria para lograr cierto delta-v (kg). GM-02. Sirve para dar pistas.
    public static float FuelNeeded(float isp, float dryMass, float targetDeltaV)
    {
        if (isp <= 0f) return 0f;
        return dryMass * (Mathf.Exp(targetDeltaV / (isp * G0)) - 1f);
    }

    // ---------- 3. Energia ----------

    // Generacion menos consumo (W) con sol. Negativo = falta energia. AM-13.
    public static float PowerBalance(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in Spacecraft(parts)) total += p.power - p.powerDraw;
        return total;
    }

    // Consumo total de la nave (W).
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

    // En sombra no se genera nada: la bateria debe aguantar todo el consumo OutageHours. AM-11/12.
    public static bool BatteryCoversOutage(List<PartData> parts)
    {
        return BatteryWh(parts) >= PowerConsumption(parts) * OutageHours;
    }

    // ---------- 4. Tripulacion ----------

    // Agua, oxigeno o comida que sobra (kg) para toda la mision. Negativo = falta. AM-02..05.
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

    // ---------- 5. Ciencia ----------

    public static float SciencePoints(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in Spacecraft(parts)) total += p.sciencePoints;
        return total;
    }

    // Segundos para bajar a la Tierra todos los datos de los instrumentos. Sin antena = infinito.
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

    // ---------- Validacion ----------

    // Revisa si la nave sirve para la mision. Regresa "" si todo esta bien,
    // o un mensaje en espanol con el primer problema (para mostrarlo en el modelo de cohete).
    public static string ValidationMessage(List<PartData> parts, DestinationData mission)
    {
        if (parts == null || parts.Count == 0) return "El cohete no tiene piezas.";
        foreach (PartData p in parts)
        {
            if (p == null) return "Hay una pieza vacia en el cohete.";
            if (p.dryMass < 0f || p.fuelMass < 0f) return "\"" + p.partName + "\" tiene masa o combustible negativo.";
        }

        int launchers = CountType(parts, PartType.LaunchService);
        if (launchers == 0) return "Elige un lanzador: SLS Block 1 o SLS Block 1B.";
        if (launchers > 1) return "Solo puedes usar un lanzador.";
        if (CountType(parts, PartType.Capsule) != 1) return "La nave necesita una capsula Orion.";
        if (CountType(parts, PartType.ServiceModule) != 1) return "La nave necesita exactamente un modulo de servicio.";
        if (CountType(parts, PartType.AbortSystem) != 1) return "Falta la torre de escape para proteger a la tripulacion.";

        if (mission != null)
        {
            float margin = DeliveryMargin(parts, mission);
            if (margin < 0f)
                return "Demasiado pesado para " + LaunchService(parts).partName + ": sobran " + (-margin).ToString("N0") + " kg. Quita modulos o usa un lanzador mas grande.";

            float dv = SpacecraftDeltaV(parts);
            if (dv < mission.requiredDeltaV)
                return "La nave no puede llegar a la orbita lunar y regresar: " + dv.ToString("N0") + " m/s de " + mission.requiredDeltaV.ToString("N0") + " m/s necesarios.";

            PartData capsule = FirstOfType(parts, PartType.Capsule);
            if (capsule.crewCapacity < mission.crewCount)
                return "La capsula no tiene lugar para " + mission.crewCount + " astronautas.";
            if (capsule.maxMissionDays < mission.missionDays)
                return "La capsula aguanta " + capsule.maxMissionDays + " dias y la mision dura " + mission.missionDays + ".";
            if (WaterMargin(parts, mission) < 0f) return "No alcanza el agua para la mision: agrega reservas de agua.";
            if (OxygenMargin(parts, mission) < 0f) return "No alcanza el oxigeno para la mision: agrega reservas de oxigeno.";
            if (FoodMargin(parts, mission) < 0f) return "No alcanza la comida para la mision: agrega reservas de comida.";
        }

        if (PowerBalance(parts) < 0f) return "No hay energia suficiente: los modulos consumen mas de lo que generan los paneles.";
        if (!BatteryCoversOutage(parts)) return "La bateria no aguanta " + OutageHours + " horas sin sol: agrega baterias.";

        if (mission != null)
        {
            float science = SciencePoints(parts);
            if (science < mission.minSciencePoints)
                return "Poca ciencia: " + science.ToString("0") + " de " + mission.minSciencePoints.ToString("0") + " puntos. Agrega instrumentos.";
            if (DownlinkSeconds(parts) > mission.maxDownlinkSeconds)
                return "Los datos de ciencia no se pueden mandar a la Tierra a tiempo: agrega una antena de largo alcance.";
        }

        return "";
    }

    public static bool IsRocketValid(List<PartData> parts, DestinationData mission)
    {
        return ValidationMessage(parts, mission) == "";
    }
}
