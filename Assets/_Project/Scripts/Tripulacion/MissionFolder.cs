using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Mission folder on the planning desk (task 4, P3).
// Page 1: mission briefing, launch checklist and mission updates (random events that happened).
// Page 2 (task 5): crew profiles and training, see CrewPage.
// More pages go as children of "Pages" and are added to the "pages" list.
// Open it with Open() or SetActive(true); the X button calls Close().
// Every time it opens it marks the task "Mission read" (gm.missionRead).
// The first time, it also rolls one planning event, which shows up in the updates.
public class MissionFolder : MonoBehaviour
{
    [Header("Pages (page 1 = mission)")]
    public List<GameObject> pages = new List<GameObject>();
    public TMP_Text pageLabel;
    public Button previousButton;
    public Button nextButton;

    [Header("Page 1: mission")]
    public TMP_Text briefingText;
    public TMP_Text checklistText;
    public TMP_Text updatesText;
    public TMP_Text detailsText;                    // optional: rocket, power, supplies and science lines
    public TMP_Text[] factValues = new TMP_Text[4]; // optional tiles: duration, crew, budget, delta-v
    public Image[] checklistNotes = new Image[4];   // optional sticky notes, one per launch task
    public TMP_Text[] checklistNoteTexts = new TMP_Text[4];

    [Header("Data")]
    public DestinationData mission; // only used if GameManager has no mission
    public bool rollEventOnFirstOpen = true;

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    const string GoodColor = "#2E7D32";
    const string BadColor = "#C62828";

    int currentPage;
    int shownEvents = -1;
    float shownMoney = -1f;

    void OnEnable()
    {
        GameManager gm = GameManager.Instance;
        if (gm.mission == null) gm.mission = mission;

        bool firstTime = !gm.missionRead;
        gm.missionRead = true;
        if (firstTime && rollEventOnFirstOpen) RollPlanningUpdate();

        ShowPage(currentPage);
        Refresh();
        onOpened.Invoke();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        RefreshChecklist(gm);

        // Only rebuild the texts when something changed
        if (gm.eventsHappened.Count != shownEvents || gm.money != shownMoney) Refresh();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        onClosed.Invoke();
        gameObject.SetActive(false);
    }

    // ---------- Pages ----------

    public void NextPage()
    {
        if (currentPage < pages.Count - 1) Sfx.Play(Sfx.FolderPage);
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        if (currentPage > 0) Sfx.Play(Sfx.FolderPage);
        ShowPage(currentPage - 1);
    }

