using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mission select panel of 00_Menu (task 26): one card per mission (DestinationData).
// Locked missions (DestinationData.locked) are shown as "Coming soon" and cannot be chosen.
// Clicking a card shows its real data; "Start mission" sets GameManager.mission and goes to planning.
// Built with SpaceApps > Build main menu.
public class MissionSelect : MonoBehaviour
{
    public List<DestinationData> missions = new List<DestinationData>();
    public RectTransform cardRow;   // HorizontalLayoutGroup: the cards are created here
    public TMP_Text details;        // data of the selected mission
    public Button startButton;
    public Sprite cardSprite;       // rounded box (UISprite)
    public Sprite planetSprite;     // circle (Knob)

    public Color cardColor = new Color(0.12f, 0.18f, 0.32f, 0.95f);
    public Color selectedColor = new Color(0.22f, 0.4f, 0.75f, 1f);

    DestinationData selected;
    readonly List<Image> cards = new List<Image>();

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (cardRow == null) return;
        for (int i = cardRow.childCount - 1; i >= 0; i--) Destroy(cardRow.GetChild(i).gameObject);
        cards.Clear();

        selected = null;
        foreach (DestinationData m in missions)
        {
            if (m == null) continue;
            AddCard(m);
            if (selected == null && !m.locked) selected = m; // the first available one starts selected
        }
        UpdateSelection();
    }

    void AddCard(DestinationData mission)
    {
        GameObject card = new GameObject(mission.name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        card.transform.SetParent(cardRow, false);
        Image image = card.GetComponent<Image>();
        image.sprite = cardSprite;
        image.type = Image.Type.Sliced;
        image.color = cardColor;
        VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 30, 30);
        layout.spacing = 18;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Planet: a colored circle
        GameObject planet = new GameObject("Planet", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        planet.transform.SetParent(card.transform, false);
        Image planetImage = planet.GetComponent<Image>();
        planetImage.sprite = planetSprite;
        planetImage.preserveAspect = true;
        planetImage.color = PlanetColor(mission);
        planetImage.raycastTarget = false;
        planet.GetComponent<LayoutElement>().preferredHeight = 170;

        AddText(card.transform, MissionName(mission), 46, FontStyles.Bold, Color.white);
        if (!string.IsNullOrEmpty(mission.missionName)) AddText(card.transform, ShortName(mission), 28, FontStyles.Normal, new Color(0.75f, 0.82f, 0.92f));
        if (mission.locked) AddText(card.transform, "Coming soon", 30, FontStyles.Italic, new Color(0.65f, 0.7f, 0.78f));
        else AddText(card.transform, "Available", 30, FontStyles.Bold, new Color(0.55f, 0.9f, 0.6f));

        Button button = card.GetComponent<Button>();
        button.targetGraphic = image;
        if (mission.locked)
        {
            button.interactable = false;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;
        }
        else
        {
            button.onClick.AddListener(() => Select(mission));
        }
        cards.Add(image);
    }

    void AddText(Transform parent, string text, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
    }

    public void Select(DestinationData mission)
    {
        if (mission == null || mission.locked) return;
        selected = mission;
        UpdateSelection();
    }

    void UpdateSelection()
    {
        int i = 0;
        foreach (DestinationData m in missions)
        {
            if (m == null) continue;
            cards[i].color = m == selected ? selectedColor : cardColor;
            i++;
        }
        if (startButton != null) startButton.interactable = selected != null;
        if (details == null) return;
        if (selected == null)
        {
            details.text = "No mission available.";
            return;
        }
        // Only data from the mission asset
        details.text = "<b>" + MissionName(selected) + "</b>   <color=#9FB0C3>" + selected.destinationName + "</color>\n" + selected.description + "\n\n"
            + "<color=#8FB8FF>Crew</color> " + selected.crewCount
            + "     <color=#8FB8FF>Duration</color> " + selected.missionDays + " days"
            + "     <color=#8FB8FF>Orion delta-v needed</color> " + selected.requiredDeltaV.ToString("N0") + " m/s"
            + "     <color=#8FB8FF>Science goal</color> " + selected.minSciencePoints.ToString("0") + " points";
    }

    // "Start mission" button
    public void StartMission()
    {
        if (selected == null) return;
        GameManager gm = GameManager.Instance;
        gm.ResetGame();
        gm.mission = selected;
        gm.GoToScene(Scenes.Planning);
    }

    // "Selene"; missions without a name use their destination
    static string MissionName(DestinationData m)
    {
        if (!string.IsNullOrEmpty(m.missionName)) return m.missionName;
        return ShortName(m);
    }

    // "Crewed lunar orbit (Artemis)" -> "Crewed lunar orbit" on the card; the full name is in the details
    static string ShortName(DestinationData m)
    {
        string name = string.IsNullOrEmpty(m.destinationName) ? m.name : m.destinationName;
        int cut = name.IndexOf(" (");
        if (cut > 0) name = name.Substring(0, cut);
        return name;
    }

    static Color PlanetColor(DestinationData m)
    {
        string n = (m.destinationName + " " + m.name).ToLower();
        if (n.Contains("mars")) return new Color(0.85f, 0.42f, 0.25f);
        if (n.Contains("europa")) return new Color(0.82f, 0.88f, 0.95f);
        return new Color(0.78f, 0.78f, 0.8f); // Moon
    }
}
