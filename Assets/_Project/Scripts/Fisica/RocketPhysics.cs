using System.Collections.Generic;
using UnityEngine;

// Calculos de fisica del cohete (tarea 17). Todo es estatico: RocketPhysics.TotalDeltaV(partes), etc.
//
// Como se arma el cohete:
//   La lista de piezas va de ARRIBA hacia ABAJO (capsula primero, motor de la primera etapa al final).
//   Cada motor cierra una etapa: la etapa son ese motor y las piezas que estan arriba de el
//   hasta el motor anterior. Las piezas arriba del ultimo motor (capsula, paneles...) son la carga util.
//
//   Ejemplo (2 etapas):  Capsula, Panel, Tanque, Motor  |  Tanque, Tanque, Motor
//                        \______ etapa 2 (arriba) _____/   \__ etapa 1 (abajo) _/
//
// Delta-v de cada etapa (ecuacion de Tsiolkovsky):  dv = Isp * g0 * ln(m0 / mf)
//   m0 = masa de todo lo que sigue arriba con su combustible
//   mf = m0 - combustible de esa etapa
// Al terminar una etapa se suelta su masa seca y se sigue con la de arriba. El total es la suma.
public static class RocketPhysics
{
    public const float G0 = 9.81f; // m/s2

    // dv de un motor con masa inicial m0 y final mf (kg). Isp en segundos.
    public static float DeltaV(float isp, float m0, float mf)
    {
        if (isp <= 0f || mf <= 0f || m0 <= mf) return 0f;
        return isp * G0 * Mathf.Log(m0 / mf);
    }

    // Masa total al despegue (kg): seca + combustible.
    public static float TotalMass(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.dryMass + p.fuelMass;
        }
        return total;
    }

    // Separa las piezas en etapas. stages[0] es la de ABAJO (la que se enciende primero).
    public static List<List<PartData>> SplitStages(List<PartData> parts)
    {
        List<List<PartData>> topToBottom = new List<List<PartData>>();
        List<PartData> current = new List<PartData>();

        foreach (PartData p in parts)
        {
            if (p == null) continue;
            current.Add(p);
            if (p.type == PartType.Engine)
            {
                topToBottom.Add(current);
                current = new List<PartData>();
            }
        }

        // La carga util (capsula, paneles...) queda dentro de la etapa mas alta, junto con su motor.
        // Lo que sobre en 'current' son piezas abajo del ultimo motor: no cuentan como etapa
        // (ValidationMessage avisa que la pieza de abajo debe ser un motor).
        topToBottom.Reverse();
        return topToBottom;
    }

    // Delta-v de cada etapa, en orden de encendido (abajo -> arriba). m/s.
    public static List<float> StageDeltaVs(List<PartData> parts)
    {
        List<float> result = new List<float>();
        List<List<PartData>> stages = SplitStages(parts);

        float massAbove = TotalMass(parts); // masa de todo lo que sigue pegado al cohete
        foreach (List<PartData> stage in stages)
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

    // Delta-v total del cohete (m/s).
    public static float TotalDeltaV(List<PartData> parts)
    {
        float total = 0f;
        foreach (float dv in StageDeltaVs(parts)) total += dv;
        return total;
    }

    // Relacion empuje/peso al despegue (motor de la etapa de abajo). Debe ser > 1 para despegar.
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

    // Energia: suma de power (W). Positivo = sobra, negativo = falta.
    public static float PowerBalance(List<PartData> parts)
    {
        float total = 0f;
        foreach (PartData p in parts)
        {
            if (p != null) total += p.power;
        }
        return total;
    }

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
        if (!HasType(parts, PartType.Engine)) return "Falta al menos un motor.";
        if (parts[parts.Count - 1].type != PartType.Engine) return "La pieza de abajo debe ser un motor.";

        float twr = LiftoffTWR(parts);
        if (twr <= 1f) return "El motor de abajo no tiene empuje para despegar (empuje/peso " + twr.ToString("0.00") + ", necesita mas de 1).";

        if (PowerBalance(parts) < 0f) return "No hay energia suficiente: agrega paneles solares o baterias.";

        if (mission != null)
        {
            float mass = TotalMass(parts);
            if (mission.maxMass > 0f && mass > mission.maxMass)
                return "Demasiado pesado: " + mass.ToString("N0") + " kg de " + mission.maxMass.ToString("N0") + " kg permitidos.";

            float dv = TotalDeltaV(parts);
            if (dv < mission.requiredDeltaV)
                return "Falta delta-v: " + dv.ToString("N0") + " m/s de " + mission.requiredDeltaV.ToString("N0") + " m/s necesarios.";
        }
        return "";
    }

    public static bool IsRocketValid(List<PartData> parts, DestinationData mission)
    {
        return ValidationMessage(parts, mission) == "";
    }
}
