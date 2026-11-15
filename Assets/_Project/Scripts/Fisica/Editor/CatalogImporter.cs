using System;
using UnityEditor;
using UnityEngine;

// Imports Cristina's catalog (Data/Catalogo/CatalogoArtemis.json) as ScriptableObjects.
// Menu: SpaceApps -> Import mission catalog.
//   - Creates or updates one PartData per part in Data/Piezas (file = catalog id).
//     If the asset already exists, its values are updated and the GUID is kept (references do not break).
//   - Creates the mission Data/Destinos/MisionArtemis.asset (Cristina's mission V13) if it does not exist.
//     If it already exists and someone changed it by hand, it is NOT overwritten: the console lists which fields
//     differ from V13. To return it to V13: SpaceApps -> Restore V13 mission.
// It can be run as many times as needed, for example when Cristina updates the catalog.
public static class CatalogImporter
{
    const string CatalogPath = "Assets/_Project/Data/Catalogo/CatalogoArtemis.json";
    const string PartsFolder = "Assets/_Project/Data/Piezas";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    [Serializable]
    class CatalogFile
    {
        public string source;
        public CatalogPart[] parts;
    }

    [Serializable]
    class CatalogPart
    {
        public string id;
        public string partName;
        public string type;
        public string description;
        public float price;
        public float dryMass;
        public float fuelMass;
        public float isp;
        public float thrust;
        public float power;
        public float powerDraw;
        public float batteryWh;
        public float payloadCapacity;
        public int crewCapacity;
        public int maxMissionDays;
        public float waterKg;
        public float oxygenKg;
        public float foodKg;
        public float sciencePoints;
        public float dataRateBps;
        public float experimentDataBits;
    }

    [MenuItem("SpaceApps/Import mission catalog")]
    public static void Import()
    {
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
        if (json == null)
        {
            Debug.LogError("Not found: " + CatalogPath);
            return;
        }

        CatalogFile catalog = JsonUtility.FromJson<CatalogFile>(json.text);
        int count = 0;
        foreach (CatalogPart c in catalog.parts)
        {
            PartType type;
            if (!Enum.TryParse(c.type, out type))
            {
                Debug.LogError("Unknown type '" + c.type + "' in " + c.id + ". It was not imported.");
                continue;
            }

            string path = PartsFolder + "/" + c.id + ".asset";
            PartData part = AssetDatabase.LoadAssetAtPath<PartData>(path);
            bool isNew = part == null;
            if (isNew) part = ScriptableObject.CreateInstance<PartData>();

            part.catalogId = c.id;
            part.partName = c.partName;
            part.type = type;
            part.description = c.description;
            part.price = c.price;
            part.dryMass = c.dryMass;
            part.fuelMass = c.fuelMass;
            part.isp = c.isp;
            part.thrust = c.thrust;
            part.payloadCapacity = c.payloadCapacity;
            part.power = c.power;
            part.powerDraw = c.powerDraw;
            part.batteryWh = c.batteryWh;
            part.crewCapacity = c.crewCapacity;
            part.maxMissionDays = c.maxMissionDays;
            part.waterKg = c.waterKg;
            part.oxygenKg = c.oxygenKg;
            part.foodKg = c.foodKg;
            part.sciencePoints = c.sciencePoints;
            part.dataRateBps = c.dataRateBps;
            part.experimentDataBits = c.experimentDataBits;
            // The icon is not touched: the art team sets it by hand.

            if (isNew) AssetDatabase.CreateAsset(part, path);
            else EditorUtility.SetDirty(part);
            count++;
        }

        ImportMission(false);
        AssetDatabase.SaveAssets();
        Debug.Log("Mission catalog imported: " + count + " parts in " + PartsFolder + " and the mission in " + MissionPath);
    }

    [MenuItem("SpaceApps/Restore V13 mission")]
    public static void RestoreMission()
    {
        if (!EditorUtility.DisplayDialog("Restore V13 mission",
            "All the values of " + MissionPath + " will be replaced with those of Cristina's mission V13.", "Restore", "Cancel"))
            return;
        ImportMission(true);
        AssetDatabase.SaveAssets();
        Debug.Log("Mission restored to V13: " + MissionPath);
    }

    // Creates the mission if it does not exist. If it exists: with overwrite it returns it to V13; without overwrite
    // it only reports which fields differ (so later changes are not erased by accident).
    static void ImportMission(bool overwrite)
    {
        DestinationData v13 = MissionV13();
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        if (mission == null)
        {
            AssetDatabase.CreateAsset(v13, MissionPath);
            return;
        }

        string current = EditorJsonUtility.ToJson(mission);
        string reference = EditorJsonUtility.ToJson(v13);
        if (current == reference)
        {
            UnityEngine.Object.DestroyImmediate(v13);
            return;
        }
        if (overwrite)
        {
            EditorUtility.CopySerialized(v13, mission);
            EditorUtility.SetDirty(mission);
        }
        else
        {
            Debug.LogWarning("The mission " + MissionPath + " has values that differ from V13 and was not overwritten. "
                + "If that was not intentional, use SpaceApps -> Restore V13 mission.\nCurrent: " + current + "\nV13: " + reference);
        }
        UnityEngine.Object.DestroyImmediate(v13);
    }

    // Values from SpaceMission_Mision_Artemis_V13.json (Cristina's Drive, folder MISION CALCULOS Y DETALLES).
    static DestinationData MissionV13()
    {
        DestinationData mission = ScriptableObject.CreateInstance<DestinationData>();
        mission.name = "MisionArtemis"; // same name as the asset so they can be compared

        mission.missionName = "Selene";
        mission.tagline = "Orbit the Moon with 3 astronauts"; // same card line as MainMenuBuilder, so the check does not warn
        mission.destinationName = "Crewed lunar orbit"; // no real agency or program names in the game
        mission.description = "Take 3 astronauts to an orbit around the Moon, collect scientific data and bring them back home safely.";
        mission.requiredDeltaV = 1060f; // 940 nominal (40 correction + 450 capture + 450 departure) + 120 reserve
        mission.missionDays = 12;
        mission.locked = false;
        mission.tliExtraMass = 682f;    // 26,536 at translunar injection - 25,854 after
        mission.crewCount = 3;   // team decision: crew of 3 (pilot, engineer, scientist)
        mission.waterPerPersonDay = 2.5f;
        mission.oxygenPerPersonDay = 0.84f;
        mission.foodPerPersonDay = 0.75f;
        mission.baseWaterKg = 101.25f; // V13 values x 3/4 (crew of 3), keeps the V13 balance
        mission.baseOxygenKg = 35.25f;
        mission.baseFoodKg = 33f;
        mission.minSciencePoints = 40f;
        mission.maxDownlinkSeconds = 3600f;

        return mission;
    }
}
