using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Rocket model screen on the planning desk (task 9b, P2). Full screen, split in two halves:
// Left half: bought parts not on the rocket (click = mount), parts on the rocket (click = take off),
//            rocket status and mission numbers.
// Right half: the low poly rocket (RocketView) live. It is built in a hidden "studio" far below the scene
//             and filmed by its own camera into a RenderTexture shown in the RawImage modelView.
// All the rules are in RocketAssembly (task 9a); this panel only calls it and redraws.
// Open it with Open() (the desk station RocketModelStation does it); the X button calls Close().
// Built with SpaceApps > Rocket model > Create prefab.
public class RocketModel : MonoBehaviour
{
    [Header("Lists")]
    public Transform trayContent;   // bought parts not mounted
    public Transform rocketContent; // parts on the rocket
    public Button rowTemplate;      // inactive button copied for each part
    public TMP_Text trayHint;       // shown when the tray is empty

    [Header("3D view (right half)")]
    public RawImage modelView;
    public Color studioBackground = new Color(0.05f, 0.07f, 0.12f);

    [Header("Texts")]
    public TMP_Text statusText;
    public TMP_Text statsText;

    [Header("Buttons")]
    public Button mountAllButton;
    public Button clearButton;
    public Button closeButton;

    [Header("Data")]
    public DestinationData mission; // only used if GameManager has no mission

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    const string OkColor = "#2E9E4F";
    const string BadColor = "#D9534F";

    readonly List<GameObject> rows = new List<GameObject>();
    int shownBought = -1;
    int shownMounted = -1;

    GameObject studio;
    Camera studioCamera;
    RenderTexture studioTexture;
    static readonly Vector3 StudioPosition = new Vector3(0f, -500f, 0f); // far from the desk

    void Awake()
    {
        if (mountAllButton != null) mountAllButton.onClick.AddListener(MountAll);
        if (clearButton != null) clearButton.onClick.AddListener(ClearRocket);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        GameManager gm = GameManager.Instance;
        if (gm.mission == null) gm.mission = mission;
        RocketAssembly.Sync(gm); // something may have been sold while the panel was closed
        Refresh();
        EnsureStudio();
        studio.SetActive(true);
        onOpened.Invoke();
    }

    void Update()
    {
        // Redraw only when the parts changed (for example a test menu bought something)
        GameManager gm = GameManager.Instance;
        if (gm.boughtParts.Count != shownBought || gm.rocketParts.Count != shownMounted) Refresh();
    }

    void OnDisable()
    {
        if (studio != null) studio.SetActive(false); // no extra camera while the screen is closed
    }

    void OnDestroy()
    {
        if (studio != null) Destroy(studio);
        if (studioTexture != null) studioTexture.Release();
    }

    // The rocket, a camera looking at it and a RenderTexture for the right half.
    // The scene's directional light also lights the studio.
    void EnsureStudio()
    {
        if (studio != null) return;
        studio = new GameObject("RocketModel_Studio");
        studio.transform.position = StudioPosition;

        GameObject rocket = new GameObject("Rocket");
        rocket.transform.SetParent(studio.transform, false);
        rocket.AddComponent<RocketView>(); // about 11 units tall with every part

        Rect r = modelView != null ? modelView.rectTransform.rect : new Rect(0, 0, 1, 1);
        int width = 1024;
        int height = r.width > 1f && r.height > 1f ? Mathf.RoundToInt(width * r.height / r.width) : 1024;
        studioTexture = new RenderTexture(width, height, 24);
        studioTexture.name = "RocketModel_View";

        GameObject camGo = new GameObject("Camera");
        camGo.transform.SetParent(studio.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 6f, -19f);
        camGo.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
        studioCamera = camGo.AddComponent<Camera>();
        studioCamera.fieldOfView = 40f;
        studioCamera.nearClipPlane = 0.1f;
        studioCamera.farClipPlane = 60f; // does not reach the desk
        studioCamera.clearFlags = CameraClearFlags.SolidColor;
        studioCamera.backgroundColor = studioBackground;
        studioCamera.targetTexture = studioTexture;

        if (modelView != null) modelView.texture = studioTexture;
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        onClosed.Invoke();
        gameObject.SetActive(false);
    }

