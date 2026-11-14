using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Escena 02_Lanzamiento (tarea 10): pantalla dividida.
//   Izquierda (2D): control de mision con operadores y una pantalla grande con la fase y los datos del vuelo.
//   Derecha (3D): tres vistas (FlightStage):
//     Liftoff  = la camara sigue al cohete despegando hasta salir de la atmosfera (primera parte de Salida).
//     Transit  = vista general de la ruta Tierra -> Luna -> Tierra (resto de Salida, Orbita lunar, primera parte de Retorno).
//     Landing  = la camara sigue a la capsula en la reentrada hasta el amerizaje (ultima parte de Retorno).
//   Entre vistas hay un fundido corto a negro.
// Se arma con el menu de Unity: SpaceApps > Armar escena de lanzamiento.
//
// Para la tarea 11 (fases del vuelo) y los minijuegos:
//   - CurrentPhase / PhaseProgress dicen en que fase va el vuelo (EventPhase.Departure, LunarOrbit, Return).
//   - PhaseStarted se dispara al empezar cada fase y MissionFinished al terminar el vuelo.
//   - CurrentStage / StageProgress / StageChanged dicen que vista 3D se esta mostrando.
//   - paused = true detiene el vuelo (por ejemplo mientras se juega un minijuego).
//   - autoAdvance = false hace que la fase no avance sola al llegar al 100%: llama NextPhase() cuando toque.
//   - Log("texto") escribe en la bitacora de control de mision.
// Los datos de vuelo (altitud, distancia, velocidad, combustible) son de prueba hasta conectar la fisica (tarea 17).
public class LaunchScreen : MonoBehaviour
{
    [Header("Vista 3D: ruta (vista general)")]
    public Transform rocket;
    public Transform earth;
    public Transform moon;
    public LineRenderer pathLine;
    [Tooltip("Distancia de la orbita lunar sobre la superficie de la Luna (unidades de la escena)")]
    public float orbitGap = 0.8f;

    [Header("Vista 3D: camara y fundido")]
    public Camera viewCamera;
    public TMP_Text viewTitleLabel;
    public Image viewFade;
    public float fadeSeconds = 0.5f;
    [Tooltip("Que tan rapido la camara alcanza al cohete en los acercamientos")]
    public float cameraFollow = 4f;

    [Header("Vista 3D: despegue (acercamiento)")]
    [Tooltip("Centro de la plataforma; la superficie esta en su altura")]
    public Transform liftoffSite;
    public Transform closeRocket;
    public Transform rocketFlame;
    [Tooltip("Parte de la fase Salida que dura el acercamiento del despegue (0 a 1)")]
    [Range(0.1f, 0.9f)] public float liftoffShare = 0.35f;
    public float padHeight = 1f;
    public float climbHeight = 150f;

    [Header("Vista 3D: reentrada y amerizaje (acercamiento)")]
    [Tooltip("Punto del amerizaje; el nivel del mar esta en su altura")]
    public Transform landingSite;
    public Transform returnCapsule;
    public Transform reentryGlow;
    public Transform parachute;
    [Tooltip("Parte de la fase Retorno que dura el acercamiento del aterrizaje (0 a 1)")]
    [Range(0.1f, 0.9f)] public float landingShare = 0.5f;
    public float reentryHeight = 150f;

    [Header("Control de mision (UI)")]
    public TMP_Text phaseLabel;
    public TMP_Text dataLabel;
    public TMP_Text logLabel;
    public RectTransform progressFill;
    public GameObject finishPanel;

    [Header("Duracion de cada fase (segundos)")]
    public float departureSeconds = 26f;
    public float lunarOrbitSeconds = 15f;
    public float returnSeconds = 20f;

    [Header("Control del vuelo")]
    [Tooltip("Si esta apagado, la fase se queda en 100% hasta que alguien llame NextPhase()")]
    public bool autoAdvance = true;
    [Tooltip("Detiene el vuelo (por ejemplo durante un minijuego)")]
    public bool paused;

