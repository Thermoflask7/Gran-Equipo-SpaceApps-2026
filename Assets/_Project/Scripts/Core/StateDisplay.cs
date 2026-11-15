using TMPro;
using UnityEngine;

// Debug text for the greybox: shows the game state.
[RequireComponent(typeof(TMP_Text))]
public class StateDisplay : MonoBehaviour
{
    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        label.text =
            "Credits: " + gm.money.ToString("N0") + "   Parts bought: " + gm.boughtParts.Count + "\n" +
            "Mission read: " + YesNo(gm.missionRead) + "   Crew ready: " + YesNo(gm.IsCrewReady()) + "\n" +
            "Rocket valid: " + YesNo(gm.rocketValid) + "   Launch planned: " + YesNo(gm.IsLaunchPlanned()) + "\n" +
            "Can launch?: " + YesNo(gm.CanLaunch()) + "   Score: " + gm.TotalScore().ToString("0");
    }

    string YesNo(bool value)
    {
        return value ? "yes" : "no";
    }
}