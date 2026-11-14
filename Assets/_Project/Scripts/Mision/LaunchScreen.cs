using System;
using TMPro;
using UnityEngine;

// Escena 02_Lanzamiento (tarea 10): pantalla dividida.
//   Izquierda (2D): control de mision con operadores y una pantalla grande con la fase y los datos del vuelo.
//   Derecha (3D): el cohete recorre la ruta Tierra -> Luna -> Tierra.
// Se arma con el menu de Unity: SpaceApps > Armar escena de lanzamiento.
//
// Para la tarea 11 (fases del vuelo) y los minijuegos:
//   - CurrentPhase / PhaseProgress dicen en que fase va el vuelo (EventPhase.Departure, LunarOrbit, Return).
//   - PhaseStarted se dispara al empezar cada fase y MissionFinished al terminar el vuelo.
//   - paused = true detiene el vuelo (por ejemplo mientras se juega un minijuego).
//   - autoAdvance = false hace que la fase no avance sola al llegar al 100%: llama NextPhase() cuando toque.
//   - Log("texto") escribe en la bitacora de control de mision.
// Los datos de vuelo (distancia, velocidad, combustible) son de prueba hasta que exista la fisica (tarea 17).
public class LaunchScreen : MonoBehaviour
{
    [Header("Vista 3D")]
    public Transform rocket;
    public Transform earth;
    public Transform moon;
    public LineRenderer pathLine;
    [Tooltip("Distancia de la orbita lunar sobre la superficie de la Luna (unidades de la escena)")]
    public float orbitGap = 0.8f;

    [Header("Control de mision (UI)")]
    public TMP_Text phaseLabel;
    public TMP_Text dataLabel;
    public TMP_Text logLabel;
    public RectTransform progressFill;
    public GameObject finishPanel;

    [Header("Duracion de cada fase (segundos)")]
    public float departureSeconds = 20f;
    public float lunarOrbitSeconds = 15f;
    public float returnSeconds = 12f;

    [Header("Control del vuelo")]
    [Tooltip("Si esta apagado, la fase se queda en 100% hasta que alguien llame NextPhase()")]
    public bool autoAdvance = true;
    [Tooltip("Detiene el vuelo (por ejemplo durante un minijuego)")]
    public bool paused;

    public EventPhase CurrentPhase { get; private set; } = EventPhase.Departure;
    public float PhaseProgress { get; private set; } // 0 a 1 dentro de la fase actual
    public bool Finished { get; private set; }

    public event Action<EventPhase> PhaseStarted;
    public event Action MissionFinished;

    const float MissionDays = 10f;      // duracion aproximada de Artemis II
    const float EarthMoonKm = 384400f;  // distancia media Tierra-Luna
    const int MaxLogLines = 6;

    float flightSeconds;
    string log = "";

    void Start()
    {
        if (finishPanel != null) finishPanel.SetActive(false);
        DrawPath();
        StartPhase(EventPhase.Departure);
        UpdateRocket();
        UpdateHud();
    }

    void Update()
    {
        if (earth != null) earth.Rotate(0f, 12f * Time.deltaTime, 0f, Space.World);
        if (moon != null) moon.Rotate(0f, 4f * Time.deltaTime, 0f, Space.World);
        if (Finished || paused) return;

        PhaseProgress = Mathf.Clamp01(PhaseProgress + Time.deltaTime / Mathf.Max(0.1f, Duration(CurrentPhase)));
        flightSeconds += Time.deltaTime;
        UpdateRocket();
        UpdateHud();

        if (PhaseProgress >= 1f && autoAdvance) NextPhase();
    }

    // ---------- Fases ----------

    public void NextPhase()
    {
        if (Finished) return;
        if (CurrentPhase == EventPhase.Departure) StartPhase(EventPhase.LunarOrbit);
        else if (CurrentPhase == EventPhase.LunarOrbit) StartPhase(EventPhase.Return);
        else Finish();
    }

    // Brinca a una fase en particular (Planning no aplica en esta escena)
    public void SetPhase(EventPhase phase)
    {
        if (phase == EventPhase.Planning) return;
        Finished = false;
        if (finishPanel != null) finishPanel.SetActive(false);
        StartPhase(phase);
    }

    // Boton de prueba "Saltar fase"
    public void SkipPhase()
    {
        NextPhase();
        UpdateRocket();
        UpdateHud();
    }

    public void Finish()
    {
        if (Finished) return;
        Finished = true;
        PhaseProgress = 1f;
        Log("Amerizaje confirmado. Tripulación a salvo.");
        if (finishPanel != null) finishPanel.SetActive(true);
        if (MissionFinished != null) MissionFinished();
    }

    // Boton "Ver resultados"
    public void GoToResults()
    {
        GameManager.Instance.GoToScene(Scenes.Results);
    }

    void StartPhase(EventPhase phase)
    {
        CurrentPhase = phase;
        PhaseProgress = 0f;
        Log("Inicia fase: " + PhaseName(phase));
        if (PhaseStarted != null) PhaseStarted(phase);
    }

    float Duration(EventPhase phase)
    {
        if (phase == EventPhase.Departure) return departureSeconds;
        if (phase == EventPhase.LunarOrbit) return lunarOrbitSeconds;
        return returnSeconds;
    }

    public static string PhaseName(EventPhase phase)
    {
        if (phase == EventPhase.Departure) return "Salida";
        if (phase == EventPhase.LunarOrbit) return "Órbita lunar";
        if (phase == EventPhase.Return) return "Retorno";
        return "Planeación";
    }

    // ---------- Bitacora ----------

