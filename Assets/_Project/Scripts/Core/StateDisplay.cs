using UnityEngine;
using UnityEngine.UI;

// Texto de depuracion para el greybox: muestra el estado de la partida.
[RequireComponent(typeof(Text))]
public class StateDisplay : MonoBehaviour
{
    Text label;

    void Awake()
    {
        label = GetComponent<Text>();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        label.text =
            "Dinero: $" + gm.money.ToString("0") + " M   Piezas compradas: " + gm.boughtParts.Count + "\n" +
            "Misión leída: " + SiNo(gm.missionRead) + "   Tripulación lista: " + SiNo(gm.IsCrewReady()) + "\n" +
            "Cohete válido: " + SiNo(gm.rocketValid) + "   Lanzamiento planeado: " + SiNo(gm.IsLaunchPlanned()) + "\n" +
            "¿Se puede lanzar?: " + SiNo(gm.CanLaunch()) + "   Puntaje: " + gm.TotalScore().ToString("0");
    }

    string SiNo(bool value)
    {
        return value ? "sí" : "no";
    }
}
