using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Launch button on the desk. Full launch when GameManager.CanLaunch() is true (the 4 GDD tasks);
// "launch early" (risky, maybe with an invalid rocket or untrained crew) when CanLaunchEarly() is true;
// locked otherwise. Clicking it fires Pressed (the 3D desk button plays its press animation)
// and moves to 02_Lanzamiento after sceneChangeDelay.
[RequireComponent(typeof(Button))]
public class LaunchButton : MonoBehaviour
{
    public TMP_Text label; // button text (optional)
    public string readyText = "LAUNCH!";
    public string earlyText = "LAUNCH EARLY";
    public string lockedText = "Locked";
    [Tooltip("Seconds between the press and the scene change, so the press animation can be seen")]
    public float sceneChangeDelay = 0.8f;
    [Tooltip("Hide the launch panel at start; it only shows when the desk launch button (LaunchStation) is clicked")]
    public bool hideUntilOpened = true;

    // Fired once when the launch is confirmed (before the scene change)
    public static event Action Pressed;

    Button button;
    bool launching;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Launch);

        // The panel must not cover the screen: it only shows when LaunchStation opens it
        LaunchStation station = FindAnyObjectByType<LaunchStation>();
        if (hideUntilOpened && (station == null || !station.IsOpen))
        {
            if (station == null)
                Debug.LogWarning("LaunchButton: no LaunchStation in the scene, so the launch panel stays hidden. Run SpaceApps > Desk > Add launch button station.");
            GameObject panel = transform.parent != null ? transform.parent.gameObject : gameObject;
            panel.SetActive(false);
        }
    }

    void Update()
    {
        if (launching) return;
        GameManager gm = GameManager.Instance;
        bool ready = gm.CanLaunch();
        bool early = !ready && gm.CanLaunchEarly();
        button.interactable = ready || early;

        if (label != null)
        {
            if (ready) label.text = readyText;
            else if (early) label.text = earlyText;
            else label.text = lockedText + " (" + LaunchChecklist.CompletedTasks(gm) + "/4)";
        }
    }

    public void Launch()
    {
        GameManager gm = GameManager.Instance;
        if (launching || !(gm.CanLaunch() || gm.CanLaunchEarly())) return;
        launching = true;
        button.interactable = false;
        if (label != null) label.text = gm.CanLaunch() ? readyText : earlyText;
        GameManager.Instance.flight.launched = true; // Results grades what was missing at launch
        GameManager.Instance.flight.moneyAtLaunch = GameManager.Instance.money; // savings are graded at launch
        // Runs on the GameManager: Pressed hides this panel, and a coroutine on an inactive object stops
        GameManager.Instance.StartCoroutine(ChangeSceneAfterDelay());
        Pressed?.Invoke();
    }

    IEnumerator ChangeSceneAfterDelay()
    {
        if (sceneChangeDelay > 0f) yield return new WaitForSecondsRealtime(sceneChangeDelay);
        GameManager.Instance.GoToScene(Scenes.Launch);
    }
}
