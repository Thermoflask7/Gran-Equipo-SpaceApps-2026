using UnityEngine;

// Menu principal (00_Menu). Va en el Canvas. Los botones llaman a estos metodos.
// Se arma solo con el menu de Unity: SpaceApps > Armar menu principal.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Paneles que se abren encima del menu (Ajustes, Space Apps). Empiezan ocultos.")]
    public GameObject[] panels;

    void Start()
    {
        GameSettings.ApplySaved();
        HidePanels();
    }

    // Iniciar juego: pasa a la etapa de planeacion
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

    // Salir: en el editor detiene el Play. En WebGL el navegador no deja cerrar la pestana,
    // asi que ahi no hace nada (Application.Quit se ignora).
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}