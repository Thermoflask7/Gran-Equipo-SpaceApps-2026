using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estado global de la partida. Sobrevive entre escenas.
// Uso desde cualquier script: GameManager.Instance.money, GameManager.Instance.CanLaunch(), etc.
public class GameManager : MonoBehaviour
{
    static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            // Si pruebas una escena sola y no hay GameManager, se crea uno con valores iniciales
            if (instance == null)
            {
                GameObject go = new GameObject("GameManager");
                instance = go.AddComponent<GameManager>();
            }
            return instance;
        }
    }

    [Header("Valores iniciales")]
    public float startingMoney = 500f; // millones de USD (presupuesto fijo)

    [Header("Mision")]
    public DestinationData mission;
    public bool missionRead; // tarea 1: el folder se abrio al menos una vez

    [Header("Dinero y piezas")]
    public float money;
    public List<PartData> boughtParts = new List<PartData>();  // compradas en SpaceMart
    public List<PartData> rocketParts = new List<PartData>();  // las que se usaron en el cohete armado
    public bool rocketValid; // tarea 3: lo pone el modelo de cohete cuando el delta-v y la masa alcanzan

    [Header("Tripulacion")]
    public AstronautData pilot;
    public AstronautData engineer;
    public AstronautData scientist;
    public List<AstronautData> reserves = new List<AstronautData>();
    public TrainingFocus pilotTraining;
    public TrainingFocus engineerTraining;
    public TrainingFocus scientistTraining;

    [Header("Lanzamiento")]
    public LaunchSiteData launchSite;
    public int launchDay = -1;    // dia elegido en el calendario (-1 = sin elegir)
    public float weatherRisk;     // 0 a 1, del sitio y dia elegidos

    [Header("Puntaje y eventos")]
    public float planningScore;
    public float missionScore;
    public List<EventData> eventsHappened = new List<EventData>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        ResetGame();
    }

    // ---------- Partida ----------

    public void ResetGame()
    {
        money = startingMoney;
        missionRead = false;
        boughtParts.Clear();
        rocketParts.Clear();
        rocketValid = false;
        pilot = null;
        engineer = null;
        scientist = null;
        reserves.Clear();
        pilotTraining = TrainingFocus.None;
        engineerTraining = TrainingFocus.None;
        scientistTraining = TrainingFocus.None;
        launchSite = null;
        launchDay = -1;
        weatherRisk = 0f;
        planningScore = 0f;
        missionScore = 0f;
        eventsHappened.Clear();
    }

    public float TotalScore()
    {
        return planningScore + missionScore;
    }

    // ---------- Dinero ----------

    public bool CanAfford(float amount)
    {
        return money >= amount;
    }

    // Regresa false si no alcanza el dinero (y no cobra nada)
    public bool Spend(float amount)
    {
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    public void AddMoney(float amount)
    {
        money += amount;
    }

    // ---------- Piezas ----------

    public bool BuyPart(PartData part)
    {
        if (part == null) return false;
        if (!Spend(part.price)) return false;
        boughtParts.Add(part);
        return true;
    }

    // Devuelve la pieza y regresa su precio completo
    public void SellPart(PartData part)
    {
        if (boughtParts.Remove(part))
        {
            rocketParts.Remove(part);
            money += part.price;
        }
    }

    public float RocketDryMass()
    {
        float total = 0f;
        foreach (PartData p in rocketParts) total += p.dryMass;
        return total;
    }

    public float RocketFuelMass()
    {
        float total = 0f;
        foreach (PartData p in rocketParts) total += p.fuelMass;
        return total;
    }

    // ---------- Tripulacion ----------

    public bool IsCrewReady()
    {
        return pilot != null && engineer != null && scientist != null
            && pilotTraining != TrainingFocus.None
            && engineerTraining != TrainingFocus.None
            && scientistTraining != TrainingFocus.None;
    }

    // Habilidad del astronauta en su rol (0 a 10) + 2 si entreno en lo mismo, maximo 10.
    // Los minijuegos multiplican su resultado (0 a 1) por SkillMultiplier.
    public float SkillMultiplier(AstronautData astronaut, TrainingFocus role)
    {
        if (astronaut == null) return 0.5f;
        int skill = 0;
        if (role == TrainingFocus.Piloting) skill = astronaut.piloting;
        if (role == TrainingFocus.Engineering) skill = astronaut.engineering;
        if (role == TrainingFocus.Research) skill = astronaut.research;
        if (TrainingOf(astronaut) == role) skill += 2;
        if (skill > 10) skill = 10;
        return skill / 10f;
    }

    TrainingFocus TrainingOf(AstronautData astronaut)
    {
        if (astronaut == pilot) return pilotTraining;
        if (astronaut == engineer) return engineerTraining;
        if (astronaut == scientist) return scientistTraining;
        return TrainingFocus.None;
    }

    // ---------- Lanzamiento ----------

    public bool IsLaunchPlanned()
    {
        return launchSite != null && launchDay >= 0;
    }

    // Las 4 tareas del escritorio (GDD). El boton de lanzamiento solo se activa si esto es true.
    public bool CanLaunch()
    {
        return missionRead && IsCrewReady() && rocketValid && IsLaunchPlanned();
    }

    // ---------- Escenas ----------

    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Avanza a la siguiente escena del loop. Despues de Resultados reinicia y vuelve al Menu.
    public void GoToNextScene()
    {
        string current = SceneManager.GetActiveScene().name;
        int index = System.Array.IndexOf(Scenes.Order, current);

        if (index < 0 || index >= Scenes.Order.Length - 1)
        {
            ResetGame();
            SceneManager.LoadScene(Scenes.Menu);
            return;
        }
        SceneManager.LoadScene(Scenes.Order[index + 1]);
    }
}
