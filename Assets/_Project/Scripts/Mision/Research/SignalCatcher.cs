using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game 2 of the research minigame (task 14, claude_c): Signal Catcher.
// Earth moves around the sky above the Moon's edge (left-right and up-down, always in view).
// Hold the mouse button and move it to aim the antenna: while the beam touches Earth, the photos fly home as
// envelopes (oldest first). Sending speed comes from planning (ResearchMath.BeamSecondsForAll: more data = more
// beam time needed). No antenna = no beam.
public class SignalCatcher : MonoBehaviour
{
    public RectTransform area;
    public TMP_Text timerLabel;
    public TMP_Text infoLabel;
    public Sprite circle;

    public event Action Finished;
    public bool Running { get; private set; }
    public float SentBits { get; private set; }

    ResearchSettings settings;
    List<ResearchPhoto> photos;
    float capturedBits;
    float rate;           // bits per second of beam on Earth
    bool antenna;
    float time;
    float endAt;
    bool everTouched;
    float envelopeTimer;

    RectTransform earth;
    RectTransform dish;   // rotating container: dish + beam
    Image beam;
    Vector2 pivot;
    float dishAngle;      // degrees from vertical, positive = right
    readonly List<RectTransform> envelopes = new List<RectTransform>();
    readonly List<float> envelopeAge = new List<float>();

    Vector2 Half { get { return area.rect.size * 0.5f; } }

    public void Begin(List<ResearchPhoto> taken, float sendRate, bool hasAntenna, ResearchSettings s)
    {
        settings = s != null ? s : ResearchSettings.Default;
        photos = taken ?? new List<ResearchPhoto>();
        rate = sendRate;
        antenna = hasAntenna && sendRate > 0f;
        capturedBits = 0f;
        foreach (ResearchPhoto p in photos) capturedBits += p.bits;
        SentBits = 0f;
        time = 0f;
        everTouched = false;
        envelopeTimer = 0f;
        dishAngle = 0f;
        envelopes.Clear();
        envelopeAge.Clear();
        ResearchUI.Clear(area);

        // Earth, the Moon's edge, then the antenna
        earth = ResearchUI.Circle(area, "Earth", 90f, EarthPosition(0f), new Color(0.2f, 0.45f, 0.95f), circle);
        ResearchUI.Circle(earth, "Land", 34f, new Vector2(-12f, 10f), new Color(0.3f, 0.75f, 0.35f), circle);
        ResearchUI.Circle(earth, "Cloud", 26f, new Vector2(16f, -14f), new Color(1f, 1f, 1f, 0.8f), circle);
        float moonSize = area.rect.width * 1.6f;
        ResearchUI.Circle(area, "Moon", moonSize, new Vector2(0f, -Half.y - moonSize * 0.5f + Half.y * 0.45f), new Color(0.55f, 0.55f, 0.58f), circle);

        pivot = new Vector2(0f, -Half.y + Half.y * 0.45f + 20f);
        ResearchUI.Box(area, "Spacecraft", new Vector2(70f, 50f), pivot + new Vector2(0f, -20f), new Color(0.85f, 0.85f, 0.88f));
        dish = ResearchUI.Box(area, "Antenna", new Vector2(10f, 10f), pivot, new Color(0f, 0f, 0f, 0f));
        // A straight bar as wide as the beam cone at Earth's usual distance (the hit test uses the angle)
        RectTransform beamRt = ResearchUI.Box(dish, "Beam", new Vector2(2f * Half.y * 1.2f * Mathf.Tan(settings.beamHalfAngle * Mathf.Deg2Rad), 1400f),
            Vector2.zero, new Color(1f, 0.9f, 0.3f, 0.18f));
        beamRt.pivot = new Vector2(0.5f, 0f);
        beamRt.anchoredPosition = Vector2.zero;
        beam = beamRt.GetComponent<Image>();
        ResearchUI.Box(dish, "Mast", new Vector2(8f, 40f), new Vector2(0f, 10f), new Color(0.7f, 0.7f, 0.75f));
        RectTransform plate = ResearchUI.Circle(dish, "Dish", 80f, new Vector2(0f, 34f), new Color(0.9f, 0.9f, 0.95f), circle);
        plate.localScale = new Vector3(1f, 0.45f, 1f);
        beam.enabled = antenna;

        if (infoLabel != null)
        {
            if (!antenna) infoLabel.text = "No long-range antenna: the photos cannot reach Earth.";
            else if (capturedBits <= 0f) infoLabel.text = "No science data on board to send.";
            else infoLabel.text = "Earth is in view! Hold the mouse button and aim the antenna at Earth.";
        }
        endAt = !antenna || capturedBits <= 0f ? 3f : settings.catcherSeconds;
        Running = true;
    }

