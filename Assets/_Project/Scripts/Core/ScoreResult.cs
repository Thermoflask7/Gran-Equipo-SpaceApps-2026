using System.Collections.Generic;

// Score breakdown. Returned by MissionScoring.Evaluate().
public class ScoreResult
{
    // Planning (45)
    public float deliveryPoints;    // 7: launcher capacity margin
    public float craftDvPoints;     // 7: spacecraft delta-v margin
    public float powerPoints;       // 3: power generation margin
    public float batteryPoints;     // 3: battery autonomy without sunlight
    public float consumablePoints;  // 4: water, oxygen and food reserves
    public float engineering;       // 24
    public float budgetPoints;      // 7
    public float trainingPoints;    // 8
    public float weatherPoints;     // 6
    public float planningBase;      // 45
    public float planningEvents;    // -3 to 3
    public float planning;          // 0 to 45 (final)

    // Mission (55)
    public float milestonePoints;   // 24
    public float minigamePoints;    // 21
    public float sciencePoints;     // 10
    public float missionBase;       // 55
    public float missionEvents;     // -3 to 3
    public float mission;           // 0 to 55 (final)

    public float total;             // 0 to 100, diagnostic score (for learning): shown even if the mission fails
    public float competitive;       // official score: = total only if status == "SUCCESS", otherwise 0

    public string status = "PENDING";  // SUCCESS, FAILURE, PENDING or INVALID
    public List<string> failedGates = new List<string>();
    public string problem = "";        // if the inputs are not valid, the reason (and everything stays at 0)

    // Task 31: an abort maps to status FAILURE, but keeps its own flag and reason
    public bool aborted;               // the mission ended early (gate MISSION_ABORT)
    public string failureReason = "";  // MissionResults.failureReason ("" = no in-flight failure)
}
