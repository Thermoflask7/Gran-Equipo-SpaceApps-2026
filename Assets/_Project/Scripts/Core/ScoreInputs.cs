// Score inputs (Cristina's V16 adapted to the Artemis physics: the SLS is a complete service).
// In the game they are filled in with MissionScoring.InputsFromGame(); in the tests too.
public class ScoreInputs
{
    // Engineering (all of it comes from RocketPhysics)
    public float deliveryCapacity;    // kg the chosen SLS carries to translunar injection (payloadCapacity)
    public float tliMass;             // kg, RocketPhysics.TliMass
    public float craftDvAvailable;    // m/s, RocketPhysics.SpacecraftDeltaV
    public float craftDvRequired;     // m/s, mission.requiredDeltaV
    public float powerNetW;           // W, RocketPhysics.PowerBalance (spacecraft generation - draw)
    public float loadW;               // W, RocketPhysics.PowerConsumption
    public float batteryWh;           // RocketPhysics.BatteryWh
    public float outageHours = RocketPhysics.OutageHours;
    public float waterMargin;         // kg left over (RocketPhysics.WaterMargin); negative = shortfall
    public float waterRequired;       // kg the crew consumes over the whole mission
    public float oxygenMargin;
    public float oxygenRequired;
    public float foodMargin;
    public float foodRequired;

    // Budget, crew and weather
    public float startingMoney;
    public float moneyAtLaunch;       // money at the moment of launch (flight events do not change it)
    public float pilotSkill;          // 0 to 1, GameManager.SkillMultiplier
    public float engineerSkill;
    public float scientistSkill;
    public float weatherRisk;         // 0 to 1

    // Flight (0 or 1)
    public float departureDone;
    public float lunarOrbitDone;
    public float safeReturnDone;

    // Minigames, raw (0 to 1, without SkillMultiplier)
    public float pilotingRaw;
    public float repairRaw;
    public float researchRaw;

    // Science
    public float sciencePoints;          // RocketPhysics.SciencePoints
    public float requiredSciencePoints;  // mission.minSciencePoints
    public float experimentFraction;     // 0 to 1
    public float downlinkFraction;       // 0 to 1

    // Sum of scoreChange of the events that happened
    public float planningEventSum;
    public float missionEventSum;
}
