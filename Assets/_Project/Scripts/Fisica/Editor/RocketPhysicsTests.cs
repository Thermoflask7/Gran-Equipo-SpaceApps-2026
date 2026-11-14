using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Pruebas de RocketPhysics dentro de Unity (tarea 17), con los 9 escenarios de referencia de la
// mision V13 de Cristina (SpaceMission_Mision_Artemis_V13.json -> reference_test_scenarios).
// Menu: SpaceApps -> Pruebas de fisica. Escribe en la consola cada prueba con OK o FALLA.
// Primero importa el catalogo (crea/actualiza los assets de Data/Piezas y la mision) y luego
// prueba con esos mismos assets, asi tambien se revisa que los datos esten bien.
public static class RocketPhysicsTests
{
    const float Budget = 3400f; // creditos iniciales (catalogo de Cristina)

    static int passed;
    static int failed;
    static DestinationData baseMission;

    [MenuItem("SpaceApps/Pruebas de fisica")]
    public static void RunAll()
    {
        passed = 0;
        failed = 0;

        CatalogImporter.Import();
        baseMission = AssetDatabase.LoadAssetAtPath<DestinationData>("Assets/_Project/Data/Destinos/MisionArtemis.asset");
        if (baseMission == null || Part("ART-CM-ORION") == null)
        {
            Debug.LogError("No se pudo importar el catalogo: no se corrieron las pruebas.");
            return;
        }
        DestinationData mission12 = Mission(12);
        DestinationData mission15 = Mission(15);

        // 1. PASS_BLOCK1_OBSERVATION
        List<PartData> p1 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        Near("1 Block 1 observacion: masa en translunar", RocketPhysics.TliMass(p1, mission12), 26660f);
        Near("1 Block 1 observacion: margen del SLS", RocketPhysics.DeliveryMargin(p1, mission12), 340f);
        Near("1 Block 1 observacion: dv de la nave", RocketPhysics.SpacecraftDeltaV(p1), 1103.27f);
        Near("1 Block 1 observacion: sobran 460 creditos", Budget - RocketPhysics.TotalPrice(p1), 460f);
        Near("1 Block 1 observacion: 48 puntos de ciencia", RocketPhysics.SciencePoints(p1), 48f);
        Valid("1 Block 1 observacion: valida", p1, mission12);

        // 2. PASS_BLOCK1B_HEAVY_SCIENCE
        List<PartData> p2 = Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY");
        Near("2 Block 1B ciencia pesada: masa en translunar", RocketPhysics.TliMass(p2, mission12), 27201f);
        Near("2 Block 1B ciencia pesada: dv de la nave", RocketPhysics.SpacecraftDeltaV(p2), 1076.34f);
        Near("2 Block 1B ciencia pesada: sobran 5 creditos", Budget - RocketPhysics.TotalPrice(p2), 5f);
        Valid("2 Block 1B ciencia pesada: valida", p2, mission12);

        // 3. PASS_BLOCK1_EXTENDED_15_DAYS
        List<PartData> p3 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD",
            "ART-CREW-RES-WATER", "ART-CREW-RES-OXYGEN", "ART-CREW-RES-FOOD");
        Near("3 Block 1 de 15 dias con reservas: masa en translunar", RocketPhysics.TliMass(p3, mission15), 26756f);
        Near("3 Block 1 de 15 dias con reservas: dv de la nave", RocketPhysics.SpacecraftDeltaV(p3), 1098.40f);
        Valid("3 Block 1 de 15 dias con reservas: valida", p3, mission15);

        // 4. FAIL_LOW_POST_TLI_PROPELLANT (modulo de servicio con 7,400 kg en vez de 7,800)
        PartData lowFuelService = Object.Instantiate(Part("ART-ESM-ORION"));
        lowFuelService.fuelMass = 7400f;
        List<PartData> p4 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        p4[1] = lowFuelService; // indice 1 = modulo de servicio
        Near("4 poco propelente: masa en translunar", RocketPhysics.TliMass(p4, mission12), 26260f);
        Near("4 poco propelente: dv de la nave", RocketPhysics.SpacecraftDeltaV(p4), 1055.32f);
        Fails("4 poco propelente: no llega", p4, mission12, "orbita lunar");