    public void Log(string message)
    {
        log = message + "\n" + log;
        string[] lines = log.Split('\n');
        if (lines.Length > MaxLogLines) log = string.Join("\n", lines, 0, MaxLogLines);
        if (logLabel != null) logLabel.text = "<b>Bitácora</b>\n" + log;
    }

    // ---------- Vista 3D ----------

    void UpdateRocket()
    {
        if (rocket == null || earth == null || moon == null) return;
        float t = Mathf.Min(PhaseProgress, 0.99f);
        Vector3 a = PathPoint(CurrentPhase, t);
        Vector3 b = PathPoint(CurrentPhase, t + 0.01f);
        rocket.position = PathPoint(CurrentPhase, PhaseProgress);
        Vector3 dir = b - a;
        if (dir.sqrMagnitude > 0.000001f) rocket.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
    }

    void DrawPath()
    {
        if (pathLine == null || earth == null || moon == null) return;
        const int steps = 40;
        EventPhase[] phases = { EventPhase.Departure, EventPhase.LunarOrbit, EventPhase.Return };
        pathLine.useWorldSpace = true;
        pathLine.positionCount = phases.Length * (steps + 1);
        int i = 0;
        foreach (EventPhase p in phases)
        {
            for (int s = 0; s <= steps; s++)
            {
                pathLine.SetPosition(i, PathPoint(p, s / (float)steps));
                i++;
            }
        }
    }

    // Posicion del cohete en la ruta: t va de 0 a 1 dentro de cada fase.
    // Salida: curva de la Tierra a la orbita lunar. Orbita: 1.5 vueltas a la Luna. Retorno: curva de regreso.
    public Vector3 PathPoint(EventPhase phase, float t)
    {
        Vector3 e = earth.position;
        Vector3 m = moon.position;
        Vector3 toMoon = m - e;
        float dist = toMoon.magnitude;
        Vector3 right = dist > 0.001f ? toMoon / dist : Vector3.right;
        Vector3 fwd = Vector3.Cross(right, Vector3.up);
        float re = earth.lossyScale.x * 0.5f;
        float ro = moon.lossyScale.x * 0.5f + orbitGap;

        if (phase == EventPhase.LunarOrbit)
        {
            float angle = t * 3f * Mathf.PI;
            return m + (right * Mathf.Sin(angle) - fwd * Mathf.Cos(angle)) * ro;
        }
        if (phase == EventPhase.Return)
        {
            Vector3 r0 = m + fwd * ro;
            return Bezier(r0, r0 - right * dist * 0.35f, e + fwd * re * 3f + right * re, e + fwd * re, t);
        }
        Vector3 d3 = m - fwd * ro;
        return Bezier(e - fwd * re, e - fwd * re * 3f + right * re, d3 - right * dist * 0.35f, d3, t);
    }

    static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    // ---------- Pantalla grande ----------

    void UpdateHud()
    {
        if (phaseLabel != null) phaseLabel.text = "FASE: " + PhaseName(CurrentPhase).ToUpper();
        if (progressFill != null) progressFill.anchorMax = new Vector2(PhaseProgress, 1f);
        if (dataLabel == null) return;

        dataLabel.text =
            "Distancia a la Tierra:  " + DistanceKm().ToString("N0") + " km\n" +
            "Velocidad:  " + FakeVelocity().ToString("0.0") + " km/s\n" +
            "Combustible:  " + FakeFuel().ToString("0") + " %\n" +
            "Tiempo de misión:  " + MissionTime();
    }

    float DistanceKm()
    {
        if (rocket == null || earth == null || moon == null) return 0f;
        float re = earth.lossyScale.x * 0.5f;
        float span = Vector3.Distance(earth.position, moon.position) - re;
        float d = Vector3.Distance(rocket.position, earth.position) - re;
        if (span <= 0.001f) return 0f;
        return Mathf.Max(0f, d / span * EarthMoonKm);
    }

    // Valores de prueba con forma realista (orbita baja ~7.8 km/s, inyeccion translunar ~10.9 km/s)
    float FakeVelocity()
    {
        float t = PhaseProgress;
        if (CurrentPhase == EventPhase.Departure)
        {
            if (t < 0.15f) return Mathf.Lerp(0f, 7.8f, t / 0.15f);
            if (t < 0.25f) return Mathf.Lerp(7.8f, 10.9f, (t - 0.15f) / 0.1f);
            return Mathf.Lerp(10.9f, 2.4f, (t - 0.25f) / 0.75f);
        }
        if (CurrentPhase == EventPhase.LunarOrbit) return 1.6f;
        if (Finished) return 0f;
        if (t < 0.9f) return Mathf.Lerp(1.6f, 11f, t / 0.9f);
        return Mathf.Lerp(11f, 0.1f, (t - 0.9f) / 0.1f); // reentrada y paracaidas
    }

    float FakeFuel()
    {
        float t = PhaseProgress;
        if (CurrentPhase == EventPhase.Departure) return 100f - 60f * Mathf.Min(1f, t / 0.25f);
        if (CurrentPhase == EventPhase.LunarOrbit) return Mathf.Lerp(40f, 30f, t);
        return Mathf.Lerp(30f, 15f, Mathf.Min(1f, t / 0.1f));
    }

    string MissionTime()
    {
        float total = departureSeconds + lunarOrbitSeconds + returnSeconds;
        float seconds = total > 0f ? flightSeconds / total * MissionDays * 86400f : 0f;
        int s = Mathf.FloorToInt(seconds);
        int days = s / 86400;
        int hours = (s % 86400) / 3600;
        int minutes = (s % 3600) / 60;
        return "T+ " + days + "d " + hours.ToString("00") + ":" + minutes.ToString("00");
    }
}
