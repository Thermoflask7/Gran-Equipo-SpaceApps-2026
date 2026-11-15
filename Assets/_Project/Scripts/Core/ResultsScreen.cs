using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 03_Resultados screen (tasks 16, 25, 35): calculates the final V16 score only once,
// stores it in GameManager and shows it in collapsible sections inside a scroll view:
// what went wrong (one entry per failed gate), planning, mission and lessons.
// Menu: SpaceApps -> Results screen -> Add to open scene.
public class ResultsScreen : MonoBehaviour
{
    public TMP_Text title;          // "Mission accomplished", "Mission failed"...
    public TMP_Text score;          // the big number
    public RectTransform content;   // content of the scroll view: the sections are created here
    public TMP_Text report;         // old single-text layout (only used if content is empty)

    // Scene played on its own (no rocket, never launched): fill a sample game so the screen can be tested
    public bool useTestValues = true;
    public bool failingTestValues = false; // true = sample with several failed gates (task 35)

    // Look of the sections
    public float headerSize = 34f;
    public float bodySize = 28f;
    public Color headerColor = new Color(0.13f, 0.19f, 0.34f, 0.95f);

    // Task 37c: local leaderboard
    bool sampleGame;            // test values: nothing is saved in the leaderboard
    int leaderboardRank = -1;   // 0-based rank of this turn in the top 10 (-1 = not in it or not saved)

    void Start()
    {
        Show();
    }

    public void Show()
    {
        GameManager gm = GameManager.Instance;
        sampleGame = false;
        if (useTestValues && !gm.flight.launched && gm.rocketParts.Count == 0)
        {
            ResultsSample.Fill(gm, !failingTestValues);
            sampleGame = true;
            Debug.Log("ResultsScreen: no game was played, showing test values.");
        }

        ScoreResult r = MissionScoring.SaveFinalScore();
        if (!sampleGame && (r.status == "SUCCESS" || r.status == "FAILURE"))
        {
            leaderboardRank = gm.session.RecordTurn(r, gm); // once per turn, even if Show runs again
            SetupMenuButton(gm);
        }
        if (title != null) title.text = TitleFor(r);
        if (score != null) score.text = r.total.ToString("0") + " / 100";
        if (content != null) BuildSections(r, gm);
        else if (report != null) report.text = BuildReport(r, gm);
    }

    static string TitleFor(ScoreResult r)
    {
        if (r.status == "SUCCESS") return "Mission accomplished!";
        if (r.status == "FAILURE" && r.aborted) return "Mission aborted";
        if (r.status == "FAILURE") return "Mission failed";
        if (r.status == "PENDING") return "Mission in progress";
        return "Invalid result";
    }

    // ---------- Collapsible sections ----------

