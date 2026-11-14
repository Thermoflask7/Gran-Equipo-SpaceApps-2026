using UnityEngine;

// Nuevos tipos siempre al final para no cambiar los numeros guardados en los assets.
// Catalogo de Cristina (V8.3): LaunchService = SLS Block 1 o 1B (se elige uno, ya trae sus etapas);
// Module = modulos opcionales de la nave (soporte de vida, reservas, navegacion, blindaje).
// FuelTank y Engine ya no se usan en la mision Artemis (el lanzador es un servicio completo).
public enum PartType { Capsule, Engine, FuelTank, SolarPanel, Battery, Antenna, Instrument, ServiceModule, AbortSystem, LaunchService, Module }

// Una pieza que se compra en la tienda. Los valores reales vienen del catalogo de Cristina
// (menu SpaceApps -> Importar catalogo Artemis).
[CreateAssetMenu(menuName = "SpaceApps/Pieza", fileName = "NuevaPieza")]
public class PartData : ScriptableObject
{
    public string catalogId; // id del catalogo, por ejemplo ART-CM-ORION
    public string partName;
    public PartType type;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Costo")]
    public float price; // creditos del juego (presupuesto inicial 3,400)

    [Header("Masa (kg)")]
    public float dryMass;
    public float fuelMass; // propelente del modulo de servicio despues de la translunar

    [Header("Motor")]
    public float isp;    // segundos (modulo de servicio)
    public float thrust; // kN

    [Header("Lanzador")]
    public float payloadCapacity; // kg que el SLS lleva a la translunar (solo LaunchService)

    [Header("Energia")]
    public float power;     // W que GENERA (paneles del modulo de servicio)
    public float powerDraw; // W que CONSUME
    public float batteryWh; // Wh que guarda

    [Header("Tripulacion")]
    public int crewCapacity;   // astronautas (capsula)
    public int maxMissionDays; // dias que aguanta (capsula)
    public float waterKg;      // reservas extra
    public float oxygenKg;
    public float foodKg;

    [Header("Ciencia")]
    public float sciencePoints;
    public float dataRateBps;        // velocidad de bajada de datos (antenas)
    public float experimentDataBits; // datos que produce el instrumento
}
