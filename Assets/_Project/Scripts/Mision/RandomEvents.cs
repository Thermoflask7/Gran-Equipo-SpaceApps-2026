using System.Collections.Generic;
using UnityEngine;

// Eventos aleatorios de la planeacion y del vuelo (tarea 15).
// Uso tipico (por ejemplo al iniciar una fase del vuelo):
//   EventData e = RandomEvents.RollAndApply(events, EventPhase.Departure);
//   if (e != null)
//   {
//       // mostrar e.eventName y e.description
//       // si e.minigameRole != TrainingFocus.None, abrir ese minijuego
//   }
public static class RandomEvents
{
    // Probabilidad del evento tomando en cuenta lo que se decidio en la planeacion.
    public static float ChanceOf(EventData e)
    {
        GameManager gm = GameManager.Instance;
        float chance = e.probability;

        // Clima: con mal clima los eventos malos son mas probables (hasta el doble) y los buenos menos.
        if (e.weatherSensitive)
        {
            if (e.isGood) chance *= 1f - gm.weatherRisk;
            else chance *= 1f + gm.weatherRisk;
        }

        // Tripulacion: si el evento malo cae en un rol, un astronauta habil y bien entrenado lo evita.
        // Habilidad 1 -> la mitad de probabilidad, habilidad 0 -> 1.5 veces.
        if (!e.isGood && e.minigameRole != TrainingFocus.None)
        {
            float skill = gm.SkillMultiplier(AstronautFor(e.minigameRole), e.minigameRole);
            chance *= 1.5f - skill;
        }

        return Mathf.Clamp01(chance);
    }

    // Tira los eventos de una fase. Cada evento tira su propia probabilidad;
    // si salen varios se queda uno al azar. No repite eventos que ya pasaron.
    // Regresa null si no pasa nada. NO aplica el evento (para eso Apply o RollAndApply).
    public static EventData Roll(List<EventData> events, EventPhase phase)
    {
        List<EventData> hits = new List<EventData>();
        foreach (EventData e in events)
        {
            if (e == null || e.phase != phase) continue;
            if (GameManager.Instance.eventsHappened.Contains(e)) continue;
            if (Random.value < ChanceOf(e)) hits.Add(e);
        }

        if (hits.Count == 0) return null;
        return hits[Random.Range(0, hits.Count)];
    }

    // Aplica dinero y puntaje del evento y lo guarda en GameManager.eventsHappened.
    public static void Apply(EventData e)
    {
        if (e == null) return;
        GameManager gm = GameManager.Instance;

        gm.AddMoney(e.moneyChange);
        if (gm.money < 0f) gm.money = 0f;

        if (e.phase == EventPhase.Planning) gm.planningScore += e.scoreChange;
        else gm.missionScore += e.scoreChange;

        gm.eventsHappened.Add(e);
    }

    public static EventData RollAndApply(List<EventData> events, EventPhase phase)
    {
        EventData e = Roll(events, phase);
        Apply(e);
        return e;
    }

    // Eventos de una fase que ya pasaron, en orden. Sirve para el folder y para Resultados.
    public static List<EventData> HappenedIn(EventPhase phase)
    {
        List<EventData> list = new List<EventData>();
        foreach (EventData e in GameManager.Instance.eventsHappened)
        {
            if (e != null && e.phase == phase) list.Add(e);
        }
        return list;
    }

    // Astronauta de la tripulacion que cubre un rol.
    public static AstronautData AstronautFor(TrainingFocus role)
    {
        GameManager gm = GameManager.Instance;
        if (role == TrainingFocus.Piloting) return gm.pilot;
        if (role == TrainingFocus.Engineering) return gm.engineer;
        if (role == TrainingFocus.Research) return gm.scientist;
        return null;
    }
}
