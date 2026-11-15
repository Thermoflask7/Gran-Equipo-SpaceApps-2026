using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// RocketAssembly tests (task 9 logic). Signed: claude_c.
// Menu: SpaceApps -> Rocket assembly tests. Writes each test to the console with OK or FAILED.
// Uses the imported catalog (menu SpaceApps -> Import Artemis catalog) and a temporary GameManager.
public static class RocketAssemblyTests
{
    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Rocket assembly tests")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        DestinationData baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        PartData block1 = Part("ART-SLS-BLOCK1");
        PartData block1b = Part("ART-SLS-BLOCK1B");
        PartData capsule = Part("ART-CM-ORION");
        PartData service = Part("ART-ESM-ORION");
        PartData tower = Part("ART-LAS-ORION");
        PartData comms = Part("ART-COMMS-DS");
        PartData lunar = Part("ART-SCI-LUNAR");
        PartData rad = Part("ART-SCI-RAD");
        if (baseMission == null || block1 == null || block1b == null || capsule == null || service == null
            || tower == null || comms == null || lunar == null || rad == null)
        {
            Debug.LogError("Rocket assembly tests: run SpaceApps -> Import Artemis catalog first. The tests were not run.");
            return;
        }
        DestinationData mission = Object.Instantiate(baseMission);
        mission.missionDays = 12; // V13 scenario 1 (Block 1 observation) is valid at 12 days

        // Temporary GameManager (Awake does not run in edit mode)
        GameObject temp = new GameObject("TestGameManager");
        temp.hideFlags = HideFlags.HideAndDontSave; // does not mark the open scene as modified
        GameManager gm = temp.AddComponent<GameManager>();
        gm.mission = mission;

        // Nothing bought
        Check("Empty rocket is not valid", !gm.rocketValid && RocketAssembly.Status(gm) != "");
        Check("Empty rocket misses SLS and the 3 Orion parts", RocketAssembly.MissingRequired(gm).Count == 4);
        Check("A part that was not bought cannot be mounted", !RocketAssembly.Mount(gm, capsule) && gm.rocketParts.Count == 0);
        Check("Null part cannot be mounted", !RocketAssembly.Mount(gm, null) && RocketAssembly.MountProblem(gm, null) != "");

        // Buy the V13 scenario 1 parts and mount them one by one
        gm.boughtParts.AddRange(new List<PartData> { block1, capsule, service, tower, comms, lunar, rad });
        Check("Bought parts appear unmounted", RocketAssembly.UnmountedParts(gm).Count == 7);
        Check("Mount capsule", RocketAssembly.Mount(gm, capsule) && RocketAssembly.IsMounted(gm, capsule));
        Check("Same part cannot be mounted twice", !RocketAssembly.Mount(gm, capsule) && gm.rocketParts.Count == 1);
        Check("Capsule alone is not valid", !gm.rocketValid);
        Check("Capsule alone misses 3 required types", RocketAssembly.MissingRequired(gm).Count == 3);

        int added = RocketAssembly.MountAll(gm);
        Check("MountAll adds the other 6 parts", added == 6 && gm.rocketParts.Count == 7);
        Check("Nothing left in the tray", RocketAssembly.UnmountedParts(gm).Count == 0);
        Check("Nothing required is missing", RocketAssembly.MissingRequired(gm).Count == 0);
        Check("Scenario 1 rocket is valid (rocketValid set)", gm.rocketValid && RocketAssembly.Status(gm) == "");

        // Unmount and mount again
        Check("Unmount the service module", RocketAssembly.Unmount(gm, service) && !RocketAssembly.IsMounted(gm, service));
        Check("Without service module it is not valid", !gm.rocketValid);
        Check("Unmounted part stays bought", gm.boughtParts.Contains(service));
        Check("Unmounting it again does nothing", !RocketAssembly.Unmount(gm, service));
        Check("Mount it back: valid again", RocketAssembly.Mount(gm, service) && gm.rocketValid);

        // Launcher swap: Block 1B replaces Block 1 on the rocket
        gm.boughtParts.Add(block1b);
        Check("Mount Block 1B swaps the launcher", RocketAssembly.Mount(gm, block1b)
            && RocketAssembly.IsMounted(gm, block1b) && !RocketAssembly.IsMounted(gm, block1));
        Check("Only one SLS on the rocket", CountType(gm.rocketParts, PartType.LaunchService) == 1);
        Check("Block 1 goes back to the tray", RocketAssembly.UnmountedParts(gm).Contains(block1));

        // MountAll never mounts a second SLS
        RocketAssembly.Clear(gm);
        Check("Clear empties the rocket", gm.rocketParts.Count == 0 && !gm.rocketValid);
        RocketAssembly.MountAll(gm);
        Check("MountAll with 2 SLS bought mounts only one", CountType(gm.rocketParts, PartType.LaunchService) == 1
            && gm.rocketParts.Count == 7);

        // Sync after selling (SpaceMart sells through gm.SellPart) and after outside edits
        gm.boughtParts.Remove(lunar); // sold without passing through SellPart
        gm.rocketParts.Add(null);
        gm.rocketParts.Add(capsule);  // duplicate
        RocketAssembly.Sync(gm);
        Check("Sync removes sold, null and repeated parts", !gm.rocketParts.Contains(lunar)
            && !gm.rocketParts.Contains(null) && CountOf(gm.rocketParts, capsule) == 1);
        Check("Sync recalculates rocketValid", gm.rocketValid == RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission));

        Object.DestroyImmediate(temp);
        Object.DestroyImmediate(mission);

        Debug.Log("Rocket assembly tests: " + passed + " OK, " + failed + " FAILED.");
        if (failed > 0) Debug.LogError("Rocket assembly: review console errors before creating a PR.");
    }

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>("Assets/_Project/Data/Piezas/" + id + ".asset");
    }

    static int CountType(List<PartData> parts, PartType type)
    {
        int n = 0;
        foreach (PartData p in parts) if (p != null && p.type == type) n++;
        return n;
    }

    static int CountOf(List<PartData> parts, PartData part)
    {
        int n = 0;
        foreach (PartData p in parts) if (p == part) n++;
        return n;
    }

    static void Check(string label, bool value)
    {
        if (value) { passed++; Debug.Log("OK rocket assembly: " + label); }
        else { failed++; Debug.LogError("FAILED rocket assembly: " + label); }
    }
}
