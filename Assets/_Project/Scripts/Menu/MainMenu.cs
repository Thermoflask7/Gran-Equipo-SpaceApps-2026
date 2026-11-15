using UnityEngine;

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