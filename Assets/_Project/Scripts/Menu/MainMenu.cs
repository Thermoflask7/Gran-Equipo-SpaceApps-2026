using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main menu (00_Menu). Goes on the Canvas. The buttons call these methods.
// It is built automatically with the Unity menu: SpaceApps > Build main menu.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Panels that open on top of the menu (Settings, Space Apps). They start hidden.")]
    public GameObject[] panels;

    void Start()
    {
        GameSettings.ApplySaved();
        HidePanels();
        AddLeaderboardButton();
    }

    // Task 37d: "Leaderboard" button above "Quit game", a runtime copy of the Settings button (the scene is not
    // changed). It opens the saved top 10 (LeaderboardDialog.OpenBoard).
    void AddLeaderboardButton()
    {
        Transform settings = FindChild(transform, "SettingsButton");
        Button button;
        if (settings != null && settings.parent.GetComponent<LayoutGroup>() != null)
        {
            GameObject copy = Instantiate(settings.gameObject, settings.parent);
            copy.name = "LeaderboardButton";
            Transform quit = settings.parent.Find("QuitButton");
            copy.transform.SetSiblingIndex(quit != null ? quit.GetSiblingIndex() : settings.GetSiblingIndex() + 1);
            button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent(); // drops the copied "open Settings" calls
            TMP_Text label = copy.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = "Leaderboard";
        }
        else button = LeaderboardDialog.CreateCornerButton();
        button.onClick.AddListener(LeaderboardDialog.OpenBoard);
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Start game: moves to the planning stage
    public void Play()
    {
        GameManager.Instance.ResetGame();
        GameManager.Instance.GoToScene(Scenes.Planning);
    }

    public void ShowPanel(GameObject panel)
    {
        HidePanels();
        if (panel != null) panel.SetActive(true);
    }

    public void HidePanels()
    {
        if (panels == null) return;
        foreach (GameObject p in panels)
        {
            if (p != null) p.SetActive(false);
        }
    }

    // Quit: in the editor it stops Play mode. In WebGL the browser does not allow closing the tab,
    // so there it does nothing (Application.Quit is ignored).
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}