using System.Collections.Generic;
using UnityEngine;

// Test values for 03_Resultados when it is played on its own (no game was played before).
// good = Block 1 observation mission that succeeds; !good = overloaded spacecraft with no data sent,
// so the Results screen shows several failed gates (task 35).
public static class ResultsSample
{
    public static void Fill(GameManager gm, bool good)
    {
        GameData data = GameData.Instance;
        gm.ResetGame();
        if (data != null && data.mission != null) gm.mission = data.mission;
        gm.missionRead = true;

        gm.rocketParts.Clear();
        AddParts(gm, data, "ART-CM-ORION", "ART-ESM-ORION", "ART-LAS-ORION", "ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        if (!good) AddParts(gm, data, "ART-SCI-HEAVY", "ART-RAD-SHIELD", "ART-CREW-HAB-BACKUP", "ART-NAV-LUNAR");
        gm.money = Mathf.Max(0f, gm.startingMoney - RocketPhysics.TotalPrice(gm.rocketParts));
        gm.rocketValid = gm.mission != null && RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission);

        gm.pilot = Astronaut("Test pilot", 1, 8, 4);
        gm.engineer = Astronaut("Test engineer", 3, 4, 7);
        gm.scientist = Astronaut("Test scientist", 9, 2, 3);
        gm.pilotTraining = TrainingFocus.Piloting;
        gm.engineerTraining = TrainingFocus.Engineering;
        gm.scientistTraining = TrainingFocus.Research;

        LaunchSiteData site = ScriptableObject.CreateInstance<LaunchSiteData>();
        site.siteName = "Test site";
        gm.launchSite = site;
        gm.launchDay = 0;
        gm.weatherRisk = 0.15f;

        MissionResults f = gm.flight;
        f.launched = true;
        f.moneyAtLaunch = gm.money;
        f.departureDone = true;
        f.lunarOrbitDone = true;
        f.safeReturnDone = true;
        f.pilotingResult = 0.9f;
        f.repairResult = 0.85f;
        f.researchResult = 0.75f;
        f.experimentFraction = 0.9f;
        f.downlinkFraction = good ? 0.85f : 0f;
        f.flightFinished = true;
    }

    static void AddParts(GameManager gm, GameData data, params string[] ids)
    {
        if (data == null) return;
        foreach (string id in ids)
        {
            foreach (PartData p in data.parts)
            {
                if (p != null && p.catalogId == id) gm.rocketParts.Add(p);
            }
        }
    }

    static AstronautData Astronaut(string name, int research, int piloting, int engineering)
    {
        AstronautData a = ScriptableObject.CreateInstance<AstronautData>();
        a.astronautName = name;
        a.research = research;
        a.piloting = piloting;
        a.engineering = engineering;
        return a;
    }
}
