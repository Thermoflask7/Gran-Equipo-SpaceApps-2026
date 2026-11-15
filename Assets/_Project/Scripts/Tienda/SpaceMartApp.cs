using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-monitor SpaceMart window inside the SpaceOS canvas.
/// Visual front-end only: tabs, cards, detail pane and cart.
/// Purchase logic, catalog and budget come from Elena's <see cref="SpaceMart"/> / <see cref="GameManager"/>.
/// </summary>
public class SpaceMartApp : MonoBehaviour
{
    ComputerDesktop desktop;
    SpaceMart shop;
    bool ready;

    GameObject window;
    RectTransform gridContent;
    RectTransform cartContent;
    TMP_Text moneyText;
    TMP_Text detailName;
    TMP_Text detailSpec;
    TMP_Text detailPrice;
    TMP_Text actionLabel;
    TMP_Text cartCountText;
    TMP_Text cartStatsText;
    TMP_Text feedbackText;
    Image detailIcon;
    Button actionButton;
    readonly List<Image> tabImages = new List<Image>();
    readonly List<GameObject> tabDots = new List<GameObject>(); // red dots, same order as SpaceMart.Filters

    string currentFilter = "All";
    PartData selected;

    static readonly Color TabOff = new Color(0.12f, 0.14f, 0.22f, 1f);
    static readonly Color TabOn = new Color(0.95f, 0.55f, 0.15f, 1f);
    static readonly Color CardColor = new Color(0.10f, 0.12f, 0.20f, 1f);
    static readonly Color PriceColor = new Color(1f, 0.85f, 0.40f);
    static readonly Color RequiredColor = new Color(0.95f, 0.22f, 0.22f, 1f);
    static Sprite dotSprite;

    void Awake() => Setup();
    void Start() => Setup();

    void OnDestroy()
    {
        if (desktop != null)
        {
            desktop.onHidden.RemoveListener(Close);
            desktop.onShown.RemoveListener(Close);
        }
    }

    void Setup()
    {
        if (ready) return;
        desktop = GetComponent<ComputerDesktop>();
        if (desktop == null) desktop = ComputerDesktop.Instance;
        if (desktop == null) return;

        shop = GetComponent<SpaceMart>();
        if (shop == null) shop = gameObject.AddComponent<SpaceMart>();
        shop.EnsureCatalog();

        BuildWindow();
        WireShopUi();

        desktop.RegisterApp(ComputerDesktop.AppSpaceMart, "SpaceMart", new Color(0.95f, 0.55f, 0.15f), Open);
        desktop.onHidden.AddListener(Close);
        desktop.onShown.AddListener(Close);
        ready = true;
        Close();
    }

    public void Open()
    {
        if (!ready) Setup();
        if (window == null) return;
        SelectFilter(currentFilter);
        window.SetActive(true);
        window.transform.SetAsLastSibling();
        BringOverlayToFront();
    }

    public void Close()
    {
        if (window != null) window.SetActive(false);
    }

    void SelectFilter(string filter)
    {
        currentFilter = filter;
        if (shop != null) shop.SetFilter(filter);
        else ApplyTabColors();
        RebuildGrid();
        RefreshView();
    }

    void ApplyTabColors()
    {
        for (int i = 0; i < tabImages.Count && i < SpaceMart.Filters.Length; i++)
        {
            if (tabImages[i] != null)
                tabImages[i].color = SpaceMart.Filters[i] == currentFilter ? TabOn : TabOff;
        }
    }

    void RebuildGrid()
    {
        if (gridContent == null || shop == null) return;
        ClearChildren(gridContent);

        List<PartData> parts = VisibleParts();
        for (int i = 0; i < parts.Count; i++)
            CreateCard(parts[i]);

        if (parts.Count == 0)
        {
            string msg = shop.catalog == null || shop.catalog.Count == 0
                ? "No catalog. Run SpaceApps > Rebuild game data list."
                : "No parts in this category.";
            var empty = CreateText("Empty", gridContent, msg, 20f, TextAlignmentOptions.Center);
            var le = empty.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 80f;
            le.preferredHeight = 80f;
        }

        if (selected == null && parts.Count > 0) selected = parts[0];
        else if (selected != null && !parts.Contains(selected) && currentFilter != "All")
        {
            // keep the selection even if it is hidden by the tab
        }

        Canvas.ForceUpdateCanvases();
    }

