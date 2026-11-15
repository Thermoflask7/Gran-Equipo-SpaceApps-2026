using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Reusable tutorial / help popup (welcome message and one short screen per desk station). Signed: Claude.
// Built at runtime (no scene setup), on the scene's OverlayCanvas when it has one.
// Folder look: cream paper, dark ink, red stamp and button.
//   TutorialPopup.ShowOnce("folder")  -> shows that station's texts (TutorialTexts) the first time only
//   TutorialPopup.Show("folder")      -> shows it again (the "?" button)
//   TutorialPopup.Show(title, body, buttonLabel, stamp, stampImage) -> stampImage (optional) replaces the stamp word
// While it is open: a full-screen backdrop blocks clicks on the desk (the stations ignore clicks over UI),
// the PC screen stops taking clicks (ComputerDesktop.SetInteractive) and Escape closes the popup
// instead of returning the camera to the desk (DeskCamera.EscapeReturns).
public class TutorialPopup : MonoBehaviour
{
    const string OverlayName = "OverlayCanvas";
    const int SortingOrder = 90; // above the station panels and the launch panel (50), below the quit dialog (100)

    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color PaperColor = new Color(0.98f, 0.96f, 0.9f, 1f);
    static readonly Color InkColor = new Color(0.12f, 0.12f, 0.16f, 1f);
    static readonly Color StampRed = new Color(0.75f, 0.15f, 0.12f, 1f);

    static TutorialPopup instance;

    GameObject panel;
    TextMeshProUGUI titleText;
    TextMeshProUGUI bodyText;
    TextMeshProUGUI buttonText;
    TextMeshProUGUI stampText;
    RawImage stampImage;
    GameObject helpButton;
    string helpId;

    DeskCamera escapeBlockedOn;
    bool restoreEscape;
    ComputerDesktop pcBlocked;

    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

    // Shows a station's tutorial only the first time in this game. Returns true if it was shown.
    public static bool ShowOnce(string id)
    {
        GameManager gm = GameManager.Instance;
        if (gm.tutorialsSeen.Contains(id) || IsOpen) return false;
        gm.tutorialsSeen.Add(id);
        Show(id);
        return true;
    }

    // Shows a station's tutorial (texts from TutorialTexts)
    public static void Show(string id)
    {
        Show(TutorialTexts.Title(id), BulletList(TutorialTexts.Bullets(id)), "Got it", "HOW TO");
    }

    public static void Show(string title, string body, string buttonLabel, string stamp, Texture2D stampImage = null)
    {
        TutorialPopup popup = Get();
        popup.titleText.text = title;
        popup.bodyText.text = body;
        popup.buttonText.text = buttonLabel;
        popup.stampText.text = stampImage != null ? "" : stamp;
        popup.SetStampImage(stampImage);
        if (!popup.panel.activeSelf) popup.Block();
        popup.panel.SetActive(true);
        popup.panel.transform.SetAsLastSibling();
    }

    public static void Close()
    {
        if (!IsOpen) return;
        instance.panel.SetActive(false);
        instance.Unblock();
    }

    // Small "?" button that reopens a tutorial, placed next to each station's own buttons. null hides it.
    public static void SetHelp(string id)
    {
        if (instance == null && id == null) return;
        TutorialPopup popup = Get();
        if (popup.helpId != id && id != null) PlaceHelp((RectTransform)popup.helpButton.transform, id);
        popup.helpId = id;
        bool show = id != null;
        if (popup.helpButton.activeSelf != show) popup.helpButton.SetActive(show);
    }

    // Where the "?" goes in each station, so it does not cover that station's UI (1920x1080 reference)
    static void PlaceHelp(RectTransform rt, string id)
    {
        Vector2 anchor = Vector2.one;         // top right corner by default
        Vector2 position = new Vector2(-30f, -30f);
        float size = 72f;
        switch (id)
        {
            case TutorialTexts.Folder:    position = new Vector2(-30f, -14f); break;            // above the folder, right of its tab
            case TutorialTexts.SpaceMart: position = new Vector2(-30f, -30f); break;            // beside the monitor
            case TutorialTexts.Rocket:    position = new Vector2(-104f, -8f); size = 64f; break; // title bar, left of the X
            case TutorialTexts.Launch:    position = new Vector2(-176f, -20f); size = 56f; break; // left of the Back button
            case TutorialTexts.Planning:  anchor = Vector2.zero; position = new Vector2(208f, 32f); size = 52f; break; // right of Back
        }
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(size, size);
        TMP_Text label = rt.GetComponentInChildren<TMP_Text>();
        if (label != null) label.fontSize = size * 0.64f;
    }

    void SetStampImage(Texture2D texture)
    {
        stampImage.texture = texture;
        stampImage.gameObject.SetActive(texture != null);
        if (texture == null) return;
        float height = 170f; // keeps the image's own proportions
        stampImage.rectTransform.sizeDelta = new Vector2(height * texture.width / Mathf.Max(1, texture.height), height);
    }

    public static string BulletList(string[] bullets)
    {
        string text = "";
        foreach (string line in bullets) text += "\u2022  " + line + "\n\n";
        return text.TrimEnd('\n');
    }

