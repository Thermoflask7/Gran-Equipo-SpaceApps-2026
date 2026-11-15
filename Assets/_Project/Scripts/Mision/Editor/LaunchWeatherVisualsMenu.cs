using UnityEditor;

// Test the launch weather visuals without going through the planning calendar.
// Use it in Play mode: start Play in 02_Lanzamiento and pick the weather during the liftoff.
public static class LaunchWeatherVisualsMenu
{
    const string Root = "SpaceApps/Debug/Launch weather/";

    [MenuItem(Root + "Sunny")] static void Sunny() { Set(WeatherCondition.Sunny); }
    [MenuItem(Root + "Cloudy")] static void Cloudy() { Set(WeatherCondition.Cloudy); }
    [MenuItem(Root + "Rain")] static void Rain() { Set(WeatherCondition.Rain); }
    [MenuItem(Root + "Storm")] static void Storm() { Set(WeatherCondition.Storm); }

    static void Set(WeatherCondition condition)
    {
        if (!EditorApplication.isPlaying)
        {
            UnityEngine.Debug.Log("Enter Play mode in 02_Lanzamiento first, then pick the weather.");
            return;
        }
        GameManager.Instance.launchWeather = condition;
        if (LaunchWeatherVisuals.Instance != null) LaunchWeatherVisuals.Instance.Refresh();
        UnityEngine.Debug.Log("Launch weather set to " + condition + ".");
    }
}
