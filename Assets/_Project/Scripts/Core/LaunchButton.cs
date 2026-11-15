using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Launch button on the desk. It stays locked until GameManager.CanLaunch()
// is true (the 4 GDD tasks). Clicking it moves to 02_Lanzamiento.
[RequireComponent(typeof(Button))]
public class LaunchButton : MonoBehaviour
{
    public TMP_Text label; // button text (optional)
    public string readyText = "LAUNCH!";
    public string lockedText = "Locked";

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Launch);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        bool ready = gm.CanLaunch();
        button.interactable = ready;

        if (label != null)
        {
            if (ready) label.text = readyText;
            else label.text = lockedText + " (" + LaunchChecklist.CompletedTasks(gm) + "/4)";
        }
    }

    public void Launch()
    {
        if (!GameManager.Instance.CanLaunch()) return;
        GameManager.Instance.flight.launched = true; // Results checks that it launched with the 4 tasks done
        GameManager.Instance.flight.moneyAtLaunch = GameManager.Instance.money; // savings are graded at launch
        GameManager.Instance.GoToScene(Scenes.Launch);
    }
}
