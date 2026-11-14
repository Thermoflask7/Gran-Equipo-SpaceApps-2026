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
        string dest = gm.destination != null ? gm.destination.destinationName : "-";
        label.text =
            "Dinero: $" + gm.money.ToString("0") + " M\n" +
            "Año: " + gm.year + "   Puntaje: " + gm.score + "\n" +
            "Piezas: " + gm.boughtParts.Count + "   Masa: " + (gm.TotalDryMass() + gm.TotalFuelMass()).ToString("0") + " kg\n" +
            "Tripulación completa: " + (gm.IsCrewComplete() ? "sí" : "no") + "   Destino: " + dest;
    }
}
