using System.Collections.Generic;
using UnityEngine;

// List of every data asset the game can use at runtime: mission, parts, astronaut candidates,
// events and launch sites. AssetDatabase does not exist in builds (WebGL), so stations and phases read from here.
// Lives in Assets/_Project/Resources/GameData.asset and is filled by the editor
// (menu SpaceApps -> Rebuild game data list, also run by the P5 data import). Do not edit it at runtime.
// Use: GameData.Instance.astronauts, GameData.Instance.events, GameData.Instance.sites...
public class GameData : ScriptableObject
{
    public const string ResourcePath = "GameData"; // Resources/GameData.asset

    public DestinationData mission;
    public List<PartData> parts = new List<PartData>();
    public List<AstronautData> astronauts = new List<AstronautData>();
    public List<EventData> events = new List<EventData>();
    public List<LaunchSiteData> sites = new List<LaunchSiteData>();

    static GameData loaded;

    // null if the asset was not generated yet (run SpaceApps -> Rebuild game data list).
    public static GameData Instance
    {
        get
        {
            if (loaded == null)
            {
                loaded = Resources.Load<GameData>(ResourcePath);
                if (loaded == null) Debug.LogWarning("GameData: missing Resources/GameData.asset. Run SpaceApps -> Rebuild game data list.");
            }
            return loaded;
        }
    }
}