    public EventPhase CurrentPhase { get; private set; } = EventPhase.Departure;
    public float PhaseProgress { get; private set; } // 0 a 1 dentro de la fase actual
    public FlightStage CurrentStage { get; private set; } = FlightStage.Liftoff;
    public bool Finished { get; private set; }

    public event Action<EventPhase> PhaseStarted;
    public event Action<FlightStage> StageChanged;
    public event Action MissionFinished;

    const float MissionDays = 10f;      // duracion aproximada de Artemis II
    const float EarthMoonKm = 384400f;  // distancia media Tierra-Luna
    const float ParkingOrbitKm = 185f;  // orbita baja al salir de la atmosfera
    const float ReentryKm = 120f;       // altura de la interfaz de reentrada
    const int MaxLogLines = 6;
    static readonly Color SkyColor = new Color(0.45f, 0.7f, 0.95f);

    float flightSeconds;
    string log = "";
    bool stageStarted;
    Vector3 overviewPosition;
    Quaternion overviewRotation;
    Vector3 flameScale = Vector3.one;
    Vector3 glowScale = Vector3.one;
    Vector3 chuteScale = Vector3.one;

    void Start()
    {
        if (viewCamera != null)
        {
            overviewPosition = viewCamera.transform.position;
            overviewRotation = viewCamera.transform.rotation;
        }
        if (rocketFlame != null) flameScale = rocketFlame.localScale;
        if (reentryGlow != null) glowScale = reentryGlow.localScale;
        if (parachute != null) chuteScale = parachute.localScale;
        if (finishPanel != null) finishPanel.SetActive(false);

        DrawPath();
        StartPhase(EventPhase.Departure);
        Refresh(true);
    }

    void Update()
    {
        if (earth != null) earth.Rotate(0f, 12f * Time.deltaTime, 0f, Space.World);
        if (moon != null) moon.Rotate(0f, 4f * Time.deltaTime, 0f, Space.World);

        if (!Finished && !paused)
        {
            PhaseProgress = Mathf.Clamp01(PhaseProgress + Time.deltaTime / Mathf.Max(0.1f, Duration(CurrentPhase)));
            flightSeconds += Time.deltaTime;
            if (PhaseProgress >= 1f && autoAdvance) NextPhase();
        }
        Refresh(false);
    }

    void Refresh(bool snapCamera)
    {
        if (UpdateStage()) snapCamera = true;
        UpdateRocket();
        UpdateLiftoff();
        UpdateLanding();
        UpdateCamera(snapCamera);
        UpdateFade();
        UpdateHud();
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
        Refresh(true);
    }

