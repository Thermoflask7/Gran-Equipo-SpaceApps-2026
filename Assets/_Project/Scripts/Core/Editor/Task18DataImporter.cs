using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Task 18 / P5: imports astronaut, event, and weather data into ScriptableObjects.
// Does not modify RocketPhysics, GameManager, rocket parts, mission data, or scenes.
// Menu: SpaceApps -> Import P5 data (astronauts, events, weather).
// Reimporting updates supported fields only; existing portraits and other assets are preserved.
public static class Task18DataImporter
{
    const string AstronautsFile = "Assets/_Project/Data/Astronautas/AstronautsP5.json";
    const string EventsFile = "Assets/_Project/Data/Eventos/EventsP5.json";
    const string SitesFile = "Assets/_Project/Data/LaunchSites/LaunchSitesWeatherP5.json";
    const string AstronautsFolder = "Assets/_Project/Data/Astronautas";
    const string EventsFolder = "Assets/_Project/Data/Eventos";
    const string SitesFolder = "Assets/_Project/Data/LaunchSites";

    [Serializable]
    class AstronautFile { public AstronautRecord[] astronauts; }
    [Serializable]
    class AstronautRecord
    {
        public string id;
        public string astronautName;
        public string bio;
        public int research;
        public int piloting;
        public int engineering;
    }

    [Serializable]
    class EventsFileRoot { public EventRecord[] events; }
    [Serializable]
    class EventRecord
    {
        public string id;
        public string eventName;
        public string description;
        public float probability;
        public bool isGood;
        public float moneyChange; // Fictional game credits, not USD.
        public int scoreChange;
        public string phase;
        public bool weatherSensitive;
        public string minigameRole;
        public string lesson;
    }

    [Serializable]
    class SitesFileRoot { public SiteRecord[] sites; }
    [Serializable]
    class SiteRecord
    {
        public string id;
        public string siteName;
        public string description;
        public float latitude;
        public float longitude;
        public bool isSelectable;
        public float[] weatherRiskByDay;
        // The JSON includes wind and cloud data, but LaunchSiteData has no matching fields.
        // This metadata is NOT imported to avoid changing P3's public data contract.
    }

