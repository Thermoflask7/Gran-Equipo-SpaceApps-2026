using UnityEditor;
using UnityEngine;

// Unity menus for the flight phases (task 11). Signed: claude_c.
//   SpaceApps -> Flight phases -> Add to open scene: adds FlightPhases to the LaunchScreen object (02_Lanzamiento).
//   SpaceApps -> Flight phases -> Test: finish minigame (Play only): closes the open minigame with a random result.
//   SpaceApps -> Flight phases -> Tests: edit-mode tests of the static helpers.
public static class FlightPhasesMenu
{
    [MenuItem("SpaceApps/Flight phases/Add to open scene")]
    public static void AddToOpenScene()
    {
        LaunchScreen screen = Object.FindAnyObjectByType<LaunchScreen>();
        if (screen == null)
        {
            Debug.LogError("Flight phases: open 02_Lanzamiento (or build it with SpaceApps -> Armar escena de lanzamiento) first.");
            return;
        }
        FlightPhases phases = screen.GetComponent<FlightPhases>();
        if (phases == null) phases = Undo.AddComponent<FlightPhases>(screen.gameObject);
        phases.screen = screen;
        EditorUtility.SetDirty(phases);
        Selection.activeObject = phases;
        Debug.Log("Flight phases: FlightPhases is on '" + screen.gameObject.name + "'. Save the scene.");
    }

    [MenuItem("SpaceApps/Flight phases/Test: finish minigame")]
    public static void FinishMinigame()
    {
        FlightPhases phases = Object.FindAnyObjectByType<FlightPhases>();
        if (phases == null || phases.CurrentMinigame == TrainingFocus.None)
        {
            Debug.Log("Flight phases: no minigame is open.");
            return;
        }
        float raw = Random.value;
        FlightPhases.SetResult(GameManager.Instance.flight, phases.CurrentMinigame, raw);
        Debug.Log("Flight phases: " + FlightPhases.RoleName(phases.CurrentMinigame) + " minigame finished with " + raw.ToString("0.00") + ".");
        phases.MinigameDone();
    }

    [MenuItem("SpaceApps/Flight phases/Test: finish minigame", true)]
    static bool FinishMinigameValid()
    {
        return Application.isPlaying;
    }
}