    public void Stop()
    {
        Running = false;
        ResearchUI.Clear(area);
        envelopes.Clear();
        envelopeAge.Clear();
    }

    // Earth swings left-right and bobs up-down in the sky, always above the Moon's edge so it can always be aimed at
    Vector2 EarthPosition(float t)
    {
        float x = Mathf.Sin(t * 2f * Mathf.PI / settings.earthPeriodSeconds) * Half.x * 0.75f;
        float y = Mathf.Lerp(Half.y * 0.05f, Half.y * 0.75f, 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / settings.earthBobSeconds));
        return new Vector2(x, y);
    }

    bool wasOnEarth; // to play the beam sound when the beam reaches Earth

    void Update()
    {
        if (!Running) { wasOnEarth = false; return; }
        float dt = Time.deltaTime;
        time += dt;
        if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(Mathf.Max(0f, endAt - time)) + " s";

        earth.anchoredPosition = EarthPosition(time);

        Vector2 mouse;
        if (ResearchUI.Held() && ResearchUI.MouseLocal(area, out mouse))
        {
            Vector2 d = mouse - pivot;
            if (d.sqrMagnitude > 1f) dishAngle = Mathf.Clamp(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, -85f, 85f);
        }
        dish.localRotation = Quaternion.Euler(0f, 0f, -dishAngle);

        Vector2 toEarth = earth.anchoredPosition - pivot;
        float earthAngle = Mathf.Atan2(toEarth.x, toEarth.y) * Mathf.Rad2Deg;
        bool onEarth = antenna && Mathf.Abs(ResearchMath.AngleGap(dishAngle, earthAngle)) <= settings.beamHalfAngle;
        if (beam != null) beam.color = onEarth ? new Color(0.4f, 1f, 0.5f, 0.3f) : new Color(1f, 0.9f, 0.3f, 0.18f);
        if (onEarth && !wasOnEarth && SentBits < capturedBits) Sfx.Play(Sfx.ZooLaserRay, 1f, 0.5f);
        wasOnEarth = onEarth;

        if (onEarth && SentBits < capturedBits)
        {
            everTouched = true;
            SentBits = Mathf.Min(capturedBits, SentBits + rate * dt);
            envelopeTimer += dt;
            if (envelopeTimer > 0.2f)
            {
                envelopeTimer = 0f;
                envelopes.Add(ResearchUI.Box(area, "Envelope", new Vector2(22f, 14f), pivot, Color.white));
                envelopeAge.Add(0f);
            }
        }
        MarkSent();
        MoveEnvelopes(dt);

        if (infoLabel != null && antenna && capturedBits > 0f)
        {
            float pct = SentBits / capturedBits * 100f;
            infoLabel.text = (onEarth ? "Sending... " : "Aim the beam at Earth! ") + "Data home: " + pct.ToString("0") + " %";
        }

        // Done: time is up, or everything is home
        if (antenna && capturedBits > 0f && SentBits >= capturedBits && endAt > time + 1.5f) endAt = time + 1.5f;
        if (time >= endAt)
        {
            Stop();
            if (Finished != null) Finished();
        }
    }

    // Photos reach Earth in the order they were taken.
    void MarkSent()
    {
        if (!everTouched) return;
        float cumulative = 0f;
        foreach (ResearchPhoto p in photos)
        {
            cumulative += p.bits;
            if (SentBits + 0.5f >= cumulative) p.sent = true;
        }
    }

    void MoveEnvelopes(float dt)
    {
        for (int i = envelopes.Count - 1; i >= 0; i--)
        {
            envelopeAge[i] += dt;
            float k = envelopeAge[i] / 0.6f;
            if (k >= 1f || envelopes[i] == null)
            {
                if (envelopes[i] != null) Destroy(envelopes[i].gameObject);
                envelopes.RemoveAt(i);
                envelopeAge.RemoveAt(i);
                continue;
            }
            envelopes[i].anchoredPosition = Vector2.Lerp(pivot, earth.anchoredPosition, k);
        }
    }
}
