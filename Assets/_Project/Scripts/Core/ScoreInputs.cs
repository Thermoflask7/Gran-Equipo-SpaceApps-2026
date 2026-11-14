// Entradas del puntaje (V16 de Cristina adaptado a la fisica Artemis: el SLS es un servicio completo).
// En el juego se llenan con MissionScoring.InputsFromGame(); en las pruebas tambien.
public class ScoreInputs
{
    // Ingenieria (todo sale de RocketPhysics)
    public float deliveryCapacity;    // kg que el SLS elegido lleva a la translunar (payloadCapacity)
    public float tliMass;             // kg, RocketPhysics.TliMass
    public float craftDvAvailable;    // m/s, RocketPhysics.SpacecraftDeltaV
    public float craftDvRequired;     // m/s, mission.requiredDeltaV
    public float powerNetW;           // W, RocketPhysics.PowerBalance (generacion - consumo de la nave)
    public float loadW;               // W, RocketPhysics.PowerConsumption
    public float batteryWh;           // RocketPhysics.BatteryWh
    public float outageHours = RocketPhysics.OutageHours;
    public float waterMargin;         // kg que sobran (RocketPhysics.WaterMargin); negativo = falta
    public float waterRequired;       // kg que consume la tripulacion en toda la mision
    public float oxygenMargin;
    public float oxygenRequired;
    public float foodMargin;
    public float foodRequired;

    // Presupuesto, tripulacion y clima
    public float startingMoney;
    public float moneyAtLaunch;       // dinero al momento de lanzar (los eventos del vuelo no lo cambian)
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
