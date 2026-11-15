using UnityEngine;

// Crew training with points, fatigue and traits (folder, page 2).
// - The crew shares PointPool training points. Each point adds +1 to that skill (Rookie: +2), max 10.
// - Only the skill of the astronaut's role matters in flight, so training off-role is wasted.
// - Too many points on one astronaut makes them Tired: their flight skill is multiplied by TiredFactor.
// - Traits (AstronautData.trait) give small bonuses and penalties that depend on the whole crew.
// Points are stored in GameManager.trainingPoints (slot * 3 + focus index, same order as CrewPage.Focuses).
public static class CrewTraining
{
    public const int PointPool = 6;
    public const int MaxPerSkill = 4;
    public const float TiredFactor = 0.85f;

    // ---------- Points ----------

    public static void EnsurePoints(GameManager gm)
    {
        if (gm.trainingPoints == null || gm.trainingPoints.Length != 9) gm.trainingPoints = new int[9];
    }

    public static int Points(GameManager gm, int slot, int focusIndex)
    {
        EnsurePoints(gm);
        if (slot < 0 || slot > 2 || focusIndex < 0 || focusIndex > 2) return 0;
        return gm.trainingPoints[slot * 3 + focusIndex];
    }

    public static int SlotPoints(GameManager gm, int slot)
    {
        int total = 0;
        for (int f = 0; f < 3; f++) total += Points(gm, slot, f);
        return total;
    }

    public static int UsedPoints(GameManager gm)
    {
        int total = 0;
        for (int slot = 0; slot < 3; slot++) total += SlotPoints(gm, slot);
        return total;
    }

    public static int PointsLeft(GameManager gm)
    {
        return PointPool - UsedPoints(gm);
    }

    // One click: +1 point. Past the max, or with no points left, the skill goes back to 0.
    public static void CyclePoint(GameManager gm, int slot, int focusIndex)
    {
        EnsurePoints(gm);
        int i = slot * 3 + focusIndex;
        int next = gm.trainingPoints[i] + 1;
        if (next > MaxPerSkill || PointsLeft(gm) <= 0) next = 0;
        gm.trainingPoints[i] = next;
        SyncFocus(gm);
    }

    public static void ClearPoints(GameManager gm)
    {
        gm.trainingPoints = new int[9];
        SyncFocus(gm);
    }

    // Keeps pilotTraining / engineerTraining / scientistTraining as "the skill with the most points"
    // so IsCrewReady, the checklist and the old tests keep working.
    public static void SyncFocus(GameManager gm)
    {
        for (int slot = 0; slot < 3; slot++)
        {
            TrainingFocus best = TrainingFocus.None;
            int bestPoints = 0;
            for (int f = 0; f < 3; f++)
            {
                int p = Points(gm, slot, f);
                if (p > bestPoints) { bestPoints = p; best = CrewPage.Focuses[f]; }
            }
            if (slot == 0) gm.pilotTraining = best;
            if (slot == 1) gm.engineerTraining = best;
            if (slot == 2) gm.scientistTraining = best;
        }
    }

    // ---------- Skill in flight ----------

    // Skill 0 to 10 of the astronaut in a role, with training, traits and crew effects.
    public static int Skill(GameManager gm, AstronautData a, TrainingFocus role)
    {
        if (a == null) return 0;
        int skill = BaseSkill(a, role);
        int slot = CrewPage.SlotOf(gm, a);
        if (slot >= 0)
        {
            int f = FocusIndex(role);
            int points = SlotPoints(gm, slot) > 0 ? Points(gm, slot, f) : LegacyPoints(gm, slot, role);
            skill += a.trait == CrewTrait.Rookie ? points * 2 : points;

            if (role == CrewPage.Focuses[slot])
            {
                if (a.trait == CrewTrait.Veteran || a.trait == CrewTrait.TeamPlayer) skill += 1;
                if (a.trait == CrewTrait.Loner) skill += 2;
            }
            skill -= LonerPenalty(gm, a);
        }
        return Mathf.Clamp(skill, 0, 10);
    }

    // 0 to 1, what the minigames and events use (GameManager.SkillMultiplier)
    public static float Multiplier(GameManager gm, AstronautData a, TrainingFocus role)
    {
        if (a == null) return 0.5f;
        float m = Skill(gm, a, role) / 10f;
        if (IsTired(gm, a)) m *= TiredFactor;
        return m;
    }

    public static bool IsTired(GameManager gm, AstronautData a)
    {
        int slot = CrewPage.SlotOf(gm, a);
        if (slot < 0) return false;
        return SlotPoints(gm, slot) >= TiredAt(a);
    }

    public static int TiredAt(AstronautData a)
    {
        if (a != null && a.trait == CrewTrait.Veteran) return 3;
        if (a != null && a.trait == CrewTrait.HardWorker) return 5;
        return 4;
    }

    // -1 for each other Loner in the crew, unless there is a Team player on board
    public static int LonerPenalty(GameManager gm, AstronautData a)
    {
        int loners = 0;
        for (int slot = 0; slot < 3; slot++)
        {
            AstronautData c = CrewPage.GetCrew(gm, slot);
            if (c == null) continue;
            if (c.trait == CrewTrait.TeamPlayer) return 0;
            if (c.trait == CrewTrait.Loner && c != a) loners++;
        }
        return loners;
    }

    // Short word for the player (the exact skills stay hidden)
    public static string Readiness(GameManager gm, int slot)
    {
        AstronautData a = CrewPage.GetCrew(gm, slot);
        if (a == null) return "";
        float m = Multiplier(gm, a, CrewPage.Focuses[slot]);
        if (m >= 0.95f) return "Excellent";
        if (m >= 0.8f) return "Good";
        if (m >= 0.6f) return "Fair";
        return "Low";
    }

    // ---------- Traits ----------

    public static string TraitName(CrewTrait trait)
    {
        if (trait == CrewTrait.Veteran) return "Veteran";
        if (trait == CrewTrait.HardWorker) return "Hard worker";
        if (trait == CrewTrait.TeamPlayer) return "Team player";
        if (trait == CrewTrait.Loner) return "Loner";
        if (trait == CrewTrait.Rookie) return "Rookie";
        return "";
    }

    public static string TraitDescription(CrewTrait trait)
    {
        if (trait == CrewTrait.Veteran) return "+1 in their role, but gets tired from 3 training points.";
        if (trait == CrewTrait.HardWorker) return "Does not get tired until 5 training points.";
        if (trait == CrewTrait.TeamPlayer) return "+1 in their role, and cancels the Loner penalty for the whole crew.";
        if (trait == CrewTrait.Loner) return "+2 in their role, but -1 to the rest of the crew unless a Team player is on board.";
        if (trait == CrewTrait.Rookie) return "Learns fast: every training point counts double.";
        return "";
    }

    // ---------- Helpers ----------

    static int BaseSkill(AstronautData a, TrainingFocus role)
    {
        if (role == TrainingFocus.Piloting) return a.piloting;
        if (role == TrainingFocus.Engineering) return a.engineering;
        if (role == TrainingFocus.Research) return a.research;
        return 0;
    }

    static int FocusIndex(TrainingFocus role)
    {
        for (int f = 0; f < 3; f++)
            if (CrewPage.Focuses[f] == role) return f;
        return -1;
    }

    // Test setups that only set pilotTraining (no points) count as 2 points in that skill, like before
    static int LegacyPoints(GameManager gm, int slot, TrainingFocus role)
    {
        return CrewPage.GetTraining(gm, slot) == role ? 2 : 0;
    }
}
