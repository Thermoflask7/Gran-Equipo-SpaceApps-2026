using TMPro;
using UnityEngine;

// Lista de las 4 tareas que desbloquean el boton de lanzamiento (ver GDD.md).
// Ponlo en un texto TMP: se actualiza solo y dice que falta y por que.
[RequireComponent(typeof(TMP_Text))]
public class LaunchChecklist : MonoBehaviour
{
    const string Done = "<color=#6FCF6F>[listo]</color> ";
    const string Missing = "<color=#FF8A65>[falta]</color> ";

    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void Update()
    {
        label.text = BuildText(GameManager.Instance);
    }

    // Cuantas de las 4 tareas ya estan completas
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

        // 1. Mision leida
        if (gm.missionRead) text += Done + "Misión leída\n";
        else text += Missing + "Lee la misión en el folder\n";

        // 2. Tripulacion asignada y entrenada
        if (gm.IsCrewReady()) text += Done + "Tripulación asignada y entrenada\n";
        else if (gm.pilot == null || gm.engineer == null || gm.scientist == null)
            text += Missing + "Asigna piloto, ingeniero y científico en el folder\n";
        else text += Missing + "Elige el entrenamiento de cada astronauta\n";

        // 3. Cohete armado y valido
        if (gm.rocketValid) text += Done + "Cohete armado y válido\n";
        else if (gm.rocketParts.Count == 0) text += Missing + "Compra piezas en SpaceMart y arma el cohete\n";
        else
        {
            string reason = RocketPhysics.ValidationMessage(gm.rocketParts, gm.mission);
            if (reason == "") reason = "Confirma el cohete en el modelo";
            text += Missing + reason + "\n";
        }

        // 4. Sitio y fecha de lanzamiento
        if (gm.IsLaunchPlanned())
            text += Done + "Lanzamiento: " + gm.launchSite.siteName + ", día " + (gm.launchDay + 1)
                + " (riesgo de clima " + (gm.weatherRisk * 100f).ToString("0") + "%)";
        else if (gm.launchSite == null) text += Missing + "Elige el sitio de lanzamiento en el globo";
        else text += Missing + "Elige la fecha en el calendario";

        return text;
    }
}
