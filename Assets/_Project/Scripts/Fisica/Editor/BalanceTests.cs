using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Pruebas de balanceo (P5) con las piezas reales del catalogo Artemis.
// Menu: SpaceApps -> Pruebas de balanceo. Escribe en la consola cada prueba con OK o FALLA.
//
// Recorre todas las combinaciones: 1 lanzador (Block 1 o 1B) + Orion (capsula, modulo de servicio,
// torre de escape) + hasta una unidad de cada uno de los 11 modulos opcionales = 2 x 2^11 = 4,096.
// Para cada una revisa por separado presupuesto, masa, delta-v, consumibles, energia, bateria,
// ciencia y comunicaciones, y compara con la enumeracion de P5 (Docs/P5/balance.py).
// Las cuentas cambian si cambian el catalogo, la mision o los 3,400 creditos: si eso es a proposito,
// hay que actualizar los valores esperados (y avisar al equipo del efecto en el balanceo).
public static class BalanceTests
{
    const float Budget = 3400f;
    const string PartsFolder = "Assets/_Project/Data/Piezas/";

    static readonly string[] Optional = {
        "ART-CREW-HAB-BACKUP", "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD",
        "ART-PWR-BACKUP", "ART-RAD-SHIELD", "ART-NAV-LUNAR", "ART-COMMS-DS",
        "ART-SCI-LUNAR", "ART-SCI-RAD", "ART-SCI-HEAVY" };
    static readonly string[] Launchers = { "ART-SLS-BLOCK1", "ART-SLS-BLOCK1B" };
    static readonly string[] Categories = { "BUDGET", "MASS", "DELTA_V", "CONSUMABLES", "POWER", "BATTERY", "SCIENCE", "DOWNLINK" };

    static int passed;
    static int failed;

    [MenuItem("SpaceApps/Pruebas de balanceo")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        DestinationData baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (baseMission == null || Part("ART-CM-ORION") == null)
        {
            Debug.LogError("No se pudo importar el catalogo: no se corrieron las pruebas.");
            return;
        }

        // Valores esperados de la enumeracion de P5 (12 y 15 dias, 3,400 creditos, sin repetir piezas)
        Enumerate(Mission(baseMission, 12), 78, 2940f, 1,
            new Dictionary<string, int> { { "BUDGET", 2401 }, { "MASS", 1202 }, { "DELTA_V", 622 }, { "CONSUMABLES", 0 },
                { "POWER", 0 }, { "BATTERY", 0 }, { "SCIENCE", 1536 }, { "DOWNLINK", 1792 } });
        Enumerate(Mission(baseMission, 15), 5, 3140f, 0,
            new Dictionary<string, int> { { "BUDGET", 2401 }, { "MASS", 1202 }, { "DELTA_V", 622 }, { "CONSUMABLES", 3584 },
                { "POWER", 0 }, { "BATTERY", 0 }, { "SCIENCE", 1536 }, { "DOWNLINK", 1792 } });

        StrategyTests(Mission(baseMission, 12), Mission(baseMission, 15));
        EdgeCases(baseMission);

        string summary = "Pruebas de balanceo: " + passed + " OK, " + failed + " FALLA";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // ---------- Enumeracion ----------

    static void Enumerate(DestinationData mission, int expectedViable, float expectedCheapest, int expectedBlock1BViable,
        Dictionary<string, int> expectedFails)
    {
        string tag = mission.missionDays + " dias";
        Dictionary<string, int> fails = new Dictionary<string, int>();
        foreach (string c in Categories) fails[c] = 0;
        int total = 0;
        int viable = 0;
        int block1BViable = 0;
        int mismatches = 0;
        float cheapest = float.MaxValue;
        float maxLoad = 0f;

        foreach (string launcher in Launchers)
        {
            for (int mask = 0; mask < (1 << Optional.Length); mask++)
            {
                List<PartData> parts = Rocket(launcher, mask);
                List<string> problems = Problems(parts, mission);
                total++;
                foreach (string p in problems) fails[p]++;
                maxLoad = Mathf.Max(maxLoad, RocketPhysics.PowerConsumption(parts));

                bool ok = problems.Count == 0;
                // ValidationMessage + presupuesto deben dar lo mismo que las revisiones por separado
                bool physicsOk = RocketPhysics.IsRocketValid(parts, mission) && RocketPhysics.TotalPrice(parts) <= Budget;
                if (ok != physicsOk) mismatches++;

                if (!ok) continue;
                viable++;
                if (launcher == "ART-SLS-BLOCK1B") block1BViable++;
                cheapest = Mathf.Min(cheapest, RocketPhysics.TotalPrice(parts));
            }
        }

        Check(tag + ": 4,096 combinaciones", total == 4096);
        Check(tag + ": " + expectedViable + " viables (salieron " + viable + ")", viable == expectedViable);
        Near(tag + ": la viable mas barata", cheapest, expectedCheapest);
        Check(tag + ": " + expectedBlock1BViable + " viables con Block 1B (salieron " + block1BViable + ")", block1BViable == expectedBlock1BViable);
        Check(tag + ": RocketPhysics.ValidationMessage coincide con las revisiones por separado (" + mismatches + " distintas)", mismatches == 0);
        foreach (string c in Categories)
            Check(tag + ": fallan por " + c + " = " + expectedFails[c] + " (salieron " + fails[c] + ")", fails[c] == expectedFails[c]);
        Debug.Log(tag + ": consumo maximo posible de la nave = " + maxLoad + " W (genera 11,000 W; la energia no limita con el catalogo actual).");
    }

    // Revisa cada requisito por separado (pueden fallar varios a la vez).
    static List<string> Problems(List<PartData> parts, DestinationData mission)
    {
        List<string> list = new List<string>();
        if (RocketPhysics.TotalPrice(parts) > Budget) list.Add("BUDGET");
        if (RocketPhysics.DeliveryMargin(parts, mission) < 0f) list.Add("MASS");
        if (RocketPhysics.SpacecraftDeltaV(parts) < mission.requiredDeltaV) list.Add("DELTA_V");
        if (RocketPhysics.WaterMargin(parts, mission) < 0f || RocketPhysics.OxygenMargin(parts, mission) < 0f
            || RocketPhysics.FoodMargin(parts, mission) < 0f) list.Add("CONSUMABLES");
        if (RocketPhysics.PowerBalance(parts) < 0f) list.Add("POWER");
        if (!RocketPhysics.BatteryCoversOutage(parts)) list.Add("BATTERY");
        if (RocketPhysics.SciencePoints(parts) < mission.minSciencePoints) list.Add("SCIENCE");
        if (RocketPhysics.DownlinkSeconds(parts) > mission.maxDownlinkSeconds) list.Add("DOWNLINK");
        return list;
    }

    static List<PartData> Rocket(string launcher, int mask)
    {
        List<PartData> parts = new List<PartData> { Part(launcher), Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        for (int i = 0; i < Optional.Length; i++)
        {
            if ((mask & (1 << i)) != 0) parts.Add(Part(Optional[i]));
        }
        return parts;
    }

    // ---------- Estrategias distintas que deben seguir siendo viables ----------

    static void StrategyTests(DestinationData mission12, DestinationData mission15)
    {
        Viable("Estrategia economica: Block 1 + antena + 2 instrumentos chicos (12 dias)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD"), mission12);
        Viable("Estrategia de ciencia pesada: Block 1B + antena + instrumento pesado (12 dias)",
            Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY"), mission12);
        Viable("Estrategia de mision larga: Block 1 + reservas de agua, oxigeno y comida (15 dias)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
                "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD"), mission15);
        Viable("Estrategia segura: Block 1 + navegacion lunar + reserva de comida (12 dias)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD", "ART-NAV-LUNAR", "ART-CREW-RES-FOOD"), mission12);
    }

