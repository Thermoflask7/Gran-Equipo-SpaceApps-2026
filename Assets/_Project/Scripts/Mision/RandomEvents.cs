using System.Collections.Generic;
using UnityEngine;

// Random events during planning and the flight (task 15).
// Typical use (for example when a flight phase starts):
//   EventData e = RandomEvents.RollAndApply(events, EventPhase.Departure);
//   if (e != null)
//   {
//       // show e.eventName and e.description
//       // if e.minigameRole != TrainingFocus.None, open that minigame
//   }
public static class RandomEvents
{
    // Probability of the event, taking into account what was decided during planning.
    public static float ChanceOf(EventData e)
    {
        GameManager gm = GameManager.Instance;
        float chance = e.probability;

        // Weather: with bad weather, bad events are more likely (up to double) and good ones less likely.
        if (e.weatherSensitive)
        {
            if (e.isGood) chance *= 1f - gm.weatherRisk;
            else chance *= 1f + gm.weatherRisk;
        }

        // Crew: if the bad event falls on a role, a skilled and well-trained astronaut prevents it.
        // Skill 1 -> half the probability, skill 0 -> 1.5 times.
        if (!e.isGood && e.minigameRole != TrainingFocus.None)
        {
            float skill = gm.SkillMultiplier(AstronautFor(e.minigameRole), e.minigameRole);
            chance *= 1.5f - skill;
        }

        return Mathf.Clamp01(chance);
    }

    // Rolls the events of a phase. Each event rolls its own probability;
    // if several come up, one is kept at random. It does not repeat events that already happened.
    // Returns null if nothing happens. It does NOT apply the event (use Apply or RollAndApply for that).
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

    // Applies the event's money and score and stores it in GameManager.eventsHappened.
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

    // Events of a phase that already happened, in order. Useful for the folder and for Results.
    public static List<EventData> HappenedIn(EventPhase phase)
    {
        List<EventData> list = new List<EventData>();
        foreach (EventData e in GameManager.Instance.eventsHappened)
        {
            if (e != null && e.phase == phase) list.Add(e);
        }
        return list;
    }

    // Crew astronaut who covers a role.
    public static AstronautData AstronautFor(TrainingFocus role)
    {
        GameManager gm = GameManager.Instance;
        if (role == TrainingFocus.Piloting) return gm.pilot;
        if (role == TrainingFocus.Engineering) return gm.engineer;
        if (role == TrainingFocus.Research) return gm.scientist;
        return null;
    }
}
