using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Fills Assets/_Project/Resources/GameData.asset with every data asset in Assets/_Project/Data.
// Menu: SpaceApps -> Rebuild game data list. The P5 data import runs it at the end.
// Run it again after adding or removing parts, astronauts, events or sites.
public static class GameDataBuilder
{
    const string ResourcesFolder = "Assets/_Project/Resources";
    const string AssetPath = ResourcesFolder + "/GameData.asset";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    [MenuItem("SpaceApps/Rebuild game data list")]
    public static GameData Rebuild()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets/_Project", "Resources");

        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(AssetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<GameData>();
            AssetDatabase.CreateAsset(data, AssetPath);
        }

        data.mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        data.parts = FindAll<PartData>("Assets/_Project/Data/Piezas");
        data.astronauts = FindAll<AstronautData>("Assets/_Project/Data/Astronautas");
        data.events = FindAll<EventData>("Assets/_Project/Data/Eventos");
        data.sites = FindAll<LaunchSiteData>("Assets/_Project/Data/LaunchSites");

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        if (data.mission == null) Debug.LogWarning("GameData: mission not found at " + MissionPath + ". Run SpaceApps -> Import mission catalog.");
        Debug.Log("GameData: " + data.parts.Count + " parts, " + data.astronauts.Count + " astronauts, "
            + data.events.Count + " events, " + data.sites.Count + " site(s).");
        return data;
    }

    // Sorted by asset name so the list does not change order between machines.
    static List<T> FindAll<T>(string folder) where T : ScriptableObject
    {
        List<T> list = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder)) return list;
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }
}
