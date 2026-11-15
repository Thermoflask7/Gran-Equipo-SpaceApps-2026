using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Maintenance minigame (task 13). It is a prefab, not a scene.
// The engineer watches 4 ship systems that wear down and fail at random.
// Each system is repaired differently, each with its own panel outside the main panel:
//   Energy: FusePanel, replace the fuses that burn out
//   Oxygen: OxygenPanel, pump the lever to keep the oxygen in the green zone
//   Communications: RadioPanel, turn the dial until Earth's frequency is tuned in
//   Propulsion: PropulsionPanel, 4 switches that must match 4 LEDs
// While a system's panel is OK (no burned fuses / green zone / tuned / LEDs matched),
// its bar rises; otherwise it drains.
//
// Use from the flight phases (task 11): add MaintenanceFlightHook next to this component in 02_Lanzamiento.
// It registers with FlightPhases for TrainingFocus.Engineering, opens this minigame and, on Completed, writes
//   FlightPhases.SetResult(gm.flight, TrainingFocus.Engineering, minigame.RawResult); flightPhases.MinigameDone();
// Never pass the Completed argument (FinalResult) to SetResult and never add to gm.missionScore by hand:
// MissionScoring applies SkillMultiplier itself and recomputes missionScore from scratch (audit F08).
//
// Result:
//   RawResult   = 0 to 1, how healthy the systems stayed on average (player performance only) -> goes to flight.repairResult
//   FinalResult = RawResult * the engineer's SkillMultiplier (GDD rule), only for showing it on screen
// The minigame does NOT touch the GameManager: MaintenanceFlightHook does.
public class MaintenanceMinigame : MonoBehaviour
{
    [Header("Systems (same order in every array)")]
    public string[] systemNames = { "Energy", "Oxygen", "Communications", "Propulsion" };
    public Image[] bars;                // fill of each bar: Filled Image, driven by fillAmount
    public TMP_Text[] statusLabels;     // "OK", "Low", "FAILURE!"
    public Button[] repairButtons;      // optional: simple button for a system without a panel (null if it has one)

    [Header("Repair panels")]
    public FusePanel fuses;
    public int fusesIndex = 0;          // position of Energy in systemNames
    public OxygenPanel oxygen;
    public int oxygenIndex = 1;         // position of Oxygen in systemNames
    public RadioPanel radio;
    public int radioIndex = 2;          // position of Communications in systemNames
    public PropulsionPanel propulsion;
    public int propulsionIndex = 3;     // position of Propulsion in systemNames

    [Header("UI")]
    public TMP_Text titleLabel;
    public TMP_Text timerLabel;
    public TMP_Text infoLabel;          // instructions before, result at the end
    public Button startButton;
    public Button continueButton;

    [Header("Balance")]
    [Tooltip("Every number of the minigame (duration, wear, failures, panels). If missing, defaults are used.")]
    public MaintenanceSettings settings;

    public bool Playing { get; private set; }
    public bool Finished { get; private set; }
    public float RawResult { get; private set; }
    public float FinalResult { get; private set; }

    // Fired when the player presses "Continue" at the end. Sends FinalResult (0 to 1).
    public event Action<float> Completed;

    float[] health;
    float[] drain;
    float timeLeft;
    float healthSum;    // sum of the average health of each frame, weighted by time
    float skill = 0.5f;
    float playDuration; // duration of this round (in case the settings change mid-round)

    static readonly Color GoodColor = new Color(0.2f, 0.8f, 0.35f);
    static readonly Color WarnColor = new Color(0.95f, 0.75f, 0.15f);
    static readonly Color BadColor = new Color(0.9f, 0.2f, 0.15f);

    void Awake()
    {
        Init();
    }

    // The assigned settings, or a default instance so the prefab works without an asset
    MaintenanceSettings Settings
    {
        get
        {
            if (settings == null)
            {
                Debug.LogWarning("MaintenanceMinigame has no MaintenanceSettings assigned: using defaults.");
                settings = ScriptableObject.CreateInstance<MaintenanceSettings>();
            }
            return settings;
        }
    }

    void Init()
    {
        if (health != null) return; // already initialized
        health = new float[systemNames.Length];
        drain = new float[systemNames.Length];

        for (int i = 0; i < repairButtons.Length; i++)
        {
            int index = i; // copy for the lambda
            if (repairButtons[i] != null) repairButtons[i].onClick.AddListener(() => Repair(index));
        }
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (continueButton != null) continueButton.onClick.AddListener(Continue);
    }

    void OnEnable()
    {
        ShowIntro();
    }

    // Starts with the crew engineer's skill.
    public void StartGame()
    {
        GameManager gm = GameManager.Instance;
        StartGame(gm.SkillMultiplier(gm.engineer, TrainingFocus.Engineering));
    }

