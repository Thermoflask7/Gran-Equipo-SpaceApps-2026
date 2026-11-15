using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Energy repair control inside the maintenance minigame (task 13).
// A row of fuses. Every few seconds a random one burns out; the player clicks it to replace it.
// While no fuse is burned, AllWorking is true and MaintenanceMinigame fills the Energy bar.
// How often they burn is set in MaintenanceSettings.
public class FusePanel : MonoBehaviour
{
    public Button[] fuses;              // one button per fuse
    public TMP_Text[] fuseLabels;       // text on each fuse: "OK" or "X"
    public TMP_Text stateLabel;         // "All good" / "2 burned"

    public Color okColor = new Color(0.95f, 0.8f, 0.3f);
    public Color burnedColor = new Color(0.35f, 0.08f, 0.06f);

    public bool Running { get; private set; }
    public int BurnedCount { get; private set; }
    public bool AllWorking => Running && BurnedCount == 0;

    static readonly Color GoodColor = new Color(0.2f, 0.8f, 0.35f);
    static readonly Color BadColor = new Color(0.9f, 0.2f, 0.15f);

    MaintenanceSettings settings;
    bool[] burned;
    float timer;
    bool listenersAdded;

    void Awake()
    {
        AddListeners();
    }

    void AddListeners()
    {
        if (listenersAdded) return;
        for (int i = 0; i < fuses.Length; i++)
        {
            int index = i; // copy for the lambda
            fuses[i].onClick.AddListener(() => Replace(index));
        }
        listenersAdded = true;
    }

    public void Begin(MaintenanceSettings s)
    {
        settings = s;
        AddListeners();
        burned = new bool[fuses.Length];
        Running = true;
        foreach (Button b in fuses) b.interactable = true;
        BurnRandom(); // start with one burned so the mechanic is obvious
        Refresh();
    }

    public void Stop()
    {
        Running = false;
        if (fuses != null)
            foreach (Button b in fuses)
                if (b != null) b.interactable = false;
        Refresh();
    }

    // Replace a burned fuse with a new one
    public void Replace(int index)
    {
        if (!Running || index < 0 || index >= burned.Length || !burned[index]) return;
        burned[index] = false;
        Sfx.Play(Sfx.MaintenanceClick);
        Refresh();
    }

    void Update()
    {
        if (!Running) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) BurnRandom();
    }

    // Burns out a random working fuse
    void BurnRandom()
    {
        timer = Random.Range(settings.minBurnSeconds, settings.maxBurnSeconds);
        int working = 0;
        for (int i = 0; i < burned.Length; i++) if (!burned[i]) working++;
        if (working == 0) return;

        int pick = Random.Range(0, working);
        for (int i = 0; i < burned.Length; i++)
        {
            if (burned[i]) continue;
            if (pick == 0) { burned[i] = true; break; }
            pick--;
        }
        Refresh();
    }

    void Refresh()
    {
        BurnedCount = 0;
        for (int i = 0; i < fuses.Length; i++)
        {
            bool isBurned = Running && burned != null && burned[i];
            if (isBurned) BurnedCount++;
            if (fuses[i] != null && fuses[i].image != null)
                fuses[i].image.color = isBurned ? burnedColor : okColor;
            if (i < fuseLabels.Length && fuseLabels[i] != null)
            {
                fuseLabels[i].text = isBurned ? "X" : "OK";
                fuseLabels[i].color = isBurned ? BadColor : Color.black;
            }
        }

        if (stateLabel == null) return;
        if (!Running) stateLabel.text = "";
        else if (BurnedCount == 0) { stateLabel.text = "All good"; stateLabel.color = GoodColor; }
        else
        {
            stateLabel.text = BurnedCount + " burned";
            stateLabel.color = BadColor;
        }
    }
}
