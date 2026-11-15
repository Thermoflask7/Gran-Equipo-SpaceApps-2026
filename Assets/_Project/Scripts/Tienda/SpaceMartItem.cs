using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row of the SpaceMart list: name, facts, price and a Buy / Swap / Sell button.
// SpaceMart clones the row template and calls Setup() once per part.
public class SpaceMartItem : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text infoText;
    public TMP_Text priceText;
    public TMP_Text buttonLabel;
    public Button button;

    SpaceMart shop;
    PartData part;

    public void Setup(SpaceMart shop, PartData part)
    {
        this.shop = shop;
        this.part = part;
        nameText.text = part.partName;
        infoText.text = SpaceMart.CategoryName(part.type) + " | " + SpaceMart.PartFacts(part) + "\n" + part.description;
        priceText.text = SpaceMart.Credits(part.price);
        button.onClick.AddListener(OnClick);
    }

    public void Refresh()
    {
        if (part == null) return;
        bool owned = shop.IsOwned(part);
        if (owned) buttonLabel.text = "Sell";
        else if (part.type == PartType.LaunchService && shop.OwnedLauncher() != null) buttonLabel.text = "Swap";
        else buttonLabel.text = "Buy";
        button.interactable = owned || shop.CanBuy(part); // gray when there are not enough credits
    }

    void OnClick()
    {
        if (shop.IsOwned(part)) shop.Sell(part);
        else shop.Buy(part);
    }
}
