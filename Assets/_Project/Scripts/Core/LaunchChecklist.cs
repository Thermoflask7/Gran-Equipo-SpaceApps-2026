using TMPro;
using UnityEngine;

// List of the 4 tasks that unlock the launch button (see GDD.md).
// Put it on a TMP text: it updates itself and says what is missing and why.
[RequireComponent(typeof(TMP_Text))]
public class LaunchChecklist : MonoBehaviour
{
    const string Done = "<color=#6FCF6F>[done]</color> ";
    const string Missing = "<color=#FF8A65>[missing]</color> ";

    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void Update()
    {
        label.text = BuildText(GameManager.Instance);
    }

    // How many of the 4 tasks are already complete
    public static int CompletedTasks(GameManager gm)
    {
        int count = 0;
        if (gm.missionRead) count++;
        if (gm.IsCrewReady()) count++;
        if (gm.rocketValid) count++;
        if (gm.IsLaunchPlanned()) count++;
        return count;
    }

    public static string BuildText(GameManager gm)
    {
        string text = "";

        // 1. Mission read
        if (gm.missionRead) text += Done + "Mission read\n";
        else text += Missing + "Read the mission in the folder\n";

        // 2. Crew assigned and trained
        if (gm.IsCrewReady()) text += Done + "Crew assigned and trained\n";
        else if (gm.pilot == null || gm.engineer == null || gm.scientist == null)
            text += Missing + "Assign a pilot, an engineer and a scientist in the folder\n";
        else text += Missing + "Choose each astronaut's training\n";

        // 3. Rocket assembled and valid
        if (gm.rocketValid) text += Done + "Rocket assembled and valid\n";
        else if (gm.rocketParts.Count == 0) text += Missing + "Buy parts in SpaceMart and assemble the rocket\n";
        else
        {
            string reason = RocketPhysics.ValidationMessage(gm.rocketParts, gm.mission);
            if (reason == "") reason = "Confirm the rocket in the model";
            text += Missing + reason + "\n";
        }

        // 4. Launch site and date
        if (gm.IsLaunchPlanned())
            text += Done + "Launch: " + gm.launchSite.siteName + ", day " + (gm.launchDay + 1)
                + " (weather risk " + (gm.weatherRisk * 100f).ToString("0") + "%)";
        else if (gm.launchSite == null) text += Missing + "Choose the launch site on the globe";
        else text += Missing + "Choose the date in the calendar";

        return text;
    }
}
