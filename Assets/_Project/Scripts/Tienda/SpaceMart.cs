using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SpaceMart app (task 8): online shop on the desk computer to buy and sell parts.
// It lists the Artemis catalog (Data/Piezas/ART-*.asset), spends credits through GameManager
// and shows what the bought parts would give with RocketPhysics: mass, Orion delta-v, power,
// consumables and science. Assembling the rocket (gm.rocketParts) is the job of the rocket model (task 9).
// The prefab is created with the menu SpaceApps > SpaceMart > Create prefab.
// The computer desktop (task 7) opens it with SetActive(true); the X button calls Close().
// The tabs on top filter the list by category (SetFilter), so the catalog is easier to read.
public class SpaceMart : MonoBehaviour
{
    [Header("Data")]
    public List<PartData> catalog = new List<PartData>();
    public DestinationData mission; // used only if GameManager has no mission yet

    [Header("UI")]
    public Transform listContent;     // content of the scroll list
    public SpaceMartItem rowTemplate; // inactive row, cloned once per part
    public TMP_Text moneyText;
    public TMP_Text statsText;
    public TMP_Text feedbackText;

    [Header("Category tabs (same order as Filters)")]
    public List<Image> filterTabs = new List<Image>();

    // Tab names. "All" shows everything; the rest match FilterOf(type).
    public static readonly string[] Filters = { "All", "Launcher", "Orion", "Science", "Crew & modules", "Power" };

    static readonly Color TabColor = new Color(0.2f, 0.24f, 0.3f, 1f);
    static readonly Color TabSelectedColor = new Color(1f, 0.6f, 0f, 1f);

    string currentFilter = "All";

    const string OkColor = "#7CFC7C";
    const string BadColor = "#FF6B5B";

    List<SpaceMartItem> rows = new List<SpaceMartItem>();