    void Update()
    {
        if (!IsOpen) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    // After every Update of this frame, so DeskCamera does not also read the Escape that closed the popup
    void LateUpdate()
    {
        if (!restoreEscape) return;
        restoreEscape = false;
        if (escapeBlockedOn != null) escapeBlockedOn.EscapeReturns = true;
        escapeBlockedOn = null;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Block()
    {
        restoreEscape = false;
        DeskCamera dc = DeskCamera.Instance;
        if (escapeBlockedOn == null && dc != null && dc.EscapeReturns)
        {
            dc.EscapeReturns = false;
            escapeBlockedOn = dc;
        }
        ComputerDesktop pc = ComputerDesktop.Instance;
        if (pc != null && pc.IsInteractive)
        {
            pc.SetInteractive(false);
            pcBlocked = pc;
        }
    }

    void Unblock()
    {
        restoreEscape = escapeBlockedOn != null;
        if (pcBlocked != null)
        {
            DeskCamera dc = DeskCamera.Instance;
            if (dc == null || dc.IsFocused) pcBlocked.SetInteractive(true); // only if we are still at the PC
            pcBlocked = null;
        }
    }

    void OnHelpClicked()
    {
        if (helpId != null) Show(helpId);
    }

    // ---------- Building the UI ----------

    static TutorialPopup Get()
    {
        if (instance == null) instance = Build();
        return instance;
    }

    static TutorialPopup Build()
    {
        GameObject layer = new GameObject("TutorialLayer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        Canvas overlay = FindOverlayCanvas();
        Canvas canvas = layer.GetComponent<Canvas>();
        if (overlay != null)
        {
            layer.transform.SetParent(overlay.transform, false);
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            Stretch((RectTransform)layer.transform);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            CanvasScaler scaler = layer.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        EnsureEventSystem();
        TutorialPopup popup = layer.AddComponent<TutorialPopup>();

        // "?" button (bottom left corner)
        RectTransform help = NewRect("HelpButton", layer.transform);
        help.anchorMin = help.anchorMax = help.pivot = Vector2.zero;
        help.anchoredPosition = new Vector2(30f, 30f);
        help.sizeDelta = new Vector2(72f, 72f);
        AddButton(help, StampRed, popup.OnHelpClicked);
        TextMeshProUGUI helpLabel = AddText(help, "Label", "?", 46f, Color.white);
        helpLabel.fontStyle = FontStyles.Bold;
        popup.helpButton = help.gameObject;
        popup.helpButton.SetActive(false);

        // Full-screen dim that blocks clicks on the desk
        RectTransform dim = NewRect("Dim", layer.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = DimColor;
        popup.panel = dim.gameObject;

        // Paper card
        RectTransform card = NewRect("Paper", dim);
        card.sizeDelta = new Vector2(1000f, 700f);
        card.gameObject.AddComponent<Image>().color = PaperColor;
        Shadow shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(8f, -8f);

        // Red band on top
        RectTransform band = NewRect("Band", card);
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = Vector2.one;
        band.pivot = new Vector2(0.5f, 1f);
        band.sizeDelta = new Vector2(0f, 14f);
        band.gameObject.AddComponent<Image>().color = StampRed;

        // Stamp (tilted, top right)
        RectTransform stamp = NewRect("Stamp", card);
        stamp.anchorMin = stamp.anchorMax = Vector2.one;
        stamp.anchoredPosition = new Vector2(-120f, -70f);
        stamp.sizeDelta = new Vector2(220f, 60f);
        stamp.localRotation = Quaternion.Euler(0f, 0f, 12f);
        popup.stampText = AddText(stamp, "Label", "", 34f, StampRed);
        popup.stampText.fontStyle = FontStyles.Bold;
        popup.stampText.characterSpacing = 6f;

        // Stamp picture (welcome), same corner, a bit tilted
        RectTransform stampPicture = NewRect("StampImage", card);
        stampPicture.anchorMin = stampPicture.anchorMax = Vector2.one;
        stampPicture.anchoredPosition = new Vector2(-130f, -80f);
        stampPicture.localRotation = Quaternion.Euler(0f, 0f, 12f);
        popup.stampImage = stampPicture.gameObject.AddComponent<RawImage>();
        popup.stampImage.raycastTarget = false;
        stampPicture.gameObject.SetActive(false);

        // Title
        RectTransform title = NewRect("Title", card);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = Vector2.one;
        title.pivot = new Vector2(0.5f, 1f);
        title.offsetMin = new Vector2(60f, -140f);
        title.offsetMax = new Vector2(-260f, -40f);
        popup.titleText = AddText(title, "Label", "", 42f, InkColor);
        popup.titleText.fontStyle = FontStyles.Bold;
        popup.titleText.alignment = TextAlignmentOptions.MidlineLeft;
        popup.titleText.enableAutoSizing = true;
        popup.titleText.fontSizeMin = 28f;
        popup.titleText.fontSizeMax = 42f;

        // Body
        RectTransform body = NewRect("Body", card);
        Stretch(body);
        body.offsetMin = new Vector2(60f, 130f);
        body.offsetMax = new Vector2(-60f, -160f);
        popup.bodyText = AddText(body, "Label", "", 28f, InkColor);
        popup.bodyText.alignment = TextAlignmentOptions.TopLeft;
        popup.bodyText.enableAutoSizing = true;
        popup.bodyText.fontSizeMin = 18f;
        popup.bodyText.fontSizeMax = 28f;

        // Button
        RectTransform button = NewRect("Button", card);
        button.anchorMin = button.anchorMax = new Vector2(0.5f, 0f);
        button.anchoredPosition = new Vector2(0f, 70f);
        button.sizeDelta = new Vector2(320f, 72f);
        AddButton(button, StampRed, Close);
        popup.buttonText = AddText(button, "Label", "Got it", 32f, Color.white);
        popup.buttonText.fontStyle = FontStyles.Bold;

        popup.panel.SetActive(false);
        return popup;
    }

    // The scene's root OverlayCanvas (active); null = make our own canvas
    static Canvas FindOverlayCanvas()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.name == OverlayName)
                return c;
        return null;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI AddText(RectTransform parent, string name, string text, float size, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Stretch(rt);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void AddButton(RectTransform rt, Color color, UnityAction onClick)
    {
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
    }
}
