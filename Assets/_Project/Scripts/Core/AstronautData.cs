using UnityEngine;

// En que se enfoca el entrenamiento de un astronauta (se elige en el folder).
public enum TrainingFocus { None, Research, Piloting, Engineering }

// Candidato a astronauta. Las habilidades son ocultas: el jugador no las ve directamente.
// No modificar estos valores en tiempo de juego (los cambios a un ScriptableObject se guardan en el editor).
// El entrenamiento elegido se guarda en GameManager, no aqui.
[CreateAssetMenu(menuName = "SpaceApps/Astronauta", fileName = "NuevoAstronauta")]
public class AstronautData : ScriptableObject
{
    public string astronautName;
    public Sprite portrait;
    [TextArea] public string bio;

    [Header("Habilidades ocultas (0 a 10)")]
    [Range(0, 10)] public int research;
    [Range(0, 10)] public int piloting;
    [Range(0, 10)] public int engineering;
}