    // ---------- Casos limite ----------

    static void EdgeCases(DestinationData baseMission)
    {
        DestinationData mission12 = Mission(baseMission, 12);
        List<PartData> good = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");

        Rejected("Sin mision asignada", good, null, "No hay mision");

        DestinationData badMission = Mission(baseMission, 12);
        badMission.requiredDeltaV = 0f;
        Rejected("Mision sin delta-v requerido", good, badMission, "delta-v requerido");
        badMission = Mission(baseMission, 12);
        badMission.baseWaterKg = -10f;
        Rejected("Mision con agua negativa", good, badMission, "valor negativo");

        List<PartData> negativeMass = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        PartData antenna = Object.Instantiate(Part("ART-COMMS-DS"));
        antenna.dryMass = -65f;
        negativeMass[4] = antenna;
        Rejected("Pieza con masa negativa", negativeMass, mission12, "negativo");

        List<PartData> negativePower = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        PartData antenna2 = Object.Instantiate(Part("ART-COMMS-DS"));
        antenna2.powerDraw = -5000f; // consumo negativo = energia gratis
        negativePower[4] = antenna2;
        Rejected("Pieza con consumo negativo", negativePower, mission12, "negativo");

        List<PartData> noData = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        PartData lunar = Object.Instantiate(Part("ART-SCI-LUNAR"));
        PartData rad = Object.Instantiate(Part("ART-SCI-RAD"));
        lunar.experimentDataBits = 0f;
        rad.experimentDataBits = 0f;
        noData[5] = lunar;
        noData[6] = rad;
        Rejected("Instrumentos con ciencia pero sin datos (0 s de transmision no cuenta como exito)", noData, mission12, "no generan datos");

        Rejected("Dos unidades de SCI-RAD (con repetidas seria la mas barata: 2,875 creditos)",
            Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-RAD", "ART-SCI-RAD"), mission12, "una unidad");
        Rejected("Dos reservas de agua", Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-WATER"), mission12, "una unidad");
    }

    // ---------- Ayudantes ----------

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>(PartsFolder + id + ".asset");
    }

    // Orion (capsula, modulo de servicio, torre de escape) despues del lanzador + las piezas que se pidan
    static List<PartData> Parts(string launcher, params string[] ids)
    {
        List<PartData> list = new List<PartData> { Part(launcher), Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        foreach (string id in ids) list.Add(Part(id));
        return list;
    }

    static DestinationData Mission(DestinationData baseMission, int days)
    {
        DestinationData m = Object.Instantiate(baseMission);
        m.missionDays = days;
        return m;
    }

    static void Viable(string name, List<PartData> parts, DestinationData mission)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        float price = RocketPhysics.TotalPrice(parts);
        Report(name + " (" + price + " creditos)", message == "" && price <= Budget, message);
    }

    static void Rejected(string name, List<PartData> parts, DestinationData mission, string expected)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        Report(name + ": se rechaza", message.Contains(expected), "mensaje: \"" + message + "\"");
    }

    static void Check(string name, bool ok)
    {
        Report(name, ok, "");
    }

    static void Near(string name, float actual, float expected)
    {
        Report(name + " = " + expected, Mathf.Abs(actual - expected) < 0.5f, "salio " + actual);
    }

    static void Report(string name, bool ok, string detail)
    {
        if (ok)
        {
            passed++;
            Debug.Log("OK     " + name);
        }
        else
        {
            failed++;
            Debug.LogError("FALLA  " + name + "  " + detail);
        }
    }
}
