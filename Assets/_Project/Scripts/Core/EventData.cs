using UnityEngine;

// Evento aleatorio de la planeacion o del vuelo (bueno o malo).
// Lo tira RandomEvents (Scripts/Mision).
[CreateAssetMenu(menuName = "SpaceApps/Evento", fileName = "NuevoEvento")]
public class EventData : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;
    [Range(0f, 1f)] public float probability = 0.3f; // probabilidad base
    public bool isGood;
    public float moneyChange; // millones de USD, puede ser negativo
    public int scoreChange;

    [Header("Cuando y como pasa")]
    public EventPhase phase = EventPhase.Departure;
    public bool weatherSensitive; // si es true, el clima del dia elegido cambia su probabilidad
    public TrainingFocus minigameRole = TrainingFocus.None; // minijuego que dispara (None = ninguno)
    [TextArea] public string lesson; // explicacion educativa para Resultados (opcional)
}