    public void ShowPage(int index)
    {
        if (pages.Count == 0) return;
        currentPage = Mathf.Clamp(index, 0, pages.Count - 1);

        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == currentPage);
        }

        if (pageLabel != null) pageLabel.text = "Page " + (currentPage + 1) + " / " + pages.Count;
        if (previousButton != null) previousButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < pages.Count - 1;
    }

    // ---------- Events ----------

    // Rolls one planning event (it can be nothing) and applies it. Public so other stations
    // or the desk can bring a new update at another moment. Returns null if nothing happened.
    public EventData RollPlanningUpdate()
    {
        GameData data = GameData.Instance;
        if (data == null) return null;

        EventData e = RandomEvents.RollAndApply(data.events, EventPhase.Planning);
        Refresh();
        return e;
    }

    // ---------- Texts ----------

    public void Refresh()
    {
        GameManager gm = GameManager.Instance;
        shownEvents = gm.eventsHappened.Count;
        shownMoney = gm.money;

        bool tiles = factValues.Length >= 4 && factValues[0] != null;
        if (briefingText != null) briefingText.text = tiles ? BuildBriefingHeader(gm) : BuildBriefing(gm);
        if (detailsText != null) detailsText.text = BuildBriefingDetails(gm);
        for (int i = 0; i < factValues.Length && tiles; i++)
            if (factValues[i] != null) factValues[i].text = FactValue(gm, i);
        if (updatesText != null) updatesText.text = BuildUpdates(gm.eventsHappened);
        RefreshChecklist(gm);
    }

    static readonly Color NoteTodo = new Color(1f, 0.94f, 0.6f, 1f);
    static readonly Color NoteDone = new Color(0.78f, 0.92f, 0.72f, 1f);

    // Sticky notes if the prefab has them, otherwise the text list
    void RefreshChecklist(GameManager gm)
    {
        bool hasNotes = checklistNotes.Length >= 4 && checklistNotes[0] != null;
        if (!hasNotes)
        {
            if (checklistText != null) checklistText.text = ChecklistOnPaper(gm);
            return;
        }

        string[] lines = LaunchChecklist.BuildText(gm).Split('\n');
        int n = 0;
        foreach (string line in lines)
        {
            if (line.Trim() == "" || n >= checklistNotes.Length) continue;
            bool done = line.Contains("[done]");
            int tagEnd = line.IndexOf("</color>");
            string task = tagEnd >= 0 ? line.Substring(tagEnd + 8).Trim() : line.Trim();
            if (checklistNotes[n] != null) checklistNotes[n].color = done ? NoteDone : NoteTodo;
            if (n < checklistNoteTexts.Length && checklistNoteTexts[n] != null)
                checklistNoteTexts[n].text = done ? "<s>" + task + "</s>\n<b><color=" + GoodColor + ">DONE</color></b>" : task;
            n++;
        }
    }

    // The launch checklist with darker colors, readable on the paper
    static string ChecklistOnPaper(GameManager gm)
    {
        return LaunchChecklist.BuildText(gm).Replace("#6FCF6F", GoodColor).Replace("#FF8A65", BadColor);
    }

    public static readonly string[] FactLabels = { "DURATION", "CREW", "BUDGET LEFT", "SPACECRAFT DELTA-V" };

    public static string FactValue(GameManager gm, int index)
    {
        DestinationData m = gm.mission;
        if (m == null) return "-";
        if (index == 0) return m.missionDays + " days";
        if (index == 1) return m.crewCount + " astronauts";
        if (index == 2) return gm.money.ToString("N0") + " cr";
        return m.requiredDeltaV.ToString("N0") + " m/s";
    }

    public static string BuildBriefingHeader(GameManager gm)
    {
        DestinationData m = gm.mission;
        if (m == null) return "No mission loaded. Run SpaceApps > Import mission catalog.";
        return "<size=130%><b>" + m.destinationName + "</b></size>\n" + m.description;
    }

    public static string BuildBriefingDetails(GameManager gm)
    {
        DestinationData m = gm.mission;
        if (m == null) return "";
        string text = "<b>Rocket:</b> one launcher + the spacecraft (capsule, service module and abort tower)\n";
        text += "<b>Power:</b> batteries for " + RocketPhysics.OutageHours + " h without sunlight\n";
        text += "<b>Supplies:</b> water, oxygen and food for the whole crew and every day\n";
        text += "<b>Science:</b> at least " + m.minSciencePoints.ToString("0") + " points, all sent to Earth in "
            + (m.maxDownlinkSeconds / 60f).ToString("0") + " min or less";
        return text;
    }

    public static string BuildBriefing(GameManager gm)
    {
        DestinationData m = gm.mission;
        if (m == null) return "No mission loaded. Run SpaceApps > Import mission catalog.";

        string text = "<size=130%><b>" + m.destinationName + "</b></size>\n";
        text += m.description + "\n\n";
        text += "<b>Duration:</b> " + m.missionDays + " days\n";
        text += "<b>Crew:</b> " + m.crewCount + " astronauts (pilot, engineer and scientist)\n";
        text += "<b>Rocket:</b> one launcher + the spacecraft (capsule, service module and abort tower)\n";
        text += "<b>Spacecraft delta-v:</b> at least " + m.requiredDeltaV.ToString("N0") + " m/s to enter lunar orbit and come back\n";
        text += "<b>Power:</b> batteries for " + RocketPhysics.OutageHours + " h without sunlight\n";
        text += "<b>Supplies:</b> water, oxygen and food for the whole crew and every day\n";
        text += "<b>Science:</b> at least " + m.minSciencePoints.ToString("0") + " points, and send all the data to Earth in "
            + (m.maxDownlinkSeconds / 60f).ToString("0") + " min or less\n";
        text += "<b>Budget:</b> " + gm.startingMoney.ToString("N0") + " cr (you have " + gm.money.ToString("N0") + " cr left)";
        return text;
    }

    // Newest update first
    public static string BuildUpdates(List<EventData> events)
    {
        string text = "";
        for (int i = events.Count - 1; i >= 0; i--)
        {
            EventData e = events[i];
            if (e == null) continue;

            string color = e.isGood ? GoodColor : BadColor;
            text += "<color=" + color + "><b>[" + PhaseName(e.phase) + "] " + e.eventName + "</b></color>\n";
            text += e.description + "\n";
            text += "<size=85%>Credits " + Signed(e.moneyChange) + " cr   Score " + Signed(e.scoreChange) + "</size>\n";
            if (!string.IsNullOrEmpty(e.lesson)) text += "<i>" + e.lesson + "</i>\n";
            text += "\n";
        }

        if (text == "") text = "No updates yet. News about the mission will show up here.";
        return text;
    }

    public static string PhaseName(EventPhase phase)
    {
        if (phase == EventPhase.Planning) return "Planning";
        if (phase == EventPhase.Departure) return "Departure";
        if (phase == EventPhase.LunarOrbit) return "Lunar orbit";
        return "Return";
    }

    static string Signed(float value)
    {
        if (value > 0f) return "+" + value.ToString("0");
        return value.ToString("0");
    }
}