    void BuildSections(ScoreResult r, GameManager gm)
    {
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        if (r.problem != "")
        {
            AddText("The score could not be calculated: " + r.problem, bodySize, content);
            return;
        }

        string official;
        if (r.status == "SUCCESS") official = r.competitive.ToString("0.0") + " / 100";
        else if (r.status == "FAILURE") official = "0 (the mission " + (r.aborted ? "was aborted" : "failed") + ")";
        else official = "pending (the flight has not finished)";
        TMP_Text summary = AddText("Learning score: <b>" + r.total.ToString("0.0") + " / 100</b>      Official score: <b>" + official + "</b>", bodySize, content);
        summary.alignment = TextAlignmentOptions.Center;

        if (r.status == "FAILURE")
        {
            AddText("<b>What went wrong</b>", headerSize, content).margin = new Vector4(0, 16, 0, 0);
            if (!string.IsNullOrEmpty(r.failureReason)) AddText(r.failureReason, bodySize, content);
            else if (OnlyScienceFailed(r)) AddText("The spacecraft returned safely, but the scientific objectives were incomplete.", bodySize, content);
            foreach (string gate in r.failedGates)
            {
                // With an in-flight failure, the missing milestones are a consequence, already explained above
                if (!string.IsNullOrEmpty(r.failureReason) && (gate == "DEPARTURE" || gate == "LUNAR_ORBIT" || gate == "RETURN")) continue;
                string gateTitle, numbers, lesson, fix;
                GateAdvice.Describe(gate, gm, out gateTitle, out numbers, out lesson, out fix);
                string body = "";
                if (lesson != "") body = GateAdvice.LessonLine(lesson);
                if (fix != "") body += (body == "" ? "" : "\n") + GateAdvice.FixLine(fix);
                AddFoldout(GateAdvice.Header(gateTitle, numbers), body, true);
            }
        }

        AddLeaderboard(gm);

        AddText("<b>Score breakdown</b>", headerSize, content).margin = new Vector4(0, 16, 0, 0);
        AddFoldout("Planning   <color=#8FB8FF>" + r.planning.ToString("0.0") + " / 45</color>", PlanningText(r), false);
        AddFoldout("Mission   <color=#8FB8FF>" + r.mission.ToString("0.0") + " / 55</color>", MissionText(r), false);

        List<string> lessons = Lessons(r, gm);
        if (lessons.Count > 0) AddFoldout("What you learned", "- " + string.Join("\n- ", lessons), false);

        // Start at the top of the list
        ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    // ---------- Leaderboard (task 37c) ----------

    // This round's ranking (2 to 4 players) and the saved top 10, with this turn highlighted
    void AddLeaderboard(GameManager gm)
    {
        if (sampleGame || !gm.session.turnRecorded) return;
        PlayerSession s = gm.session;
        LeaderboardEntry mine = s.roundEntries.Count > 0 ? s.roundEntries[s.roundEntries.Count - 1] : null;

        AddText("<b>Leaderboard</b>", headerSize, content).margin = new Vector4(0, 16, 0, 0);
        if (s.PlayerCount > 1 && mine != null)
        {
            string roundHeader = (s.HasNextPlayer ? "This round so far" : "This round: final ranking")
                                 + "   <color=#8FB8FF>" + s.roundEntries.Count + " / " + s.PlayerCount + " players</color>";
            AddFoldout(roundHeader, RankingText(s.RoundRanking(), mine), true);
        }

        List<LeaderboardEntry> top = Leaderboard.Load();
        LeaderboardEntry saved = leaderboardRank >= 0 && leaderboardRank < top.Count ? top[leaderboardRank] : null;
        string place = saved != null ? s.CurrentName + " is #" + (leaderboardRank + 1)
                                     : s.CurrentName + " is not in the top " + Leaderboard.MaxEntries;
        AddFoldout("Top " + Leaderboard.MaxEntries + "   <color=#8FB8FF>" + place + "</color>", RankingText(top, saved), s.PlayerCount == 1);
    }

    static string RankingText(List<LeaderboardEntry> list, LeaderboardEntry highlight)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            LeaderboardEntry e = list[i];
            string result = e.success ? "<color=#6FCF97>Success</color>"
                                      : "<color=#EB5757>Failed</color>  <size=80%>(learning " + e.diagnostic.ToString("0") + ")</size>";
            string line = (i + 1) + ".<pos=7%>" + e.playerName + "<pos=45%>" + e.competitive.ToString("0.0") + "<pos=60%>" + result;
            if (e == highlight) line = "<color=#FFD54F><b>" + line + "   (you)</b></color>";
            if (i > 0) sb.Append('\n');
            sb.Append(line);
        }
        return sb.ToString();
    }

    // More players waiting: the "Back to menu" button becomes "Next player: name" (same mission, fresh game)
    void SetupMenuButton(GameManager gm)
    {
        PlayerSession s = gm.session;
        if (!s.HasNextPlayer) return;
        NextSceneButton next = FindAnyObjectByType<NextSceneButton>();
        if (next == null) return;
        Button button = next.GetComponent<Button>();
        button.onClick.RemoveAllListeners(); // drops NextSceneButton's "go to menu" listener (added in its Awake)
        button.onClick.AddListener(() =>
        {
            if (!s.NextPlayer()) return;
            DestinationData mission = gm.mission;
            gm.ResetGame();
            gm.mission = mission;
            gm.GoToScene(Scenes.Planning);
        });
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = 18f;
            label.text = "Next player: " + s.players[s.current + 1];
        }
    }

    TMP_Text AddText(string text, float size, Transform parent)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    // A header bar that opens and closes its body when clicked
    void AddFoldout(string header, string body, bool open)
    {
        GameObject section = new GameObject("Section", typeof(RectTransform), typeof(VerticalLayoutGroup));
        section.transform.SetParent(content, false);
        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
        headerGo.transform.SetParent(section.transform, false);
        Image headerImage = headerGo.GetComponent<Image>();
        headerImage.color = headerColor;
        HorizontalLayoutGroup row = headerGo.GetComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(24, 24, 14, 14);
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = false;
        TMP_Text headerText = AddText("", headerSize, headerGo.transform);
        headerText.fontStyle = FontStyles.Bold;

        TMP_Text bodyText = AddText(body, bodySize, section.transform);
        bodyText.margin = new Vector4(48, 10, 24, 14);
        bodyText.lineSpacing = 12f;
        bodyText.gameObject.SetActive(open && body != "");
        SetHeader(headerText, header, open, body != "");

        Button button = headerGo.GetComponent<Button>();
        button.targetGraphic = headerImage;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        if (body == "") return; // nothing to open
        button.onClick.AddListener(() =>
        {
            bool nowOpen = !bodyText.gameObject.activeSelf;
            bodyText.gameObject.SetActive(nowOpen);
            SetHeader(headerText, header, nowOpen, true);
        });
    }

    static void SetHeader(TMP_Text text, string header, bool open, bool canOpen)
    {
        string mark = !canOpen ? " " : open ? "-" : "+";
        text.text = "<color=#8FB8FF>" + mark + "</color>   " + header;
    }

    static string PlanningText(ScoreResult r)
    {
        StringBuilder sb = new StringBuilder();
        Row(sb, "Spacecraft engineering", r.engineering, 24f, false);
        Row(sb, "Launcher capacity", r.deliveryPoints, 7f, true);
        Row(sb, "Spacecraft delta-v", r.craftDvPoints, 7f, true);
        Row(sb, "Power generation", r.powerPoints, 3f, true);
        Row(sb, "Batteries", r.batteryPoints, 3f, true);
        Row(sb, "Water, oxygen and food", r.consumablePoints, 4f, true);
        Row(sb, "Budget savings", r.budgetPoints, 7f, false);
        Row(sb, "Crew training", r.trainingPoints, 8f, false);
        Row(sb, "Site and date (weather)", r.weatherPoints, 6f, false);
        if (r.planningEvents != 0f) sb.AppendLine("Events<pos=70%>" + r.planningEvents.ToString("+0;-0"));
        return sb.ToString().TrimEnd();
    }

    static string MissionText(ScoreResult r)
    {
        StringBuilder sb = new StringBuilder();
        Row(sb, "Flight stages", r.milestonePoints, 24f, false);
        Row(sb, "Minigames", r.minigamePoints, 21f, false);
        Row(sb, "Science", r.sciencePoints, 10f, false);
        if (r.missionEvents != 0f) sb.AppendLine("Events<pos=70%>" + r.missionEvents.ToString("+0;-0"));
        return sb.ToString().TrimEnd();
    }

    // "label ......... points / max", with the points lined up in a column
    static void Row(StringBuilder sb, string label, float points, float max, bool sub)
    {
        string color = sub ? "<color=#9FB0C3>" : "<color=#FFFFFF>";
        sb.AppendLine(color + (sub ? "      " : "") + label + "<pos=70%>" + points.ToString("0.0") + " / " + max.ToString("0") + "</color>");
    }

    public static string BuildReport(ScoreResult r, GameManager gm)
    {
        StringBuilder sb = new StringBuilder();
        if (r.problem != "")
        {
            sb.AppendLine("The score could not be calculated: " + r.problem);
            return sb.ToString();
        }

        // Two separate scores: the diagnostic one (for learning) and the official one (only if the mission is won)
        sb.AppendLine("Learning score: " + r.total.ToString("0.0") + " / 100");
        if (r.status == "SUCCESS") sb.AppendLine("Official score: " + r.competitive.ToString("0.0") + " / 100");
        else if (r.status == "FAILURE") sb.AppendLine("Official score: 0 (the mission " + (r.aborted ? "was aborted" : "failed") + ")");
        else sb.AppendLine("Official score: pending (the flight has not finished)");
        sb.AppendLine();

        if (r.status == "FAILURE")
        {
            // Task 31: flight failure (what happened in flight) is shown apart from incomplete objectives
            sb.AppendLine("<b>What went wrong</b>");
            if (!string.IsNullOrEmpty(r.failureReason)) sb.AppendLine(r.failureReason);
            else if (OnlyScienceFailed(r)) sb.AppendLine("The spacecraft returned safely, but the scientific objectives were incomplete.");
            foreach (string gate in r.failedGates)
            {
                // With an in-flight failure, the missing milestones are a consequence, already explained above
                if (!string.IsNullOrEmpty(r.failureReason) && (gate == "DEPARTURE" || gate == "LUNAR_ORBIT" || gate == "RETURN")) continue;
                sb.AppendLine(GateAdvice.Explain(gate, gm)); // task 35: problem, lesson and fix
            }
            sb.AppendLine();
        }

        sb.AppendLine("<b>Planning: " + r.planning.ToString("0.0") + " / 45</b>");
        Line(sb, "Spacecraft engineering", r.engineering, 24f);
        Line(sb, "  Launcher capacity", r.deliveryPoints, 7f);
        Line(sb, "  Spacecraft delta-v", r.craftDvPoints, 7f);
        Line(sb, "  Power generation", r.powerPoints, 3f);
        Line(sb, "  Batteries", r.batteryPoints, 3f);
        Line(sb, "  Water, oxygen and food", r.consumablePoints, 4f);
        Line(sb, "Budget savings", r.budgetPoints, 7f);
        Line(sb, "Crew training", r.trainingPoints, 8f);
        Line(sb, "Site and date (weather)", r.weatherPoints, 6f);
        if (r.planningEvents != 0f) sb.AppendLine("  Events: " + r.planningEvents.ToString("+0;-0"));

        sb.AppendLine();
        sb.AppendLine("<b>Mission: " + r.mission.ToString("0.0") + " / 55</b>");
        Line(sb, "Flight stages", r.milestonePoints, 24f);
        Line(sb, "Minigames", r.minigamePoints, 21f);
        Line(sb, "Science", r.sciencePoints, 10f);
        if (r.missionEvents != 0f) sb.AppendLine("  Events: " + r.missionEvents.ToString("+0;-0"));

        sb.AppendLine();
        sb.AppendLine("<b>What you learned</b>");
        foreach (string lesson in Lessons(r, gm)) sb.AppendLine("- " + lesson);

        return sb.ToString();
    }

    // Generic lessons of the game (also used by BuildReport)
    public static List<string> Lessons(ScoreResult r, GameManager gm)
    {
        List<string> list = new List<string>();
        if (r.deliveryPoints < 3.5f)
            list.Add("Your spacecraft was very close to the launcher mass limit. Every extra kilogram needs more fuel and a bigger rocket.");
        if (r.craftDvPoints < 3.5f)
            list.Add("Little spare delta-v on the spacecraft: dv = Isp x g0 x ln(m0/mf). If the spacecraft is heavier, the same fuel goes less far.");
        if (r.powerPoints + r.batteryPoints < 3f)
            list.Add("Power was at the limit. Behind the Moon there is no sunlight: the batteries must cover the power draw.");
        if (r.consumablePoints < 2f)
            list.Add("Water, oxygen and food were very tight. Missions carry reserves in case the trip takes longer.");
        if (r.budgetPoints < 3.5f)
            list.Add("You spent almost the whole budget. Real missions keep money for the unexpected.");
        if (r.trainingPoints < 6f)
            list.Add("Train each astronaut in their own role: that way they perform better in the minigames and avoid problems.");
        if (r.weatherPoints < 4f)
            list.Add("You launched in bad weather. Wind and storms increase the risk during departure.");
        if (r.sciencePoints < 7f)
            list.Add("Buying instruments is not enough: you have to run the experiments and send the data to Earth.");
        if (gm != null && gm.flight.HasFailed) list.Add(FailureLesson(gm.flight.failureCode));
        if (gm != null)
        {
            foreach (EventData e in gm.eventsHappened)
            {
                if (e != null && !string.IsNullOrEmpty(e.lesson)) list.Add(e.eventName + ": " + e.lesson);
            }
        }
        if (r.status == "SUCCESS" && r.total >= 90f) list.Add("Excellent! Your decisions were very close to those of a real mission team.");
        return list;
    }

    static void Line(StringBuilder sb, string label, float points, float max)
    {
        sb.AppendLine("  " + label + ": " + points.ToString("0.0") + " / " + max.ToString("0"));
    }

    // Text for each mandatory condition that failed
    public static string GateText(string gate)
    {
        switch (gate)
        {
            case "ROCKET_INVALID": return "The rocket was not valid for the mission.";
            case "CREW_NOT_READY": return "The crew was not complete or trained.";
            case "DELIVERY": return "The launcher could not carry that much mass toward the Moon.";
            case "CRAFT_DELTA_V": return "The spacecraft did not have enough delta-v to enter lunar orbit and return.";
            case "POWER": return "The modules drew more power than the solar arrays generated.";
            case "BATTERY": return "The battery could not last through the periods without sunlight.";
            case "CONSUMABLES": return "There was not enough water, oxygen or food for the whole mission.";
            case "DEPARTURE": return "The departure was not completed.";
            case "LUNAR_ORBIT": return "Lunar orbit was not reached.";
            case "RETURN": return "The crew did not return safely.";
            case "SCIENCE": return "The science goal was not met.";
            case "NO_DOWNLINK": return "The science data was not sent to Earth.";
            case "MISSION_ABORT": return "The mission was aborted before its objectives were completed.";
            case "DEPARTURE_FAILURE": return "The departure failed: the spacecraft could not continue to the Moon.";
            case "LUNAR_ORBIT_FAILURE": return "A critical problem in lunar orbit ended the mission early.";
            case "RETURN_FAILURE": return "The safe-return requirement was not satisfied.";
        }
        return gate;
    }

    // True if the only failed gates are the science objectives (the flight itself went well)
    static bool OnlyScienceFailed(ScoreResult r)
    {
        if (r.failedGates.Count == 0) return false;
        foreach (string gate in r.failedGates)
        {
            if (gate != "SCIENCE" && gate != "NO_DOWNLINK") return false;
        }
        return true;
    }

    // Educational explanation of an in-flight failure (gameplay rules, not real NASA certification limits)
    public static string FailureLesson(string code)
    {
        switch (code)
        {
            case "DEPARTURE_FAILURE":
                return "Departure is the most demanding part of the flight. A valid spacecraft and a pilot trained in piloting make a safe ascent much more likely; real missions abort rather than continue with a dangerous trajectory.";
            case "LUNAR_ORBIT_FAILURE":
                return "Far from Earth the crew must fix problems on its own. A skilled, trained engineer and spare power and consumables give the crew time to recover; when systems cannot be restored, the safest choice is to end the mission and come home.";
            case "RETURN_FAILURE":
                return "Coming home needs precise flying and working systems until splashdown. Planning margins (delta-v, power, consumables) and a trained crew protect the return.";
        }
        return "Every phase depends on the one before: planning, training and in-flight decisions all add up.";
    }
}
