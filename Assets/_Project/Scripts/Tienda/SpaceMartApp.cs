using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App SpaceMart: ventana de tienda dentro del canvas de SpaceOS.
/// Se registra con ComputerDesktop.RegisterApp("spacemart") y construye su UI en runtime.
/// Solo interfaz y valores placeholder; no descuenta presupuesto.
/// </summary>
public class SpaceMartApp : MonoBehaviour
{
    [SerializeField] SpaceMartCatalog catalog;

    ComputerDesktop desktop;
    SpaceMartCatalog runtimeCatalog;
    bool ownsCatalog;
    bool ready;

    GameObject window;
    RectTransform gridContent;
    RectTransform detailRoot;
    RectTransform cartContent;
    TMP_Text detailName;
    TMP_Text detailSpec;
    TMP_Text detailPrice;
    Image detailIcon;
    TMP_Text cartCountText;
    TMP_Text cartTotalText;
    Button addButton;
    readonly List<Button> tabButtons = new List<Button>();
    readonly List<CartLine> cart = new List<CartLine>();

    SpaceMartCategory currentCategory = SpaceMartCategory.Cohetes;
    SpaceMartItem selected;

    struct CartLine
    {
        public SpaceMartItem item;
        public int quantity;
    }

    public void SetCatalog(SpaceMartCatalog value)
    {
        catalog = value;
    }

    void Awake() => Setup();
    void Start() => Setup();

    void OnDestroy()
    {
        if (desktop != null)
        {
            desktop.onHidden.RemoveListener(Close);
            desktop.onShown.RemoveListener(Close);
        }
        if (ownsCatalog && runtimeCatalog != null)
        {
            if (Application.isPlaying) Destroy(runtimeCatalog);
            else DestroyImmediate(runtimeCatalog);
        }
    }

    void Setup()
    {
        if (ready) return;
        desktop = GetComponent<ComputerDesktop>();
        if (desktop == null) desktop = ComputerDesktop.Instance;
        if (desktop == null) return;

        EnsureCatalog();
        BuildWindow();
        desktop.RegisterApp(ComputerDesktop.AppSpaceMart, "SpaceMart", new Color(0.95f, 0.55f, 0.15f), Open);
        desktop.onHidden.AddListener(Close);
        desktop.onShown.AddListener(Close);
        ready = true;
        Close();
    }

    void EnsureCatalog()
    {
        if (catalog != null && catalog.items != null && catalog.items.Count > 0)
        {
            runtimeCatalog = catalog;
            ownsCatalog = false;
            return;
        }
        runtimeCatalog = SpaceMartCatalog.CreateDefault();
        ownsCatalog = true;
    }

    public void Open()
    {
        if (!ready) Setup();
        if (window == null) return;
        SelectCategory(currentCategory);
        if (selected == null)
        {
            var first = runtimeCatalog.ItemsIn(currentCategory);
            if (first.Count > 0) ShowDetail(first[0]);
            else ShowDetail(null);
        }
        else ShowDetail(selected);
        RefreshCart();
        window.SetActive(true);
        window.transform.SetAsLastSibling();
        BringOverlayToFront();
    }

    public void Close()
    {
        if (window != null) window.SetActive(false);
    }

    void SelectCategory(SpaceMartCategory category)
    {
        currentCategory = category;
        for (int i = 0; i < tabButtons.Count; i++)
        {
            bool on = (SpaceMartCategory)i == category;
            var img = tabButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = on ? new Color(0.95f, 0.55f, 0.15f) : new Color(0.12f, 0.14f, 0.22f, 1f);
        }
        RebuildGrid();
    }

