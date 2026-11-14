// Lo que paso en el vuelo (02_Lanzamiento). Lo llenan las fases y los minijuegos (P4)
// y lo lee la pantalla de Resultados para calcular el puntaje V16 de Cristina.
// Vive en GameManager.flight y se reinicia con ResetGame().
//
// Uso desde el vuelo:
//   GameManager.Instance.flight.departureDone = true;       // al terminar la salida
//   GameManager.Instance.flight.pilotingResult = 0.8f;      // resultado CRUDO del minijuego (0 a 1),
//                                                           // SIN multiplicar por SkillMultiplier
//   GameManager.Instance.flight.flightFinished = true;      // al acabar el vuelo (bien o mal)
[System.Serializable]
public class MissionResults
{
    public bool launched;        // el boton de lanzamiento se presiono con CanLaunch() en true
    public float moneyAtLaunch;  // dinero al lanzar (lo pone LaunchButton). El ahorro se califica con esto
    public bool flightFinished;  // el vuelo ya termino (con exito o no). Mientras sea false el resultado es "pendiente"

    // Hitos del vuelo (en orden)
    public bool departureDone;   // salida exitosa
    public bool lunarOrbitDone;  // llego a orbita lunar
    public bool safeReturnDone;  // regreso seguro con la tripulacion

    // Resultado crudo de cada minijuego, de 0 a 1. -1 = no se jugo (cuenta como 0)
    public float pilotingResult = -1f;
    public float repairResult = -1f;
    public float researchResult = -1f;

    // Ciencia del vuelo, de 0 a 1. -1 = no se hizo (cuenta como 0)
    public float experimentFraction = -1f; // experimentos hechos / experimentos planeados
    public float downlinkFraction = -1f;   // datos mandados a la Tierra / datos planeados (no puede ser mayor que experimentFraction)

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
