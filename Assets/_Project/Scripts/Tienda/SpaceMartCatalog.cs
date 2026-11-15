using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalogo de SpaceMart. Edita este ScriptableObject (o sustituye los items)
/// para cambiar los productos placeholder.
/// Menu: Assets > Create > SpaceApps > SpaceMart Catalog.
/// </summary>
[CreateAssetMenu(menuName = "SpaceApps/SpaceMart Catalog", fileName = "SpaceMartCatalog")]
public class SpaceMartCatalog : ScriptableObject
{
    public List<SpaceMartItem> items = new List<SpaceMartItem>();

    public List<SpaceMartItem> ItemsIn(SpaceMartCategory category)
    {
        var list = new List<SpaceMartItem>();
        if (items == null) return list;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].category == category)
                list.Add(items[i]);
        }
        return list;
    }

    /// <summary>Catalogo de ejemplo (3 productos por categoria) si no hay asset asignado.</summary>
    public static SpaceMartCatalog CreateDefault()
    {
        var catalog = CreateInstance<SpaceMartCatalog>();
        catalog.name = "SpaceMartCatalog (default)";
        AddDefaultItems(catalog.items);
        return catalog;
    }

    public static void AddDefaultItems(List<SpaceMartItem> items)
    {
        // Cohetes
        items.Add(new SpaceMartItem("cohete_atlas", "Atlas-C", SpaceMartCategory.Cohetes, 12f, "Cohete ligero de 1 etapa", new Color(0.80f, 0.50f, 0.22f)));
        items.Add(new SpaceMartItem("cohete_saturno", "Saturno-L", SpaceMartCategory.Cohetes, 45f, "Cohete medio de 2 etapas", new Color(0.85f, 0.60f, 0.30f)));
        items.Add(new SpaceMartItem("cohete_artemis", "Artemis Heavy", SpaceMartCategory.Cohetes, 90f, "Cohete pesado de 3 etapas", new Color(0.95f, 0.70f, 0.35f)));
        // Motores
        items.Add(new SpaceMartItem("motor_merlin", "Motor Merlin-G", SpaceMartCategory.Motores, 8f, "Isp 310 s, empuje medio", new Color(0.55f, 0.55f, 0.60f)));
        items.Add(new SpaceMartItem("motor_rs_lunar", "Motor RS-Lunar", SpaceMartCategory.Motores, 18f, "Isp 450 s, criogenico", new Color(0.40f, 0.65f, 0.85f)));
        items.Add(new SpaceMartItem("motor_ionico", "Motor Ionico X", SpaceMartCategory.Motores, 22f, "Isp 3000 s, bajo empuje", new Color(0.45f, 0.80f, 0.90f)));
        // Combustible
        items.Add(new SpaceMartItem("fuel_rp1", "Queroseno RP-1", SpaceMartCategory.Combustible, 2f, "Denso y barato, 20 t", new Color(0.70f, 0.35f, 0.15f)));
        items.Add(new SpaceMartItem("fuel_lh2", "Hidrogeno LH2", SpaceMartCategory.Combustible, 5f, "Alto Isp, 12 t", new Color(0.55f, 0.75f, 1.00f)));
        items.Add(new SpaceMartItem("fuel_lch4", "Metano LCH4", SpaceMartCategory.Combustible, 3.5f, "Equilibrado, 16 t", new Color(0.45f, 0.85f, 0.55f)));
        // Modulos
        items.Add(new SpaceMartItem("mod_orion", "Capsula Orion-S", SpaceMartCategory.Modulos, 25f, "Tripulacion 3, reentrada", new Color(0.85f, 0.85f, 0.90f)));
        items.Add(new SpaceMartItem("mod_servicio", "Modulo de servicio", SpaceMartCategory.Modulos, 14f, "Energia y propulsion", new Color(0.60f, 0.62f, 0.70f)));
        items.Add(new SpaceMartItem("mod_habitat", "Habitat lunar breve", SpaceMartCategory.Modulos, 30f, "Estadia de 7 dias", new Color(0.70f, 0.55f, 0.40f)));
        // Instrumentos
        items.Add(new SpaceMartItem("ins_camara", "Camara de cartografia", SpaceMartCategory.Instrumentos, 4f, "Imagenes de alta resolucion", new Color(0.35f, 0.70f, 0.55f)));
        items.Add(new SpaceMartItem("ins_espectro", "Espectrometro", SpaceMartCategory.Instrumentos, 6f, "Analisis de regolito", new Color(0.55f, 0.40f, 0.80f)));
        items.Add(new SpaceMartItem("ins_antena", "Antena de alta ganancia", SpaceMartCategory.Instrumentos, 3f, "Enlace de espacio profundo", new Color(0.90f, 0.85f, 0.40f)));
    }

    public static string CategoryLabel(SpaceMartCategory category)
    {
        switch (category)
        {
            case SpaceMartCategory.Cohetes: return "Cohetes";
            case SpaceMartCategory.Motores: return "Motores";
            case SpaceMartCategory.Combustible: return "Combustible";
            case SpaceMartCategory.Modulos: return "Módulos";
            case SpaceMartCategory.Instrumentos: return "Instrumentos";
            default: return category.ToString();
        }
    }

    public static string FormatPrice(float price)
    {
        return price.ToString("0.##") + " M USD";
    }
}