    void RebuildGrid()
    {
        if (gridContent == null || runtimeCatalog == null) return;
        for (int i = gridContent.childCount - 1; i >= 0; i--)
        {
            var child = gridContent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        var items = runtimeCatalog.ItemsIn(currentCategory);
        for (int i = 0; i < items.Count; i++)
            CreateCard(items[i]);

        Canvas.ForceUpdateCanvases();
    }

    void ShowDetail(SpaceMartItem item)
    {
        selected = item;
        bool has = item != null;
        if (detailIcon != null)
        {
            detailIcon.color = has ? item.color : new Color(0.2f, 0.2f, 0.25f);
        }
        if (detailName != null)
            detailName.text = has ? item.name : "Selecciona un producto";
        if (detailSpec != null)
            detailSpec.text = has ? (SpaceMartCatalog.CategoryLabel(item.category) + "\n\n" + item.spec) : "";
        if (detailPrice != null)
            detailPrice.text = has ? SpaceMartCatalog.FormatPrice(item.price) : "";
        if (addButton != null)
            addButton.interactable = has;
    }

    void AddSelectedToCart()
    {
        if (selected == null) return;
        for (int i = 0; i < cart.Count; i++)
        {
            if (cart[i].item.id == selected.id)
            {
                var line = cart[i];
                line.quantity++;
                cart[i] = line;
                RefreshCart();
                return;
            }
        }
        cart.Add(new CartLine { item = selected, quantity = 1 });
        RefreshCart();
    }

    void RemoveFromCart(string id)
    {
        for (int i = 0; i < cart.Count; i++)
        {
            if (cart[i].item.id != id) continue;
            var line = cart[i];
            line.quantity--;
            if (line.quantity <= 0) cart.RemoveAt(i);
            else cart[i] = line;
            break;
        }
        RefreshCart();
    }

    void RefreshCart()
    {
        if (cartContent != null)
        {
            for (int i = cartContent.childCount - 1; i >= 0; i--)
            {
                var child = cartContent.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            for (int i = 0; i < cart.Count; i++)
                CreateCartRow(cart[i]);
        }

        int count = 0;
        float total = 0f;
        for (int i = 0; i < cart.Count; i++)
        {
            count += cart[i].quantity;
            total += cart[i].item.price * cart[i].quantity;
        }
        if (cartCountText != null)
            cartCountText.text = count == 1 ? "1 articulo" : count + " articulos";
        if (cartTotalText != null)
            cartTotalText.text = "Total  " + SpaceMartCatalog.FormatPrice(total);
    }

    void BringOverlayToFront()
    {
        var apagada = desktop.transform.Find("Root/Pantalla/Apagada");
        if (apagada != null) apagada.SetAsLastSibling();
    }

    // ---- UI ----

    void BuildWindow()
    {
        if (window != null) return;
        Transform pantalla = desktop.transform.Find("Root/Pantalla");
        if (pantalla == null) pantalla = desktop.transform;

        window = new GameObject("SpaceMartWindow", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(pantalla, false);
        var windowRt = (RectTransform)window.transform;
        Stretch(windowRt, 24f, 72f, 24f, 100f);
        window.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.16f, 0.98f);

        // Barra de titulo
        var title = CreateImage("Titulo", window.transform, new Color(0.08f, 0.10f, 0.20f, 1f));
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = Vector2.zero;
        title.sizeDelta = new Vector2(0f, 52f);
        var accent = CreateImage("Acento", title, new Color(0.95f, 0.55f, 0.15f, 1f));
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(1f, 0f);
        accent.pivot = new Vector2(0.5f, 0f);
        accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(0f, 3f);
        var titleLabel = CreateText("Nombre", title, "SpaceMart", 26f, TextAlignmentOptions.MidlineLeft);
        titleLabel.rectTransform.offsetMin = new Vector2(16f, 0f);
        titleLabel.rectTransform.offsetMax = new Vector2(-80f, 0f);

        var close = CreateButton("Cerrar", title, new Color(0.75f, 0.20f, 0.20f, 1f));
        var closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-10f, 0f);
        closeRt.sizeDelta = new Vector2(44f, 36f);
        CreateText("X", close.transform, "X", 22f, TextAlignmentOptions.Center);
        close.onClick.AddListener(Close);

        // Pestañas
        var tabs = CreateRect("Pestanas", window.transform);
        tabs.anchorMin = new Vector2(0f, 1f);
        tabs.anchorMax = new Vector2(1f, 1f);
        tabs.pivot = new Vector2(0.5f, 1f);
        tabs.anchoredPosition = new Vector2(0f, -52f);
        tabs.sizeDelta = new Vector2(0f, 48f);
        var tabRow = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabRow.padding = new RectOffset(8, 8, 6, 6);
        tabRow.spacing = 6f;
        tabRow.childAlignment = TextAnchor.MiddleLeft;
        tabRow.childForceExpandWidth = true;
        tabRow.childForceExpandHeight = true;
        tabButtons.Clear();
        var categories = new[]
        {
            SpaceMartCategory.Cohetes,
            SpaceMartCategory.Motores,
            SpaceMartCategory.Combustible,
            SpaceMartCategory.Modulos,
            SpaceMartCategory.Instrumentos
        };
        for (int i = 0; i < categories.Length; i++)
        {
            var cat = categories[i];
            var tab = CreateButton("Tab_" + cat, tabs, new Color(0.12f, 0.14f, 0.22f, 1f));
            var le = tab.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 36f;
            CreateText("Etiqueta", tab.transform, SpaceMartCatalog.CategoryLabel(cat), 18f, TextAlignmentOptions.Center);
            tab.onClick.AddListener(() => SelectCategory(cat));
            tabButtons.Add(tab);
        }

        // Cuerpo: lista + detalle/carrito
        var body = CreateRect("Cuerpo", window.transform);
        Stretch(body, 8f, 8f, 8f, 108f);

        var listPane = CreateRect("Lista", body);
        listPane.anchorMin = Vector2.zero;
        listPane.anchorMax = new Vector2(0.60f, 1f);
        listPane.offsetMin = Vector2.zero;
        listPane.offsetMax = new Vector2(-6f, 0f);
        gridContent = CreateScrollArea(listPane, "Productos");

        var right = CreateRect("Derecha", body);
        right.anchorMin = new Vector2(0.60f, 0f);
        right.anchorMax = Vector2.one;
        right.offsetMin = new Vector2(6f, 0f);
        right.offsetMax = Vector2.zero;

        detailRoot = CreateImage("Detalle", right, new Color(0.07f, 0.09f, 0.16f, 1f));
        detailRoot.anchorMin = new Vector2(0f, 0.42f);
        detailRoot.anchorMax = Vector2.one;
        detailRoot.offsetMin = Vector2.zero;
        detailRoot.offsetMax = Vector2.zero;
        BuildDetail(detailRoot);

        var cartPane = CreateImage("Carrito", right, new Color(0.07f, 0.09f, 0.16f, 1f));
        cartPane.anchorMin = Vector2.zero;
        cartPane.anchorMax = new Vector2(1f, 0.42f);
        cartPane.offsetMin = Vector2.zero;
        cartPane.offsetMax = new Vector2(0f, -6f);
        BuildCart(cartPane);

        window.transform.SetAsLastSibling();
        BringOverlayToFront();
    }

    void BuildDetail(RectTransform root)
    {
        var iconGo = new GameObject("Icono", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root, false);
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.anchorMin = new Vector2(0f, 1f);
        iconRt.anchorMax = new Vector2(0f, 1f);
        iconRt.pivot = new Vector2(0f, 1f);
        iconRt.anchoredPosition = new Vector2(16f, -16f);
        iconRt.sizeDelta = new Vector2(72f, 72f);
        detailIcon = iconGo.GetComponent<Image>();
        detailIcon.color = new Color(0.2f, 0.2f, 0.25f);
        detailIcon.raycastTarget = false;

        detailName = CreateText("Nombre", root, "Selecciona un producto", 24f, TextAlignmentOptions.MidlineLeft);
        detailName.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailName.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailName.rectTransform.pivot = new Vector2(0.5f, 1f);
        detailName.rectTransform.anchoredPosition = new Vector2(0f, -16f);
        detailName.rectTransform.sizeDelta = new Vector2(-20f, 36f);
        detailName.rectTransform.offsetMin = new Vector2(100f, detailName.rectTransform.offsetMin.y);

        detailSpec = CreateText("Specs", root, "", 18f, TextAlignmentOptions.TopLeft);
        Stretch(detailSpec.rectTransform, 16f, 70f, 16f, 100f);
        detailSpec.enableAutoSizing = false;
        detailSpec.fontSize = 18f;

        detailPrice = CreateText("Precio", root, "", 22f, TextAlignmentOptions.MidlineLeft);
        detailPrice.rectTransform.anchorMin = new Vector2(0f, 0f);
        detailPrice.rectTransform.anchorMax = new Vector2(0.45f, 0f);
        detailPrice.rectTransform.pivot = new Vector2(0f, 0f);
        detailPrice.rectTransform.anchoredPosition = new Vector2(16f, 16f);
        detailPrice.rectTransform.sizeDelta = new Vector2(0f, 44f);
        detailPrice.color = new Color(1f, 0.85f, 0.40f);

        addButton = CreateButton("Agregar", root, new Color(0.95f, 0.55f, 0.15f, 1f));
        var addRt = (RectTransform)addButton.transform;
        addRt.anchorMin = new Vector2(1f, 0f);
        addRt.anchorMax = new Vector2(1f, 0f);
        addRt.pivot = new Vector2(1f, 0f);
        addRt.anchoredPosition = new Vector2(-12f, 12f);
        addRt.sizeDelta = new Vector2(210f, 48f);
        CreateText("Etiqueta", addButton.transform, "Agregar al carrito", 18f, TextAlignmentOptions.Center);
        addButton.onClick.AddListener(AddSelectedToCart);
        addButton.interactable = false;
    }

    void BuildCart(RectTransform root)
    {
        var header = CreateText("TituloCarrito", root, "Carrito", 20f, TextAlignmentOptions.MidlineLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 1f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.pivot = new Vector2(0.5f, 1f);
        header.rectTransform.anchoredPosition = Vector2.zero;
        header.rectTransform.sizeDelta = new Vector2(-16f, 32f);
        header.rectTransform.offsetMin = new Vector2(12f, header.rectTransform.offsetMin.y);

        var listHost = CreateRect("ListaCarrito", root);
        Stretch(listHost, 8f, 44f, 8f, 32f);
        cartContent = CreateScrollArea(listHost, "Lineas");

        cartCountText = CreateText("Cuenta", root, "0 articulos", 16f, TextAlignmentOptions.MidlineLeft);
        cartCountText.rectTransform.anchorMin = new Vector2(0f, 0f);
        cartCountText.rectTransform.anchorMax = new Vector2(0.45f, 0f);
        cartCountText.rectTransform.pivot = new Vector2(0f, 0f);
        cartCountText.rectTransform.anchoredPosition = new Vector2(12f, 6f);
        cartCountText.rectTransform.sizeDelta = new Vector2(0f, 32f);

        cartTotalText = CreateText("Total", root, "Total  0 M USD", 18f, TextAlignmentOptions.MidlineRight);
        cartTotalText.rectTransform.anchorMin = new Vector2(0.40f, 0f);
        cartTotalText.rectTransform.anchorMax = new Vector2(1f, 0f);
        cartTotalText.rectTransform.pivot = new Vector2(1f, 0f);
        cartTotalText.rectTransform.anchoredPosition = new Vector2(-12f, 6f);
        cartTotalText.rectTransform.sizeDelta = new Vector2(0f, 32f);
        cartTotalText.color = new Color(1f, 0.85f, 0.40f);
    }

    void CreateCard(SpaceMartItem item)
    {
        var card = CreateButton("Item_" + item.id, gridContent, new Color(0.10f, 0.12f, 0.20f, 1f));
        var le = card.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 96f;
        le.preferredHeight = 96f;
        le.flexibleWidth = 1f;

        var iconGo = new GameObject("Icono", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(card.transform, false);
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(12f, 0f);
        iconRt.sizeDelta = new Vector2(64f, 64f);
        var icon = iconGo.GetComponent<Image>();
        icon.color = item.color;
        icon.raycastTarget = false;

        var name = CreateText("Nombre", card.transform, item.name, 22f, TextAlignmentOptions.MidlineLeft);
        name.rectTransform.anchorMin = new Vector2(0f, 0.50f);
        name.rectTransform.anchorMax = new Vector2(1f, 1f);
        name.rectTransform.offsetMin = new Vector2(88f, 0f);
        name.rectTransform.offsetMax = new Vector2(-12f, -6f);

        var spec = CreateText("Spec", card.transform, item.spec, 16f, TextAlignmentOptions.MidlineLeft);
        spec.rectTransform.anchorMin = new Vector2(0f, 0.22f);
        spec.rectTransform.anchorMax = new Vector2(1f, 0.55f);
        spec.rectTransform.offsetMin = new Vector2(88f, 0f);
        spec.rectTransform.offsetMax = new Vector2(-12f, 0f);
        spec.color = new Color(0.80f, 0.82f, 0.88f);

        var price = CreateText("Precio", card.transform, SpaceMartCatalog.FormatPrice(item.price), 18f, TextAlignmentOptions.MidlineLeft);
        price.rectTransform.anchorMin = new Vector2(0f, 0f);
        price.rectTransform.anchorMax = new Vector2(1f, 0.28f);
        price.rectTransform.offsetMin = new Vector2(88f, 6f);
        price.rectTransform.offsetMax = new Vector2(-12f, 0f);
        price.color = new Color(1f, 0.85f, 0.40f);

        SpaceMartItem captured = item;
        card.onClick.AddListener(() => ShowDetail(captured));
    }

    void CreateCartRow(CartLine line)
    {
        var row = CreateImage("Linea_" + line.item.id, cartContent, new Color(1f, 1f, 1f, 0.04f));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;
        le.flexibleWidth = 1f;

        var label = CreateText("Texto", row, line.item.name + "  x" + line.quantity, 16f, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        label.rectTransform.offsetMax = new Vector2(-86f, 0f);

        var remove = CreateButton("Quitar", row, new Color(0.35f, 0.18f, 0.18f, 1f));
        var rrt = (RectTransform)remove.transform;
        rrt.anchorMin = new Vector2(1f, 0.15f);
        rrt.anchorMax = new Vector2(1f, 0.85f);
        rrt.pivot = new Vector2(1f, 0.5f);
        rrt.anchoredPosition = new Vector2(-6f, 0f);
        rrt.sizeDelta = new Vector2(70f, 0f);
        CreateText("Etiqueta", remove.transform, "Quitar", 14f, TextAlignmentOptions.Center);
        string id = line.item.id;
        remove.onClick.AddListener(() => RemoveFromCart(id));
    }

    RectTransform CreateScrollArea(RectTransform host, string contentName)
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(host, false);
        var vpRt = (RectTransform)viewport.transform;
        Stretch(vpRt, 0f, 0f, 0f, 0f);
        var vpImage = viewport.GetComponent<Image>();
        vpImage.color = new Color(0f, 0f, 0f, 0.15f);
        vpImage.raycastTarget = true;

        var contentGo = new GameObject(contentName, typeof(RectTransform));
        contentGo.transform.SetParent(viewport.transform, false);
        var content = (RectTransform)contentGo.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = host.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = vpRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.inertia = true;
        return content;
    }

    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static Button CreateButton(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateImage(name, parent, color);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = color;
        button.colors = colors;
        return button;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rt = CreateRect(name, parent);
        Stretch(rt, 0f, 0f, 0f, 0f);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}
