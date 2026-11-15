using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Propulsion repair control inside the maintenance minigame (task 13).
// 4 LEDs and 4 switches: each switch belongs to the LED in its row.
// Every few seconds all 4 LEDs change at random (at the same time).
// While every switch matches its LED (on = checked), Matched is true
// and MaintenanceMinigame slowly fills the Propulsion bar instead of draining it.
// How often the LEDs change is set in MaintenanceSettings.
public class PropulsionPanel : MonoBehaviour
{
    public Toggle[] toggles;    // same order as leds
    public Image[] leds;
    public TMP_Text stateLabel; // "Aligned" / "Misaligned" (optional)

    public Color ledOnColor = new Color(1f, 0.85f, 0.2f);
    public Color ledOffColor = new Color(0.2f, 0.2f, 0.22f);

    public bool Running { get; private set; }

    MaintenanceSettings settings;
    bool[] ledStates;
    float timer;

    static readonly Color GoodColor = new Color(0.2f, 0.8f, 0.35f);
    static readonly Color BadColor = new Color(0.9f, 0.2f, 0.15f);

    // true if every switch matches its LED
    public bool Matched
    {
        get
        {
            if (!Running || ledStates == null) return false;
            for (int i = 0; i < ledStates.Length; i++)
                if (toggles[i].isOn != ledStates[i]) return false;
            return true;
        }
    }

    public void Begin(MaintenanceSettings s)
    {
        settings = s;
        ledStates = new bool[leds.Length];
        Running = true;
        foreach (Toggle t in toggles)
        {
            t.isOn = false;
            t.interactable = true;
        }
        ChangeLeds();
    }

    public void Stop()
    {
        Running = false;
        if (toggles == null) return;
        foreach (Toggle t in toggles)
            if (t != null) t.interactable = false;
        if (stateLabel != null) stateLabel.text = "";
    }

    void Update()
    {
        if (!Running) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) ChangeLeds();

        if (stateLabel != null)
        {
            bool ok = Matched;
            stateLabel.text = ok ? "Aligned" : "Misaligned";
            stateLabel.color = ok ? GoodColor : BadColor;
        }
    }

    // New random pattern, always different from the switches so the player has to flip something
    void ChangeLeds()
    {
        int tries = 0;
        do
        {
            for (int i = 0; i < ledStates.Length; i++)
                ledStates[i] = Random.value < 0.5f;
            tries++;
        } while (Matched && tries < 20);

        for (int i = 0; i < leds.Length; i++)
            leds[i].color = ledStates[i] ? ledOnColor : ledOffColor;

        timer = Random.Range(settings.ledMinChangeSeconds, settings.ledMaxChangeSeconds);
    }
}
