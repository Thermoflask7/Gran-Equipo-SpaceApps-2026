using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Communications repair control inside the maintenance minigame (task 13).
// A radio: the player turns the dial (Slider) to tune in the ground station on Earth.
// The correct frequency is hidden; the signal meter rises the closer the dial gets.
// Every few seconds the correct frequency moves (the ship is moving, so the signal drifts).
// While the dial is within tolerance, Tuned is true and MaintenanceMinigame fills the bar.
// Frequencies, tolerance and timing are set in MaintenanceSettings.
public class RadioPanel : MonoBehaviour
{
    public Slider dial;                 // 0 to 1, shown as a frequency (range in MaintenanceSettings)
    public Image signalFill;            // horizontal Filled Image: signal strength
    public TMP_Text frequencyLabel;     // "2245.0 MHz"
    public TMP_Text stateLabel;         // "Tuned", "Weak signal", "No signal"

    public bool Running { get; private set; }
    public float Signal { get; private set; }   // 0 to 1
    public bool Tuned => Running && Mathf.Abs(dial.value - target) <= settings.tuneTolerance;

    static readonly Color GoodColor = new Color(0.2f, 0.8f, 0.35f);
    static readonly Color WarnColor = new Color(0.95f, 0.75f, 0.15f);
    static readonly Color BadColor = new Color(0.9f, 0.2f, 0.15f);

    MaintenanceSettings settings;
    float target;   // correct frequency, 0 to 1 on the dial's scale
    float timer;

    public void Begin(MaintenanceSettings s)
    {
        settings = s;
        Running = true;
        dial.interactable = true;
        dial.value = Random.value;
        MoveTarget();
        Refresh();
    }

    public void Stop()
    {
        Running = false;
        if (dial != null) dial.interactable = false;
        Signal = 0f;
        Refresh();
    }

    void Update()
    {
        if (!Running) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) MoveTarget();
        Refresh();
    }

    // New correct frequency, away from the dial so the player has to search for it
    void MoveTarget()
    {
        int tries = 0;
        do
        {
            target = Random.Range(0.05f, 0.95f);
            tries++;
        } while (Mathf.Abs(dial.value - target) < settings.signalRange && tries < 20);
        timer = Random.Range(settings.radioMinChangeSeconds, settings.radioMaxChangeSeconds);
    }

    void Refresh()
    {
        if (Running)
            Signal = Mathf.Clamp01(1f - Mathf.Abs(dial.value - target) / settings.signalRange);

        if (frequencyLabel != null && settings != null) // before Begin there are no settings: keep the prefab's text
            frequencyLabel.text = Mathf.Lerp(settings.minFrequency, settings.maxFrequency, dial.value).ToString("0.0") + " MHz";

        Color c = Tuned ? GoodColor : (Signal > 0f ? WarnColor : BadColor);
        if (signalFill != null)
        {
            signalFill.fillAmount = Signal;
            signalFill.color = c;
        }
        if (stateLabel != null)
        {
            if (!Running) stateLabel.text = "";
            else stateLabel.text = Tuned ? "Tuned" : (Signal > 0f ? "Weak signal" : "No signal");
            stateLabel.color = c;
        }
    }
}
