using UnityEngine;

// La mision (por ahora: orbita lunar tripulada estilo Artemis, mision V13 de Cristina).
// Define lo que la nave necesita para poder lanzarse.
[CreateAssetMenu(menuName = "SpaceApps/Mision", fileName = "NuevaMision")]
public class DestinationData : ScriptableObject
{
    public string destinationName;
    [TextArea] public string description;
    public float launchDeltaV;   // ya no se usa: el SLS es un servicio completo (se deja para no romper assets)
    public float requiredDeltaV; // m/s que debe dar la nave despues de la translunar: llegada a orbita lunar + regreso + reserva
    public float maxMass;        // ya no se usa (el limite es la capacidad del SLS elegido)
    public int missionDays;
    public bool locked; // true = aparece como "proximamente"

    [Header("Masa en la translunar (modelo AM-19)")]
    public float tliExtraMass = 682f; // kg: masa en la translunar (26,536) menos masa despues (25,854)

    [Header("Tripulacion y consumibles")]
    public int crewCount = 4;
    public float waterPerPersonDay = 2.5f;  // kg
    public float oxygenPerPersonDay = 0.84f;
    public float foodPerPersonDay = 0.75f;
    public float baseWaterKg = 135f;        // ya incluidos en la masa de Orion
    public float baseOxygenKg = 47f;
    public float baseFoodKg = 44f;

    [Header("Ciencia")]
    public float minSciencePoints = 40f;
    public float maxDownlinkSeconds = 3600f; // bajar todos los datos en maximo 1 hora
}