    // Starts with a given skill (0 to 1). Useful for testing.
    public void StartGame(float skillMultiplier)
    {
        Init(); // in case it is called before the object is activated
        skill = Mathf.Clamp01(skillMultiplier);
        MaintenanceSettings s = Settings;
        for (int i = 0; i < health.Length; i++)
        {
            health[i] = UnityEngine.Random.Range(0.6f, 0.9f);
            drain[i] = UnityEngine.Random.Range(s.minDrain, s.maxDrain);
        }
        playDuration = s.duration;
        timeLeft = playDuration;
        healthSum = 0f;
        Playing = true;
        Finished = false;

        SetButtonsActive(true);
        if (fuses != null) fuses.Begin(s);
        if (oxygen != null) oxygen.Begin(s);
        if (radio != null) radio.Begin(s);
        if (propulsion != null) propulsion.Begin(s);
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (infoLabel != null)
            infoLabel.text = "Repair each system with its panel before it fails.";
        Refresh();
    }

    public void Repair(int index)
    {
        if (!Playing || index < 0 || index >= health.Length) return;
        if (HasPanel(index)) return; // that system is repaired with its panel
        health[index] = Mathf.Min(1f, health[index] + Settings.repairAmount);
        Refresh();
    }

    // true if the system has its own repair panel
    bool HasPanel(int index)
    {
        return (index == fusesIndex && fuses != null)
            || (index == oxygenIndex && oxygen != null)
            || (index == radioIndex && radio != null)
            || (index == propulsionIndex && propulsion != null);
    }

    // true if the system's panel is OK right now
    bool PanelOk(int index)
    {
        if (index == fusesIndex && fuses != null) return fuses.AllWorking;
        if (index == oxygenIndex && oxygen != null) return oxygen.InSweetSpot;
        if (index == radioIndex && radio != null) return radio.Tuned;
        if (index == propulsionIndex && propulsion != null) return propulsion.Matched;
        return false;
    }

    void Update()
    {
        if (!Playing) return;

        MaintenanceSettings s = Settings;
        float dt = Time.deltaTime;
        float failureChance = s.FailureChanceFor(skill); // a better engineer: fewer sudden failures
        float average = 0f;
        for (int i = 0; i < health.Length; i++)
        {
            if (PanelOk(i))
                health[i] += s.panelRefill * dt; // panel OK: fills slowly
            else
                health[i] -= drain[i] * dt;
            if (UnityEngine.Random.value < failureChance * dt / health.Length)
                health[i] -= s.failureDamage; // sudden failure in a random system
            health[i] = Mathf.Clamp01(health[i]);
            average += health[i];
        }
        average /= health.Length;
        healthSum += average * Mathf.Min(dt, timeLeft);

        timeLeft -= dt;
        Refresh();
        if (timeLeft <= 0f) EndGame();
    }

    void EndGame()
    {
        Playing = false;
        Finished = true;
        timeLeft = 0f;
        RawResult = Mathf.Clamp01(healthSum / playDuration);
        FinalResult = RawResult * skill;

        SetButtonsActive(false);
        if (fuses != null) fuses.Stop();
        if (oxygen != null) oxygen.Stop();
        if (radio != null) radio.Stop();
        if (propulsion != null) propulsion.Stop();
        Refresh();
        if (continueButton != null) continueButton.gameObject.SetActive(true);
        if (infoLabel != null) infoLabel.text = ResultText();
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
        if (health != null)
            for (int i = 0; i < health.Length; i++) health[i] = 1f;

        SetButtonsActive(false);
        if (fuses != null) fuses.Stop();
        if (oxygen != null) oxygen.Stop();
        if (radio != null) radio.Stop();
        if (propulsion != null) propulsion.Stop();
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        int seconds = Mathf.CeilToInt(Settings.duration);
        if (timerLabel != null) timerLabel.text = seconds + " s";
        if (infoLabel != null)
            infoLabel.text = "The ship's systems wear down during the flight.\n"
                + "Keep them up for " + seconds + " seconds.";
        Refresh();
    }

    string ResultText()
    {
        GameManager gm = GameManager.Instance;
        string engineer = gm.engineer != null ? gm.engineer.astronautName : "your engineer";
        return "Healthy systems: " + Mathf.RoundToInt(RawResult * 100) + "%"
            + "  ×  skill of " + engineer + ": " + Mathf.RoundToInt(skill * 100) + "%"
            + "  =  " + Mathf.RoundToInt(FinalResult * 100) + "%\n"
            + "On a crewed spacecraft, power, life support, communications and propulsion depend on each other: "
            + "a failure nobody handles can force the mission to abort.";
    }

    void SetButtonsActive(bool active)
    {
        foreach (Button b in repairButtons)
            if (b != null) b.interactable = active;
    }

    void Refresh()
    {
        if (timerLabel != null && Playing) timerLabel.text = Mathf.CeilToInt(timeLeft) + " s";

        for (int i = 0; i < health.Length; i++)
        {
            float h = health[i];
            Color c = h > 0.5f ? GoodColor : (h > 0.2f ? WarnColor : BadColor);

            if (i < bars.Length && bars[i] != null)
            {
                bars[i].fillAmount = h; // the Image is Filled, horizontal, from the left
                bars[i].color = c;
            }
            if (i < statusLabels.Length && statusLabels[i] != null)
            {
                statusLabels[i].text = h > 0.5f ? "OK" : (h > 0f ? "Low" : "FAILURE!");
                statusLabels[i].color = c;
            }
        }
    }
}