    List<PartData> VisibleParts()
    {
        var list = new List<PartData>();
        if (shop == null || shop.catalog == null) return list;
        for (int i = 0; i < shop.catalog.Count; i++)
        {
            PartData part = shop.catalog[i];
            if (part == null) continue;
            if (currentFilter == "All" || SpaceMart.FilterOf(part.type) == currentFilter)
                list.Add(part);
        }
        return list;
    }

    void ShowDetail(PartData part)
    {
        selected = part;
        RefreshDetail();
    }

    void RefreshView()
    {
        if (shop != null) shop.Refresh();
        else if (moneyText != null && GameManager.Instance != null)
            moneyText.text = "Credits: " + SpaceMart.Credits(GameManager.Instance.money);

        ApplyTabColors();
        RefreshDetail();
        RefreshCart();
        RefreshCardBadges();
        RefreshRequiredDots();
    }

    void RefreshDetail()
    {
        bool has = selected != null;
        if (detailIcon != null)
        {
            if (has && selected.icon != null)
            {
                detailIcon.sprite = selected.icon;
                detailIcon.color = Color.white;
            }
            else
            {
                detailIcon.sprite = null;
                detailIcon.color = has ? ColorOf(selected) : new Color(0.2f, 0.2f, 0.25f);
            }
        }
        if (detailName != null)
            detailName.text = has ? selected.partName : "Select a product";
        if (detailSpec != null)
        {
            if (!has) detailSpec.text = "";
            else
            {
                string facts = SpaceMart.PartFacts(selected);
                detailSpec.text = SpaceMart.CategoryName(selected.type)
                    + (string.IsNullOrEmpty(facts) ? "" : "\n" + facts)
                    + (string.IsNullOrEmpty(selected.description) ? "" : "\n\n" + selected.description);
            }
        }
        if (detailPrice != null)
        {
            if (!has) detailPrice.text = "";
            else if (shop != null && shop.IsOwned(selected))
                detailPrice.text = "Owned  " + SpaceMart.Credits(selected.price);
            else if (shop != null)
                detailPrice.text = SpaceMart.Credits(shop.CostOf(selected));
            else
                detailPrice.text = SpaceMart.Credits(selected.price);
        }

        RefreshActionButton();
    }

    void RefreshActionButton()
    {
        if (actionButton == null || actionLabel == null) return;
        if (selected == null || shop == null)
        {
            actionLabel.text = "Buy";
            actionButton.interactable = false;
            return;
        }

        if (shop.IsOwned(selected))
        {
            actionLabel.text = "Sell";
            actionButton.interactable = true;
        }
        else if (selected.type == PartType.LaunchService && shop.OwnedLauncher() != null)
        {
            actionLabel.text = "Swap";
            actionButton.interactable = shop.CanBuy(selected);
        }
        else
        {
            actionLabel.text = "Buy";
            actionButton.interactable = shop.CanBuy(selected);
        }
    }

    void OnActionClicked()
    {
        if (selected == null || shop == null) return;
        if (shop.IsOwned(selected)) shop.Sell(selected);
        else shop.Buy(selected);
        RefreshView();
    }

    void RefreshCart()
    {
        if (cartContent == null) return;

        // Keep the stats block (first child) and rebuild the owned-part rows.
        for (int i = cartContent.childCount - 1; i >= 0; i--)
        {
            Transform child = cartContent.GetChild(i);
            if (child.name == "Stats") continue;
            DestroyChild(child.gameObject);
        }

        List<PartData> owned = new List<PartData>();
        if (GameManager.Instance != null && GameManager.Instance.boughtParts != null)
        {
            for (int i = 0; i < GameManager.Instance.boughtParts.Count; i++)
            {
                PartData part = GameManager.Instance.boughtParts[i];
                if (part != null) owned.Add(part);
            }
        }

        for (int i = 0; i < owned.Count; i++)
            CreateCartRow(owned[i]);

        if (cartCountText != null)
            cartCountText.text = owned.Count == 1 ? "1 part owned" : owned.Count + " parts owned";
    }

