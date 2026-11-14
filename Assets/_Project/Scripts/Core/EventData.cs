using UnityEngine;

// Evento aleatorio durante la mision (bueno o malo).
[CreateAssetMenu(menuName = "SpaceApps/Evento", fileName = "NuevoEvento")]
public class EventData : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;
    [Range(0f, 1f)] public float probability = 0.3f;
    public bool isGood;
    public float moneyChange; // millones de USD, puede ser negativo
    public int scoreChange;
}
