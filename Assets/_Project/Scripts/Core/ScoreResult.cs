using System.Collections.Generic;

// Desglose del puntaje. Lo regresa MissionScoring.Evaluate().
public class ScoreResult
{
    // Planeacion (45)
    public float deliveryPoints;    // 7: margen de capacidad del SLS
    public float craftDvPoints;     // 7: margen de delta-v de Orion
    public float powerPoints;       // 3: margen de generacion electrica
    public float batteryPoints;     // 3: autonomia de baterias sin sol
    public float consumablePoints;  // 4: reservas de agua, oxigeno y comida
    public float engineering;       // 24
    public float budgetPoints;      // 7
    public float trainingPoints;    // 8
    public float weatherPoints;     // 6
    public float planningBase;      // 45
    public float planningEvents;    // -3 a 3
    public float planning;          // 0 a 45 (final)

    // Mision (55)
    public float milestonePoints;   // 24
    public float minigamePoints;    // 21
    public float sciencePoints;     // 10
    public float missionBase;       // 55
    public float missionEvents;     // -3 a 3
    public float mission;           // 0 a 55 (final)

    public float total;             // 0 a 100, puntaje de diagnostico (para aprender): se muestra aunque la mision falle
    public float competitive;       // puntaje oficial: = total solo si status == "SUCCESS", si no 0

    public string status = "PENDING";  // SUCCESS, FAILURE, PENDING o INVALID
    public List<string> failedGates = new List<string>();
    public string problem = "";        // si las entradas no son validas, el motivo (y todo queda en 0)
}
