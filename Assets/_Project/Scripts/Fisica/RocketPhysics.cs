using System.Collections.Generic;
using UnityEngine;

// Calculos de fisica del cohete (tarea 17). Todo es estatico: RocketPhysics.LaunchDeltaV(partes), etc.
//
// Como se arma el cohete:
//   La lista de piezas va de ARRIBA hacia ABAJO (capsula primero, motor de la primera etapa al final).
//   Las piezas arriba del primer motor son la NAVE (capsula Orion, modulo de servicio, modulos opcionales,
//   torre de escape). Despues, cada motor cierra una etapa del cohete: la etapa son ese motor y los tanques
//   que estan arriba de el hasta el motor anterior.
//
//   Ejemplo:  Escape, Capsula, ModuloServicio, Ciencia | Tanque, Motor | Tanque, Tanque, Motor
//             \____________ nave (carga util) ________/  \_ etapa 2 _/   \____ etapa 1 ____/
//
// Hay dos calculos de delta-v (ecuacion de Tsiolkovsky: dv = Isp * g0 * ln(m0 / mf)):
//   1. Cohete (lo arma el jugador): sus etapas deben llevar la nave de la Tierra a la inyeccion translunar.
//      Debe dar al menos mission.launchDeltaV. Al terminar una etapa se suelta su masa seca.
//   2. Nave (modelo de Cristina, Orion despues de la translunar): con el Isp y el propelente del modulo de
//      servicio debe dar al menos mission.requiredDeltaV (llegar a la orbita lunar, regresar y reserva).
//      La torre de escape no cuenta aqui: se suelta durante el despegue.
public static class RocketPhysics
{
    public const float G0 = 9.80665f;      // m/s2 (gravedad estandar, igual que los datos de Cristina)
    public const float OutageHours = 1.5f;  // horas sin sol que la bateria debe aguantar (dato de balanceo)

    // dv de un motor con masa inicial m0 y final mf (kg). Isp en segundos.
    public static float DeltaV(float isp, float m0, float mf)
    {
        if (isp <= 0f || mf <= 0f || m0 <= mf) return 0f;
        return isp * G0 * Mathf.Log(m0 / mf);
    }

