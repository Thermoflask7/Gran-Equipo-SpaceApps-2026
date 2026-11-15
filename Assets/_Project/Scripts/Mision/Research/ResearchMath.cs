using System.Collections.Generic;
using UnityEngine;

// Pure logic of the research minigame (task 14, claude_c): which targets appear, how much data each one carries,
// and the three results. No UI, so it is tested in edit mode (SpaceApps > Research minigame > Tests).
//   experimentFraction = captured data / planned data            (data-weighted: big ice scans count more)
//   downlinkFraction   = data that reached Earth / planned data, never above experimentFraction
//   raw                = 0.45 x experiments + 0.45 x downlink + 0.10 x gold-star share (0..1)
// raw never includes the scientist skill: MissionScoring multiplies by it once.
public static class ResearchMath
{
    public enum TargetType { Landmark, Spark, Ice }

    public const string LunarId = "ART-SCI-LUNAR";
    public const string RadiationId = "ART-SCI-RAD";
    public const string HeavyId = "ART-SCI-HEAVY";

    // One science target of the photo game
    public class Target
    {
        public TargetType type;
        public float bits;       // data it carries
        public float spawnTime;  // seconds into the photo game
        public string source;    // instrument name
    }

    // The instruments mounted define the targets. Each instrument splits its data among its targets.
    public static List<Target> PlanTargets(List<PartData> parts, ResearchSettings s)
    {
        if (s == null) s = ResearchSettings.Default;
        List<Target> list = new List<Target>();
        foreach (PartData p in RocketPhysics.Spacecraft(parts))
        {
            if (p == null || p.experimentDataBits <= 0f) continue;
            TargetType type = TypeOf(p);
            int count = Mathf.Max(1, CountFor(type, s));
            for (int i = 0; i < count; i++)
                list.Add(new Target { type = type, bits = p.experimentDataBits / count, source = p.partName });
        }

        // Interleave types (landmark, spark, ice, landmark...) and spread them evenly over the game,
        // leaving the last seconds free so the last target can cross the screen
        list = Interleave(list);
        float first = 1f;
        float last = Mathf.Max(first, s.safariSeconds - s.landmarkSeconds - 0.5f);
        for (int i = 0; i < list.Count; i++)
            list[i].spawnTime = list.Count == 1 ? first : Mathf.Lerp(first, last, i / (float)(list.Count - 1));
        return list;
    }

    public static TargetType TypeOf(PartData p)
    {
        if (p != null && p.catalogId == RadiationId) return TargetType.Spark;
        if (p != null && p.catalogId == HeavyId) return TargetType.Ice;
        return TargetType.Landmark; // ART-SCI-LUNAR and any other instrument
    }

    static int CountFor(TargetType type, ResearchSettings s)
    {
        if (type == TargetType.Spark) return s.sparkTargets;
        if (type == TargetType.Ice) return s.iceTargets;
        return s.landmarkTargets;
    }

    static List<Target> Interleave(List<Target> list)
    {
        List<Target>[] byType = { new List<Target>(), new List<Target>(), new List<Target>() };
        foreach (Target t in list) byType[(int)t.type].Add(t);
        List<Target> result = new List<Target>();
        bool added = true;
        for (int i = 0; added; i++)
        {
            added = false;
            foreach (List<Target> group in byType)
            {
                if (i < group.Count)
                {
                    result.Add(group[i]);
                    added = true;
                }
            }
        }
        return result;
    }

    // Total planned data (bits): the same value as RocketPhysics.DataBits
    public static float PlannedBits(List<Target> targets)
    {
        float total = 0f;
        if (targets != null) foreach (Target t in targets) total += t.bits;
        return total;
    }

    public static float ExperimentFraction(float capturedBits, float plannedBits)
    {
        if (plannedBits <= 0f) return 0f;
        return Clean(capturedBits / plannedBits);
    }

    public static float DownlinkFraction(float sentBits, float plannedBits, float experimentFraction)
    {
        if (plannedBits <= 0f) return 0f;
        return Mathf.Min(Clean(sentBits / plannedBits), Clean(experimentFraction));
    }

    public static float Raw(float experimentFraction, float downlinkFraction, float goldShare, ResearchSettings s)
    {
        if (s == null) s = ResearchSettings.Default;
        return Clean(s.experimentWeight * Clean(experimentFraction)
                   + s.downlinkWeight * Clean(downlinkFraction)
                   + s.goldWeight * Clean(goldShare));
    }

    // Beam seconds needed to send ALL the planned data: min + extra x (real downlink time / mission maximum).
    // Infinity when there is no antenna, 0 when there is no data.
    public static float BeamSecondsForAll(List<PartData> parts, DestinationData mission, ResearchSettings s)
    {
        if (s == null) s = ResearchSettings.Default;
        float seconds = RocketPhysics.DownlinkSeconds(parts);
        if (seconds <= 0f) return 0f;
        if (float.IsInfinity(seconds) || float.IsNaN(seconds)) return float.PositiveInfinity;
        float max = mission != null && mission.maxDownlinkSeconds > 0f ? mission.maxDownlinkSeconds : 3600f;
        return s.minBeamSeconds + s.extraBeamSeconds * Mathf.Min(seconds / max, 1f);
    }

    // Data sent per second of beam on Earth (bits/s in game time). 0 without antenna.
    public static float SendRate(float plannedBits, float beamSecondsForAll)
    {
        if (plannedBits <= 0f || beamSecondsForAll <= 0f || float.IsInfinity(beamSecondsForAll)) return 0f;
        return plannedBits / beamSecondsForAll;
    }

    public static bool HasAntenna(List<PartData> parts)
    {
        foreach (PartData p in RocketPhysics.Spacecraft(parts)) if (p != null && p.dataRateBps > 0f) return true;
        return false;
    }

    // Smallest signed difference between two angles in degrees (-180..180)
    public static float AngleGap(float a, float b)
    {
        return Mathf.DeltaAngle(a, b);
    }

    public static float Clean(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return Mathf.Clamp01(v);
    }
}
