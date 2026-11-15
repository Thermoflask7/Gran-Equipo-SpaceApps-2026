using UnityEditor;
using UnityEngine;

// Play mode test menu for the desk tutorials (DeskTutorials / TutorialPopup). Signed: Claude.
// Nothing to install: DeskTutorials creates itself at runtime in any scene with a DeskCamera.
public static class TutorialsMenu
{
    [MenuItem("SpaceApps/Tutorials/Test: reset tutorials")]
    static void ResetTutorials()
    {
        GameManager gm = GameManager.Instance;
        gm.welcomeSeen = false;
        gm.tutorialsSeen.Clear();
        TutorialPopup.Close();
        if (Object.FindAnyObjectByType<DeskTutorials>() != null) DeskTutorials.ShowWelcome();
        Debug.Log("Tutorials reset: the welcome message and every station tutorial will show again.");
    }

    [MenuItem("SpaceApps/Tutorials/Test: reset tutorials", true)]
    static bool ResetTutorialsValidate()
    {
        return Application.isPlaying;
    }
}
