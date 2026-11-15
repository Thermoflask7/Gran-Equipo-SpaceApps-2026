using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mission select panel of 00_Menu (task 26): one card per mission (DestinationData), ordered by difficulty.
// Any card can be clicked to see its info; locked missions (DestinationData.locked) say "Coming soon"
// and cannot be started. "Start mission" sets GameManager.mission and goes to planning.
// Built with SpaceApps > Build main menu.
public class MissionSelect : MonoBehaviour
{
    public List<DestinationData> missions = new List<DestinationData>();
    public RectTransform cardRow;      // HorizontalLayoutGroup: the cards are created here
    public RectTransform detailsArea;  // VerticalLayoutGroup: info of the selected mission
    public Button startButton;
    public Sprite cardSprite;          // rounded box (UISprite)
    public Sprite planetSprite;        // circle (Knob)

    public Color cardColor = new Color(0.12f, 0.18f, 0.32f, 0.95f);
    public Color selectedColor = new Color(0.22f, 0.4f, 0.75f, 1f);
    public Color tileColor = new Color(0.1f, 0.15f, 0.28f, 0.95f);

    DestinationData selected;
    readonly List<Image> cards = new List<Image>();
    readonly List<DestinationData> cardMissions = new List<DestinationData>();

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (cardRow == null) return;
        Clear(cardRow);
        cards.Clear();
        cardMissions.Clear();

        selected = null;
        foreach (DestinationData m in missions)
        {
            if (m == null) continue;
            AddCard(m);
            if (selected == null && !m.locked) selected = m; // the first available one starts selected
        }
        UpdateSelection();
    }

    public void Select(DestinationData mission)
    {
        if (mission == null) return;
        selected = mission;
        UpdateSelection();
    }

    // "Start mission" button
    public void StartMission()
    {
        if (selected == null || selected.locked) return;
        GameManager gm = GameManager.Instance;
        gm.ResetGame();
        gm.mission = selected;
        gm.GoToScene(Scenes.Planning);
    }

    // ---------- Cards ----------

    void AddCard(DestinationData mission)
    {
        GameObject card = new GameObject(mission.name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        card.transform.SetParent(cardRow, false);
        Image image = card.GetComponent<Image>();
        image.sprite = cardSprite;
        image.type = Image.Type.Sliced;
        VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 22, 22);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddText(card.transform, DifficultyText(mission), 26, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        // Planet: a colored circle (faded if the mission is locked)
        GameObject planet = new GameObject("Planet", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        planet.transform.SetParent(card.transform, false);
        Image planetImage = planet.GetComponent<Image>();
        planetImage.sprite = planetSprite;
        planetImage.preserveAspect = true;
        Color planetColor = PlanetColor(mission);
        if (mission.locked) planetColor.a = 0.4f;
        planetImage.color = planetColor;
        planetImage.raycastTarget = false;
        planet.GetComponent<LayoutElement>().preferredHeight = 130;

        AddText(card.transform, MissionName(mission), 40, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        if (!string.IsNullOrEmpty(mission.tagline))
            AddText(card.transform, mission.tagline, 24, FontStyles.Normal, new Color(0.75f, 0.82f, 0.92f), TextAlignmentOptions.Center);
        if (mission.locked) AddText(card.transform, "Coming soon", 26, FontStyles.Italic, new Color(0.65f, 0.7f, 0.78f), TextAlignmentOptions.Center);
        else AddText(card.transform, "Available", 26, FontStyles.Bold, new Color(0.55f, 0.9f, 0.6f), TextAlignmentOptions.Center);

        Button button = card.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => Select(mission));
        cards.Add(image);
        cardMissions.Add(mission);
    }

    void UpdateSelection()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Color c = cardMissions[i] == selected ? selectedColor : cardColor;
            if (cardMissions[i].locked) c.a *= 0.6f;
            cards[i].color = c;
        }
        if (startButton != null) startButton.interactable = selected != null && !selected.locked;
        ShowDetails();
    }

    // ---------- Details (styled like the Results screen) ----------

    void ShowDetails()
    {
        if (detailsArea == null) return;
        Clear(detailsArea);
        if (selected == null)
        {
            AddText(detailsArea, "No mission available.", 30, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
            return;
        }

        AddText(detailsArea, "<b>" + MissionName(selected) + "</b>   <size=75%>" + DifficultyText(selected) + "</size>",
            44, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
        AddText(detailsArea, selected.description, 28, FontStyles.Normal, new Color(0.85f, 0.9f, 0.96f), TextAlignmentOptions.Left);

        if (selected.locked)
        {
            AddText(detailsArea, "<color=#9FB0C3><i>Coming soon: this mission cannot be played yet.</i></color>", 28, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
            return;
        }

        // Stat tiles: big number + label (only data from the mission asset)
        GameObject row = new GameObject("Stats", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(detailsArea, false);
        row.GetComponent<LayoutElement>().preferredHeight = 130;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        AddTile(row.transform, selected.crewCount.ToString(), "Crew");
        AddTile(row.transform, selected.missionDays.ToString(), "Days");
        AddTile(row.transform, selected.requiredDeltaV.ToString("N0") + " m/s", "Spacecraft delta-v");
        AddTile(row.transform, selected.minSciencePoints.ToString("0"), "Science points");
    }

    void AddTile(Transform parent, string value, string label)
    {
        GameObject tile = new GameObject("Tile", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        tile.transform.SetParent(parent, false);
        Image image = tile.GetComponent<Image>();
        image.sprite = cardSprite;
        image.type = Image.Type.Sliced;
        image.color = tileColor;
        image.raycastTarget = false;
        VerticalLayoutGroup layout = tile.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 14, 14);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        AddText(tile.transform, value, 44, FontStyles.Bold, new Color(0.56f, 0.72f, 1f), TextAlignmentOptions.Center);
        AddText(tile.transform, label, 24, FontStyles.Normal, new Color(0.62f, 0.69f, 0.76f), TextAlignmentOptions.Center);
    }

    // ---------- Helpers ----------

    void AddText(Transform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = alignment;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
    }

    static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
    }

    static string DifficultyText(DestinationData m)
    {
        if (m.difficulty <= 1) return "<color=#6FCF97>EASY</color>";
        if (m.difficulty == 2) return "<color=#F2C94C>MEDIUM</color>";
        return "<color=#EB5757>HARD</color>";
    }

    // "Selene"; missions without a name use their destination
    static string MissionName(DestinationData m)
    {
        if (!string.IsNullOrEmpty(m.missionName)) return m.missionName;
        return ShortName(m);
    }

    // Destination without the text in parentheses: "Crewed lunar orbit (x)" -> "Crewed lunar orbit"
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
        if (n.Contains("satellite") || n.Contains("earth")) return new Color(0.3f, 0.55f, 0.95f); // Earth
        return new Color(0.78f, 0.78f, 0.8f); // Moon
    }
}
