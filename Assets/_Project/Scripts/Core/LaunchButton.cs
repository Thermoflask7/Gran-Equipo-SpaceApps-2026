using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Boton de lanzamiento del escritorio. Esta bloqueado hasta que GameManager.CanLaunch()
// sea true (las 4 tareas del GDD). Al hacer clic pasa a 02_Lanzamiento.
[RequireComponent(typeof(Button))]
public class LaunchButton : MonoBehaviour
{
    public TMP_Text label; // texto del boton (opcional)
    public string readyText = "¡LANZAR!";
    public string lockedText = "Bloqueado";

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
        GameManager.Instance.flight.launched = true; // Resultados revisa que se lanzo con las 4 tareas
        GameManager.Instance.GoToScene(Scenes.Launch);
    }
}