    // Boton de prueba "Saltar": despegue -> viaje -> orbita lunar -> retorno -> aterrizaje -> fin
    public void SkipPhase()
    {
        if (Finished) return;
        if (CurrentStage == FlightStage.Liftoff) PhaseProgress = liftoffShare;
        else if (CurrentPhase == EventPhase.Return && CurrentStage == FlightStage.Transit) PhaseProgress = 1f - landingShare;
        else NextPhase();
        Refresh(true);
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

    // ---------- Vistas 3D (FlightStage) ----------

    public static string StageName(FlightStage stage)
    {
        if (stage == FlightStage.Liftoff) return "Despegue";
        if (stage == FlightStage.Landing) return "Aterrizaje";
        return "Viaje";
    }

    // Que vista toca segun la fase y su progreso
    public FlightStage StageFor(EventPhase phase, float progress)
    {
        if (phase == EventPhase.Departure && progress < liftoffShare) return FlightStage.Liftoff;
        if (phase == EventPhase.Return && progress >= 1f - landingShare) return FlightStage.Landing;
        return FlightStage.Transit;
    }

    // 0 a 1 dentro de la vista actual (en Transit, dentro del tramo de viaje de la fase actual)
    public float StageProgress
    {
        get
        {
            float t = PhaseProgress;
            if (CurrentStage == FlightStage.Liftoff) return Mathf.Clamp01(t / liftoffShare);
            if (CurrentStage == FlightStage.Landing) return Mathf.Clamp01((t - (1f - landingShare)) / landingShare);
            return TransitT();
        }
    }

    // Avance del cohete en la ruta general de la fase actual
    float TransitT()
    {
        float t = PhaseProgress;
        if (CurrentPhase == EventPhase.Departure) return Mathf.Clamp01((t - liftoffShare) / (1f - liftoffShare));
        if (CurrentPhase == EventPhase.Return) return Mathf.Clamp01(t / (1f - landingShare));
        return t;
    }

    // Regresa true si cambio la vista
    bool UpdateStage()
    {
        FlightStage stage = Finished ? FlightStage.Landing : StageFor(CurrentPhase, PhaseProgress);
        if (stageStarted && stage == CurrentStage) return false;
        stageStarted = true;
        CurrentStage = stage;
        Log("Vista: " + StageName(stage));
        if (viewTitleLabel != null) viewTitleLabel.text = ViewTitle(stage);
        if (StageChanged != null) StageChanged(stage);
        return true;
    }

    static string ViewTitle(FlightStage stage)
    {
        if (stage == FlightStage.Liftoff) return "Despegue";
        if (stage == FlightStage.Landing) return "Reentrada y amerizaje";
        return "Ruta: Tierra - Luna - Tierra";
    }

    // Fundido a negro alrededor del cambio de vista (sin estado: depende solo del tiempo que falta o paso)
    void UpdateFade()
    {
        if (viewFade == null) return;
        float alpha = 0f;
        if (!Finished && fadeSeconds > 0f)
        {
            float secondsToSwitch = float.MaxValue;
            if (CurrentPhase == EventPhase.Departure) secondsToSwitch = (PhaseProgress - liftoffShare) * departureSeconds;
            if (CurrentPhase == EventPhase.Return) secondsToSwitch = (PhaseProgress - (1f - landingShare)) * returnSeconds;
            alpha = 1f - Mathf.Clamp01(Mathf.Abs(secondsToSwitch) / fadeSeconds);
        }
        Color c = viewFade.color;
        c.a = alpha;
        viewFade.color = c;
    }

    void UpdateCamera(bool snap)
    {
        if (viewCamera == null) return;
        Transform cam = viewCamera.transform;
        if (CurrentStage == FlightStage.Transit)
        {
            cam.SetPositionAndRotation(overviewPosition, overviewRotation);
            viewCamera.backgroundColor = Color.black;
            return;
        }

        bool up = CurrentStage == FlightStage.Liftoff;
        Transform target = up ? closeRocket : returnCapsule;
        Transform site = up ? liftoffSite : landingSite;
        if (target == null || site == null) return;

        float k = StageProgress;
        // Despegue: empieza cerca del suelo y termina arriba mirando hacia abajo (se ve la curva de la Tierra).
        // Aterrizaje: al reves.
        Vector3 offset = up
            ? Vector3.Lerp(new Vector3(10f, -4f, -30f), new Vector3(20f, 45f, -45f), k)
            : Vector3.Lerp(new Vector3(20f, 45f, -45f), new Vector3(-12f, 4f, -28f), Mathf.Clamp01(k / 0.6f));
        Vector3 desired = target.position + offset;
        desired.y = Mathf.Max(desired.y, site.position.y + 3f);
        cam.position = snap ? desired : Vector3.Lerp(cam.position, desired, 1f - Mathf.Exp(-cameraFollow * Time.deltaTime));

        Vector3 look = target.position + Vector3.up * (up ? 5f : 0f) - cam.position;
        if (look.sqrMagnitude > 0.001f) cam.rotation = Quaternion.LookRotation(look, Vector3.up);

        // Cielo azul cerca del suelo, negro fuera de la atmosfera
        float altitude = target.position.y - site.position.y;
        float space = Mathf.Clamp01(altitude / ((up ? climbHeight : reentryHeight) * 0.45f));
        viewCamera.backgroundColor = Color.Lerp(SkyColor, Color.black, space);
    }

    // ---------- Despegue ----------

    Vector3 LiftoffPoint(float u)
    {
        float s = Mathf.Clamp01((u - 0.12f) / 0.88f); // los primeros instantes: encendido en la plataforma
        float h = climbHeight * s * s;
        float side = climbHeight * 0.25f * s * s * s;  // giro gradual (gravity turn)
        return liftoffSite.position + new Vector3(side, padHeight + h, 0f);
    }

    void UpdateLiftoff()
    {
        if (closeRocket == null || liftoffSite == null) return;
        float u = CurrentPhase == EventPhase.Departure && CurrentStage == FlightStage.Liftoff ? StageProgress : 1f;
        if (CurrentPhase != EventPhase.Departure) u = 1f;

        Vector3 a = LiftoffPoint(Mathf.Min(u, 0.99f));
        Vector3 b = LiftoffPoint(Mathf.Min(u, 0.99f) + 0.01f);
        closeRocket.position = LiftoffPoint(u);
        Vector3 dir = b - a;
        closeRocket.rotation = dir.sqrMagnitude > 0.0001f
            ? Quaternion.FromToRotation(Vector3.up, dir.normalized)
            : Quaternion.identity;

        if (rocketFlame != null)
        {
            float ignition = Mathf.Clamp01((u - 0.04f) / 0.06f);
            float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 12f, 0f);
            rocketFlame.localScale = flameScale * ignition * flicker;
        }
    }

