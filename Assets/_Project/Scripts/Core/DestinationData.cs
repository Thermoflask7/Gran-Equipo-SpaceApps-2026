using UnityEngine;

// La mision (por ahora: orbita lunar tripulada estilo Artemis).
// Define lo que el cohete necesita para poder lanzarse.
[CreateAssetMenu(menuName = "SpaceApps/Mision", fileName = "NuevaMision")]
public class DestinationData : ScriptableObject
{
    public string destinationName;
    [TextArea] public string description;
    public float launchDeltaV;   // m/s que debe dar el cohete: despegue a orbita baja + inyeccion translunar
    public float requiredDeltaV; // m/s que debe dar la nave despues de la translunar: llegada a orbita lunar + regreso + reserva
    public float maxMass;        // kg, masa maxima al despegue
    public int missionDays;
    public bool locked; // true = aparece como "proximamente"
}