    static float Mass(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts) total += p.dryMass + p.fuelMass;
        return total;
    }

    // Masa total al despegue (kg): seca + combustible de todo.
    public static float TotalMass(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.dryMass + p.fuelMass;
        }
        return total;
    }

    // ---------- Partes del cohete ----------

    // La nave: todo lo que esta arriba del primer motor (incluye la torre de escape).
    public static List<PartData> Payload(List<PartData> parts)
    {
        List<PartData> result = new List<PartData>();
        foreach (PartData p in parts)
        {
            if (p == null) continue;
            if (p.type == PartType.Engine) break;
            result.Add(p);
        }
        return result;
    }

    // Etapas del cohete. stages[0] es la de ABAJO (la que se enciende primero).
    // No incluye la nave. Piezas abajo del ultimo motor no cuentan (ValidationMessage lo avisa).
    public static List<List<PartData>> SplitStages(List<PartData> parts)
    {
        List<List<PartData>> topToBottom = new List<List<PartData>>();
        List<PartData> current = new List<PartData>();
        bool payloadDone = false;

        foreach (PartData p in parts)
        {
            if (p == null) continue;
            if (p.type == PartType.Engine) payloadDone = true;
            if (!payloadDone) continue; // es parte de la nave

            current.Add(p);
            if (p.type == PartType.Engine)
            {
                topToBottom.Add(current);
                current = new List<PartData>();
            }
        }

        topToBottom.Reverse();
        return topToBottom;
    }

    // ---------- 1. Cohete ----------

    // Delta-v de cada etapa, en orden de encendido (abajo -> arriba). m/s.
    public static List<float> StageDeltaVs(List<PartData> parts)
    {
        List<float> result = new List<float>();
        float massAbove = TotalMass(parts); // masa de todo lo que sigue pegado al cohete

        foreach (List<PartData> stage in SplitStages(parts))
        {
            float fuel = 0f;
            float dry = 0f;
            float isp = 0f;
            foreach (PartData p in stage)
            {
                fuel += p.fuelMass;
                dry += p.dryMass;
                if (p.type == PartType.Engine) isp = p.isp;
            }

            float m0 = massAbove;
            float mf = m0 - fuel;
            result.Add(DeltaV(isp, m0, mf));

            massAbove = mf - dry; // se suelta la etapa vacia
        }
        return result;
    }

    // Delta-v total del cohete (m/s): despegue + inyeccion translunar.
    public static float LaunchDeltaV(List<PartData> parts)
    {
        float total = 0f;
        foreach (float dv in StageDeltaVs(parts)) total += dv;
        return total;
    }

    // Relacion empuje/peso al despegue (motor de la etapa de abajo). Debe ser > 1 para despegar.
    // thrust esta en kN.
    public static float LiftoffTWR(List<PartData> parts)
    {
        List<List<PartData>> stages = SplitStages(parts);
        if (stages.Count == 0) return 0f;

        float thrustKN = 0f;
        foreach (PartData p in stages[0])
        {
            if (p.type == PartType.Engine) thrustKN += p.thrust;
        }

        float weight = TotalMass(parts) * G0; // N
        if (weight <= 0f) return 0f;
        return thrustKN * 1000f / weight;
    }

    // ---------- 2. Nave (Orion despues de la translunar) ----------

    // Las piezas de la nave que viajan a la Luna (sin la torre de escape).
    public static List<PartData> Spacecraft(List<PartData> parts)
    {
        List<PartData> result = new List<PartData>();
        foreach (PartData p in Payload(parts))
        {
            if (p.type != PartType.AbortSystem) result.Add(p);
        }
        return result;
    }

    // Delta-v de la nave con el modulo de servicio. m/s.
    public static float SpacecraftDeltaV(List<PartData> parts)
    {
        List<PartData> craft = Spacecraft(parts);
        float isp = 0f;
        float fuel = 0f;
        foreach (PartData p in craft)
        {
            fuel += p.fuelMass;
            if (p.type == PartType.ServiceModule) isp = p.isp;
        }
        float m0 = Mass(craft);
        return DeltaV(isp, m0, m0 - fuel);
    }

    // Combustible que necesitaria la nave para lograr cierto delta-v (kg). Sirve para dar pistas al jugador.
    public static float FuelNeeded(float isp, float dryMass, float targetDeltaV)
    {
        if (isp <= 0f) return 0f;
        return dryMass * (Mathf.Exp(targetDeltaV / (isp * G0)) - 1f);
    }

    // ---------- Energia ----------

    // Suma de power (W) de todas las piezas. Positivo = sobra, negativo = falta.
    public static float PowerBalance(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.power;
        }
        return total;
    }

    // Consumo total (W): la suma de los power negativos, en positivo.
    public static float PowerConsumption(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts)
        {
            if (p != null && p.power < 0f) total -= p.power;
        }
        return total;
    }

    public static float BatteryWh(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.batteryWh;
        }
        return total;
    }

    // La bateria debe aguantar todo el consumo durante OutageHours sin sol.
    public static bool BatteryCoversOutage(List<PartData> parts)
    {
        return BatteryWh(parts) >= PowerConsumption(parts) * OutageHours;
    }

    // ---------- Validacion ----------

    static bool HasType(List<PartData> parts, PartType type)
    {
        foreach (PartData p in parts)
        {
            if (p != null && p.type == type) return true;
        }
        return false;
    }

    // Revisa si el cohete sirve para la mision. Regresa "" si todo esta bien,
    // o un mensaje en espanol con el primer problema (para mostrarlo en el modelo de cohete).
    public static string ValidationMessage(List<PartData> parts, DestinationData mission)
    {
        if (parts == null || parts.Count == 0) return "El cohete no tiene piezas.";
        if (!HasType(parts, PartType.Capsule)) return "Falta la capsula para la tripulacion.";
        if (!HasType(parts, PartType.ServiceModule)) return "Falta el modulo de servicio de la nave.";
        if (!HasType(parts, PartType.Engine)) return "Falta al menos un motor.";
        if (parts[parts.Count - 1].type != PartType.Engine) return "La pieza de abajo debe ser un motor.";

        float twr = LiftoffTWR(parts);
        if (twr <= 1f) return "El cohete no puede despegar: empuje/peso " + twr.ToString("0.00") + ", necesita mas de 1. Agrega motores o quita peso.";

        if (mission != null)
        {
            float mass = TotalMass(parts);
            if (mission.maxMass > 0f && mass > mission.maxMass)
                return "Demasiado pesado: " + mass.ToString("N0") + " kg de " + mission.maxMass.ToString("N0") + " kg permitidos.";

            float launchDv = LaunchDeltaV(parts);
            if (launchDv < mission.launchDeltaV)
                return "El cohete no llega a la Luna: " + launchDv.ToString("N0") + " m/s de " + mission.launchDeltaV.ToString("N0") + " m/s necesarios.";

            float craftDv = SpacecraftDeltaV(parts);
            if (craftDv < mission.requiredDeltaV)
                return "La nave no puede llegar a la orbita lunar y regresar: " + craftDv.ToString("N0") + " m/s de " + mission.requiredDeltaV.ToString("N0") + " m/s necesarios.";
        }

        if (PowerBalance(parts) < 0f) return "No hay energia suficiente: agrega paneles solares.";
        if (!BatteryCoversOutage(parts)) return "La bateria no aguanta " + OutageHours + " horas sin sol: agrega baterias.";

        return "";
    }

    public static bool IsRocketValid(List<PartData> parts, DestinationData mission)
    {
        return ValidationMessage(parts, mission) == "";
    }
}