    // ---------- Buttons ----------

    public void Mount(PartData part)
    {
        GameManager gm = GameManager.Instance;
        if (!RocketAssembly.Mount(gm, part) && statusText != null)
            statusText.text = "<color=" + BadColor + ">" + RocketAssembly.MountProblem(gm, part) + "</color>";
        else
            Refresh();
    }

    public void Unmount(PartData part)
    {
        RocketAssembly.Unmount(GameManager.Instance, part);
        Refresh();
    }

    public void MountAll()
    {
        RocketAssembly.MountAll(GameManager.Instance);
        Refresh();
    }

    public void ClearRocket()
    {
        RocketAssembly.Clear(GameManager.Instance);
        Refresh();
    }

    // ---------- Drawing ----------

    public void Refresh()
    {
        GameManager gm = GameManager.Instance;
        shownBought = gm.boughtParts.Count;
        shownMounted = gm.rocketParts.Count;

        foreach (GameObject row in rows) Destroy(row);
        rows.Clear();

        List<PartData> tray = RocketAssembly.UnmountedParts(gm);
        foreach (PartData p in tray) AddRow(trayContent, p, true);
        foreach (PartData p in gm.rocketParts) if (p != null) AddRow(rocketContent, p, false);

        if (trayHint != null)
        {
            trayHint.gameObject.SetActive(tray.Count == 0);
            trayHint.text = gm.boughtParts.Count == 0
                ? "You have no parts yet.\nBuy them in SpaceMart (on the computer)."
                : "Every bought part is on the rocket.";
        }
        if (mountAllButton != null) mountAllButton.interactable = tray.Count > 0;
        if (clearButton != null) clearButton.interactable = gm.rocketParts.Count > 0;
        if (statusText != null) statusText.text = BuildStatus(gm);
        if (statsText != null) statsText.text = BuildRocketStats(gm);
    }

    void AddRow(Transform parent, PartData part, bool mount)
    {
        if (rowTemplate == null || parent == null) return;
        Button b = Instantiate(rowTemplate, parent);
        b.gameObject.SetActive(true);
        b.gameObject.name = part.partName;
        TMP_Text label = b.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = (mount ? "+ " : "- ") + "<b>" + part.partName + "</b>\n<size=75%>" + SpaceMart.CategoryName(part.type) + "</size>";
        if (mount) b.onClick.AddListener(() => Mount(part));
        else b.onClick.AddListener(() => Unmount(part));
        rows.Add(b.gameObject);
    }

    // What is missing, or that the rocket is ready (this is what turns the checklist task green)
    public static string BuildStatus(GameManager gm)
    {
        List<PartType> missing = RocketAssembly.MissingRequired(gm);
        if (missing.Count > 0)
        {
            List<string> names = new List<string>();
            foreach (PartType t in missing) names.Add(RequiredName(t));
            return "<color=" + BadColor + "><b>Not ready.</b></color> Missing: " + string.Join(", ", names) + ".";
        }
        string problem = RocketAssembly.Status(gm);
        if (problem != "") return "<color=" + BadColor + "><b>Not ready.</b></color> " + problem;
        return "<color=" + OkColor + "><b>Ready to fly!</b></color> The rocket can do the mission.";
    }

    // The same numbers as the SpaceMart cart, but for the parts on the rocket
    public static string BuildRocketStats(GameManager gm)
    {
        string s = SpaceMart.BuildStats(gm.rocketParts, gm.mission);
        int end = s.IndexOf("\n\n"); // drop the cart header (title, parts and credits)
        if (end >= 0) s = s.Substring(end + 2);
        int last = s.LastIndexOf("\n\n"); // drop the cart's final line (BuildStatus already says it)
        if (last >= 0) s = s.Substring(0, last);
        return s;
    }

    public static string RequiredName(PartType type)
    {
        if (type == PartType.LaunchService) return "a heavy launcher";
        if (type == PartType.Capsule) return "the crew capsule";
        if (type == PartType.ServiceModule) return "the service module";
        if (type == PartType.AbortSystem) return "the launch abort system";
        return type.ToString();
    }
}
