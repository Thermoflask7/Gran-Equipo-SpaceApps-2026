using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Research minigame tests (task 14). Signed: claude_c.
// Menu: SpaceApps > Research minigame > Tests (edit mode). Pure logic (ResearchMath), the hook's write rule and
// the scoring with real catalog parts. The games themselves (mouse, timing) are checked by playing (see the design doc).
public static class ResearchMinigameTests
{
    const string PartsFolder = "Assets/_Project/Data/Piezas/";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    static int passed;
    static int failed;
    static ResearchSettings S { get { return ResearchSettings.Default; } }

    [MenuItem("SpaceApps/Research minigame/Tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        PartData lunar = Part(ResearchMath.LunarId);
        PartData rad = Part(ResearchMath.RadiationId);
        PartData heavy = Part(ResearchMath.HeavyId);
        PartData antenna = Part("ART-COMMS-DS");
        if (mission == null || lunar == null || rad == null || heavy == null || antenna == null)
        {
            Debug.LogError("Research minigame tests: the mission catalog could not be loaded (SpaceApps > Import mission catalog).");
            return;
        }

        Planning(lunar, rad, heavy, antenna);
        Formulas();
        Beam(mission, lunar, rad, heavy, antenna);
        HookWrite();
        Scoring(mission);
        Flight();

        string summary = "Research minigame tests: " + passed + " OK, " + failed + " FAILED.";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    [MenuItem("SpaceApps/Research minigame/Tests", true)]
    static bool RunAllValid()
    {
        return !Application.isPlaying;
    }

    static void Planning(PartData lunar, PartData rad, PartData heavy, PartData antenna)
    {
        List<ResearchMath.Target> plan = ResearchMath.PlanTargets(new List<PartData> { lunar, rad, antenna }, S);
        Check("LUNAR + RAD: 6 landmarks + 12 sparks", Count(plan, ResearchMath.TargetType.Landmark) == 6 && Count(plan, ResearchMath.TargetType.Spark) == 12 && plan.Count == 18);
        // Floats near 1e8 are only precise to a few units: compare with a relative tolerance (0.001 %)
        Check("Planned data = RocketPhysics.DataBits (125 Mbit)",
            NearRel(ResearchMath.PlannedBits(plan), RocketPhysics.DataBits(new List<PartData> { lunar, rad, antenna }))
            && NearRel(ResearchMath.PlannedBits(plan), 125000000f));
        Check("Types are interleaved", plan.Count > 1 && plan[0].type != plan[1].type);

        bool ordered = true;
        bool inside = true;
        for (int i = 0; i < plan.Count; i++)
        {
            if (i > 0 && plan[i].spawnTime < plan[i - 1].spawnTime) ordered = false;
            if (plan[i].spawnTime < 0f || plan[i].spawnTime > S.safariSeconds - S.landmarkSeconds) inside = false;
        }
        Check("Targets spawn in order, early enough to cross the screen", ordered && inside);

        List<ResearchMath.Target> all = ResearchMath.PlanTargets(new List<PartData> { lunar, rad, heavy, antenna }, S);
        Check("HEAVY adds 3 ice scans (21 targets)", Count(all, ResearchMath.TargetType.Ice) == 3 && all.Count == 21);
        Check("Each ice scan carries a third of 250 Mbit", NearRel(Find(all, ResearchMath.TargetType.Ice).bits, 250000000f / 3f));

        List<ResearchMath.Target> none = ResearchMath.PlanTargets(new List<PartData> { antenna }, S);
        Check("No instruments: no targets, no planned data", none.Count == 0 && ResearchMath.PlannedBits(none) == 0f);
        Check("Null parts are safe", ResearchMath.PlanTargets(null, null).Count == 0);
        Check("Antenna detection", ResearchMath.HasAntenna(new List<PartData> { lunar, antenna }) && !ResearchMath.HasAntenna(new List<PartData> { lunar }));
    }

    static void Formulas()
    {
        float b = 125f;
        Check("Nothing captured = 0", ResearchMath.ExperimentFraction(0f, b) == 0f);
        Check("Everything captured = 1", Near(ResearchMath.ExperimentFraction(b, b), 1f, 0.0001f));
        Check("No planned data = 0 (no division by zero)", ResearchMath.ExperimentFraction(10f, 0f) == 0f && ResearchMath.DownlinkFraction(10f, 0f, 1f) == 0f);
        Check("Downlink never above experiments", Near(ResearchMath.DownlinkFraction(b, b, 0.6f), 0.6f, 0.0001f));
        Check("NaN is 0", ResearchMath.ExperimentFraction(float.NaN, b) == 0f && ResearchMath.Raw(float.NaN, 0f, 0f, S) == 0f);
        Check("Doing nothing: raw 0", ResearchMath.Raw(0f, 0f, 0f, S) == 0f);
        Check("Perfect play: raw 1", Near(ResearchMath.Raw(1f, 1f, 1f, S), 1f, 0.0001f));
        // Attentive player of the design table: 5/6 landmarks (2 gold), 10/12 sparks, 85 % of it sent
        float exp = (5f * 100f / 6f + 10f * 25f / 12f) / 125f;
        float down = ResearchMath.DownlinkFraction(0.85f * exp * b, b, exp);
        float raw = ResearchMath.Raw(exp, down, 2f / 5f, S);
        Check("Attentive player (design table): exp 0.83, downlink 0.71, raw 0.73",
            Near(exp, 0.83f, 0.01f) && Near(down, 0.71f, 0.01f) && Near(raw, 0.73f, 0.01f));
        Check("Gold stars alone are at most 10 %", ResearchMath.Raw(0f, 0f, 1f, S) <= 0.10f + 0.0001f);
    }

    static void Beam(DestinationData mission, PartData lunar, PartData rad, PartData heavy, PartData antenna)
    {
        float two = ResearchMath.BeamSecondsForAll(new List<PartData> { lunar, rad, antenna }, mission, S);
        float heavyOnly = ResearchMath.BeamSecondsForAll(new List<PartData> { heavy, antenna }, mission, S);
        float three = ResearchMath.BeamSecondsForAll(new List<PartData> { lunar, rad, heavy, antenna }, mission, S);
        Check("Beam time: LUNAR + RAD 9.9 s, HEAVY 11.9 s, all three 13.8 s",
            Near(two, 9.93f, 0.05f) && Near(heavyOnly, 11.86f, 0.05f) && Near(three, 13.79f, 0.05f));
        Check("All beam times fit in the Signal Catcher time", three < S.catcherSeconds - 5f);
        float none = ResearchMath.BeamSecondsForAll(new List<PartData> { lunar, rad }, mission, S);
        Check("No antenna: infinite beam time, send rate 0", float.IsInfinity(none) && ResearchMath.SendRate(125f, none) == 0f);
        Check("Send rate = planned data / beam time", Near(ResearchMath.SendRate(125f, 10f), 12.5f, 0.0001f));
        Check("Angle gap wraps around", Near(ResearchMath.AngleGap(170f, -170f), 20f, 0.001f));
    }

    static void HookWrite()
    {
        MissionResults f = new MissionResults();
        ResearchFlightHook.Write(f, new ResearchResult { experimentFraction = 0.8f, downlinkFraction = 0.9f });
        Check("Hook writes the fractions, downlink capped by experiments", Near(f.experimentFraction, 0.8f, 0.0001f) && Near(f.downlinkFraction, 0.8f, 0.0001f));
        ResearchFlightHook.Write(f, new ResearchResult { experimentFraction = 0.3f, downlinkFraction = 0.1f });
        Check("A second, worse play keeps the best values", Near(f.experimentFraction, 0.8f, 0.0001f) && Near(f.downlinkFraction, 0.8f, 0.0001f));
        MissionResults zero = new MissionResults();
        ResearchFlightHook.Write(zero, new ResearchResult());
        Check("Doing nothing writes 0 (not -1, never invented)", zero.experimentFraction == 0f && zero.downlinkFraction == 0f);
        ResearchFlightHook.Write(null, null);
        Check("Null is safe", true);
    }

    static void Scoring(DestinationData mission)
    {
        GameObject go = new GameObject("Test GameManager");
        go.hideFlags = HideFlags.HideAndDontSave;
        GameManager gm = go.AddComponent<GameManager>();
        try
        {
            MissionScoringTests.FillExampleGame(gm, mission);
            gm.flight.experimentFraction = -1f;
            gm.flight.downlinkFraction = -1f;
            gm.flight.researchResult = -1f;
            Check("Without research the mission fails the science gates", MissionScoring.EvaluateGame(gm).failedGates.Contains("SCIENCE"));

            ResearchFlightHook.Write(gm.flight, new ResearchResult { experimentFraction = 0.83f, downlinkFraction = 0.71f });
            FlightPhases.SetResult(gm.flight, TrainingFocus.Research, 0.73f);
            ScoreResult r = MissionScoring.EvaluateGame(gm);
            Check("Attentive research makes a valid game a SUCCESS (" + string.Join(",", r.failedGates) + ")", r.status == "SUCCESS");

            gm.flight.experimentFraction = 0.9f;
            gm.flight.downlinkFraction = 0f;
            Check("Photos without sending fail NO_DOWNLINK", MissionScoring.EvaluateGame(gm).failedGates.Contains("NO_DOWNLINK"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static void Flight()
    {
        // Research never aborts the flight (task 31), even with raw 0
        MissionFailureRules.Outcome o = MissionFailureRules.Judge(TrainingFocus.Research, EventPhase.LunarOrbit, 0f, 0.3f,
            EventSeverity.Minor, 0, MissionFailureSettings.Default);
        Check("Research result 0 never aborts", o.decision == MissionFailureRules.Decision.Continue);
    }

    // ---------- Helpers ----------

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>(PartsFolder + id + ".asset");
    }

    static int Count(List<ResearchMath.Target> list, ResearchMath.TargetType type)
    {
        int n = 0;
        foreach (ResearchMath.Target t in list) if (t.type == type) n++;
        return n;
    }

    static ResearchMath.Target Find(List<ResearchMath.Target> list, ResearchMath.TargetType type)
    {
        foreach (ResearchMath.Target t in list) if (t.type == type) return t;
        return new ResearchMath.Target();
    }

    static bool Near(float a, float b, float tolerance)
    {
        return Mathf.Abs(a - b) <= tolerance;
    }

    static bool NearRel(float a, float b)
    {
        return Mathf.Abs(a - b) <= Mathf.Max(1f, Mathf.Abs(b) * 0.00001f);
    }

    static void Check(string name, bool ok)
    {
        if (ok)
        {
            passed++;
            Debug.Log("OK: " + name);
        }
        else
        {
            failed++;
            Debug.LogError("FAILED: " + name);
        }
    }
}
