using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Piloting minigame (task 12), by Claude-M. It is a prefab, not a scene.
// The pilot runs a sequence of timed commands (QTE): booster separation, MECO, parachutes...
// In each step a marker sweeps across a gauge ONCE and the pilot clicks anywhere on the screen (or presses Space)
// inside the green window. The brighter middle of the window is PERFECT. The window shrinks and the
// marker speeds up every step. The sequence depends on the flight phase (PilotingSettings.StepsFor):
// departure = ascent to translunar injection, lunar orbit = orbit burns, return = reentry and parachutes.
//
// Use from the flight phases (task 11): PilotingFlightHook (on an always-active object of 02_Lanzamiento)
// registers with FlightPhases for TrainingFocus.Piloting, calls SetPhase(current phase), opens this
// minigame and, on Completed, calls FlightPhases.CompleteMinigame(Piloting, RawResult).
// Never pass FinalResult to the flight: MissionScoring applies SkillMultiplier itself.
//
// Result:
//   RawResult   = 0 to 1, average score of the steps (player performance only) -> flight.pilotingResult
//   FinalResult = RawResult * the pilot's SkillMultiplier (GDD rule), only for showing it on screen
// The minigame does NOT touch the GameManager (it only reads the pilot for the skill and the text).
public class PilotingMinigame : MonoBehaviour
{
    enum State { Intro, Ready, Sweep, Feedback, Done }

    [Header("UI")]
    public TMP_Text titleLabel;
    public TMP_Text stepCounterLabel;   // "Command 2 / 5"
    public TMP_Text stepNameLabel;      // "Booster separation"
    public TMP_Text descriptionLabel;   // what the command does in the real mission
    public TMP_Text feedbackLabel;      // PERFECT, GOOD, EARLY, LATE, MISSED
    public TMP_Text infoLabel;          // instructions before, result at the end
    public RectTransform gauge;         // background bar; the window and marker are its children
    public RectTransform window;        // green window (anchors set from code)
    public RectTransform perfectZone;   // child of window (anchors set from code)
    public RectTransform marker;        // thin vertical line (anchors set from code)
    public Button fireButton;
    public Button startButton;
    public Button continueButton;

    [Header("Balance")]
    [Tooltip("Every number and step text of the minigame. If missing, defaults are used.")]
    public PilotingSettings settings;

    [Tooltip("Sequence to play. PilotingFlightHook sets it from the flight before opening the minigame.")]
    public EventPhase phase = EventPhase.Departure;

    public bool Playing { get; private set; }
    public bool Finished { get; private set; }
    public float RawResult { get; private set; }
    public float FinalResult { get; private set; }

    // Fired when the player presses "Continue" at the end. Sends FinalResult (0 to 1).
    public event Action<float> Completed;

    State state = State.Intro;
    PilotingSettings.PilotingStep[] steps;
    int stepIndex;
    float timer;         // time left in Ready / Feedback
    float sweepDuration; // seconds of the current sweep
    float markerPos;     // 0 to 1
    float windowCenter;
    float windowWidth;
    bool pressed;        // FIRE already used in this step
    float scoreSum;
    int perfectCount;
    float skill = 0.5f;
    bool initialized;

