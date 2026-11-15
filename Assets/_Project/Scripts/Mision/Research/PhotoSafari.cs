using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game 1 of the research minigame (task 14, claude_c): Moon Photo Safari.
// Far side of the Moon: a viewfinder follows the mouse and the science targets of the mounted instruments
// cross the screen from many directions:
//   landmark (ART-SCI-LUNAR)  -> crosses the surface from the left or the right; click it inside the viewfinder
//                                (centered = gold star)
//   spark    (ART-SCI-RAD)    -> radiation balls flying in from every side and diagonal; click them
//   ice      (ART-SCI-HEAVY)  -> dark crater with blue ice crossing the surface; hold the click until the ring fills
// Targets follow a fixed schedule and fixed directions (ResearchMath.PlanTargets): no luck involved.
public class PhotoSafari : MonoBehaviour
{
    public RectTransform area;      // shared play area (masked), set by the builder
    public RectTransform photoBar;  // thumbnails of the photos taken
    public TMP_Text timerLabel;
    public TMP_Text infoLabel;
    public Sprite circle;

    public event Action Finished;
    public bool Running { get; private set; }
    public readonly List<ResearchPhoto> photos = new List<ResearchPhoto>();
    public int TargetsShown { get; private set; }

    class Live
    {
        public ResearchMath.Target target;
        public RectTransform rt;
        public Image ring;     // ice scan progress
        public float born;
        public float life;
        public Vector2 from;
        public Vector2 to;
        public float hold;
        public bool done;
    }

    // Travel directions of the sparks, in order: right, down-left, up, down-right, left, up-right, down, up-left
    static readonly Vector2[] SparkDirections =
    {
        new Vector2(1f, 0f), new Vector2(-1f, -1f), new Vector2(0f, 1f), new Vector2(1f, -1f),
        new Vector2(-1f, 0f), new Vector2(1f, 1f), new Vector2(0f, -1f), new Vector2(-1f, 1f),
    };

    ResearchSettings settings;
    List<ResearchMath.Target> plan;
    readonly List<Live> live = new List<Live>();
    int nextTarget;
    int sparkCount;
    int surfaceCount;
    float time;
    bool skipFirstClick;
    RectTransform viewfinder;
    Image flash;

    Vector2 Half { get { return area.rect.size * 0.5f; } }
    float SurfaceTop { get { return -Half.y * 0.1f; } }

