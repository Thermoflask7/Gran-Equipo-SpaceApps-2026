using System;
using UnityEngine;

public enum SpaceMartCategory
{
    Cohetes,
    Motores,
    Combustible,
    Modulos,
    Instrumentos
}

/// <summary>
/// Una pieza de la tienda SpaceMart. Valores placeholder; mas adelante se pueden
/// reemplazar por PartData reales.
/// </summary>
[Serializable]
public class SpaceMartItem
{
    public string id;
    public string name;
    public SpaceMartCategory category;
    public float price;
    [TextArea] public string spec;
    public Color color = Color.gray;

    public SpaceMartItem() { }

    public SpaceMartItem(string id, string name, SpaceMartCategory category, float price, string spec, Color color)
    {
        this.id = id;
        this.name = name;
        this.category = category;
        this.price = price;
        this.spec = spec;
        this.color = color;
    }
}