using UnityEngine;
using UnityEngine.SceneManagement;

// Desk tutorials (01_Planeacion). Signed: Claude.
// Created by itself at runtime in any scene with a DeskCamera, so the scene needs no setup.
// - The first time the desk loads in a game: welcome message (GameManager.welcomeSeen).
// - The first time each station is open (and the camera has arrived): its tutorial with
//   TutorialPopup.ShowOnce(id) (GameManager.tutorialsSeen). SpaceMart shows its tutorial when the app opens.
// - While a station is open, a "?" button reopens its tutorial.
// It only reads the stations' IsOpen (FolderStation, PcStation, RocketModelStation, PlanningStation,
// LaunchStation) and adds one listener to the SpaceMart app, so their scripts are not changed.
public class DeskTutorials : MonoBehaviour
{
    static DeskTutorials instance;

    FolderStation folder;
    PcStation pc;
    RocketModelStation rocket;
    PlanningStation planning;
    LaunchStation launch;
    bool spaceMartHooked;
    float nextFind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance != null) return;
        if (FindAnyObjectByType<DeskCamera>() == null) return;
        new GameObject("DeskTutorials").AddComponent<DeskTutorials>();
    }

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        FindStations();
        if (!GameManager.Instance.welcomeSeen) ShowWelcome();
    }

    // Welcome message (also used by the test menu)
    public static void ShowWelcome()
    {
        GameManager gm = GameManager.Instance;
        gm.welcomeSeen = true;
        TutorialPopup.Show(TutorialTexts.WelcomeTitle, TutorialTexts.WelcomeBody(gm), TutorialTexts.WelcomeButton, "TOP SECRET");
    }

    void FindStations()
    {
        if (folder == null) folder = FindAnyObjectByType<FolderStation>();
        if (pc == null) pc = FindAnyObjectByType<PcStation>();
        if (rocket == null) rocket = FindAnyObjectByType<RocketModelStation>();
        if (planning == null) planning = FindAnyObjectByType<PlanningStation>();
        if (launch == null) launch = FindAnyObjectByType<LaunchStation>();
    }

    void Update()
    {
        // Stations or the SpaceMart app that are set up a bit later
        if (Time.unscaledTime >= nextFind && (!spaceMartHooked || folder == null || pc == null || rocket == null || planning == null || launch == null))
        {
            nextFind = Time.unscaledTime + 1f;
            FindStations();
            HookSpaceMart();
        }

        string current = OpenStation();
        DeskCamera dc = DeskCamera.Instance;
        if (dc != null && dc.IsMoving) current = null; // wait until the camera arrives

        TutorialPopup.SetHelp(current);
        if (current != null && current != TutorialTexts.SpaceMart) TutorialPopup.ShowOnce(current);
    }

    // Id of the station that is open now (null = at the desk)
    string OpenStation()
    {
        if (folder != null && folder.IsOpen) return TutorialTexts.Folder;
        if (pc != null && pc.IsOpen) return TutorialTexts.SpaceMart;
        if (rocket != null && rocket.IsOpen) return TutorialTexts.Rocket;
        if (planning != null && planning.IsOpen) return TutorialTexts.Planning;
        if (launch != null && launch.IsOpen) return TutorialTexts.Launch;
        return null;
    }

    void HookSpaceMart()
    {
        if (spaceMartHooked) return;
        ComputerDesktop desktop = ComputerDesktop.Instance;
        if (desktop == null) return;
        ComputerApp app = desktop.GetApp(ComputerDesktop.AppSpaceMart);
        if (app == null || !app.HasHandlers) return; // wait until SpaceMartApp registers it
        app.AddListener(OnSpaceMartOpened);
        spaceMartHooked = true;
    }

    void OnSpaceMartOpened()
    {
        TutorialPopup.ShowOnce(TutorialTexts.SpaceMart);
    }
}