    public void Begin(List<ResearchMath.Target> targets, ResearchSettings s)
    {
        settings = s != null ? s : ResearchSettings.Default;
        plan = targets ?? new List<ResearchMath.Target>();
        photos.Clear();
        live.Clear();
        ResearchUI.Clear(area);
        ResearchUI.Clear(photoBar);
        nextTarget = 0;
        sparkCount = 0;
        surfaceCount = 0;
        time = 0f;
        TargetsShown = 0;
        skipFirstClick = true;

        // Sky (area background) + far-side surface with a few fixed craters
        RectTransform surface = ResearchUI.Box(area, "Surface", new Vector2(area.rect.width, Half.y + SurfaceTop), Vector2.zero, new Color(0.42f, 0.42f, 0.45f));
        surface.anchoredPosition = new Vector2(0f, (-Half.y + SurfaceTop) * 0.5f);
        for (int i = 0; i < 7; i++)
        {
            float x = -Half.x + (i + 0.5f) * area.rect.width / 7f;
            float y = Mathf.Lerp(-Half.y * 0.9f, SurfaceTop - 40f, (i * 37 % 10) / 10f);
            ResearchUI.Circle(area, "Crater", 30f + (i * 23 % 50), new Vector2(x, y), new Color(0.35f, 0.35f, 0.38f), circle);
        }
        // A few stars
        for (int i = 0; i < 25; i++)
        {
            float x = -Half.x + (i * 73 % 100) / 100f * area.rect.width;
            float y = SurfaceTop + 20f + (i * 41 % 100) / 100f * (Half.y - SurfaceTop - 20f);
            ResearchUI.Circle(area, "Star", 3f + i % 3, new Vector2(x, y), new Color(1f, 1f, 1f, 0.6f), circle);
        }

        viewfinder = ResearchUI.Circle(area, "Viewfinder", settings.viewfinderRadius * 2f, Vector2.zero, new Color(1f, 1f, 1f, 0.12f), circle);
        ResearchUI.Circle(viewfinder, "Gold", settings.goldRadius * 2f, Vector2.zero, new Color(1f, 0.85f, 0.2f, 0.18f), circle);
        ResearchUI.Box(viewfinder, "CrossH", new Vector2(settings.viewfinderRadius * 2f, 2f), Vector2.zero, new Color(1f, 1f, 1f, 0.5f));
        ResearchUI.Box(viewfinder, "CrossV", new Vector2(2f, settings.viewfinderRadius * 2f), Vector2.zero, new Color(1f, 1f, 1f, 0.5f));
        flash = ResearchUI.Box(area, "Flash", area.rect.size, Vector2.zero, new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();

        if (infoLabel != null)
            infoLabel.text = plan.Count == 0
                ? "No science instruments on board: there is nothing to measure."
                : "Click the targets inside the viewfinder. Hold the click on blue ice to scan it.";
        Running = true;
    }

    public void Stop()
    {
        Running = false;
        ResearchUI.Clear(area);
        live.Clear();
    }

    void Update()
    {
        if (!Running) return;
        float dt = Time.deltaTime;
        time += dt;
        if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(Mathf.Max(0f, settings.safariSeconds - time)) + " s";

        // Spawn science targets on schedule
        while (nextTarget < plan.Count && time >= plan[nextTarget].spawnTime) Spawn(plan[nextTarget++]);

        Vector2 mouse;
        bool hasMouse = ResearchUI.MouseLocal(area, out mouse);
        if (hasMouse) viewfinder.anchoredPosition = mouse;
        bool click = ResearchUI.Clicked() && !skipFirstClick;
        bool held = ResearchUI.Held() && !skipFirstClick;
        skipFirstClick = false;

        Live best = null;
        float bestDistance = float.MaxValue;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Live l = live[i];
            float age = time - l.born;
            if (l.done || age > l.life)
            {
                Destroy(l.rt.gameObject);
                live.RemoveAt(i);
                continue;
            }
            l.rt.anchoredPosition = Vector2.Lerp(l.from, l.to, age / l.life);
            float distance = Vector2.Distance(mouse, l.rt.anchoredPosition);
            bool inside = hasMouse && distance <= settings.viewfinderRadius;

            if (l.target.type == ResearchMath.TargetType.Ice)
            {
                if (inside && held)
                {
                    l.hold += dt;
                    if (l.ring != null) l.ring.fillAmount = l.hold / settings.iceHoldSeconds;
                    if (l.hold >= settings.iceHoldSeconds) Capture(l, false);
                }
                continue;
            }
            if (click && inside && distance < bestDistance)
            {
                best = l;
                bestDistance = distance;
            }
        }
        if (best != null) Capture(best, best.target.type == ResearchMath.TargetType.Landmark && bestDistance <= settings.goldRadius);

        if (flash != null && flash.color.a > 0f) flash.color = new Color(1f, 1f, 1f, Mathf.Max(0f, flash.color.a - dt * 3f));

        if (time >= settings.safariSeconds)
        {
            Stop();
            if (Finished != null) Finished();
        }
    }

    // ---------- Targets ----------

