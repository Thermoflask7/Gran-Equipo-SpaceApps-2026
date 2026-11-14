using UnityEngine;

// Nuevos tipos siempre al final para no cambiar los numeros guardados en los assets
public enum PartType { Capsule, Engine, FuelTank, SolarPanel, Battery, Antenna, Instrument, ServiceModule, AbortSystem }

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
    public float fuelMass; // tanques y modulo de servicio (propelente de la nave)

    [Header("Motor")]
    public float isp;    // segundos (motores y modulo de servicio)
    public float thrust; // kN (solo motores)

    [Header("Energia")]
    public float power;     // W: positivo genera, negativo consume
    public float batteryWh; // Wh que guarda (baterias y modulo de servicio)
}