        // 5. FAIL_BLOCK1_EXCEEDS_TLI_MASS
        List<PartData> p5 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-HEAVY");
        Near("5 Block 1 con ciencia pesada: masa en translunar", RocketPhysics.TliMass(p5, mission12), 27201f);
        Fails("5 Block 1 con ciencia pesada: no cabe", p5, mission12, "Demasiado pesado");

        // 6. FAIL_NO_DATA_DOWNLINK
        List<PartData> p6 = Parts("ART-SLS-BLOCK1", "ART-SCI-LUNAR", "ART-SCI-RAD");
        Near("6 sin antena: dv de la nave", RocketPhysics.SpacecraftDeltaV(p6), 1106.60f);
        Fails("6 sin antena: no se bajan los datos", p6, mission12, "antena");

        // 7. FAIL_BUDGET_BLOCK1B_INSTRUMENTS (el presupuesto lo revisa GameManager al comprar)
        List<PartData> p7 = Parts("ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-HEAVY", "ART-CREW-RES-FOOD");
        Near("7 Block 1B con comida extra: cuesta 3,450 creditos", RocketPhysics.TotalPrice(p7), 3450f);
        Check("7 Block 1B con comida extra: no alcanza el presupuesto", RocketPhysics.TotalPrice(p7) > Budget);

        // 8. FAIL_LOW_SCIENCE_POINTS
        List<PartData> p8 = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR");
        Near("8 poca ciencia: dv de la nave", RocketPhysics.SpacecraftDeltaV(p8), 1103.99f);
        Fails("8 poca ciencia: 28 de 40 puntos", p8, mission12, "Poca ciencia");

        // 9. FAIL_LONGER_MISSION_WITHOUT_CREW_RESERVES
        Fails("9 15 dias sin reservas: falta agua", p1, mission15, "agua");
        Check("9 15 dias sin reservas: falta oxigeno y comida",
            RocketPhysics.OxygenMargin(p1, mission15) < 0f && RocketPhysics.FoodMargin(p1, mission15) < 0f);

        // Reglas de armado
        Fails("10 sin lanzador", Parts("ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD"), mission12, "Elige un lanzador");
        Fails("11 dos lanzadores", Parts("ART-SLS-BLOCK1", "ART-SLS-BLOCK1B", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD"), mission12, "Solo puedes usar");
        List<PartData> noEscape = Parts("ART-SLS-BLOCK1", "ART-COMMS-DS", "ART-SCI-LUNAR", "ART-SCI-RAD");
        noEscape.Remove(Part("ART-LAS-ORION"));
        Fails("12 sin torre de escape", noEscape, mission12, "torre de escape");

        string summary = "Pruebas de fisica: " + passed + " OK, " + failed + " FALLA";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    static PartData Part(string id)
    {
        return AssetDatabase.LoadAssetAtPath<PartData>("Assets/_Project/Data/Piezas/" + id + ".asset");
    }

    // Orion obligatoria (capsula, modulo de servicio, torre de escape) + las piezas que se pasen.
    static List<PartData> Parts(params string[] ids)
    {
        List<PartData> list = new List<PartData> { Part("ART-CM-ORION"), Part("ART-ESM-ORION"), Part("ART-LAS-ORION") };
        foreach (string id in ids) list.Add(Part(id));
        return list;
    }

    // Copia de la mision importada con otra duracion (no modifica el asset).
    static DestinationData Mission(int days)
    {
        DestinationData m = Object.Instantiate(baseMission);
        m.missionDays = days;
        return m;
    }

    static void Valid(string name, List<PartData> parts, DestinationData mission)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        Report(name, message == "", message);
    }

    static void Fails(string name, List<PartData> parts, DestinationData mission, string expected)
    {
        string message = RocketPhysics.ValidationMessage(parts, mission);
        Report(name, message.Contains(expected), "mensaje: \"" + message + "\"");
    }

    static void Check(string name, bool ok)
    {
        Report(name, ok, "");
    }

    // Tolerancia de 0.5 (float de Unity contra los valores de Cristina).
    static void Near(string name, float actual, float expected)
    {
        Report(name + ": " + actual.ToString("0.00"), Mathf.Abs(actual - expected) <= 0.5f, "esperado " + expected.ToString("0.00"));
    }

    static void Report(string name, bool ok, string detail)
    {
        if (ok) { passed++; Debug.Log("OK     " + name); }
        else { failed++; Debug.LogError("FALLA  " + name + "  " + detail); }
    }
}