    [MenuItem("SpaceApps/Import P5 data (astronauts, events, weather)")]
    public static void Import()
    {
        AstronautFile a = Read<AstronautFile>(AstronautsFile);
        EventsFileRoot e = Read<EventsFileRoot>(EventsFile);
        SitesFileRoot s = Read<SitesFileRoot>(SitesFile);
        if (a == null || e == null || s == null) return;

        List<string> errors = Validate(a, e, s);
        if (errors.Count > 0)
        {
            foreach (string error in errors) Debug.LogError("P5 data: " + error);
            Debug.LogError("P5: import CANCELLED; no assets were created or updated.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(SitesFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "LaunchSites");

        foreach (AstronautRecord source in a.astronauts)
        {
            string path = AstronautsFolder + "/" + source.id + ".asset";
            AstronautData data = Upsert<AstronautData>(path);
            data.astronautName = source.astronautName;
            data.bio = source.bio;
            data.research = source.research;
            data.piloting = source.piloting;
            data.engineering = source.engineering;
            // Leave portrait unchanged; the art team manages it.
            EditorUtility.SetDirty(data);
        }

        foreach (EventRecord source in e.events)
        {
            string path = EventsFolder + "/" + source.id + ".asset";
            EventData data = Upsert<EventData>(path);
            data.eventName = source.eventName;
            data.description = source.description;
            data.probability = source.probability;
            data.isGood = source.isGood;
            data.moneyChange = source.moneyChange;
            data.scoreChange = source.scoreChange;
            EventPhase phase;
            TrainingFocus role;
            Enum.TryParse(source.phase, out phase); // validated earlier
            Enum.TryParse(source.minigameRole, out role);
            data.phase = phase;
            data.weatherSensitive = source.weatherSensitive;
            data.minigameRole = role;
            data.lesson = source.lesson;
            EditorUtility.SetDirty(data);
        }

        int selectable = 0;
        foreach (SiteRecord source in s.sites)
        {
            if (!source.isSelectable) continue; // Reference-only sites are not included in the playable globe.
            string path = SitesFolder + "/" + source.id + ".asset";
            LaunchSiteData data = Upsert<LaunchSiteData>(path);
            data.siteName = source.siteName;
            data.description = source.description;
            data.latitude = source.latitude;
            data.longitude = source.longitude;
            data.weatherRiskByDay = (float[])source.weatherRiskByDay.Clone();
            EditorUtility.SetDirty(data);
            selectable++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("P5: imported " + a.astronauts.Length + " astronauts, "
            + e.events.Length + " events and " + selectable + " site(s)."
            + " Rocket parts and mission data were not changed.");
    }

    static T Read<T>(string path) where T : class
    {
        TextAsset file = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (file == null)
        {
            Debug.LogError("P5: missing JSON file " + path);
            return null;
        }
        try { return JsonUtility.FromJson<T>(file.text); }
        catch (Exception ex)
        {
            Debug.LogError("P5: invalid JSON in " + path + ": " + ex.Message);
            return null;
        }
    }

    static T Upsert<T>(string path) where T : ScriptableObject
    {
        T data = AssetDatabase.LoadAssetAtPath<T>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(data, path);
        }
        return data;
    }

    static bool SafeId(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        foreach (char c in id)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return false;
        }
        return true;
    }

    static List<string> Validate(AstronautFile a, EventsFileRoot e, SitesFileRoot s)
    {
        List<string> errors = new List<string>();
        if (a.astronauts == null || a.astronauts.Length < 4) errors.Add("At least four astronaut candidates are required.");
        if (e.events == null || e.events.Length == 0) errors.Add("No events were provided.");
        if (s.sites == null || s.sites.Length == 0) errors.Add("No launch sites were provided.");
        if (errors.Count > 0) return errors;

        HashSet<string> ids = new HashSet<string>();
        foreach (AstronautRecord r in a.astronauts)
        {
            if (r == null || !SafeId(r.id) || !ids.Add(r.id)) { errors.Add("Astronaut ID is missing, duplicated, or invalid."); continue; }
            if (string.IsNullOrEmpty(r.astronautName) || string.IsNullOrEmpty(r.bio)) errors.Add(r.id + ": missing name or biography.");
            if (!SkillOk(r.research) || !SkillOk(r.piloting) || !SkillOk(r.engineering)) errors.Add(r.id + ": skill scores must be between 0 and 10.");
        }

        ids.Clear();
        foreach (EventRecord r in e.events)
        {
            if (r == null || !SafeId(r.id) || !ids.Add(r.id)) { errors.Add("Event ID is missing, duplicated, or invalid."); continue; }
            EventPhase phase;
            TrainingFocus role;
            if (!Enum.TryParse(r.phase, out phase) || !Enum.IsDefined(typeof(EventPhase), phase)) errors.Add(r.id + ": unknown event phase.");
            if (!Enum.TryParse(r.minigameRole, out role) || !Enum.IsDefined(typeof(TrainingFocus), role)) errors.Add(r.id + ": unknown minigame role.");
            if (float.IsNaN(r.probability) || r.probability < 0f || r.probability > 1f) errors.Add(r.id + ": probability must be between 0 and 1.");
            if (float.IsNaN(r.moneyChange) || float.IsInfinity(r.moneyChange) || Mathf.Abs(r.moneyChange) > 300f) errors.Add(r.id + ": fictional credit change exceeds the allowed range.");
            if (r.scoreChange < -3 || r.scoreChange > 3) errors.Add(r.id + ": scoreChange must be between -3 and 3.");
            if (string.IsNullOrEmpty(r.eventName) || string.IsNullOrEmpty(r.lesson)) errors.Add(r.id + ": missing event name or educational lesson.");
        }

        ids.Clear();
        int selectableCount = 0;
        foreach (SiteRecord r in s.sites)
        {
            if (r == null || !SafeId(r.id) || !ids.Add(r.id)) { errors.Add("Launch-site ID is missing, duplicated, or invalid."); continue; }
            if (float.IsNaN(r.latitude) || float.IsNaN(r.longitude) || Mathf.Abs(r.latitude) > 90f || Mathf.Abs(r.longitude) > 180f) errors.Add(r.id + ": invalid geographic coordinates.");
            if (r.weatherRiskByDay == null || r.weatherRiskByDay.Length != 14) errors.Add(r.id + ": exactly 14 daily weather-risk values are required.");
            else foreach (float risk in r.weatherRiskByDay)
                if (float.IsNaN(risk) || risk < 0f || risk > 1f) errors.Add(r.id + ": daily weather risk must be between 0 and 1.");
            if (r.isSelectable) selectableCount++;
        }
        if (selectableCount == 0) errors.Add("At least one playable launch site is required.");
        return errors;
    }

    static bool SkillOk(int value) { return value >= 0 && value <= 10; }
}
