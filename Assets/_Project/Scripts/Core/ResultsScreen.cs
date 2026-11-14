using System.Text;
using TMPro;
using UnityEngine;

// Pantalla de 03_Resultados (tarea 16): calcula el puntaje final V16 una sola vez,
// lo guarda en GameManager y muestra el desglose con una explicacion de cada decision.
// Menu: SpaceApps -> Pantalla de resultados -> Agregar a la escena abierta.
public class ResultsScreen : MonoBehaviour
{
    public TMP_Text title;   // "Mision exitosa", "Mision fallida"...
    public TMP_Text score;   // el numero grande
    public TMP_Text report;  // desglose y lecciones

    void Start()
    {
        Show();
    }

    public void Show()
    {
        ScoreResult r = MissionScoring.SaveFinalScore();
        if (title != null) title.text = TitleFor(r);
        if (score != null) score.text = r.total.ToString("0") + " / 100";
        if (report != null) report.text = BuildReport(r, GameManager.Instance);
    }

    static string TitleFor(ScoreResult r)
    {
        if (r.status == "SUCCESS") return "¡Mision exitosa!";
        if (r.status == "FAILURE") return "Mision fallida";
        if (r.status == "PENDING") return "Mision en curso";
        return "Resultado no valido";
    }

    public static string BuildReport(ScoreResult r, GameManager gm)
    {
        StringBuilder sb = new StringBuilder();
        if (r.problem != "")
        {
            sb.AppendLine("No se pudo calcular el puntaje: " + r.problem);
            return sb.ToString();
        }

        if (r.status == "FAILURE")
        {
            sb.AppendLine("<b>Que salio mal</b>");
            foreach (string gate in r.failedGates) sb.AppendLine("- " + GateText(gate));
            sb.AppendLine("Puntaje para aprender: " + r.total.ToString("0") + " (el puntaje oficial es 0 porque la mision fallo).");
            sb.AppendLine();
        }

        sb.AppendLine("<b>Planeacion: " + r.planning.ToString("0.0") + " / 45</b>");
        Line(sb, "Ingenieria del cohete", r.engineering, 24f);
        Line(sb, "Ahorro del presupuesto", r.budgetPoints, 7f);
        Line(sb, "Entrenamiento de la tripulacion", r.trainingPoints, 8f);
        Line(sb, "Sitio y fecha (clima)", r.weatherPoints, 6f);
        if (r.planningEvents != 0f) sb.AppendLine("  Eventos: " + r.planningEvents.ToString("+0;-0"));

        sb.AppendLine();
        sb.AppendLine("<b>Mision: " + r.mission.ToString("0.0") + " / 55</b>");
        Line(sb, "Etapas del vuelo", r.milestonePoints, 24f);
        Line(sb, "Minijuegos", r.minigamePoints, 21f);
        Line(sb, "Ciencia", r.sciencePoints, 10f);
        if (r.missionEvents != 0f) sb.AppendLine("  Eventos: " + r.missionEvents.ToString("+0;-0"));

        sb.AppendLine();
        sb.AppendLine("<b>Lo que aprendiste</b>");
        if (r.massPoints < 2f)
            sb.AppendLine("- Tu nave iba muy justa de masa para el SLS. Cada kilo extra necesita mas combustible y un cohete mas grande.");
        if (r.craftDvPoints < 3.5f)
            sb.AppendLine("- Poco delta-v de sobra en Orion: dv = Isp x g0 x ln(m0/mf). Si la nave pesa mas, el mismo combustible rinde menos.");
        if (r.energyPoints < 2f)
            sb.AppendLine("- La energia iba al limite. Detras de la Luna no hay sol: las baterias deben aguantar el consumo.");
        if (r.budgetPoints < 3.5f)
            sb.AppendLine("- Gastaste casi todo el presupuesto. Las misiones reales guardan dinero para imprevistos.");
        if (r.trainingPoints < 6f)
            sb.AppendLine("- Entrena a cada astronauta en su propio rol: asi rinden mas en los minijuegos y evitan problemas.");
        if (r.weatherPoints < 4f)
            sb.AppendLine("- Lanzaste con mal clima. El viento y las tormentas aumentan el riesgo en la salida.");
        if (r.sciencePoints < 7f)
            sb.AppendLine("- Comprar instrumentos no basta: hay que hacer los experimentos y mandar los datos a la Tierra.");
        if (gm != null)
        {
            foreach (EventData e in gm.eventsHappened)
            {
                if (e != null && !string.IsNullOrEmpty(e.lesson)) sb.AppendLine("- " + e.eventName + ": " + e.lesson);
            }
        }
        if (r.total >= 90f) sb.AppendLine("- ¡Excelente! Tomaste decisiones muy parecidas a las de un equipo de mision real.");

        return sb.ToString();
    }

    static void Line(StringBuilder sb, string label, float points, float max)
    {
        sb.AppendLine("  " + label + ": " + points.ToString("0.0") + " / " + max.ToString("0"));
    }

    // Texto en espanol de cada condicion obligatoria que fallo
    public static string GateText(string gate)
    {
        switch (gate)
        {
            case "NO_LAUNCH": return "No se lanzo con las 4 tareas de la planeacion completas.";
            case "ROCKET_INVALID": return "El cohete no era valido para la mision.";
            case "CREW_NOT_READY": return "La tripulacion no estaba completa o entrenada.";
            case "LAUNCH_DELTA_V":
            case "MASS":
            case "TWR": return "El SLS no podia llevar tanta masa a la translunar.";
            case "CRAFT_DELTA_V": return "Orion no tenia suficiente delta-v para entrar a orbita lunar y regresar.";
            case "POWER": return "No alcanzaba la energia o la bateria para los periodos sin sol.";
            case "DEPARTURE": return "La salida no se completo.";
            case "LUNAR_ORBIT": return "No se llego a la orbita lunar.";
            case "RETURN": return "La tripulacion no regreso a salvo.";
            case "SCIENCE": return "No se cumplio la meta de ciencia.";
            case "NO_DOWNLINK": return "No se mandaron los datos de ciencia a la Tierra.";
        }
        return gate;
    }
}
