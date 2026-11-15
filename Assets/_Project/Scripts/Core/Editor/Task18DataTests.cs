using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// P5 Editor integration tests (these tests have not yet been run inside Unity).
// Menu: SpaceApps -> Test P5 data.
public static class Task18DataTests
{
    static int passed;
    static int failed;
    const string AFolder = "Assets/_Project/Data/Astronautas";
    const string EFolder = "Assets/_Project/Data/Eventos";
    const string SFolder = "Assets/_Project/Data/LaunchSites";

    [MenuItem("SpaceApps/Test P5 data")]
    public static void Run()
    {
        passed = 0;
        failed = 0;
        Task18DataImporter.Import();

        string[] astronautGuids = AssetDatabase.FindAssets("t:AstronautData", new[] { AFolder });
        string[] eventGuids = AssetDatabase.FindAssets("t:EventData", new[] { EFolder });
        string[] siteGuids = AssetDatabase.FindAssets("t:LaunchSiteData", new[] { SFolder });
        Check("10 astronaut candidates available", astronautGuids.Length >= 10);
        Check("16 events available", eventGuids.Length >= 16);
        Check("LC-39B imported", siteGuids.Length >= 1);

        int good = 0, bad = 0;
        HashSet<EventPhase> phases = new HashSet<EventPhase>();
        HashSet<TrainingFocus> roles = new HashSet<TrainingFocus>();
        foreach (string guid in eventGuids)
        {
            EventData e = AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(guid));
            if (e == null) continue;
            if (e.isGood) good++; else bad++;
            phases.Add(e.phase);
            roles.Add(e.minigameRole);
            Check("Valid probability: " + e.eventName, e.probability >= 0f && e.probability <= 1f);
            Check("Bounded score change: " + e.eventName, e.scoreChange >= -3 && e.scoreChange <= 3);
        }
        Check("Positive and negative events", good >= 5 && bad >= 5);
        Check("Events in all four phases", phases.Count == 4);
        Check("All minigame roles represented", roles.Contains(TrainingFocus.Piloting)
            && roles.Contains(TrainingFocus.Engineering) && roles.Contains(TrainingFocus.Research));

        LaunchSiteData site = AssetDatabase.LoadAssetAtPath<LaunchSiteData>(SFolder + "/SITE-KSC-LC39B.asset");
        Check("LC-39B coordinates", site != null && Mathf.Abs(site.latitude - 28.627306f) < 0.01f
            && Mathf.Abs(site.longitude + 80.620869f) < 0.01f);
        Check("14-day calendar", site != null && site.weatherRiskByDay != null && site.weatherRiskByDay.Length == 14);
        if (site != null && site.weatherRiskByDay != null)
        {
            float min = 1f, max = 0f;
            foreach (float risk in site.weatherRiskByDay)
            {
                Check("Weather risk between 0 and 1", risk >= 0f && risk <= 1f);
                min = Mathf.Min(min, risk);
                max = Mathf.Max(max, risk);
            }
            Check("Calendar provides varied weather risks", min <= 0.2f && max >= 0.7f);
        }

        // Existing contract: catalog and mission data must remain unchanged.
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        Check("Mission: crew of 3, 12 days", mission != null && mission.crewCount == 3 && mission.missionDays == 12);
        Check("V13 Orion delta-v requirement", mission != null && Mathf.Abs(mission.requiredDeltaV - 1060f) < 0.01f);

        // Runtime list (Resources/GameData.asset), filled by the import.
        GameData data = Resources.Load<GameData>(GameData.ResourcePath);
        Check("GameData asset exists", data != null);
        if (data != null)
        {
            Check("GameData: mission", data.mission == mission && mission != null);
            Check("GameData: 16 parts", data.parts.Count == 16);
            Check("GameData: all astronauts", data.astronauts.Count == astronautGuids.Length);
            Check("GameData: all events", data.events.Count == eventGuids.Length);
            Check("GameData: all sites", data.sites.Count == siteGuids.Length && data.sites.Contains(site));
            Check("GameData: no empty entries", !data.parts.Contains(null) && !data.astronauts.Contains(null)
                && !data.events.Contains(null) && !data.sites.Contains(null));
        }

        // GameManager.SetLaunch on a temporary object (Awake does not run in edit mode).
        GameObject temp = new GameObject("TestGameManager");
        temp.hideFlags = HideFlags.HideAndDontSave; // does not mark the open scene as modified
        GameManager gm = temp.AddComponent<GameManager>();
        Check("SetLaunch rejects a null site", !gm.SetLaunch(null, 0) && gm.launchSite == null && gm.launchDay == -1);
        Check("SetLaunch rejects day -1", !gm.SetLaunch(site, -1) && gm.launchDay == -1);
        Check("SetLaunch rejects day 14", !gm.SetLaunch(site, 14) && gm.launchDay == -1);
        if (site != null)
        {
            Check("SetLaunch day 7 (index 6) sets site, day and risk 0.86", gm.SetLaunch(site, 6)
                && gm.launchSite == site && gm.launchDay == 6 && Mathf.Abs(gm.weatherRisk - 0.86f) < 0.001f
                && gm.IsLaunchPlanned());
            Check("Bad day keeps the previous choice", !gm.SetLaunch(site, 99) && gm.launchDay == 6);
        }
        Object.DestroyImmediate(temp);

        Debug.Log("P5 data tests: " + passed + " OK, " + failed + " FAILED."
            + " Run these tests inside Unity to confirm integration.");
        if (failed > 0) Debug.LogError("P5: review console errors before creating a PR.");
    }

    static void Check(string label, bool value)
    {
        if (value) { passed++; Debug.Log("OK P5: " + label); }
        else { failed++; Debug.LogError("FAILED P5: " + label); }
    }
}
