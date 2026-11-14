// Entradas del puntaje V16 de Cristina (Drive: CRISTINA/PUNTUACION/SpaceMission_Puntuacion_V16.json).
// Los nombres siguen su "input_contract" para poder comparar con sus pruebas.
// En el juego se llenan con MissionScoring.InputsFromGame(); en las pruebas, a mano.
public class ScoreInputs
{
    // Ingenieria
    public float launchDvAvailable;   // m/s (con el SLS se llena con MissionScoring.InputsFromGame)
    public float launchDvRequired;
    public float craftDvAvailable;    // m/s, RocketPhysics.SpacecraftDeltaV
    public float craftDvRequired;     // m/s, mission.requiredDeltaV
    public float massLimit;           // kg, capacidad del SLS a la translunar
    public float massForLimit;        // kg, RocketPhysics.TliMass
    public float liftoffTwr;
    public float powerNetW;           // W, generacion - consumo de la nave
    public float loadW;               // W, consumo de la nave
    public float batteryWh;
    public float outageHours = RocketPhysics.OutageHours;

    // Presupuesto, tripulacion y clima
    public float startingMoney;
    public float moneyRemaining;
    public float pilotSkill;          // 0 a 1, GameManager.SkillMultiplier
    public float engineerSkill;
    public float scientistSkill;
    public float weatherRisk;         // 0 a 1

    // Vuelo (0 o 1)
    public float departureDone;
    public float lunarOrbitDone;
    public float safeReturnDone;

    // Minijuegos, crudos (0 a 1, sin SkillMultiplier)
    public float pilotingRaw;
    public float repairRaw;
    public float researchRaw;

    // Ciencia
    public float sciencePoints;          // RocketPhysics.SciencePoints
    public float requiredSciencePoints;  // mission.minSciencePoints
    public float experimentFraction;     // 0 a 1
    public float downlinkFraction;       // 0 a 1

    // Suma de scoreChange de los eventos que pasaron
    public float planningEventSum;
    public float missionEventSum;
}