    // ---------- Reentrada y amerizaje ----------

    Vector3 LandingPoint(float u)
    {
        float h;
        float side = 0f;
        if (u < 0.6f)
        {
            // Reentrada: baja rapido y frena con la atmosfera
            float k = u / 0.6f;
            h = Mathf.Lerp(reentryHeight, 30f, 1f - (1f - k) * (1f - k));
            side = reentryHeight * 0.6f * (1f - k) * (1f - k);
        }
        else if (u < 0.95f)
        {
            // Paracaidas
            h = Mathf.Lerp(30f, 0.5f, (u - 0.6f) / 0.35f);
        }
        else
        {
            // En el agua, flotando
            h = 0.5f + 0.15f * Mathf.Sin(Time.time * 2f);
        }
        return landingSite.position + new Vector3(-side, h, 0f);
    }

    void UpdateLanding()
    {
        if (returnCapsule == null || landingSite == null) return;
        float u = CurrentStage == FlightStage.Landing ? StageProgress : 0f;

        Vector3 a = LandingPoint(Mathf.Min(u, 0.99f));
        Vector3 b = LandingPoint(Mathf.Min(u, 0.99f) + 0.01f);
        returnCapsule.position = LandingPoint(u);
        Vector3 dir = b - a;
        // En la reentrada el escudo termico (abajo de la capsula) va por delante; con paracaidas, derecha
        returnCapsule.rotation = u < 0.6f && dir.sqrMagnitude > 0.0001f
            ? Quaternion.FromToRotation(Vector3.up, -dir.normalized)
            : Quaternion.identity;

        if (reentryGlow != null)
        {
            float heat = 1f - Mathf.Clamp01(u / 0.4f);
            float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 10f, 5f);
            reentryGlow.localScale = glowScale * heat * flicker;
        }
        if (parachute != null)
        {
            float open = Mathf.Clamp01((u - 0.6f) / 0.05f);
            parachute.gameObject.SetActive(open > 0f);
            parachute.localScale = chuteScale * open;
        }
    }

    // ---------- Bitacora ----------

    public void Log(string message)
    {
        log = message + "\n" + log;
        string[] lines = log.Split('\n');
        if (lines.Length > MaxLogLines) log = string.Join("\n", lines, 0, MaxLogLines);
        if (logLabel != null) logLabel.text = "<b>Bitácora</b>\n" + log;
    }

    // ---------- Vista general ----------

    void UpdateRocket()
    {
        if (rocket == null || earth == null || moon == null) return;
        float t = TransitT();
        float ta = Mathf.Min(t, 0.99f);
        Vector3 dir = PathPoint(CurrentPhase, ta + 0.01f) - PathPoint(CurrentPhase, ta);
        rocket.position = PathPoint(CurrentPhase, t);
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

    // Posicion en la ruta general: t va de 0 a 1 dentro de cada fase.
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
        if (phaseLabel != null) phaseLabel.text = "FASE: " + PhaseName(CurrentPhase).ToUpper() + " · " + StageName(CurrentStage).ToUpper();
        if (progressFill != null) progressFill.anchorMax = new Vector2(PhaseProgress, 1f);
        if (dataLabel == null) return;

        string where;
        if (CurrentStage == FlightStage.Liftoff) where = "Altitud:  " + LiftoffAltitudeKm().ToString("N0") + " km";
        else if (CurrentStage == FlightStage.Landing) where = "Altitud:  " + LandingAltitudeKm().ToString("N0") + " km";
        else where = "Distancia a la Tierra:  " + TransitDistanceKm().ToString("N0") + " km";

        float v = FakeVelocity();
        dataLabel.text =
            where + "\n" +
            "Velocidad:  " + v.ToString(v < 1f ? "0.00" : "0.0") + " km/s\n" +
            "Combustible:  " + FakeFuel().ToString("0") + " %\n" +
            "Tiempo de misión:  " + MissionTime();
    }

    float LiftoffAltitudeKm()
    {
        if (closeRocket == null || liftoffSite == null) return 0f;
        float h = closeRocket.position.y - liftoffSite.position.y - padHeight;
        return Mathf.Max(0f, h / climbHeight * ParkingOrbitKm);
    }

    float LandingAltitudeKm()
    {
        if (returnCapsule == null || landingSite == null || Finished) return 0f;
        float h = returnCapsule.position.y - landingSite.position.y - 0.5f;
        return Mathf.Max(0f, h / reentryHeight * ReentryKm);
    }

    float TransitDistanceKm()
    {
        float km = DistanceKm();
        if (CurrentPhase == EventPhase.Departure) return Mathf.Max(ParkingOrbitKm, km);
        if (CurrentPhase == EventPhase.Return) return Mathf.Max(ReentryKm, km);
        return km;
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

    // Valores de prueba con forma realista (orbita baja ~7.8 km/s, inyeccion translunar ~10.9 km/s,
    // reentrada ~11 km/s, paracaidas unos metros por segundo)
    float FakeVelocity()
    {
        if (Finished) return 0f;
        float k = StageProgress;
        if (CurrentStage == FlightStage.Liftoff) return 7.8f * Mathf.Clamp01((k - 0.12f) / 0.88f);
        if (CurrentStage == FlightStage.Landing)
        {
            if (k < 0.6f) return Mathf.Lerp(11f, 0.15f, 1f - (1f - k / 0.6f) * (1f - k / 0.6f));
            if (k < 0.95f) return Mathf.Lerp(0.15f, 0.01f, (k - 0.6f) / 0.35f);
            return 0f;
        }
        if (CurrentPhase == EventPhase.Departure)
        {
            if (k < 0.15f) return Mathf.Lerp(7.8f, 10.9f, k / 0.15f);
            return Mathf.Lerp(10.9f, 2.4f, (k - 0.15f) / 0.85f);
        }
        if (CurrentPhase == EventPhase.LunarOrbit) return 1.6f;
        return Mathf.Lerp(1.6f, 11f, k);
    }

    float FakeFuel()
    {
        float k = StageProgress;
        if (CurrentStage == FlightStage.Liftoff) return Mathf.Lerp(100f, 55f, Mathf.Clamp01((k - 0.1f) / 0.9f));
        if (CurrentStage == FlightStage.Landing) return Mathf.Lerp(22f, 20f, k);
        if (CurrentPhase == EventPhase.Departure) return Mathf.Lerp(55f, 40f, Mathf.Clamp01(k / 0.15f));
        if (CurrentPhase == EventPhase.LunarOrbit) return Mathf.Lerp(40f, 30f, k);
        return Mathf.Lerp(30f, 22f, Mathf.Clamp01(k / 0.1f));
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
