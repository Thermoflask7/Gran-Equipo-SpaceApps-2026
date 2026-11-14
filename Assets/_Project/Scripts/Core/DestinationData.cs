using UnityEngine;

// Destino de la mision (por ahora: orbita lunar).
[CreateAssetMenu(menuName = "SpaceApps/Destino", fileName = "NuevoDestino")]
public class DestinationData : ScriptableObject
{
    public string destinationName;
    [TextArea] public string description;
    public float requiredDeltaV; // m/s totales desde la superficie de la Tierra
    public int missionDays;
    public bool locked; // true = aparece como "proximamente"
}
