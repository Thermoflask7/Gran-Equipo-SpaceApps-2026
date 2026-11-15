using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// "SpaceOS" desktop that is ALWAYS visible on the monitor screen. The Canvas is rendered
/// to a RenderTexture (PC_UICamera) and shown on the curved mesh PC_Screen (CRT filter).
/// It is only interactive while the camera is focused on the PC (Show/Hide enable and
/// disable clicks, they do not hide it).
/// The app icons are generated at runtime inside <see cref="appGrid"/>.
/// To connect an app: ComputerDesktop.Instance.RegisterApp("spacemart", "SpaceMart", color, OpenShop)
/// or GetApp("spacemart").AddListener(OpenShop).
/// </summary>
public class ComputerDesktop : MonoBehaviour
{
    public const string AppSpaceMart = "spacemart";
    public const string AppAjustes = "ajustes";
    public const string AppTechTree = "techtree";
    public const string AppApagar = "apagar";

    static ComputerDesktop instance;
    public static ComputerDesktop Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<ComputerDesktop>(FindObjectsInactive.Include);
            return instance;
        }
    }

    [Header("References (set by SpaceApps > Build desk PC)")]
    [SerializeField] GameObject root;
    [SerializeField] RectTransform appGrid;
    [SerializeField] Button backButton;
    [SerializeField] GameObject comingSoonPanel;
    [SerializeField] TMP_Text comingSoonText;
    [SerializeField] Button comingSoonCloseButton;
    [SerializeField] TMP_Text clockText;
    [Tooltip("Black image on top of everything: screen turned off.")]
    [SerializeField] GameObject screenOff;

    [Header("Apps")]
    [SerializeField] List<ComputerApp> apps = new List<ComputerApp>
    {
        new ComputerApp(AppSpaceMart, "SpaceMart", new Color(0.95f, 0.55f, 0.15f)),
        new ComputerApp(AppAjustes, "Settings", new Color(0.45f, 0.5f, 0.6f)),
        new ComputerApp(AppTechTree, "Tech tree", new Color(0.3f, 0.7f, 0.45f), false),
        new ComputerApp(AppApagar, "Shut down", new Color(0.85f, 0.2f, 0.2f)),
    };

    [Header("Events")]
    public UnityEvent onShown;
    public UnityEvent onHidden;

    public bool IsVisible => root != null && root.activeSelf;
    public bool IsInteractive { get; private set; }
    public bool IsPoweredOn => screenOff == null || !screenOff.activeSelf;
    public IReadOnlyList<ComputerApp> Apps => apps;

    PcStation owner;
    bool iconsBuilt;
    readonly List<GameObject> icons = new List<GameObject>();
    CanvasGroup group;
    GraphicRaycaster raycaster;

    void Awake()
    {
        if (instance == null) instance = this;
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (comingSoonCloseButton != null) comingSoonCloseButton.onClick.AddListener(CloseComingSoon);
        group = GetComponent<CanvasGroup>();
        raycaster = GetComponent<GraphicRaycaster>();
        var canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main; // event camera of the world canvas
        if (root != null) root.SetActive(true);
        CloseComingSoon();
        SetInteractive(false);
    }

    void Start()
    {
        if (!iconsBuilt) RebuildIcons(); // the icons are visible from the desk even if they cannot be clicked
        if (GetComponent<SpaceMart>() == null)
            gameObject.AddComponent<SpaceMart>();
        if (GetComponent<SpaceMartApp>() == null)
            gameObject.AddComponent<SpaceMartApp>();
        if (GetComponent<AjustesApp>() == null)
            gameObject.AddComponent<AjustesApp>();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (clockText != null && IsVisible && IsPoweredOn) clockText.text = "SpaceOS   " + DateTime.Now.ToString("HH:mm");
    }

    /// <summary>The camera reached the PC: turns the screen on if it was off and enables clicks.</summary>
    public void Show(PcStation from)
    {
        owner = from;
        if (!iconsBuilt) RebuildIcons();
        CloseComingSoon();
        SetPower(true);
        SetInteractive(true);
        onShown?.Invoke();
    }

    /// <summary>The camera leaves: the screen stays visible but no longer gets clicks.</summary>
    public void Hide()
    {
        CloseComingSoon();
        SetInteractive(false);
        onHidden?.Invoke();
    }

    /// <summary>Enables/disables clicks on the screen (CanvasGroup + GraphicRaycaster).</summary>
    public void SetInteractive(bool value)
    {
        IsInteractive = value;
        if (group != null) { group.interactable = value; group.blocksRaycasts = value; }
        if (raycaster != null) raycaster.enabled = value;
    }

    /// <summary>Turns the monitor screen on or off (black).</summary>
    public void SetPower(bool on)
    {
        if (screenOff != null) screenOff.SetActive(!on);
    }

    /// <summary>Back button: closes the PC and returns the camera to the desk.</summary>
    public void Back()
    {
        if (owner != null) owner.Close();
        else { Hide(); DeskCamera.Instance?.ReturnToDesk(); }
    }

    public ComputerApp GetApp(string id) => apps.Find(a => a.id == id);

    /// <summary>Registers (or completes) an app. If it exists, it only adds the listener and enables it.</summary>
    public ComputerApp RegisterApp(string id, string label, Color color, UnityAction onOpen)
    {
        var app = GetApp(id);
        if (app == null)
        {
            app = new ComputerApp(id, label, color);
            int apagar = apps.FindIndex(a => a.id == AppApagar);
            if (apagar >= 0) apps.Insert(apagar, app); else apps.Add(app); // Shut down always last
        }
        else
        {
            if (!string.IsNullOrEmpty(label)) app.label = label;
            app.color = color;
        }
        app.interactable = true;
        app.AddListener(onOpen);
        if (iconsBuilt) RebuildIcons();
        return app;
    }

    public void OpenApp(string id)
    {
        var app = GetApp(id);
        if (app == null || !app.interactable) return;
        CloseComingSoon();
        if (app.id == AppApagar && !app.HasHandlers) { QuitToMenuDialog.Open(); return; } // asks before leaving to the main menu
        if (app.HasHandlers) app.onOpen.Invoke();
        else ShowComingSoon(app.label + "\n\n" + app.comingSoonMessage);
    }

    public void ShowComingSoon(string message)
    {
        if (comingSoonText != null) comingSoonText.text = message;
        if (comingSoonPanel != null) comingSoonPanel.SetActive(true);
    }

    public void CloseComingSoon()
    {
        if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
    }

    public void RebuildIcons()
    {
        if (appGrid == null) return;
        foreach (var go in icons) if (go != null) Destroy(go);
        icons.Clear();
        foreach (var app in apps) icons.Add(CreateIcon(app));
        iconsBuilt = true;
    }

    GameObject CreateIcon(ComputerApp app)
    {
        var go = new GameObject("App_" + app.id, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(appGrid, false);
        var bg = go.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.06f);

        var iconGo = new GameObject("Icono", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.anchorMin = new Vector2(0.15f, 0.3f);
        iconRt.anchorMax = new Vector2(0.85f, 0.95f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
        var icon = iconGo.GetComponent<Image>();
        icon.color = app.interactable ? app.color : Color.Lerp(app.color, Color.gray, 0.6f);
        icon.raycastTarget = false;

        var labelGo = new GameObject("Etiqueta", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0.28f);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = app.interactable ? app.label : app.label + "\n<size=70%>pronto</size>";
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 28f;
        label.color = app.interactable ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        label.raycastTarget = false;

        var button = go.GetComponent<Button>();
        button.targetGraphic = bg;
        button.interactable = app.interactable;
        string id = app.id;
        button.onClick.AddListener(() => OpenApp(id));
        return go;
    }
}