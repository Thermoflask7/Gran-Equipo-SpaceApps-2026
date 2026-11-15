// What happened during the flight (02_Lanzamiento). Filled in by the phases and the minigames (P4)
// and read by the Results screen to calculate Cristina's V16 score.
// Lives in GameManager.flight and is reset with ResetGame().
//
// Use from the flight:
//   GameManager.Instance.flight.departureDone = true;       // when the departure ends
//   GameManager.Instance.flight.pilotingResult = 0.8f;      // RAW minigame result (0 to 1),
//                                                           // NOT multiplied by SkillMultiplier
//   GameManager.Instance.flight.flightFinished = true;      // when the flight ends (well or badly)
[System.Serializable]
public class MissionResults
{
    public bool launched;        // the launch button was pressed with CanLaunch() true
    public float moneyAtLaunch;  // money at launch (set by LaunchButton). Savings are graded with this
    public bool flightFinished;  // the flight has ended (successfully or not). While false the result is "pending"

    // Flight milestones (in order)
    public bool departureDone;   // successful departure
    public bool lunarOrbitDone;  // reached lunar orbit
    public bool safeReturnDone;  // safe return with the crew

    // Raw result of each minigame, from 0 to 1. -1 = not played (counts as 0)
    public float pilotingResult = -1f;
    public float repairResult = -1f;
    public float researchResult = -1f;

    // Flight science, from 0 to 1. -1 = not done (counts as 0)
    public float experimentFraction = -1f; // experiments done / experiments planned
    public float downlinkFraction = -1f;   // data sent to Earth / data planned (cannot be greater than experimentFraction)

    public void Reset()
    {
        launched = false;
        moneyAtLaunch = 0f;
        flightFinished = false;
        departureDone = false;
        lunarOrbitDone = false;
        safeReturnDone = false;
        pilotingResult = -1f;
        repairResult = -1f;
        researchResult = -1f;
        experimentFraction = -1f;
        downlinkFraction = -1f;
    }
}