    void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm.mission == null) gm.mission = mission; // so the scene can be tested on its own

        catalog.Sort(CompareParts);
        foreach (PartData part in catalog)
        {
            if (part == null) continue;
            SpaceMartItem row = Instantiate(rowTemplate, listContent);
            row.gameObject.SetActive(true);
            row.Setup(this, part);
            rows.Add(row);
        }
        if (feedbackText != null) feedbackText.text = "";
        Refresh();
    }

    void OnEnable()
    {
        // Money can change while the app is closed (events), so refresh when it opens again
        if (rows.Count > 0) Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // Called by the category tabs (filter = one of Filters)
    public void SetFilter(string filter)
    {
        currentFilter = filter;
        Refresh();
    }

    // Tab where each part type appears
    public static string FilterOf(PartType type)
    {
        if (type == PartType.LaunchService) return "Launcher";
        if (type == PartType.Capsule || type == PartType.ServiceModule || type == PartType.AbortSystem) return "Orion";
        if (type == PartType.Instrument || type == PartType.Antenna) return "Science";
        if (type == PartType.SolarPanel || type == PartType.Battery) return "Power";
        return "Crew & modules"; // Module and anything else
    }

    // ---------- Buying and selling ----------

    public bool IsOwned(PartData part)
    {
        return GameManager.Instance.boughtParts.Contains(part);
    }

    // The SLS you already bought (null if none)
    public PartData OwnedLauncher()
    {
        return RocketPhysics.LaunchService(GameManager.Instance.boughtParts);
    }

    // Credits needed to buy it. Buying an SLS swaps the one you have, which is refunded.
    public float CostOf(PartData part)
    {
        float cost = part.price;
        PartData oldLauncher = OwnedLauncher();
        if (part.type == PartType.LaunchService && oldLauncher != null) cost -= oldLauncher.price;
        return cost;
    }

    // Only one unit of each part (RocketPhysics.MaxCopiesPerPart = 1)
    public bool CanBuy(PartData part)
    {
        return !IsOwned(part) && GameManager.Instance.CanAfford(CostOf(part));
    }

    public void Buy(PartData part)
    {
        GameManager gm = GameManager.Instance;
        if (IsOwned(part)) return;
        if (!gm.CanAfford(CostOf(part)))
        {
            Say("Not enough credits for " + part.partName + ".");
            return;
        }

        string text = "Bought " + part.partName + " (-" + Credits(part.price) + ").";
        PartData oldLauncher = OwnedLauncher();
        if (part.type == PartType.LaunchService && oldLauncher != null)
        {
            gm.SellPart(oldLauncher);
            text = "Swapped " + oldLauncher.partName + " for " + part.partName + ".";
        }

        gm.BuyPart(part);
        UpdateRocketValid(gm);
        Say(text);
        Refresh();
    }

    public void Sell(PartData part)
    {
        GameManager gm = GameManager.Instance;
        if (!IsOwned(part)) return;
        gm.SellPart(part);
        UpdateRocketValid(gm);
        Say("Sold " + part.partName + " (+" + Credits(part.price) + ").");
        Refresh();
    }

    // SellPart also removes the part from the assembled rocket, so its check is redone
    static void UpdateRocketValid(GameManager gm)
    {
        gm.rocketValid = RocketPhysics.IsRocketValid(gm.rocketParts, gm.mission);
    }

    void Say(string text)
    {
        if (feedbackText != null) feedbackText.text = text;
    }

    // ---------- Screen ----------

    public void Refresh()
    {
        GameManager gm = GameManager.Instance;
        if (moneyText != null) moneyText.text = "Credits: " + Credits(gm.money);
        foreach (SpaceMartItem row in rows)
        {
            row.Refresh();
            bool visible = currentFilter == "All" || FilterOf(row.Part.type) == currentFilter;
            row.gameObject.SetActive(visible);
        }
        for (int i = 0; i < filterTabs.Count && i < Filters.Length; i++)
        {
            if (filterTabs[i] != null) filterTabs[i].color = Filters[i] == currentFilter ? TabSelectedColor : TabColor;
        }
        if (statsText != null) statsText.text = BuildStats(gm.boughtParts, gm.mission);
    }

    // Summary of the bought parts, with OK / LOW for each mission requirement
    public static string BuildStats(List<PartData> parts, DestinationData mission)
    {
        string s = "<b>Your cart</b>\n";
        s += parts.Count + " parts, " + Credits(RocketPhysics.TotalPrice(parts)) + " spent\n\n";

        PartData sls = RocketPhysics.LaunchService(parts);
        if (sls == null)
            s += Line("Launcher", "none (choose an SLS)", false);
        else if (mission != null)
            s += Line("Mass to the Moon", Kg(RocketPhysics.TliMass(parts, mission)) + " of " + Kg(sls.payloadCapacity),
                RocketPhysics.DeliveryMargin(parts, mission) >= 0f);

        if (mission != null)
        {
            float dv = RocketPhysics.SpacecraftDeltaV(parts);
            s += Line("Orion delta-v", dv.ToString("N0") + " of " + mission.requiredDeltaV.ToString("N0") + " m/s",
                dv >= mission.requiredDeltaV);
        }

        float balance = RocketPhysics.PowerBalance(parts);
        s += Line("Power in sunlight", Signed(balance) + " W spare", balance >= 0f && parts.Count > 0);

        float neededWh = RocketPhysics.PowerConsumption(parts) * RocketPhysics.OutageHours;
        s += Line("Battery", RocketPhysics.BatteryWh(parts).ToString("N0") + " of " + neededWh.ToString("N0")
            + " Wh (" + RocketPhysics.OutageHours + " h in shadow)", RocketPhysics.BatteryCoversOutage(parts) && parts.Count > 0);

        if (mission != null)
        {
            float water = RocketPhysics.WaterMargin(parts, mission);
            float oxygen = RocketPhysics.OxygenMargin(parts, mission);
            float food = RocketPhysics.FoodMargin(parts, mission);
            s += Line("Water", Signed(water) + " kg spare", water >= 0f);
            s += Line("Oxygen", Signed(oxygen) + " kg spare", oxygen >= 0f);
            s += Line("Food", Signed(food) + " kg spare", food >= 0f);

            float science = RocketPhysics.SciencePoints(parts);
            s += Line("Science", science.ToString("0") + " of " + mission.minSciencePoints.ToString("0") + " points",
                science >= mission.minSciencePoints);

            float seconds = RocketPhysics.DownlinkSeconds(parts);
            string downlink;
            if (float.IsInfinity(seconds)) downlink = "no antenna";
            else if (seconds <= 0f) downlink = "no data yet";
            else downlink = (seconds / 60f).ToString("N0") + " of " + (mission.maxDownlinkSeconds / 60f).ToString("N0") + " min";
            s += Line("Send data to Earth", downlink, seconds > 0f && seconds <= mission.maxDownlinkSeconds);
        }

        string problem = RocketPhysics.ValidationMessage(parts, mission);
        if (problem == "")
            s += "\n<color=" + OkColor + ">These parts make a valid spacecraft. Assemble it on the rocket model.</color>";
        else
            s += "\n<b>Next problem:</b> " + problem;
        return s;
    }

    static string Line(string label, string value, bool ok)
    {
        string tag = ok ? "<color=" + OkColor + ">OK</color>" : "<color=" + BadColor + ">LOW</color>";
        return label + ": " + value + "  " + tag + "\n";
    }

    // ---------- Helpers (also used by SpaceMartItem) ----------

    public static string Credits(float amount)
    {
        return amount.ToString("N0") + " cr";
    }

    static string Kg(float kg)
    {
        return kg.ToString("N0") + " kg";
    }

    static string Signed(float value)
    {
        return (value >= 0f ? "+" : "") + value.ToString("N0");
    }

    public static string CategoryName(PartType type)
    {
        if (type == PartType.LaunchService) return "Launcher (choose one)";
        if (type == PartType.Capsule || type == PartType.ServiceModule || type == PartType.AbortSystem) return "Orion (required)";
        if (type == PartType.Module) return "Module";
        if (type == PartType.SolarPanel) return "Solar panel";
        if (type == PartType.Battery) return "Battery";
        if (type == PartType.Antenna) return "Antenna";
        if (type == PartType.Instrument) return "Science";
        if (type == PartType.Engine) return "Engine";
        if (type == PartType.FuelTank) return "Fuel tank";
        return type.ToString();
    }

    // Short list of the numbers that matter for this part
    public static string PartFacts(PartData p)
    {
        List<string> facts = new List<string>();
        if (p.payloadCapacity > 0f) facts.Add("carries " + Kg(p.payloadCapacity) + " to the Moon");
        if (p.dryMass + p.fuelMass > 0f) facts.Add(Kg(p.dryMass + p.fuelMass));
        if (p.fuelMass > 0f) facts.Add(Kg(p.fuelMass) + " propellant, Isp " + p.isp.ToString("0") + " s");
        if (p.power > 0f) facts.Add("makes " + p.power.ToString("N0") + " W");
        if (p.powerDraw > 0f) facts.Add("uses " + p.powerDraw.ToString("N0") + " W");
        if (p.batteryWh > 0f) facts.Add("stores " + p.batteryWh.ToString("N0") + " Wh");
        if (p.crewCapacity > 0) facts.Add(p.crewCapacity + " crew, " + p.maxMissionDays + " days");
        if (p.waterKg > 0f) facts.Add("+" + Kg(p.waterKg) + " water");
        if (p.oxygenKg > 0f) facts.Add("+" + Kg(p.oxygenKg) + " oxygen");
        if (p.foodKg > 0f) facts.Add("+" + Kg(p.foodKg) + " food");
        if (p.sciencePoints > 0f) facts.Add(p.sciencePoints.ToString("0") + " science points");
        if (p.dataRateBps > 0f) facts.Add("sends " + (p.dataRateBps / 1000000f).ToString("0.##") + " Mbit/s");
        if (p.experimentDataBits > 0f) facts.Add((p.experimentDataBits / 8000000f).ToString("0.#") + " MB of data");
        return string.Join(", ", facts);
    }

    // Launcher first, then Orion, then the optional parts; cheaper first inside each group
    static int CompareParts(PartData a, PartData b)
    {
        if (a == null || b == null) return 0;
        int byCategory = CategoryOrder(a.type).CompareTo(CategoryOrder(b.type));
        if (byCategory != 0) return byCategory;
        return a.price.CompareTo(b.price);
    }

    static int CategoryOrder(PartType type)
    {
        if (type == PartType.LaunchService) return 0;
        if (type == PartType.Capsule) return 1;
        if (type == PartType.ServiceModule) return 2;
        if (type == PartType.AbortSystem) return 3;
        if (type == PartType.Instrument) return 4;
        if (type == PartType.Antenna) return 5;
        if (type == PartType.Module) return 6;
        if (type == PartType.SolarPanel) return 7;
        if (type == PartType.Battery) return 8;
        return 9;
    }
}