    void RefreshCardBadges()
    {
        if (gridContent == null || shop == null) return;
        for (int i = 0; i < gridContent.childCount; i++)
        {
            Transform card = gridContent.GetChild(i);
            TMP_Text badge = null;
            Transform badgeTr = card.Find("Owned");
            if (badgeTr != null) badge = badgeTr.GetComponent<TMP_Text>();
            if (badge == null) continue;
            string id = card.name.StartsWith("Item_") ? card.name.Substring(5) : "";
            PartData part = FindPart(id);
            badge.text = (part != null && shop.IsOwned(part)) ? "Owned" : "";
        }
    }

    // Red dot on the tabs and cards of required types with no part bought yet
    void RefreshRequiredDots()
    {
        for (int i = 0; i < tabDots.Count && i < SpaceMart.Filters.Length; i++)
        {
            bool missing = false;
            foreach (PartType type in RocketAssembly.RequiredTypes)
                if (SpaceMart.FilterOf(type) == SpaceMart.Filters[i] && IsRequiredMissing(type)) missing = true;
            if (tabDots[i] != null) tabDots[i].SetActive(missing);
        }

        if (gridContent == null) return;
        for (int i = 0; i < gridContent.childCount; i++)
        {
            Transform card = gridContent.GetChild(i);
            Transform dot = card.Find("RequiredDot");
            if (dot == null) continue;
            string id = card.name.StartsWith("Item_") ? card.name.Substring(5) : "";
            PartData part = FindPart(id);
            dot.gameObject.SetActive(part != null && IsRequiredMissing(part.type));
        }
    }

