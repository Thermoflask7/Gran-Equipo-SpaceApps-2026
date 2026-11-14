using System.Collections.Generic;

// Desglose del puntaje V16. Lo regresa MissionScoring.Evaluate().
public class ScoreResult
{
    // Planeacion (45)
    public float launchDvPoints;   // 6
    public float craftDvPoints;    // 7
    public float massPoints;       // 4
    public float twrPoints;        // 3
    public float energyPoints;     // 4
    public float engineering;      // 24
    public float budgetPoints;     // 7
    public float trainingPoints;   // 8
    public float weatherPoints;    // 6
    public float planningBase;     // 45
    public float planningEvents;   // -3 a 3
    public float planning;         // 0 a 45 (final)

    // Mision (55)
    public float milestonePoints;  // 24
    public float minigamePoints;   // 21
    public float sciencePoints;    // 10
    public float missionBase;      // 55
    public float missionEvents;    // -3 a 3
    public float mission;          // 0 a 55 (final)

    public float total;            // 0 a 100, "diagnostico": se muestra aunque la mision falle
    public float competitive;      // = total solo si la mision fue exitosa, si no 0

    public string status = "PENDING";  // SUCCESS, FAILURE o PENDING
    public List<string> failedGates = new List<string>();
    public string problem = "";        // si las entradas no son validas, el motivo (y todo queda en 0)
}
