using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estado global de la partida. Sobrevive entre escenas.
// Uso desde cualquier script: GameManager.Instance.Money, GameManager.Instance.GoToNextScene(), etc.
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
    public float startingMoney = 500f; // millones de USD

    [Header("Estado de la partida")]
    public float money;
    public int year = 1;
    public int score;
    public DestinationData destination;
    public List<PartData> boughtParts = new List<PartData>();

    [Header("Tripulacion")]
    public AstronautData pilot;
    public AstronautData engineer;
    public AstronautData scientist;
    public List<AstronautData> reserves = new List<AstronautData>();

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
        year = 1;
        score = 0;
        destination = null;
        boughtParts.Clear();
        pilot = null;
        engineer = null;
        scientist = null;
        reserves.Clear();
    }

    public void AddScore(int points)
    {
        score += points;
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
            money += part.price;
        }
    }

    public float TotalDryMass()
    {
        float total = 0f;
        foreach (PartData p in boughtParts) total += p.dryMass;
        return total;
    }

    public float TotalFuelMass()
    {
        float total = 0f;
        foreach (PartData p in boughtParts) total += p.fuelMass;
        return total;
    }

    // ---------- Tripulacion ----------

    public bool IsCrewComplete()
    {
        return pilot != null && engineer != null && scientist != null;
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
