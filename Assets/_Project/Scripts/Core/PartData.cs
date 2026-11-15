using UnityEngine;

// New types always go at the end so the numbers saved in the assets do not change.
// Cristina's catalog (V8.3): LaunchService = standard launcher or 1B (you choose one; it already includes its stages);
// Module = optional spacecraft modules (life support, reserves, navigation, shielding).
// FuelTank and Engine are no longer used in the lunar mission (the launcher is a complete service).
public enum PartType { Capsule, Engine, FuelTank, SolarPanel, Battery, Antenna, Instrument, ServiceModule, AbortSystem, LaunchService, Module }

// A part bought in the shop. The real values come from Cristina's catalog
// (menu SpaceApps -> Import mission catalog).
[CreateAssetMenu(menuName = "SpaceApps/Part", fileName = "NewPart")]
public class PartData : ScriptableObject
{
    public string catalogId; // catalog id, for example ART-CM-ORION
    public string partName;
    public PartType type;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cost")]
    public float price; // game credits (starting budget 3,400)

    [Header("Mass (kg)")]
    public float dryMass;
    public float fuelMass; // service module propellant after translunar injection

    [Header("Engine")]
    public float isp;    // seconds (service module)
    public float thrust; // kN

    [Header("Launcher")]
    public float payloadCapacity; // kg the launcher carries to translunar injection (LaunchService only)

    [Header("Power")]
    public float power;     // W it GENERATES (service module solar arrays)
    public float powerDraw; // W it CONSUMES
    public float batteryWh; // Wh it stores

    [Header("Crew")]
    public int crewCapacity;   // astronauts (capsule)
    public int maxMissionDays; // days it can last (capsule)
    public float waterKg;      // extra reserves
    public float oxygenKg;
    public float foodKg;

    [Header("Science")]
    public float sciencePoints;
    public float dataRateBps;        // data downlink rate (antennas)
    public float experimentDataBits; // data produced by the instrument
}
