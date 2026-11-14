using System;
using UnityEditor;
using UnityEngine;

// Importa el catalogo de Cristina (Data/Catalogo/CatalogoArtemis.json) como ScriptableObjects.
// Menu: SpaceApps -> Importar catalogo Artemis.
//   - Crea o actualiza una PartData por pieza en Data/Piezas (archivo = id del catalogo).
//     Si el asset ya existe se actualizan sus valores y se conserva el GUID (no se rompen referencias).
//   - Crea o actualiza la mision Data/Destinos/MisionArtemis.asset (mision V13 de Cristina).
// Se puede correr las veces que sea, por ejemplo cuando Cristina actualice el catalogo.
public static class CatalogImporter
{
    const string CatalogPath = "Assets/_Project/Data/Catalogo/CatalogoArtemis.json";
    const string PartsFolder = "Assets/_Project/Data/Piezas";
    const string MissionPath = "Assets/_Project/Data/Destinos/MisionArtemis.asset";

    [Serializable]
    class CatalogFile
    {
        public string source;
        public CatalogPart[] parts;
    }

    [Serializable]
    class CatalogPart
    {
        public string id;
        public string partName;
        public string type;
        public string description;
        public float price;
        public float dryMass;
        public float fuelMass;
        public float isp;
        public float thrust;
        public float power;
        public float powerDraw;
        public float batteryWh;
        public float payloadCapacity;
        public int crewCapacity;
        public int maxMissionDays;
        public float waterKg;
        public float oxygenKg;
        public float foodKg;
        public float sciencePoints;
        public float dataRateBps;
        public float experimentDataBits;
    }

    [MenuItem("SpaceApps/Importar catalogo Artemis")]
    public static void Import()
    {
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
        if (json == null)
        {
            Debug.LogError("No encontre " + CatalogPath);
            return;
        }

        CatalogFile catalog = JsonUtility.FromJson<CatalogFile>(json.text);
        int count = 0;
        foreach (CatalogPart c in catalog.parts)
        {
            PartType type;
            if (!Enum.TryParse(c.type, out type))
            {
                Debug.LogError("Tipo desconocido '" + c.type + "' en " + c.id + ". No se importo.");
                continue;
            }

            string path = PartsFolder + "/" + c.id + ".asset";
            PartData part = AssetDatabase.LoadAssetAtPath<PartData>(path);
            bool isNew = part == null;
            if (isNew) part = ScriptableObject.CreateInstance<PartData>();

            part.catalogId = c.id;
            part.partName = c.partName;
            part.type = type;
            part.description = c.description;
            part.price = c.price;
            part.dryMass = c.dryMass;
            part.fuelMass = c.fuelMass;
            part.isp = c.isp;
            part.thrust = c.thrust;
            part.payloadCapacity = c.payloadCapacity;
            part.power = c.power;
            part.powerDraw = c.powerDraw;
            part.batteryWh = c.batteryWh;
            part.crewCapacity = c.crewCapacity;
            part.maxMissionDays = c.maxMissionDays;
            part.waterKg = c.waterKg;
            part.oxygenKg = c.oxygenKg;
            part.foodKg = c.foodKg;
            part.sciencePoints = c.sciencePoints;
            part.dataRateBps = c.dataRateBps;
            part.experimentDataBits = c.experimentDataBits;
            // El icono no se toca: lo pone el equipo de arte a mano.

            if (isNew) AssetDatabase.CreateAsset(part, path);
            else EditorUtility.SetDirty(part);
            count++;
        }

        ImportMission();
        AssetDatabase.SaveAssets();
        Debug.Log("Catalogo Artemis importado: " + count + " piezas en " + PartsFolder + " y la mision en " + MissionPath);
    }

    // Valores de SpaceMission_Mision_Artemis_V13.json (Drive de Cristina, carpeta MISION CALCULOS Y DETALLES).
    static void ImportMission()
    {
        DestinationData mission = AssetDatabase.LoadAssetAtPath<DestinationData>(MissionPath);
        bool isNew = mission == null;
        if (isNew) mission = ScriptableObject.CreateInstance<DestinationData>();

        mission.destinationName = "Orbita lunar tripulada (Artemis)";
        mission.description = "Lleva a 4 astronautas en Orion a una orbita alrededor de la Luna (NRHO), recolecta datos y regresa. Mision educativa inspirada en Artemis, no es una mision real de la NASA.";
        mission.requiredDeltaV = 1060f; // 940 nominal (40 correccion + 450 captura + 450 salida) + 120 de reserva
        mission.missionDays = 12;
        mission.locked = false;
        mission.tliExtraMass = 682f;    // 26,536 en la translunar - 25,854 despues
        mission.crewCount = 4;
        mission.waterPerPersonDay = 2.5f;
        mission.oxygenPerPersonDay = 0.84f;
        mission.foodPerPersonDay = 0.75f;
        mission.baseWaterKg = 135f;
        mission.baseOxygenKg = 47f;
        mission.baseFoodKg = 44f;
        mission.minSciencePoints = 40f;
        mission.maxDownlinkSeconds = 3600f;

        if (isNew) AssetDatabase.CreateAsset(mission, MissionPath);
        else EditorUtility.SetDirty(mission);
    }
}
