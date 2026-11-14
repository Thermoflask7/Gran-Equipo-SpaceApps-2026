using UnityEngine;

// La mision (por ahora: orbita lunar tripulada estilo Artemis).
// Define lo que el cohete necesita para poder lanzarse.
[CreateAssetMenu(menuName = "SpaceApps/Mision", fileName = "NuevaMision")]
public class DestinationData : ScriptableObject
{
    public string destinationName;
    [TextArea] public string description;
    public float requiredDeltaV; // m/s totales: orbita baja + inyeccion translunar + insercion lunar + regreso
    public float maxMass;        // kg, masa maxima al despegue
    public int missionDays;
    public bool locked; // true = aparece como "proximamente"
}
