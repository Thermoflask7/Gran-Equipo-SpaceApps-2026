using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Research minigame (task 14, claude_c). Design: Docs/Task14/RESEARCH_MINIGAME_DESIGN.md
//   Intro -> Game 1 "Moon Photo Safari" (PhotoSafari) -> Game 2 "Signal Catcher" (SignalCatcher) -> Album -> Continue.
// The instruments and the antenna mounted on the rocket (gm.rocketParts) decide the targets and the sending speed.
// On "Continue" it fires Completed(ResearchResult) and deactivates itself. It does NOT touch GameManager:
// ResearchFlightHook writes the results and calls FlightPhases.CompleteMinigame.
// Prefab: SpaceApps > Research minigame > Create prefab.
public class ResearchMinigame : MonoBehaviour
{
    [Header("Parts")]
    public PhotoSafari safari;
    public SignalCatcher catcher;
    public ResearchSettings settings;
    public Sprite circle;

    [Header("UI")]
    public TMP_Text titleLabel;
    public GameObject introPanel;
    public TMP_Text introText;
    public Button startButton;
    public GameObject albumPanel;
    public RectTransform albumGrid;
    public TMP_Text albumText;
    public Button continueButton;

    public event Action<ResearchResult> Completed;
    public ResearchResult Result { get; private set; }
    public bool Playing { get; private set; }
    public bool Finished { get; private set; }

    List<PartData> parts = new List<PartData>();
    DestinationData mission;
    List<ResearchMath.Target> plan = new List<ResearchMath.Target>();
    bool hasAntenna;
    bool wired;

    ResearchSettings S { get { return settings != null ? settings : ResearchSettings.Default; } }

    void Awake()
    {
        Wire();
    }

    void Wire()
    {
        if (wired) return;
        wired = true;
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (continueButton != null) continueButton.onClick.AddListener(Continue);
        if (safari != null) safari.Finished += OnSafariFinished;
        if (catcher != null) catcher.Finished += OnCatcherFinished;
    }

    void OnEnable()
    {
        Wire();
        GameManager gm = GameManager.Instance;
        Prepare(gm.rocketParts, gm.mission);
        ShowIntro();
    }

    void OnDisable()
    {
        if (safari != null && safari.Running) safari.Stop();
        if (catcher != null && catcher.Running) catcher.Stop();
        Playing = false;
    }

    // Reads the rocket: which targets will appear and whether there is an antenna
    public void Prepare(List<PartData> rocketParts, DestinationData missionData)
    {
        parts = rocketParts != null ? new List<PartData>(rocketParts) : new List<PartData>();
        mission = missionData;
        plan = ResearchMath.PlanTargets(parts, S);
        hasAntenna = ResearchMath.HasAntenna(parts);
        Finished = false;
        Result = null;
    }

    void ShowIntro()
    {
        if (introPanel != null) introPanel.SetActive(true);
        if (albumPanel != null) albumPanel.SetActive(false);
        if (titleLabel != null) titleLabel.text = "Science in lunar orbit";
        if (introText != null) introText.text = IntroText();
    }

