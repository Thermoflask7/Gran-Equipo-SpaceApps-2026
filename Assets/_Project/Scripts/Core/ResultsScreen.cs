using System.Text;
using TMPro;
using UnityEngine;

// 03_Resultados screen (task 16): calculates the final V16 score only once,
// stores it in GameManager and shows the breakdown with an explanation of each decision.
// Menu: SpaceApps -> Results screen -> Add to open scene.
public class ResultsScreen : MonoBehaviour
{
    public TMP_Text title;   // "Mission accomplished", "Mission failed"...
    public TMP_Text score;   // the big number
    public TMP_Text report;  // breakdown and lessons

    void Start()
    {
        Show();
    }

    public void Show()
    {
        ScoreResult r = MissionScoring.SaveFinalScore();
        if (title != null) title.text = TitleFor(r);
        if (score != null) score.text = r.total.ToString("0") + " / 100";
        if (report != null) report.text = BuildReport(r, GameManager.Instance);
    }

    static string TitleFor(ScoreResult r)
    {
        if (r.status == "SUCCESS") return "Mission accomplished!";
        if (r.status == "FAILURE") return "Mission failed";
        if (r.status == "PENDING") return "Mission in progress";
        return "Invalid result";
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
        else if (r.status == "FAILURE") sb.AppendLine("Official score: 0 (the mission failed)");
        else sb.AppendLine("Official score: pending (the flight has not finished)");
        sb.AppendLine();

        if (r.status == "FAILURE")
        {
            sb.AppendLine("<b>What went wrong</b>");
            foreach (string gate in r.failedGates) sb.AppendLine("- " + GateText(gate));
            sb.AppendLine();
        }

        sb.AppendLine("<b>Planning: " + r.planning.ToString("0.0") + " / 45</b>");
        Line(sb, "Spacecraft engineering", r.engineering, 24f);
        Line(sb, "  SLS capacity", r.deliveryPoints, 7f);
        Line(sb, "  Orion delta-v", r.craftDvPoints, 7f);
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
        if (r.deliveryPoints < 3.5f)
            sb.AppendLine("- Your spacecraft was very close to the SLS mass limit. Every extra kilogram needs more fuel and a bigger rocket.");
        if (r.craftDvPoints < 3.5f)
            sb.AppendLine("- Little spare delta-v on Orion: dv = Isp x g0 x ln(m0/mf). If the spacecraft is heavier, the same fuel goes less far.");
        if (r.powerPoints + r.batteryPoints < 3f)
            sb.AppendLine("- Power was at the limit. Behind the Moon there is no sunlight: the batteries must cover the power draw.");
        if (r.consumablePoints < 2f)
            sb.AppendLine("- Water, oxygen and food were very tight. Missions carry reserves in case the trip takes longer.");
        if (r.budgetPoints < 3.5f)
            sb.AppendLine("- You spent almost the whole budget. Real missions keep money for the unexpected.");
        if (r.trainingPoints < 6f)
            sb.AppendLine("- Train each astronaut in their own role: that way they perform better in the minigames and avoid problems.");
        if (r.weatherPoints < 4f)
            sb.AppendLine("- You launched in bad weather. Wind and storms increase the risk during departure.");
        if (r.sciencePoints < 7f)
            sb.AppendLine("- Buying instruments is not enough: you have to run the experiments and send the data to Earth.");
        if (gm != null)
        {
            foreach (EventData e in gm.eventsHappened)
            {
                if (e != null && !string.IsNullOrEmpty(e.lesson)) sb.AppendLine("- " + e.eventName + ": " + e.lesson);
            }
        }
        if (r.status == "SUCCESS" && r.total >= 90f) sb.AppendLine("- Excellent! Your decisions were very close to those of a real mission team.");

        return sb.ToString();
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
            case "DELIVERY": return "The SLS could not carry that much mass to translunar injection.";
            case "CRAFT_DELTA_V": return "Orion did not have enough delta-v to enter lunar orbit and return.";
            case "POWER": return "The modules drew more power than the solar arrays generated.";
            case "BATTERY": return "The battery could not last through the periods without sunlight.";
            case "CONSUMABLES": return "There was not enough water, oxygen or food for the whole mission.";
            case "DEPARTURE": return "The departure was not completed.";
            case "LUNAR_ORBIT": return "Lunar orbit was not reached.";
            case "RETURN": return "The crew did not return safely.";
            case "SCIENCE": return "The science goal was not met.";
            case "NO_DOWNLINK": return "The science data was not sent to Earth.";
        }
        return gate;
    }
}
