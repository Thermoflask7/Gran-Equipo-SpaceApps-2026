using System;
using UnityEngine;

public enum SpaceOsShopCategory
{
    Rockets,
    Engines,
    Fuel,
    Modules,
    Instruments
}

/// <summary>
/// One placeholder product in the SpaceOS shop window. Values are placeholders;
/// later they can be replaced by real PartData from Elena's SpaceMart catalog.
/// </summary>
[Serializable]
public class SpaceOsShopItem
{
    public string id;
    public string name;
    public SpaceOsShopCategory category;
    public float price;
    [TextArea] public string spec;
    public Color color = Color.gray;

    public SpaceOsShopItem() { }

    public SpaceOsShopItem(string id, string name, SpaceOsShopCategory category, float price, string spec, Color color)
    {
        this.id = id;
        this.name = name;
        this.category = category;
        this.price = price;
        this.spec = spec;
        this.color = color;
    }
}