    string IntroText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>1. Moon Photo Safari</b> - fly over the far side and photograph what your instruments can see.");
        sb.AppendLine("<b>2. Signal Catcher</b> - aim the antenna at Earth to send your photos home.");
        sb.AppendLine();
        int landmarks = 0, sparks = 0, ice = 0;
        foreach (ResearchMath.Target t in plan)
        {
            if (t.type == ResearchMath.TargetType.Spark) sparks++;
            else if (t.type == ResearchMath.TargetType.Ice) ice++;
            else landmarks++;
        }
        if (plan.Count == 0) sb.AppendLine("No science instruments on board: there is nothing to measure.");
        if (landmarks > 0) sb.AppendLine("- Camera: " + landmarks + " landmarks. Click them inside the viewfinder (centered = gold star).");
        if (sparks > 0) sb.AppendLine("- Radiation detector: " + sparks + " sparks. Click them as they fly by.");
        if (ice > 0) sb.AppendLine("- Ice sensor: " + ice + " dark craters. Hold the click on the blue ice.");
        sb.AppendLine(hasAntenna ? "- Long-range antenna: ready." : "- No long-range antenna: the data cannot be sent home.");
        sb.AppendLine();
        sb.Append("Keep your eyes open... something else might be hiding in the craters!");
        return sb.ToString();
    }

    public void StartGame()
    {
        if (Playing) return;
        Playing = true;
        Finished = false;
        if (introPanel != null) introPanel.SetActive(false);
        if (albumPanel != null) albumPanel.SetActive(false);
        if (titleLabel != null) titleLabel.text = "1/2  Moon Photo Safari";
        if (safari != null) safari.Begin(plan, S);
        else OnSafariFinished();
    }

    void OnSafariFinished()
    {
        if (!Playing) return;
        if (titleLabel != null) titleLabel.text = "2/2  Signal Catcher";
        float plannedBits = ResearchMath.PlannedBits(plan);
        float rate = ResearchMath.SendRate(plannedBits, ResearchMath.BeamSecondsForAll(parts, mission, S));
        List<ResearchPhoto> photos = safari != null ? safari.photos : new List<ResearchPhoto>();
        if (catcher != null) catcher.Begin(photos, rate, hasAntenna, S);
        else OnCatcherFinished();
    }

    void OnCatcherFinished()
    {
        if (!Playing) return;
        Playing = false;
        Result = Calculate();
        Finished = true;
        ShowAlbum();
    }

    ResearchResult Calculate()
    {
        List<ResearchPhoto> photos = safari != null ? safari.photos : new List<ResearchPhoto>();
        ResearchResult r = new ResearchResult();
        r.plannedBits = ResearchMath.PlannedBits(plan);
        r.targetsShown = safari != null ? safari.TargetsShown : 0;
        int landmarks = 0, gold = 0, aliens = 0;
        foreach (ResearchPhoto p in photos)
        {
            if (p.alien)
            {
                aliens++;
                continue;
            }
            r.sciencePhotos++;
            r.capturedBits += p.bits;
            if (p.type == ResearchMath.TargetType.Landmark)
            {
                landmarks++;
                if (p.gold) gold++;
            }
        }
        r.sentBits = catcher != null ? Mathf.Min(catcher.SentBits, r.capturedBits) : 0f;
        r.goldShare = landmarks > 0 ? gold / (float)landmarks : 0f;
        int appearances = S.alienTimes != null ? S.alienTimes.Length : 0;
        r.alienShare = appearances > 0 ? Mathf.Clamp01(aliens / (float)appearances) : 0f;
        r.experimentFraction = ResearchMath.ExperimentFraction(r.capturedBits, r.plannedBits);
        r.downlinkFraction = ResearchMath.DownlinkFraction(r.sentBits, r.plannedBits, r.experimentFraction);
        r.raw = ResearchMath.Raw(r.experimentFraction, r.downlinkFraction, r.goldShare, r.alienShare, S);
        return r;
    }

    void ShowAlbum()
    {
        if (titleLabel != null) titleLabel.text = "Your photo album";
        if (albumPanel != null) albumPanel.SetActive(true);
        List<ResearchPhoto> photos = safari != null ? safari.photos : new List<ResearchPhoto>();

        if (albumGrid != null)
        {
            ResearchUI.Clear(albumGrid);
            foreach (ResearchPhoto p in photos)
            {
                RectTransform cell = ResearchUI.Box(albumGrid, "Cell", new Vector2(80f, 90f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
                RectTransform thumb = PhotoSafari.AddThumbnail(cell, p, circle);
                thumb.anchoredPosition = new Vector2(0f, 12f);
                if (!p.sent)
                {
                    CanvasGroup group = thumb.gameObject.AddComponent<CanvasGroup>();
                    group.alpha = 0.35f;
                }
                string label = p.alien ? "Surprise!" : p.sent ? "Home" : "On board";
                Color color = p.alien ? new Color(0.6f, 1f, 0.5f) : p.sent ? new Color(0.4f, 1f, 0.5f) : new Color(0.7f, 0.7f, 0.75f);
                ResearchUI.Label(cell, "State", label, 14f, new Vector2(0f, -32f), new Vector2(80f, 20f), color);
            }
        }
        if (albumText != null) albumText.text = AlbumText(Result, photos, hasAntenna);
    }

    // End-of-minigame text: results, the alien stamp and one lesson
    public static string AlbumText(ResearchResult r, List<ResearchPhoto> photos, bool antenna)
    {
        StringBuilder sb = new StringBuilder();
        bool alien = false;
        foreach (ResearchPhoto p in photos) if (p.alien) alien = true;

        sb.AppendLine("Experiments: " + (r.experimentFraction * 100f).ToString("0") + " % of the planned data ("
            + r.sciencePhotos + " of " + r.targetsShown + " targets).");
        sb.AppendLine("Data home: " + (r.downlinkFraction * 100f).ToString("0") + " %.   Research performance: "
            + (r.raw * 100f).ToString("0") + " %.");
        sb.AppendLine();
        if (alien)
        {
            sb.AppendLine("<b>Surprise photo!</b> No aliens have ever been found on the Moon. The real treasure lunar missions look for is "
                + "water ice hidden in craters that never see sunlight: one day it could give astronauts water, air and rocket fuel.");
            sb.AppendLine();
        }
        if (r.plannedBits <= 0f)
            sb.Append("Without science instruments there was nothing to measure. Instruments are what turn a trip into a science mission.");
        else if (!antenna)
            sb.Append("Your instruments collected data, but no long-range antenna was installed to send it home.");
        else if (r.capturedBits > 0f && r.downlinkFraction < r.experimentFraction - 0.05f)
            sb.Append("You collected more than you could send. Real missions plan their observations and their contact time with Earth together.");
        else
            sb.Append("Behind the Moon there is no contact with Earth: real spacecraft keep their data on board and send it when they see Earth again.");
        return sb.ToString();
    }

    void Continue()
    {
        if (!Finished) return;
        if (Completed != null) Completed(Result);
        gameObject.SetActive(false);
    }
}
