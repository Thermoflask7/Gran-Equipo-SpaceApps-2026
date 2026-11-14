using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Escritorio "SpaceOS" que se ve cuando la camara esta enfocada en la PC.
/// Va en el Canvas PC_Desktop; muestra/oculta <see cref="root"/>.
/// Los iconos de las apps se generan en runtime dentro de <see cref="appGrid"/>.
/// Para conectar una app: ComputerDesktop.Instance.RegisterApp("spacemart", "SpaceMart", color, AbrirTienda)
/// o GetApp("spacemart").AddListener(AbrirTienda).
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

    [Header("Referencias (las arma SpaceApps > Armar PC del escritorio)")]
    [SerializeField] GameObject root;
    [SerializeField] RectTransform appGrid;
    [SerializeField] Button backButton;
    [SerializeField] GameObject comingSoonPanel;
    [SerializeField] TMP_Text comingSoonText;
    [SerializeField] Button comingSoonCloseButton;
    [SerializeField] TMP_Text clockText;

    [Header("Apps")]
    [SerializeField] List<ComputerApp> apps = new List<ComputerApp>
    {
        new ComputerApp(AppSpaceMart, "SpaceMart", new Color(0.95f, 0.55f, 0.15f)),
        new ComputerApp(AppAjustes, "Ajustes", new Color(0.45f, 0.5f, 0.6f)),
        new ComputerApp(AppTechTree, "Tech tree", new Color(0.3f, 0.7f, 0.45f), false),
        new ComputerApp(AppApagar, "Apagar", new Color(0.85f, 0.2f, 0.2f)),
    };

    [Header("Eventos")]
    public UnityEvent onShown;
    public UnityEvent onHidden;

    public bool IsVisible => root != null && root.activeSelf;
    public IReadOnlyList<ComputerApp> Apps => apps;

    PcStation owner;
    bool iconsBuilt;
    readonly List<GameObject> icons = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (comingSoonCloseButton != null) comingSoonCloseButton.onClick.AddListener(CloseComingSoon);
        if (root != null) root.SetActive(false);
        CloseComingSoon();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (clockText != null && IsVisible) clockText.text = "SpaceOS   " + DateTime.Now.ToString("HH:mm");
    }

    public void Show(PcStation from)
    {
        owner = from;
        if (!iconsBuilt) RebuildIcons();
        CloseComingSoon();
        if (root != null) root.SetActive(true);
        onShown?.Invoke();
    }

    public void Hide()
    {
        CloseComingSoon();
        if (root != null) root.SetActive(false);
        onHidden?.Invoke();
    }

    /// <summary>Boton Volver: cierra la PC y regresa la camara al escritorio.</summary>
    public void Back()
    {
        if (owner != null) owner.Close();
        else { Hide(); DeskCamera.Instance?.ReturnToDesk(); }
    }

    public ComputerApp GetApp(string id) => apps.Find(a => a.id == id);

    /// <summary>Registra (o completa) una app. Si ya existe, solo agrega el listener y la activa.</summary>
    public ComputerApp RegisterApp(string id, string label, Color color, UnityAction onOpen)
    {
        var app = GetApp(id);
        if (app == null)
        {
            app = new ComputerApp(id, label, color);
            int apagar = apps.FindIndex(a => a.id == AppApagar);
            if (apagar >= 0) apps.Insert(apagar, app); else apps.Add(app); // Apagar siempre al final
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
        if (app.id == AppApagar && !app.HasHandlers) { Back(); return; }
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