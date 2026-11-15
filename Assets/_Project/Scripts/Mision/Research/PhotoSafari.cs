using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game 1 of the research minigame (task 14, claude_c): Moon Photo Safari.
// The far side of the Moon scrolls under the spacecraft. A viewfinder follows the mouse:
//   landmark (ART-SCI-LUNAR)  -> click while it is inside the viewfinder (centered = gold star)
//   spark    (ART-SCI-RAD)    -> click it as it zips across the sky
//   ice      (ART-SCI-HEAVY)  -> hold the click on it until the ring fills
//   little alien (bonus)      -> peeks out of a crater at fixed times; click it. Never counted as science.
// Targets follow a fixed schedule (ResearchMath.PlanTargets): no luck involved.
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
        public ResearchMath.Target target; // null for the alien
        public RectTransform rt;
        public Image ring;                 // ice scan progress
        public float born;
        public float life;
        public Vector2 from;
        public Vector2 to;
        public float hold;
        public bool done;
    }

    ResearchSettings settings;
    List<ResearchMath.Target> plan;
    readonly List<Live> live = new List<Live>();
    int nextTarget;
    int nextAlien;
    float time;
    bool skipFirstClick;
    RectTransform viewfinder;
    Image flash;
    RectTransform surface;
    float craterTimer;

    Vector2 Half { get { return area.rect.size * 0.5f; } }
    float SurfaceTop { get { return -Half.y * 0.1f; } }

    public void Begin(List<ResearchMath.Target> targets, ResearchSettings s)
    {
        settings = s != null ? s : ResearchSettings.Default;
        plan = targets ?? new List<ResearchMath.Target>();
        photos.Clear();
        live.Clear();
        decorations.Clear();
        ResearchUI.Clear(area);
        ResearchUI.Clear(photoBar);
        nextTarget = 0;
        nextAlien = 0;
        time = 0f;
        TargetsShown = 0;
        craterTimer = 0f;
        skipFirstClick = true;

        // Sky (area background) + far-side surface + decorative craters
        surface = ResearchUI.Box(area, "Surface", new Vector2(area.rect.width, Half.y + SurfaceTop), Vector2.zero, new Color(0.42f, 0.42f, 0.45f));
        surface.anchoredPosition = new Vector2(0f, (-Half.y + SurfaceTop) * 0.5f);
        for (int i = 0; i < 6; i++) SpawnDecoration(-Half.x + i * area.rect.width / 5f);

        viewfinder = ResearchUI.Circle(area, "Viewfinder", settings.viewfinderRadius * 2f, Vector2.zero, new Color(1f, 1f, 1f, 0.12f), circle);
        ResearchUI.Circle(viewfinder, "Gold", settings.goldRadius * 2f, Vector2.zero, new Color(1f, 0.85f, 0.2f, 0.18f), circle);
        ResearchUI.Box(viewfinder, "CrossH", new Vector2(settings.viewfinderRadius * 2f, 2f), Vector2.zero, new Color(1f, 1f, 1f, 0.5f));
        ResearchUI.Box(viewfinder, "CrossV", new Vector2(2f, settings.viewfinderRadius * 2f), Vector2.zero, new Color(1f, 1f, 1f, 0.5f));
        flash = ResearchUI.Box(area, "Flash", area.rect.size, Vector2.zero, new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();

        if (infoLabel != null)
            infoLabel.text = plan.Count == 0
                ? "No science instruments on board: only tourist photos today!"
                : "Far side of the Moon. Click targets inside the viewfinder. Hold the click on blue ice to scan it.";
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

        // Spawn science targets and the alien on schedule
        while (nextTarget < plan.Count && time >= plan[nextTarget].spawnTime) Spawn(plan[nextTarget++], nextTarget);
        if (settings.alienTimes != null && nextAlien < settings.alienTimes.Length && time >= settings.alienTimes[nextAlien])
            SpawnAlien(nextAlien++);

        craterTimer += dt;
        if (craterTimer > 1.2f)
        {
            craterTimer = 0f;
            SpawnDecoration(Half.x + 60f);
        }
        MoveDecorations(dt);

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
            Place(l, age);
            float distance = Vector2.Distance(mouse, l.rt.anchoredPosition);
            bool inside = hasMouse && distance <= settings.viewfinderRadius;

            if (l.target != null && l.target.type == ResearchMath.TargetType.Ice)
            {
                if (inside && held)
                {
                    l.hold += dt;
                    if (l.ring != null) l.ring.fillAmount = l.hold / settings.iceHoldSeconds;
                    if (l.hold >= settings.iceHoldSeconds) Capture(l, false);
                }
                continue;
            }
            if (click && inside && distance < bestDistance && Clickable(l, age))
            {
                best = l;
                bestDistance = distance;
            }
        }
        if (best != null) Capture(best, best.target != null && best.target.type == ResearchMath.TargetType.Landmark && bestDistance <= settings.goldRadius);

        if (flash != null && flash.color.a > 0f) flash.color = new Color(1f, 1f, 1f, Mathf.Max(0f, flash.color.a - dt * 3f));

        if (time >= settings.safariSeconds)
        {
            Stop();
            if (Finished != null) Finished();
        }
    }

    // ---------- Targets ----------

    void Spawn(ResearchMath.Target t, int index)
    {
        TargetsShown++;
        Live l = new Live { target = t, born = time };
        if (t.type == ResearchMath.TargetType.Spark)
        {
            l.life = settings.sparkSeconds;
            bool down = index % 2 == 0;
            l.from = new Vector2(-Half.x - 30f, down ? Half.y * 0.85f : Half.y * 0.25f);
            l.to = new Vector2(Half.x + 30f, down ? Half.y * 0.3f : Half.y * 0.8f);
            l.rt = ResearchUI.Circle(area, "Spark", 34f, l.from, ResearchUI.ColorOf(t.type), circle);
            ResearchUI.Circle(l.rt, "Core", 14f, Vector2.zero, Color.white, circle);
        }
        else
        {
            l.life = settings.landmarkSeconds;
            float lane = index % 2 == 0 ? -Half.y * 0.35f : -Half.y * 0.7f;
            l.from = new Vector2(Half.x + 70f, lane);
            l.to = new Vector2(-Half.x - 70f, lane);
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

    void SpawnAlien(int index)
    {
        Live l = new Live { target = null, born = time, life = settings.alienSeconds + 0.6f };
        float x = (index % 2 == 0 ? -0.35f : 0.4f) * Half.x;
        l.rt = ResearchUI.Box(area, "AlienCrater", new Vector2(150f, 120f), new Vector2(x, -Half.y * 0.2f), new Color(0f, 0f, 0f, 0f));
        RectTransform alien = ResearchUI.Alien(l.rt, circle);
        alien.anchoredPosition = new Vector2(0f, -60f);
        ResearchUI.Circle(l.rt, "Rim", 150f, new Vector2(0f, -70f), new Color(0.36f, 0.36f, 0.39f), circle); // drawn over the alien
        l.from = l.to = l.rt.anchoredPosition;
        viewfinder.SetAsLastSibling();
        flash.transform.SetAsLastSibling();
        live.Add(l);
    }

    void Place(Live l, float age)
    {
        if (l.target == null)
        {
            // Alien: rises (0.3 s), waves, hides (0.3 s)
            RectTransform alien = (RectTransform)l.rt.GetChild(0);
            float up = Mathf.Clamp01(age / 0.3f) * Mathf.Clamp01((l.life - age) / 0.3f);
            alien.anchoredPosition = new Vector2(0f, Mathf.Lerp(-60f, 25f, up));
            alien.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 12f) * 8f * up);
            return;
        }
        l.rt.anchoredPosition = Vector2.Lerp(l.from, l.to, age / l.life);
    }

    bool Clickable(Live l, float age)
    {
        if (l.target == null) return age >= 0.25f && age <= l.life - 0.25f; // alien only while it is out
        return l.target.type != ResearchMath.TargetType.Ice;
    }

    void Capture(Live l, bool gold)
    {
        l.done = true;
        ResearchPhoto photo = new ResearchPhoto
        {
            alien = l.target == null,
            type = l.target != null ? l.target.type : ResearchMath.TargetType.Landmark,
            bits = l.target != null ? l.target.bits : 0f,
            gold = gold,
        };
        photos.Add(photo);
        if (flash != null) flash.color = photo.alien ? new Color(0.8f, 1f, 0.6f, 0.8f) : new Color(1f, 1f, 1f, 0.6f);
        AddThumbnail(photoBar, photo, circle);
        if (infoLabel != null)
            infoLabel.text = photo.alien ? "Surprise photo! What was that?"
                : gold ? "Perfect shot! Gold star."
                : l.target.type == ResearchMath.TargetType.Ice ? "Ice scan complete!"
                : l.target.type == ResearchMath.TargetType.Spark ? "Radiation spark caught!" : "Got it!";
    }

    // Thumbnail used in the photo bar, the send queue and the album
    public static RectTransform AddThumbnail(RectTransform parent, ResearchPhoto photo, Sprite circle)
    {
        if (parent == null) return null;
        RectTransform t = ResearchUI.Box(parent, "Photo", new Vector2(56f, 56f), Vector2.zero, new Color(0.1f, 0.12f, 0.18f));
        if (photo.alien) ResearchUI.Alien(t, circle).localScale = Vector3.one * 0.45f;
        else ResearchUI.Circle(t, "Subject", 36f, Vector2.zero, ResearchUI.ColorOf(photo.type), circle);
        if (photo.gold) ResearchUI.Circle(t, "Star", 16f, new Vector2(20f, 20f), new Color(1f, 0.85f, 0.2f), circle);
        return t;
    }

    // ---------- Decoration ----------

    readonly List<RectTransform> decorations = new List<RectTransform>();

    void SpawnDecoration(float x)
    {
        float size = UnityEngine.Random.Range(30f, 80f);
        float y = UnityEngine.Random.Range(-Half.y * 0.9f, SurfaceTop - 30f);
        RectTransform d = ResearchUI.Circle(area, "Crater", size, new Vector2(x, y), new Color(0.35f, 0.35f, 0.38f), circle);
        d.SetSiblingIndex(1); // right above the surface, below targets
        decorations.Add(d);
    }

    void MoveDecorations(float dt)
    {
        float speed = area.rect.width / settings.landmarkSeconds;
        for (int i = decorations.Count - 1; i >= 0; i--)
        {
            RectTransform d = decorations[i];
            if (d == null)
            {
                decorations.RemoveAt(i);
                continue;
            }
            d.anchoredPosition += Vector2.left * speed * dt;
            if (d.anchoredPosition.x < -Half.x - 100f)
            {
                Destroy(d.gameObject);
                decorations.RemoveAt(i);
            }
        }
    }
}