    // True if the type is required for launch and no part of that type was bought
    static bool IsRequiredMissing(PartType type)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;
        if (System.Array.IndexOf(RocketAssembly.RequiredTypes, type) < 0) return false;
        foreach (PartData p in gm.boughtParts)
            if (p != null && p.type == type) return false;
        return true;
    }

    PartData FindPart(string catalogId)
    {
        if (shop == null || shop.catalog == null) return null;
        for (int i = 0; i < shop.catalog.Count; i++)
        {
            PartData part = shop.catalog[i];
            if (part != null && part.catalogId == catalogId) return part;
        }
        return null;
    }

    void WireShopUi()
    {
        if (shop == null) return;
        shop.moneyText = moneyText;
        shop.statsText = cartStatsText;
        shop.feedbackText = feedbackText;
        shop.filterTabs = new List<Image>(tabImages);
        shop.Refresh();
    }

    void BringOverlayToFront()
    {
        var apagada = desktop.transform.Find("Root/Pantalla/Apagada");
        if (apagada != null) apagada.SetAsLastSibling();
    }

    static Color ColorOf(PartData part)
    {
        if (part == null) return Color.gray;
        string filter = SpaceMart.FilterOf(part.type);
        if (filter == "Launcher") return new Color(0.95f, 0.60f, 0.25f);
        if (filter == "Spacecraft") return new Color(0.85f, 0.85f, 0.90f);
        if (filter == "Science") return new Color(0.40f, 0.75f, 0.55f);
        if (filter == "Power") return new Color(0.40f, 0.75f, 0.95f);
        return new Color(0.70f, 0.55f, 0.40f);
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            DestroyChild(parent.GetChild(i).gameObject);
    }

    static void DestroyChild(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
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

        var title = CreateImage("Title", window.transform, new Color(0.08f, 0.10f, 0.20f, 1f));
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = Vector2.zero;
        title.sizeDelta = new Vector2(0f, 52f);
        var accent = CreateImage("Accent", title, new Color(0.95f, 0.55f, 0.15f, 1f));
        accent.anchorMin = new Vector2(0f, 0f);
        accent.anchorMax = new Vector2(1f, 0f);
        accent.pivot = new Vector2(0.5f, 0f);
        accent.anchoredPosition = Vector2.zero;
        accent.sizeDelta = new Vector2(0f, 3f);
        var titleLabel = CreateText("Name", title, "SpaceMart", 26f, TextAlignmentOptions.MidlineLeft);
        titleLabel.rectTransform.offsetMin = new Vector2(16f, 0f);
        titleLabel.rectTransform.offsetMax = new Vector2(-280f, 0f);

        moneyText = CreateText("Credits", title, "Credits: 0 cr", 20f, TextAlignmentOptions.MidlineRight);
        moneyText.color = PriceColor;
        moneyText.rectTransform.offsetMin = new Vector2(200f, 0f);
        moneyText.rectTransform.offsetMax = new Vector2(-64f, 0f);

        var legend = CreateRect("RequiredLegend", title);
        legend.anchorMin = legend.anchorMax = legend.pivot = new Vector2(0f, 0.5f);
        legend.anchoredPosition = new Vector2(170f, 0f);
        legend.sizeDelta = new Vector2(260f, 30f);
        CreateDot(legend, new Vector2(0f, 0.5f), new Vector2(7f, 0f), 14f).SetActive(true);
        var legendLabel = CreateText("Label", legend, "Required for launch", 16f, TextAlignmentOptions.MidlineLeft);
        legendLabel.rectTransform.offsetMin = new Vector2(22f, 0f);
        legendLabel.color = RequiredColor;

        var close = CreateButton("Close", title, new Color(0.75f, 0.20f, 0.20f, 1f));
        var closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = closeRt.anchorMax = closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-10f, 0f);
        closeRt.sizeDelta = new Vector2(44f, 36f);
        CreateText("X", close.transform, "X", 22f, TextAlignmentOptions.Center);
        close.onClick.AddListener(Close);

        var tabs = CreateRect("Tabs", window.transform);
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
        tabImages.Clear();
        tabDots.Clear();
        for (int i = 0; i < SpaceMart.Filters.Length; i++)
        {
            string filter = SpaceMart.Filters[i];
            var tab = CreateButton("Tab_" + filter, tabs, TabOff);
            var le = tab.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 36f;
            CreateText("Label", tab.transform, filter, 16f, TextAlignmentOptions.Center);
            string captured = filter;
            tab.onClick.AddListener(() => SelectFilter(captured));
            tabImages.Add(tab.GetComponent<Image>());
            tabDots.Add(CreateDot(tab.transform, new Vector2(1f, 1f), new Vector2(-9f, -9f), 12f));
        }

        var body = CreateRect("Body", window.transform);
        Stretch(body, 8f, 36f, 8f, 108f);

        var listPane = CreateRect("List", body);
        listPane.anchorMin = Vector2.zero;
        listPane.anchorMax = new Vector2(0.60f, 1f);
        listPane.offsetMin = Vector2.zero;
        listPane.offsetMax = new Vector2(-6f, 0f);
        gridContent = CreateScrollArea(listPane, "Products");

        var right = CreateRect("Right", body);
        right.anchorMin = new Vector2(0.60f, 0f);
        right.anchorMax = Vector2.one;
        right.offsetMin = new Vector2(6f, 0f);
        right.offsetMax = Vector2.zero;

        var detailRoot = CreateImage("Detail", right, new Color(0.07f, 0.09f, 0.16f, 1f));
        detailRoot.anchorMin = new Vector2(0f, 0.42f);
        detailRoot.anchorMax = Vector2.one;
        detailRoot.offsetMin = Vector2.zero;
        detailRoot.offsetMax = Vector2.zero;
        BuildDetail(detailRoot);

        var cartPane = CreateImage("Cart", right, new Color(0.07f, 0.09f, 0.16f, 1f));
        cartPane.anchorMin = Vector2.zero;
        cartPane.anchorMax = new Vector2(1f, 0.42f);
        cartPane.offsetMin = Vector2.zero;
        cartPane.offsetMax = new Vector2(0f, -6f);
        BuildCart(cartPane);

        feedbackText = CreateText("Feedback", window.transform, "", 16f, TextAlignmentOptions.MidlineLeft);
        Stretch(feedbackText.rectTransform, 16f, 8f, 16f, 0f);
        feedbackText.rectTransform.anchorMin = new Vector2(0f, 0f);
        feedbackText.rectTransform.anchorMax = new Vector2(1f, 0f);
        feedbackText.rectTransform.pivot = new Vector2(0.5f, 0f);
        feedbackText.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        feedbackText.rectTransform.sizeDelta = new Vector2(-32f, 24f);
        feedbackText.color = new Color(0.85f, 0.88f, 0.70f);

        window.transform.SetAsLastSibling();
        BringOverlayToFront();
    }

    void BuildDetail(RectTransform root)
    {
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
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

        detailName = CreateText("Name", root, "Select a product", 24f, TextAlignmentOptions.MidlineLeft);
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

        detailPrice = CreateText("Price", root, "", 22f, TextAlignmentOptions.MidlineLeft);
        detailPrice.rectTransform.anchorMin = new Vector2(0f, 0f);
        detailPrice.rectTransform.anchorMax = new Vector2(0.45f, 0f);
        detailPrice.rectTransform.pivot = new Vector2(0f, 0f);
        detailPrice.rectTransform.anchoredPosition = new Vector2(16f, 16f);
        detailPrice.rectTransform.sizeDelta = new Vector2(0f, 44f);
        detailPrice.color = PriceColor;

        actionButton = CreateButton("Action", root, new Color(0.95f, 0.55f, 0.15f, 1f));
        var addRt = (RectTransform)actionButton.transform;
        addRt.anchorMin = new Vector2(1f, 0f);
        addRt.anchorMax = new Vector2(1f, 0f);
        addRt.pivot = new Vector2(1f, 0f);
        addRt.anchoredPosition = new Vector2(-12f, 12f);
        addRt.sizeDelta = new Vector2(210f, 48f);
        actionLabel = CreateText("Label", actionButton.transform, "Buy", 18f, TextAlignmentOptions.Center);
        actionButton.onClick.AddListener(OnActionClicked);
        actionButton.interactable = false;
    }

    void BuildCart(RectTransform root)
    {
        var header = CreateText("CartTitle", root, "Your cart", 20f, TextAlignmentOptions.MidlineLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 1f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.pivot = new Vector2(0.5f, 1f);
        header.rectTransform.anchoredPosition = Vector2.zero;
        header.rectTransform.sizeDelta = new Vector2(-16f, 32f);
        header.rectTransform.offsetMin = new Vector2(12f, header.rectTransform.offsetMin.y);

        var listHost = CreateRect("CartList", root);
        Stretch(listHost, 8f, 44f, 8f, 32f);
        cartContent = CreateScrollArea(listHost, "Lines");

        cartStatsText = CreateText("Stats", cartContent, "", 15f, TextAlignmentOptions.TopLeft);
        var statsLe = cartStatsText.gameObject.AddComponent<LayoutElement>();
        statsLe.minHeight = 80f;
        statsLe.flexibleWidth = 1f;
        cartStatsText.color = new Color(0.80f, 0.82f, 0.88f);
        cartStatsText.overflowMode = TextOverflowModes.Overflow;

        cartCountText = CreateText("Count", root, "0 parts owned", 16f, TextAlignmentOptions.MidlineLeft);
        cartCountText.rectTransform.anchorMin = new Vector2(0f, 0f);
        cartCountText.rectTransform.anchorMax = new Vector2(1f, 0f);
        cartCountText.rectTransform.pivot = new Vector2(0f, 0f);
        cartCountText.rectTransform.anchoredPosition = new Vector2(12f, 6f);
        cartCountText.rectTransform.sizeDelta = new Vector2(-24f, 32f);
    }

    void CreateCard(PartData part)
    {
        string id = string.IsNullOrEmpty(part.catalogId) ? part.partName : part.catalogId;
        var card = CreateButton("Item_" + id, gridContent, CardColor);
        var le = card.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 280f;
        le.preferredHeight = 280f;
        le.flexibleWidth = 1f;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(card.transform, false);
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(16f, 0f);
        iconRt.sizeDelta = new Vector2(160f, 160f);
        var icon = iconGo.GetComponent<Image>();
        if (part.icon != null)
        {
            icon.sprite = part.icon;
            icon.color = Color.white;
        }
        else icon.color = ColorOf(part);
        icon.raycastTarget = false;

        var name = CreateText("Name", card.transform, part.partName, 22f, TextAlignmentOptions.MidlineLeft);
        name.rectTransform.anchorMin = new Vector2(0f, 0.62f);
        name.rectTransform.anchorMax = new Vector2(1f, 1f);
        name.rectTransform.offsetMin = new Vector2(196f, 0f);
        name.rectTransform.offsetMax = new Vector2(-12f, -6f);

        string facts = SpaceMart.PartFacts(part);
        var spec = CreateText("Spec", card.transform, string.IsNullOrEmpty(facts) ? part.description : facts, 16f, TextAlignmentOptions.MidlineLeft);
        spec.rectTransform.anchorMin = new Vector2(0f, 0.28f);
        spec.rectTransform.anchorMax = new Vector2(1f, 0.64f);
        spec.rectTransform.offsetMin = new Vector2(196f, 0f);
        spec.rectTransform.offsetMax = new Vector2(-12f, 0f);
        spec.color = new Color(0.80f, 0.82f, 0.88f);

        var price = CreateText("Price", card.transform, SpaceMart.Credits(part.price), 18f, TextAlignmentOptions.MidlineLeft);
        price.rectTransform.anchorMin = new Vector2(0f, 0f);
        price.rectTransform.anchorMax = new Vector2(1f, 0.30f);
        price.rectTransform.offsetMin = new Vector2(196f, 6f);
        price.rectTransform.offsetMax = new Vector2(-12f, 0f);
        price.color = PriceColor;

        var badge = CreateText("Owned", card.transform, shop != null && shop.IsOwned(part) ? "Owned" : "", 16f, TextAlignmentOptions.MidlineRight);
        badge.rectTransform.anchorMin = new Vector2(0f, 0f);
        badge.rectTransform.anchorMax = new Vector2(1f, 0.22f);
        badge.rectTransform.offsetMin = new Vector2(196f, 6f);
        badge.rectTransform.offsetMax = new Vector2(-12f, 0f);
        badge.color = new Color(0.55f, 0.90f, 0.55f);

        GameObject dot = CreateDot(card.transform, new Vector2(0f, 1f), new Vector2(12f, -12f), 16f);
        dot.SetActive(IsRequiredMissing(part.type));

        PartData captured = part;
        card.onClick.AddListener(() => ShowDetail(captured));
    }

    void CreateCartRow(PartData part)
    {
        var row = CreateImage("Line_" + part.catalogId, cartContent, new Color(1f, 1f, 1f, 0.04f));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;
        le.flexibleWidth = 1f;

        var label = CreateText("Text", row, part.partName + "  " + SpaceMart.Credits(part.price), 16f, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        label.rectTransform.offsetMax = new Vector2(-86f, 0f);

        var sell = CreateButton("Sell", row, new Color(0.35f, 0.18f, 0.18f, 1f));
        var rrt = (RectTransform)sell.transform;
        rrt.anchorMin = new Vector2(1f, 0.15f);
        rrt.anchorMax = new Vector2(1f, 0.85f);
        rrt.pivot = new Vector2(1f, 0.5f);
        rrt.anchoredPosition = new Vector2(-6f, 0f);
        rrt.sizeDelta = new Vector2(70f, 0f);
        CreateText("Label", sell.transform, "Sell", 14f, TextAlignmentOptions.Center);
        PartData captured = part;
        sell.onClick.AddListener(() =>
        {
            if (shop != null) shop.Sell(captured);
            if (selected == captured) ShowDetail(captured);
            RefreshView();
        });
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

    // Small red circle (inactive until RefreshRequiredDots turns it on)
    static GameObject CreateDot(Transform parent, Vector2 anchor, Vector2 position, float size)
    {
        RectTransform rt = CreateImage("RequiredDot", parent, RequiredColor);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(size, size);
        var image = rt.GetComponent<Image>();
        image.sprite = DotSprite();
        image.raycastTarget = false;
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    // Circle sprite made in code (the built-in Knob sprite is only reachable from the editor)
    static Sprite DotSprite()
    {
        if (dotSprite != null) return dotSprite;
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        dotSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return dotSprite;
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
        colors.disabledColor = new Color(0.25f, 0.25f, 0.28f, 0.7f);
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
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}
