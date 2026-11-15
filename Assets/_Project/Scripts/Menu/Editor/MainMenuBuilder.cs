using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Unity menu: SpaceApps > Build main menu
// Builds 00_Menu: removes the greybox state text and Next button, and creates the 5 buttons
// and the Settings and Space Apps panels, already wired up. It can be run again: it deletes and recreates
// only what it creates (Buttons, SettingsPanel, SpaceAppsPanel). Then adjust colors and texts by hand.
public static class MainMenuBuilder
{
    static readonly Color ButtonColor = new Color(0.15f, 0.22f, 0.35f);
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f);

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
            "Estado", "BotonSiguiente", "Botones", "PanelAjustes", "PanelSpaceApps" })
        {
            Transform t = root.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        MainMenu menu = canvas.GetComponent<MainMenu>();
        if (menu == null) menu = canvas.gameObject.AddComponent<MainMenu>();

        // ---------- Buttons ----------
        GameObject column = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        column.transform.SetParent(root, false);
        Place((RectTransform)column.transform, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(620, 620));
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
        Transform settingsBody;
        GameObject settingsPanel = CreatePanel(root, "SettingsPanel", "Settings", menu, out settingsBody);
        BuildSettings(settingsPanel, settingsBody);

        Transform infoBody;
        GameObject infoPanel = CreatePanel(root, "SpaceAppsPanel", "NASA Space Apps Challenge 2026", menu, out infoBody);
        TextMeshProUGUI info = CreateText(infoBody, "Text",
            "<b>The challenge</b>\n" +
            "Space Mission Design Game, proposed by the Canadian Space Agency: a game to understand how a space mission is planned and carried out.\n\n" +
            "<b>Our project</b>\n" +
            "You plan an Artemis-style crewed lunar orbit mission: you read the mission, choose and train the crew, buy parts and assemble the rocket, and pick the launch site and date. Then you fly the mission: departure, lunar orbit and return, solving minigames and events.\n\n" +
            "<b>The team</b>\n" +
            "[Team name and the 5 members]",
            32);
        info.alignment = TextAlignmentOptions.TopLeft;
        Stretch(info.rectTransform);

        // ---------- Connections ----------
        menu.panels = new[] { settingsPanel, infoPanel };
        UnityEventTools.AddVoidPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddObjectPersistentListener<GameObject>(settings.onClick, menu.ShowPanel, settingsPanel);
        UnityEventTools.AddObjectPersistentListener<GameObject>(spaceApps.onClick, menu.ShowPanel, infoPanel);
        UnityEventTools.AddVoidPersistentListener(quit.onClick, menu.Quit);

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

    static GameObject CreatePanel(Transform root, string name, string title, MainMenu menu, out Transform body)
    {
        GameObject bg = new GameObject(name, typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root, false);
        Stretch((RectTransform)bg.transform);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f); // blocks clicks on the menu behind it

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(bg.transform, false);
        Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 850));
        box.GetComponent<Image>().color = BoxColor;

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
        go.GetComponent<Image>().color = ButtonColor;
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