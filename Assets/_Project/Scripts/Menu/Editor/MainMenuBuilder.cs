using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu de Unity: SpaceApps > Armar menu principal
// Arma 00_Menu: quita el texto de estado y el boton Siguiente del greybox, y crea los 5 botones
// y los paneles de Ajustes y Space Apps ya conectados. Se puede volver a correr: borra y recrea
// solo lo que crea (Botones, PanelAjustes, PanelSpaceApps). Despues acomoda colores y textos a mano.
public static class MainMenuBuilder
{
    static readonly Color ButtonColor = new Color(0.15f, 0.22f, 0.35f);
    static readonly Color BoxColor = new Color(0.08f, 0.1f, 0.16f);

    [MenuItem("SpaceApps/Armar menu principal")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + Scenes.Menu + ".unity");

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Menu principal", "No encontre un Canvas en 00_Menu.", "OK");
            return;
        }
        Transform root = canvas.transform;
        foreach (string n in new[] { "Estado", "BotonSiguiente", "Botones", "PanelAjustes", "PanelSpaceApps" })
        {
            Transform t = root.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        MainMenu menu = canvas.GetComponent<MainMenu>();
        if (menu == null) menu = canvas.gameObject.AddComponent<MainMenu>();

        // ---------- Botones ----------
        GameObject column = new GameObject("Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
        column.transform.SetParent(root, false);
        Place((RectTransform)column.transform, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(620, 620));
        VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 22;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;

        Button play = CreateButton(column.transform, "BotonIniciar", "Iniciar juego", 44);
        Button creative = CreateButton(column.transform, "BotonModoCreativo", "Modo creativo (próximamente)", 36);
        Button settings = CreateButton(column.transform, "BotonAjustes", "Ajustes", 44);
        Button spaceApps = CreateButton(column.transform, "BotonSpaceApps", "Space Apps", 44);
        Button quit = CreateButton(column.transform, "BotonSalir", "Salir del juego", 44);
        creative.interactable = false;

        // ---------- Paneles ----------
        Transform settingsBody;
        GameObject settingsPanel = CreatePanel(root, "PanelAjustes", "Ajustes", menu, out settingsBody);
        BuildSettings(settingsPanel, settingsBody);

        Transform infoBody;
        GameObject infoPanel = CreatePanel(root, "PanelSpaceApps", "NASA Space Apps Challenge 2026", menu, out infoBody);
        TextMeshProUGUI info = CreateText(infoBody, "Texto",
            "<b>El reto</b>\n" +
            "Space Mission Design Game, propuesto por la Agencia Espacial Canadiense: un juego para entender cómo se planea y se ejecuta una misión espacial.\n\n" +
            "<b>Nuestro proyecto</b>\n" +
            "Planeas una misión tripulada de órbita lunar estilo Artemis: lees la misión, eliges y entrenas a la tripulación, compras piezas y armas el cohete, y escoges sitio y fecha de lanzamiento. Luego vuelas la misión: salida, órbita lunar y retorno, resolviendo minijuegos y eventos.\n\n" +
            "<b>El equipo</b>\n" +
            "[Nombre del equipo y de los 5 integrantes]",
            32);
        info.alignment = TextAlignmentOptions.TopLeft;
        Stretch(info.rectTransform);

        // ---------- Conexiones ----------
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
        Debug.Log("Menu principal armado en 00_Menu.");
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

        TextMeshProUGUI volLabel = CreateText(body, "TextoVolumen", "Volumen", 40);
        volLabel.alignment = TextAlignmentOptions.Left;
        Place(volLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(900, 70));

        GameObject sliderGo = DefaultControls.CreateSlider(res);
        sliderGo.name = "SliderVolumen";
        sliderGo.transform.SetParent(body, false);
        Place((RectTransform)sliderGo.transform, new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(900, 40));
        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject toggleGo = DefaultControls.CreateToggle(res);
        toggleGo.name = "TogglePantallaCompleta";
        toggleGo.transform.SetParent(body, false);
        Transform oldLabel = toggleGo.transform.Find("Label");
        if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject);
        TextMeshProUGUI tLabel = CreateText(toggleGo.transform, "Label", "Pantalla completa", 16);
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
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f); // bloquea clics al menu de atras

        GameObject box = new GameObject("Caja", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(bg.transform, false);
        Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 850));
        box.GetComponent<Image>().color = BoxColor;

        TextMeshProUGUI t = CreateText(box.transform, "Titulo", title, 56);
        Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -75), new Vector2(1100, 100));

        Button close = CreateButton(box.transform, "BotonCerrar", "X", 48);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-65, -65), new Vector2(90, 90));
        UnityEventTools.AddVoidPersistentListener(close.onClick, menu.HidePanels);

        GameObject content = new GameObject("Contenido", typeof(RectTransform));
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
        TextMeshProUGUI t = CreateText(go.transform, "Texto", label, size);
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