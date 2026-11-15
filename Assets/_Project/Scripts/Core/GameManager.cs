using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Global game state. Survives between scenes.
// Use from any script: GameManager.Instance.money, GameManager.Instance.CanLaunch(), etc.
public class GameManager : MonoBehaviour
{
    static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            // If you test a scene on its own and there is no GameManager, one is created with starting values
            if (instance == null)
            {
                GameObject go = new GameObject("GameManager");
                instance = go.AddComponent<GameManager>();
            }
            return instance;
        }
    }

    [Header("Starting values")]
    public float startingMoney = 3400f; // game credits (fixed budget, Cristina's catalog)

    [Header("Mission")]
    public DestinationData mission;
    public bool missionRead; // task 1: the folder was opened at least once

    [Header("Money and parts")]
    public float money;
    public List<PartData> boughtParts = new List<PartData>();  // bought in SpaceMart
    public List<PartData> rocketParts = new List<PartData>();  // the ones used in the assembled rocket
    public bool rocketValid; // task 3: set by the rocket model when delta-v and mass are enough

    [Header("Crew")]
    public AstronautData pilot;
    public AstronautData engineer;
    public AstronautData scientist;
    public List<AstronautData> reserves = new List<AstronautData>();
    public TrainingFocus pilotTraining;
    public TrainingFocus engineerTraining;
    public TrainingFocus scientistTraining;

    [Header("Launch")]
    public LaunchSiteData launchSite;
    public int launchDay = -1;    // day chosen in the calendar (-1 = not chosen)
    public float weatherRisk;     // 0 to 1, for the chosen site and day
    public WeatherCondition launchWeather = WeatherCondition.Sunny; // sky on the chosen day (launch scene visuals)
    public float launchWindKnots; // wind on the chosen day

    [Header("Score and events")]
    public float planningScore;
    public float missionScore;
    public List<EventData> eventsHappened = new List<EventData>();

    [Header("Flight (filled in by the phases and minigames, read by Results)")]
    public MissionResults flight = new MissionResults();

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

        // A GameManager created automatically (scene tested on its own) still gets the mission
        GameData data = GameData.Instance;
        if (mission == null && data != null) mission = data.mission;
    }

    // ---------- Game ----------

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
        launchWeather = WeatherCondition.Sunny;
        launchWindKnots = 0f;
        planningScore = 0f;
        missionScore = 0f;
        eventsHappened.Clear();
        flight.Reset();
    }

    // Before Results it is only what the events added. In Results,
    // MissionScoring.SaveFinalScore() sets the final V16 score (0 to 100).
    public float TotalScore()
    {
        return planningScore + missionScore;
    }

    // ---------- Money ----------

    public bool CanAfford(float amount)
    {
        return money >= amount;
    }

    // Returns false if there is not enough money (and charges nothing)
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

    // ---------- Parts ----------

    public bool BuyPart(PartData part)
    {
        if (part == null) return false;
        if (!Spend(part.price)) return false;
        boughtParts.Add(part);
        return true;
    }

    // Returns the part and refunds its full price
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

    // ---------- Crew ----------

    public bool IsCrewReady()
    {
        return pilot != null && engineer != null && scientist != null
            && pilotTraining != TrainingFocus.None
            && engineerTraining != TrainingFocus.None
            && scientistTraining != TrainingFocus.None;
    }

    // Astronaut's skill in their role (0 to 10) + 2 if they trained in the same thing, max 10.
    // The minigames multiply their result (0 to 1) by SkillMultiplier.
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

    // ---------- Launch ----------

    // Globe/calendar: chooses the site and the day (index in site.weatherRiskByDay) and sets weatherRisk.
    // Returns false and changes nothing if the site is null or the day is out of range.
    public bool SetLaunch(LaunchSiteData site, int day)
    {
        if (site == null || site.weatherRiskByDay == null) return false;
        if (day < 0 || day >= site.weatherRiskByDay.Length) return false;
        launchSite = site;
        launchDay = day;
        weatherRisk = Mathf.Clamp01(site.weatherRiskByDay[day]);
        return true;
    }

    public bool IsLaunchPlanned()
    {
        return launchSite != null && launchDay >= 0;
    }

    // The 4 desk tasks (GDD). The launch button is only enabled if this is true.
    public bool CanLaunch()
    {
        return missionRead && IsCrewReady() && rocketValid && IsLaunchPlanned();
    }

    // Minimum to launch early, at the player's own risk: mission read, the 3 astronauts assigned
    // (training optional), parts in the rocket (it may be invalid or badly designed) and launch site + date.
    // What is missing is not blocked here: it hurts the flight and the score later.
    public bool CanLaunchEarly()
    {
        return missionRead && pilot != null && engineer != null && scientist != null
            && rocketParts.Count > 0 && IsLaunchPlanned();
    }

    // ---------- Scenes ----------

    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Moves to the next scene in the loop. After Results it resets and goes back to the Menu.
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
