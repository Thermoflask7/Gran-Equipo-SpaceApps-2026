using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Unity menu: SpaceApps > Build main menu
// Builds 00_Menu: removes the greybox state text and Next button, and creates the space background,
// the game title, the 5 buttons and the Mission select (task 26), Settings and Space Apps panels, already
// wired up. It also creates the locked missions (Mars, Europa) if they do not exist. It can be run again:
// it deletes and recreates only what it creates. Then adjust colors and texts by hand.
public static class MainMenuBuilder
{
    static readonly Color ButtonColor = new Color(0.16f, 0.27f, 0.5f, 0.9f);
    static readonly Color BoxColor = new Color(0.05f, 0.07f, 0.14f, 0.95f);

    // Placeholder name of the game: change it here and run the menu again
    const string GameTitle = "Space Mission Simulator";
    const string GameSubtitle = "Plan it. Build it. Fly it.";

    const string DestinationsFolder = "Assets/_Project/Data/Destinos/";

    static Sprite RoundedSprite
    {
        get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); }
    }

    [MenuItem("SpaceApps/Build main menu")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + Scenes.Menu + ".unity");

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Main menu", "No Canvas found in 00_Menu.", "OK");
            return;
        }
        Transform root = canvas.transform;
        // The Spanish names are the ones created by older versions of this menu (so they also get replaced).
        foreach (string n in new[] { "State", "NextButton", "Buttons", "SettingsPanel", "SpaceAppsPanel",
            "Estado", "BotonSiguiente", "Botones", "PanelAjustes", "PanelSpaceApps",
            "SpaceBackground", "LaunchBackground", "GameTitle", "GameSubtitle", "MissionPanel" })
        {
            Transform t = root.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        MainMenu menu = canvas.GetComponent<MainMenu>();
        if (menu == null) menu = canvas.gameObject.AddComponent<MainMenu>();

        // ---------- Space background and title ----------
        // The old flat background and the greybox title ("Game") are hidden, not deleted
        foreach (string n in new[] { "Background", "Titulo" })
        {
            Transform t = root.Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
        // Animated background: sky, hills and a rocket lifting off (drawn in code by LaunchBackground)
        GameObject bg = new GameObject("LaunchBackground", typeof(RectTransform), typeof(LaunchBackground));
        bg.transform.SetParent(root, false);
        Stretch((RectTransform)bg.transform);
        bg.transform.SetAsFirstSibling();

        TextMeshProUGUI title = CreateText(root, "GameTitle", GameTitle, 110);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.8f, 0.88f, 1f);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1600, 150));
        TextMeshProUGUI subtitle = CreateText(root, "GameSubtitle", GameSubtitle, 40);
        subtitle.color = new Color(0.62f, 0.7f, 0.8f);
        Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(1600, 60));

        // ---------- Buttons ----------
        GameObject column = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        column.transform.SetParent(root, false);
        Place((RectTransform)column.transform, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(580, 560));
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 22;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;

        Button play = CreateButton(column.transform, "PlayButton", "Start game", 44);
        Button creative = CreateButton(column.transform, "CreativeModeButton", "Creative mode (coming soon)", 36);
        Button settings = CreateButton(column.transform, "SettingsButton", "Settings", 44);
        Button spaceApps = CreateButton(column.transform, "SpaceAppsButton", "Space Apps", 44);
        Button quit = CreateButton(column.transform, "QuitButton", "Quit game", 44);
        creative.interactable = false;

        // ---------- Panels ----------
        Transform missionBody;
        GameObject missionPanel = CreatePanel(root, "MissionPanel", "Choose your mission", menu, out missionBody, new Vector2(1600, 940));
        BuildMissionSelect(missionPanel, missionBody);

        Transform settingsBody;
        GameObject settingsPanel = CreatePanel(root, "SettingsPanel", "Settings", menu, out settingsBody, new Vector2(1300, 850));
        BuildSettings(settingsPanel, settingsBody);

        Transform infoBody;
        GameObject infoPanel = CreatePanel(root, "SpaceAppsPanel", "NASA Space Apps Challenge 2026", menu, out infoBody, new Vector2(1300, 850));
        TextMeshProUGUI info = CreateText(infoBody, "Text",
            "<b>The challenge</b>\n" +
            "Space Mission Design Game, proposed by the Canadian Space Agency: a game to understand how a space mission is planned and carried out.\n\n" +
            "<b>Our project</b>\n" +
            "You plan a crewed lunar orbit mission: you read the mission, choose and train the crew, buy parts and assemble the rocket, and pick the launch site and date. Then you fly the mission: departure, lunar orbit and return, solving minigames and events.\n\n" +
            "<b>The team</b>\n" +
            "[Team name and the 5 members]",
            32);
        info.alignment = TextAlignmentOptions.TopLeft;
        Stretch(info.rectTransform);

        // ---------- Connections ----------
        menu.panels = new[] { missionPanel, settingsPanel, infoPanel };
        UnityEventTools.AddObjectPersistentListener<GameObject>(play.onClick, menu.ShowPanel, missionPanel); // choose the mission first
        UnityEventTools.AddObjectPersistentListener<GameObject>(settings.onClick, menu.ShowPanel, settingsPanel);
        UnityEventTools.AddObjectPersistentListener<GameObject>(spaceApps.onClick, menu.ShowPanel, infoPanel);
        UnityEventTools.AddVoidPersistentListener(quit.onClick, menu.Quit);

        missionPanel.SetActive(false);
        settingsPanel.SetActive(false);
        infoPanel.SetActive(false);

        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main menu built in 00_Menu.");
    }

    static void BuildSettings(GameObject panel, Transform body)
    {
        GameSettings gs = panel.AddComponent<GameSettings>();
        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };

        TextMeshProUGUI volLabel = CreateText(body, "VolumeText", "Volume", 40);
        volLabel.alignment = TextAlignmentOptions.Left;
        Place(volLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(900, 70));

        GameObject sliderGo = DefaultControls.CreateSlider(res);
        sliderGo.name = "VolumeSlider";
        sliderGo.transform.SetParent(body, false);
        Place((RectTransform)sliderGo.transform, new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(900, 40));
        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject toggleGo = DefaultControls.CreateToggle(res);
        toggleGo.name = "FullscreenToggle";
        toggleGo.transform.SetParent(body, false);
        Transform oldLabel = toggleGo.transform.Find("Label");
        if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject);
        TextMeshProUGUI tLabel = CreateText(toggleGo.transform, "Label", "Fullscreen", 16);
        tLabel.alignment = TextAlignmentOptions.Left;
        tLabel.rectTransform.anchorMin = Vector2.zero;
        tLabel.rectTransform.anchorMax = Vector2.one;
        tLabel.rectTransform.offsetMin = new Vector2(28, 0);
        tLabel.rectTransform.offsetMax = Vector2.zero;
        Place((RectTransform)toggleGo.transform, new Vector2(0.5f, 1f), new Vector2(0, -260), new Vector2(360, 24));
        toggleGo.transform.localScale = Vector3.one * 2.5f;
        Toggle toggle = toggleGo.GetComponent<Toggle>();

        gs.volumeSlider = slider;
        gs.fullscreenToggle = toggle;
        UnityEventTools.AddPersistentListener<float>(slider.onValueChanged, gs.SetVolume);
        UnityEventTools.AddPersistentListener<bool>(toggle.onValueChanged, gs.SetFullscreen);
    }

    // Task 26: cards row + details + "Start mission" (the cards are created at runtime by MissionSelect)
    static void BuildMissionSelect(GameObject panel, Transform body)
    {
        MissionSelect select = panel.AddComponent<MissionSelect>();
        select.cardSprite = RoundedSprite;
        select.planetSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        // Ordered from easy to hard. Only Selene can be played; the others are "Coming soon".
        select.missions.Add(LockedMission("MissionSatellite", "Eos", "Satellite launch", "Put a satellite in orbit", "Launch an uncrewed rocket and put a satellite into orbit around the Earth.", 1));
        select.missions.Add(Selene());
        select.missions.Add(LockedMission("MissionMoonLanding", "Endymion", "Moon landing", "Land a crew on the Moon", "Land a crew on the surface of the Moon and bring them back home.", 3));
        select.missions.Add(LockedMission("MissionMars", "Deimos", "Mars landing", "Land a crew on Mars", "The longest trip: land a crew on Mars.", 3));

        GameObject row = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(body, false);
        RectTransform rowRt = (RectTransform)row.transform;
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.sizeDelta = new Vector2(0, 400);
        rowRt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 40;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        select.cardRow = rowRt;

        // Info of the selected mission (title, description and stat tiles, created at runtime)
        GameObject details = new GameObject("Details", typeof(RectTransform), typeof(VerticalLayoutGroup));
        details.transform.SetParent(body, false);
        RectTransform dr = (RectTransform)details.transform;
        dr.anchorMin = Vector2.zero;
        dr.anchorMax = Vector2.one;
        dr.offsetMin = new Vector2(10, 125);
        dr.offsetMax = new Vector2(-10, -425);
        VerticalLayoutGroup detailsLayout = details.GetComponent<VerticalLayoutGroup>();
        detailsLayout.spacing = 14;
        detailsLayout.childControlWidth = detailsLayout.childControlHeight = true;
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = false;
        select.detailsArea = dr;

        Button start = CreateButton(body, "StartMissionButton", "Start mission", 44);
        Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(480, 95));
        start.GetComponent<Image>().color = new Color(0.2f, 0.62f, 0.38f);
        UnityEventTools.AddVoidPersistentListener(start.onClick, select.StartMission);
        select.startButton = start;
    }

    // "Coming soon" mission: only a name, a description and a difficulty, no numbers (it cannot be played)
    static DestinationData LockedMission(string file, string missionName, string name, string tagline, string description, int difficulty)
    {
        string path = DestinationsFolder + file + ".asset";
        DestinationData m = AssetDatabase.LoadAssetAtPath<DestinationData>(path);
        if (m == null)
        {
            m = ScriptableObject.CreateInstance<DestinationData>();
            AssetDatabase.CreateAsset(m, path);
        }
        m.missionName = missionName;
        m.destinationName = name;
        m.tagline = tagline;
        m.description = description;
        m.locked = true;
        m.difficulty = difficulty;
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    // The playable lunar orbit mission. The game must not name real agencies or programs,
    // so the texts imported from the catalog are replaced (the numbers are not touched).
    static DestinationData Selene()
    {
        DestinationData m = AssetDatabase.LoadAssetAtPath<DestinationData>(DestinationsFolder + "MisionArtemis.asset");
        if (m == null) return null;
        m.missionName = "Selene";
        m.destinationName = "Crewed lunar orbit";
        m.tagline = "Orbit the Moon with 3 astronauts";
        m.description = "Take 3 astronauts to an orbit around the Moon, collect scientific data and bring them back home safely.";
        m.difficulty = 2;
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    static GameObject CreatePanel(Transform root, string name, string title, MainMenu menu, out Transform body, Vector2 size)
    {
        GameObject bg = new GameObject(name, typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root, false);
        Stretch((RectTransform)bg.transform);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f); // blocks clicks on the menu behind it

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(bg.transform, false);
        Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Image boxImage = box.GetComponent<Image>();
        boxImage.sprite = RoundedSprite;
        boxImage.type = Image.Type.Sliced;
        boxImage.color = BoxColor;

        TextMeshProUGUI t = CreateText(box.transform, "Title", title, 56);
        Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -75), new Vector2(1100, 100));

        Button close = CreateButton(box.transform, "CloseButton", "X", 48);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-65, -65), new Vector2(90, 90));
        UnityEventTools.AddVoidPersistentListener(close.onClick, menu.HidePanels);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(box.transform, false);
        RectTransform cr = (RectTransform)content.transform;
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = new Vector2(70, 60);
        cr.offsetMax = new Vector2(-70, -150);
        body = content.transform;
        return bg;
    }

    static Button CreateButton(Transform parent, string name, string label, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = ButtonColor;
        // Lighter on hover, darker when pressed
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.85f, 0.85f);
        colors.highlightedColor = new Color(1f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        button.colors = colors;
        TextMeshProUGUI t = CreateText(go.transform, "Text", label, size);
        Stretch(t.rectTransform);
        return go.GetComponent<Button>();
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return t;
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}