    void Spawn(ResearchMath.Target t)
    {
        TargetsShown++;
        Live l = new Live { target = t, born = time };
        if (t.type == ResearchMath.TargetType.Spark)
        {
            // From one side of the screen to the opposite one, through a point inside the play area
            int n = sparkCount++;
            Vector2 dir = SparkDirections[n % SparkDirections.Length].normalized;
            Vector2 through = new Vector2(((n * 37 % 100) / 100f - 0.5f) * Half.x, ((n * 61 % 100) / 100f - 0.5f) * Half.y);
            l.from = Edge(through, -dir);
            l.to = Edge(through, dir);
            l.life = Mathf.Max(1.2f, settings.sparkSeconds * Vector2.Distance(l.from, l.to) / area.rect.width);
            l.rt = ResearchUI.Circle(area, "Spark", 42f, l.from, ResearchUI.ColorOf(t.type), circle);
            ResearchUI.Circle(l.rt, "Core", 18f, Vector2.zero, Color.white, circle);
        }
        else
        {
            // Surface targets cross from the right or from the left, in three lanes
            int n = surfaceCount++;
            float lane = -Half.y * (0.3f + 0.25f * (n % 3));
            bool fromRight = n % 2 == 0;
            l.life = settings.landmarkSeconds;
            l.from = new Vector2(fromRight ? Half.x + 70f : -Half.x - 70f, lane);
            l.to = new Vector2(fromRight ? -Half.x - 70f : Half.x + 70f, lane);
            if (t.type == ResearchMath.TargetType.Ice)
            {
                l.rt = ResearchUI.Circle(area, "IceCrater", 110f, l.from, new Color(0.12f, 0.13f, 0.18f), circle);
                ResearchUI.Circle(l.rt, "Ice", 34f, Vector2.zero, ResearchUI.ColorOf(t.type), circle);
                l.ring = ResearchUI.Circle(l.rt, "ScanRing", 130f, Vector2.zero, new Color(0.4f, 0.8f, 1f, 0.45f), circle).GetComponent<Image>();
                l.ring.type = Image.Type.Filled;
                l.ring.fillMethod = Image.FillMethod.Radial360;
                l.ring.fillAmount = 0f;
                l.ring.transform.SetAsFirstSibling();
            }
            else
            {
                l.rt = ResearchUI.Circle(area, "Landmark", 100f, l.from, new Color(0.62f, 0.6f, 0.56f), circle);
                ResearchUI.Circle(l.rt, "Floor", 70f, new Vector2(4f, -4f), new Color(0.3f, 0.29f, 0.28f), circle);
                ResearchUI.Circle(l.rt, "Peak", 16f, new Vector2(-6f, 4f), new Color(0.75f, 0.73f, 0.7f), circle);
            }
        }
        viewfinder.SetAsLastSibling();
        flash.transform.SetAsLastSibling();
        live.Add(l);
    }

    // Point just outside the play area, going from p along dir
    Vector2 Edge(Vector2 p, Vector2 dir)
    {
        float margin = 40f;
        float tx = dir.x > 0f ? (Half.x + margin - p.x) / dir.x : dir.x < 0f ? (-Half.x - margin - p.x) / dir.x : float.MaxValue;
        float ty = dir.y > 0f ? (Half.y + margin - p.y) / dir.y : dir.y < 0f ? (-Half.y - margin - p.y) / dir.y : float.MaxValue;
        return p + dir * Mathf.Min(tx, ty);
    }

    void Capture(Live l, bool gold)
    {
        l.done = true;
        ResearchPhoto photo = new ResearchPhoto { type = l.target.type, bits = l.target.bits, gold = gold };
        photos.Add(photo);
        if (flash != null) flash.color = new Color(1f, 1f, 1f, 0.6f);
        AddThumbnail(photoBar, photo, circle);
        if (infoLabel != null)
            infoLabel.text = gold ? "Perfect shot! Gold star."
                : l.target.type == ResearchMath.TargetType.Ice ? "Ice scan complete!"
                : l.target.type == ResearchMath.TargetType.Spark ? "Radiation caught!" : "Got it!";
    }

    // Thumbnail used in the photo bar and the album
    public static RectTransform AddThumbnail(RectTransform parent, ResearchPhoto photo, Sprite circle)
    {
        if (parent == null) return null;
        RectTransform t = ResearchUI.Box(parent, "Photo", new Vector2(56f, 56f), Vector2.zero, new Color(0.1f, 0.12f, 0.18f));
        ResearchUI.Circle(t, "Subject", 36f, Vector2.zero, ResearchUI.ColorOf(photo.type), circle);
        if (photo.gold) ResearchUI.Circle(t, "Star", 16f, new Vector2(20f, 20f), new Color(1f, 0.85f, 0.2f), circle);
        return t;
    }
}
