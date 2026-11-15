using UnityEditor;
using UnityEngine;

/// <summary>Prints the generated 7-day weather of the 3 launch sites to the Console.</summary>
public static class LaunchWeatherDebugMenu
{
    [MenuItem("SpaceApps/Debug/Print launch weather")]
    static void Print()
    {
        foreach (string id in new[] { "KSC", "CCSFS", "VSFB" })
            Debug.Log("[LaunchWeather] " + LaunchWeatherModel.Describe(id));
    }

    [MenuItem("SpaceApps/Debug/Reroll launch weather")]
    static void Reroll()
    {
        LaunchWeatherModel.Reseed();
        Print();
    }
}
