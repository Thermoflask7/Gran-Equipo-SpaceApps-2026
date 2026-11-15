using UnityEngine;

// What an astronaut's training focuses on (chosen in the folder).
public enum TrainingFocus { None, Research, Piloting, Engineering }

// Personality shown in the folder. Effects in CrewTraining (Scripts/Tripulacion).
public enum CrewTrait { None, Veteran, HardWorker, TeamPlayer, Loner, Rookie }

// Astronaut candidate. The skills are hidden: the player does not see them directly.
// Do not modify these values at runtime (changes to a ScriptableObject are saved in the editor).
// The chosen training is stored in GameManager, not here.
[CreateAssetMenu(menuName = "SpaceApps/Astronaut", fileName = "NewAstronaut")]
public class AstronautData : ScriptableObject
{
    public string astronautName;
    public Sprite portrait;
    [TextArea] public string bio;

    [Header("Hidden skills (0 to 10)")]
    [Range(0, 10)] public int research;
    [Range(0, 10)] public int piloting;
    [Range(0, 10)] public int engineering;

    [Header("Trait (visible, set by SpaceApps > Mission folder > Assign astronaut traits)")]
    public CrewTrait trait;
}
