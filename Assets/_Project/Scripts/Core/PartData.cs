using UnityEngine;

public enum PartType { Capsule, Engine, FuelTank, SolarPanel, Battery, Antenna, Instrument }

// Una pieza que se compra en la tienda. Los valores reales los llena P5.
[CreateAssetMenu(menuName = "SpaceApps/Pieza", fileName = "NuevaPieza")]
public class PartData : ScriptableObject
{
    public string partName;
    public PartType type;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Costo")]
    public float price; // millones de USD

    [Header("Masa (kg)")]
    public float dryMass;
    public float fuelMass; // solo tanques

    [Header("Motor")]
    public float isp;    // segundos (solo motores)
    public float thrust; // kN (solo motores)

    [Header("Energia")]
    public float power; // W: positivo genera, negativo consume
}
