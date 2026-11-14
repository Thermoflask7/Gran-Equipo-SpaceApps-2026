using UnityEngine;

// Candidato a astronauta. Las habilidades son ocultas: el jugador no las ve directamente.
// No modificar estos valores en tiempo de juego (los cambios a un ScriptableObject se guardan en el editor).
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
