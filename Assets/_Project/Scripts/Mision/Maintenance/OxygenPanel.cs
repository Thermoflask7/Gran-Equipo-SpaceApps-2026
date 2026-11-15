using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Oxygen repair control inside the maintenance minigame (task 13).
// It has its own oxygen level bar that drops on its own. The player pumps by moving
// the lever (Slider) up and down: every movement raises the oxygen.
// While the level is inside the green zone, InSweetSpot is true
// and MaintenanceMinigame fills the Oxygen repair bar; otherwise it drains.
// The numbers (green zone, decay, pump strength) are set in MaintenanceSettings.
public class OxygenPanel : MonoBehaviour
{
    public Slider pump;                 // pump lever (0 to 1)
    public Image oxygenFill;            // vertical Filled Image: oxygen level
    public RectTransform sweetZone;     // green zone marker (positioned from the settings)
    public TMP_Text stateLabel;         // "In range", "Low", "High"

    public float Oxygen { get; private set; }
    public bool Running { get; private set; }
    public bool InSweetSpot => Running && Oxygen >= settings.sweetMin && Oxygen <= settings.sweetMax;

    static readonly Color GoodColor = new Color(0.2f, 0.8f, 0.35f);
    static readonly Color BadColor = new Color(0.9f, 0.2f, 0.15f);
    static readonly Color FillColor = new Color(0.35f, 0.7f, 1f);

    MaintenanceSettings settings;
    float lastPumpValue;

    bool soundWired;

    void PlayPumpSound(float value)
    {
        if (Running) Sfx.Play(Sfx.MaintenanceScroll, 1f, 0.25f);
    }

    public void Begin(MaintenanceSettings s)
    {
        settings = s;
        Oxygen = settings.oxygenStartLevel;
        Running = true;
        if (pump != null)
        {
            pump.SetValueWithoutNotify(0f);
            pump.interactable = true;
            if (!soundWired) pump.onValueChanged.AddListener(PlayPumpSound);
            soundWired = true;
            lastPumpValue = pump.value;
        }
        Refresh();
    }

    public void Stop()
    {
        Running = false;
        if (pump != null) pump.interactable = false;
        Refresh();
    }

    void Update()
    {
        if (!Running) return;

        // Pumping: how far the lever moved since the last frame (up or down)
        float pumped = 0f;
        if (pump != null)
        {
            pumped = Mathf.Abs(pump.value - lastPumpValue);
            lastPumpValue = pump.value;
        }
        Oxygen = Mathf.Clamp01(Oxygen + pumped * settings.pumpGain - settings.oxygenDecay * Time.deltaTime);
        Refresh();
    }

    void PlaceSweetZone()
    {
        if (sweetZone == null || settings == null) return;
        sweetZone.anchorMin = new Vector2(0f, settings.sweetMin);
        sweetZone.anchorMax = new Vector2(1f, settings.sweetMax);
        sweetZone.offsetMin = sweetZone.offsetMax = Vector2.zero;
    }

    void Refresh()
    {
        PlaceSweetZone(); // in case the zone was changed in the settings during play
        if (oxygenFill != null)
        {
            oxygenFill.fillAmount = Running ? Oxygen : 0f;
            oxygenFill.color = FillColor;
        }
        if (stateLabel == null) return;
        if (!Running) stateLabel.text = "";
        else if (InSweetSpot) { stateLabel.text = "In range"; stateLabel.color = GoodColor; }
        else if (Oxygen < settings.sweetMin) { stateLabel.text = "Low: pump"; stateLabel.color = BadColor; }
        else { stateLabel.text = "High: wait"; stateLabel.color = BadColor; }
    }
}
