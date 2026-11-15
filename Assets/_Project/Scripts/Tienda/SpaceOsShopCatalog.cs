using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeholder catalog for the SpaceOS shop window (SpaceMartApp).
/// Edit this ScriptableObject (or replace the items) to change the placeholder products.
/// Menu: Assets > Create > SpaceApps > SpaceOS Shop Catalog.
/// This is not Elena's SpaceMart purchase catalog (PartData / SpaceMart).
/// </summary>
[CreateAssetMenu(menuName = "SpaceApps/SpaceOS Shop Catalog", fileName = "SpaceOsShopCatalog")]
public class SpaceOsShopCatalog : ScriptableObject
{
    public List<SpaceOsShopItem> items = new List<SpaceOsShopItem>();

    public List<SpaceOsShopItem> ItemsIn(SpaceOsShopCategory category)
    {
        var list = new List<SpaceOsShopItem>();
        if (items == null) return list;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].category == category)
                list.Add(items[i]);
        }
        return list;
    }

    /// <summary>Example catalog (3 products per category) if no asset is assigned.</summary>
    public static SpaceOsShopCatalog CreateDefault()
    {
        var catalog = CreateInstance<SpaceOsShopCatalog>();
        catalog.name = "SpaceOsShopCatalog (default)";
        AddDefaultItems(catalog.items);
        return catalog;
    }

    public static void AddDefaultItems(List<SpaceOsShopItem> items)
    {
        // Rockets
        items.Add(new SpaceOsShopItem("cohete_atlas", "Atlas-C", SpaceOsShopCategory.Rockets, 12f, "Light 1-stage rocket", new Color(0.80f, 0.50f, 0.22f)));
        items.Add(new SpaceOsShopItem("cohete_saturno", "Saturn-L", SpaceOsShopCategory.Rockets, 45f, "Medium 2-stage rocket", new Color(0.85f, 0.60f, 0.30f)));
        items.Add(new SpaceOsShopItem("cohete_artemis", "Artemis Heavy", SpaceOsShopCategory.Rockets, 90f, "Heavy 3-stage rocket", new Color(0.95f, 0.70f, 0.35f)));
        // Engines
        items.Add(new SpaceOsShopItem("motor_merlin", "Merlin-G Engine", SpaceOsShopCategory.Engines, 8f, "Isp 310 s, medium thrust", new Color(0.55f, 0.55f, 0.60f)));
        items.Add(new SpaceOsShopItem("motor_rs_lunar", "RS-Lunar Engine", SpaceOsShopCategory.Engines, 18f, "Isp 450 s, cryogenic", new Color(0.40f, 0.65f, 0.85f)));
        items.Add(new SpaceOsShopItem("motor_ionico", "Ionic Engine X", SpaceOsShopCategory.Engines, 22f, "Isp 3000 s, low thrust", new Color(0.45f, 0.80f, 0.90f)));
        // Fuel
        items.Add(new SpaceOsShopItem("fuel_rp1", "RP-1 Kerosene", SpaceOsShopCategory.Fuel, 2f, "Dense and cheap, 20 t", new Color(0.70f, 0.35f, 0.15f)));
        items.Add(new SpaceOsShopItem("fuel_lh2", "LH2 Hydrogen", SpaceOsShopCategory.Fuel, 5f, "High Isp, 12 t", new Color(0.55f, 0.75f, 1.00f)));
        items.Add(new SpaceOsShopItem("fuel_lch4", "LCH4 Methane", SpaceOsShopCategory.Fuel, 3.5f, "Balanced, 16 t", new Color(0.45f, 0.85f, 0.55f)));
        // Modules
        items.Add(new SpaceOsShopItem("mod_orion", "Orion-S Capsule", SpaceOsShopCategory.Modules, 25f, "Crew of 3, reentry", new Color(0.85f, 0.85f, 0.90f)));
        items.Add(new SpaceOsShopItem("mod_servicio", "Service module", SpaceOsShopCategory.Modules, 14f, "Power and propulsion", new Color(0.60f, 0.62f, 0.70f)));
        items.Add(new SpaceOsShopItem("mod_habitat", "Short lunar habitat", SpaceOsShopCategory.Modules, 30f, "7-day stay", new Color(0.70f, 0.55f, 0.40f)));
        // Instruments
        items.Add(new SpaceOsShopItem("ins_camara", "Mapping camera", SpaceOsShopCategory.Instruments, 4f, "High-resolution imagery", new Color(0.35f, 0.70f, 0.55f)));
        items.Add(new SpaceOsShopItem("ins_espectro", "Spectrometer", SpaceOsShopCategory.Instruments, 6f, "Regolith analysis", new Color(0.55f, 0.40f, 0.80f)));
        items.Add(new SpaceOsShopItem("ins_antena", "High-gain antenna", SpaceOsShopCategory.Instruments, 3f, "Deep-space link", new Color(0.90f, 0.85f, 0.40f)));
    }

    public static string CategoryLabel(SpaceOsShopCategory category)
    {
        switch (category)
        {
            case SpaceOsShopCategory.Rockets: return "Rockets";
            case SpaceOsShopCategory.Engines: return "Engines";
            case SpaceOsShopCategory.Fuel: return "Fuel";
            case SpaceOsShopCategory.Modules: return "Modules";
            case SpaceOsShopCategory.Instruments: return "Instruments";
            default: return category.ToString();
        }
    }

    public static string FormatPrice(float price)
    {
        return price.ToString("0.##") + " M USD";
    }
}