    static readonly Color PerfectColor = new Color(0.35f, 0.95f, 0.5f);
    static readonly Color GoodColor = new Color(0.2f, 0.8f, 0.35f);
    static readonly Color WarnColor = new Color(0.95f, 0.75f, 0.15f);
    static readonly Color BadColor = new Color(0.9f, 0.2f, 0.15f);

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;
        // The FIRE button is only a visual cue: a click ANYWHERE fires (read in Update on mouse down, so the
        // timing is the moment of the press, not the release). No onClick listener, so a click never counts twice.
        if (fireButton != null)
        {
            TMP_Text label = fireButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "FIRE  (click)";
        }
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (continueButton != null) continueButton.onClick.AddListener(Continue);
    }

    void OnEnable()
    {
        ShowIntro();
    }

    // The assigned settings, or a default instance so the prefab works without an asset
    PilotingSettings Settings
    {
        get
        {
            if (settings == null)
            {
                Debug.LogWarning("PilotingMinigame has no PilotingSettings assigned: using defaults.");
                settings = ScriptableObject.CreateInstance<PilotingSettings>();
            }
            return settings;
        }
    }

    // Chooses the command sequence. Call it before activating the object (or before StartGame).
    public void SetPhase(EventPhase flightPhase)
    {
        phase = flightPhase;
        if (state == State.Intro && isActiveAndEnabled) ShowIntro();
    }

    // Starts with the crew pilot's skill.
    public void StartGame()
    {
        GameManager gm = GameManager.Instance;
        StartGame(gm.SkillMultiplier(gm.pilot, TrainingFocus.Piloting));
    }

    // Starts with a given skill (0 to 1). Useful for testing.
    public void StartGame(float skillMultiplier)
    {
        Init();
        skill = Mathf.Clamp01(skillMultiplier);
        steps = Settings.StepsFor(phase);
        scoreSum = 0f;
        perfectCount = 0;
        Playing = true;
        Finished = false;
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (fireButton != null) fireButton.gameObject.SetActive(true);
        if (infoLabel != null) infoLabel.text = "Click anywhere when the marker is inside the green window.";

        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("PilotingMinigame: no steps for phase " + phase + "; the result is 1.");
            EndGame();
            return;
        }
        BeginStep(0);
    }

    // FIRE: a click anywhere (or Space / a touch). Only the first press of each sweep counts.
    public void Fire()
    {
        // Keep the UI from holding a selected button that Space could "submit"
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (state != State.Sweep || pressed) return;
        pressed = true;
        ResolveStep(markerPos);
    }

    void BeginStep(int index)
    {
        stepIndex = index;
        PilotingSettings s = Settings;
        windowWidth = s.WindowFor(index);
        float half = windowWidth / 2f;
        float lo = Mathf.Max(half, s.minWindowCenter);
        float hi = Mathf.Min(1f - half, s.maxWindowCenter);
        windowCenter = UnityEngine.Random.Range(lo, Mathf.Max(lo, hi));
        sweepDuration = s.SweepFor(index, phase);
        markerPos = 0f;
        pressed = false;
        state = State.Ready;
        timer = s.readyTime;

        PilotingSettings.PilotingStep step = steps[index];
        if (stepCounterLabel != null) stepCounterLabel.text = "Command " + (index + 1) + " / " + steps.Length;
        if (stepNameLabel != null) stepNameLabel.text = step.stepName;
        if (descriptionLabel != null) descriptionLabel.text = step.description;
        if (feedbackLabel != null)
        {
            feedbackLabel.text = "Stand by...";
            feedbackLabel.color = Color.white;
        }
        if (fireButton != null) fireButton.interactable = false;
        PlaceWindow();
        PlaceMarker();
    }

    void Update()
    {
        if (!Playing) return;
        float dt = Time.deltaTime;

        switch (state)
        {
            case State.Ready:
                timer -= dt;
                if (timer <= 0f)
                {
                    state = State.Sweep;
                    if (fireButton != null) fireButton.interactable = true;
                    if (feedbackLabel != null) feedbackLabel.text = "";
                }
                break;

            case State.Sweep:
                if (FirePressedThisFrame())
                {
                    Fire();
                    break;
                }
                markerPos += dt / Mathf.Max(0.05f, sweepDuration);
                if (markerPos >= 1f)
                {
                    markerPos = 1f;
                    PlaceMarker();
                    ResolveStep(-1f); // the marker went past without a press
                    break;
                }
                PlaceMarker();
                break;

            case State.Feedback:
                timer -= dt;
                if (timer <= 0f)
                {
                    if (stepIndex + 1 < steps.Length) BeginStep(stepIndex + 1);
                    else EndGame();
                }
                break;
        }
    }

    // Left click anywhere on the screen (main input), a touch (WebGL on a tablet) or Space (optional shortcut)
    static bool FirePressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    // marker < 0 = no press (MISSED)
    void ResolveStep(float pressedAt)
    {
        float score = pressedAt < 0f ? 0f : Settings.ScoreFor(pressedAt, windowCenter, windowWidth);
        scoreSum += score;
        if (score >= 1f) perfectCount++;

        if (feedbackLabel != null)
        {
            feedbackLabel.text = FeedbackText(pressedAt, score);
            feedbackLabel.color = score >= 1f ? PerfectColor : (score >= Settings.edgeScore ? GoodColor
                : (score > 0f ? WarnColor : BadColor));
        }
        if (fireButton != null) fireButton.interactable = false;
        state = State.Feedback;
        timer = Settings.feedbackTime;
    }

    string FeedbackText(float pressedAt, float score)
    {
        if (pressedAt < 0f) return "MISSED!";
        if (score >= 1f) return "PERFECT!";
        if (score >= Settings.edgeScore) return "GOOD";
        return pressedAt < windowCenter ? "TOO EARLY" : "TOO LATE";
    }

    void EndGame()
    {
        Playing = false;
        Finished = true;
        state = State.Done;
        int count = steps != null ? steps.Length : 0;
        RawResult = count > 0 ? Mathf.Clamp01(scoreSum / count) : 1f;
        FinalResult = RawResult * skill;

        if (fireButton != null) fireButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(true);
        if (stepCounterLabel != null) stepCounterLabel.text = "Sequence complete";
        if (stepNameLabel != null) stepNameLabel.text = "";
        if (descriptionLabel != null) descriptionLabel.text = "";
        if (feedbackLabel != null) feedbackLabel.text = "";
        if (infoLabel != null) infoLabel.text = ResultText(count);
    }

    void Continue()
    {
        if (!Finished) return;
        Completed?.Invoke(FinalResult);
        gameObject.SetActive(false);
    }

    void ShowIntro()
    {
        Playing = false;
        Finished = false;
        state = State.Intro;
        PilotingSettings.PilotingStep[] seq = Settings.StepsFor(phase);
        int count = seq != null ? seq.Length : 0;

        if (titleLabel != null) titleLabel.text = "Flight control: " + PhaseName(phase);
        if (stepCounterLabel != null) stepCounterLabel.text = count + " commands";
        if (stepNameLabel != null) stepNameLabel.text = "";
        if (descriptionLabel != null) descriptionLabel.text = "";
        if (feedbackLabel != null) feedbackLabel.text = "";
        if (infoLabel != null)
            infoLabel.text = "Every command must be sent at the right moment.\n"
                + "Click anywhere when the marker crosses the green window. The middle is PERFECT.";
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (fireButton != null) fireButton.gameObject.SetActive(false);

        windowCenter = 0.6f;
        windowWidth = Settings.windowWidth;
        markerPos = 0f;
        PlaceWindow();
        PlaceMarker();
    }

    string ResultText(int count)
    {
        GameManager gm = GameManager.Instance;
        string pilot = gm.pilot != null ? gm.pilot.astronautName : "your pilot";
        return "Timing: " + Mathf.RoundToInt(RawResult * 100) + "% (" + perfectCount + " / " + count + " perfect)"
            + "  ×  skill of " + pilot + ": " + Mathf.RoundToInt(skill * 100) + "%"
            + "  =  " + Mathf.RoundToInt(FinalResult * 100) + "%\n"
            + "In a real flight most of these commands are automatic, but the crew and mission control watch "
            + "each one: a separation or a parachute at the wrong moment can end the mission.";
    }

    public static string PhaseName(EventPhase p)
    {
        switch (p)
        {
            case EventPhase.LunarOrbit: return "lunar orbit";
            case EventPhase.Return: return "return and reentry";
            default: return "ascent";
        }
    }

    // ---------- Gauge drawing (anchors, so it works at any size) ----------

    void PlaceWindow()
    {
        if (window != null)
        {
            window.anchorMin = new Vector2(Mathf.Clamp01(windowCenter - windowWidth / 2f), 0f);
            window.anchorMax = new Vector2(Mathf.Clamp01(windowCenter + windowWidth / 2f), 1f);
            window.offsetMin = window.offsetMax = Vector2.zero;
        }
        if (perfectZone != null)
        {
            float pf = Settings.perfectFraction;
            perfectZone.anchorMin = new Vector2(0.5f - pf / 2f, 0f);
            perfectZone.anchorMax = new Vector2(0.5f + pf / 2f, 1f);
            perfectZone.offsetMin = perfectZone.offsetMax = Vector2.zero;
        }
    }

    void PlaceMarker()
    {
        if (marker == null) return;
        marker.anchorMin = new Vector2(markerPos, 0f);
        marker.anchorMax = new Vector2(markerPos, 1f);
        marker.offsetMin = new Vector2(-4f, -12f);
        marker.offsetMax = new Vector2(4f, 12f);
    }
}
