using UnityEngine;
using UnityEngine.UI;

// Animated menu background: sky over the Earth, hills, a launch tower and a rocket that lifts off
// with fire and smoke, leaves the screen and starts again. Everything is drawn in code (no image files).
// Put it on an empty RectTransform that fills the canvas, as the first child (behind the menu).
public class LaunchBackground : MonoBehaviour
{
    [Range(0f, 1f)] public float rocketX = 0.8f; // horizontal position of the pad (0 = left, 1 = right)
    public float groundHeight = 190f;            // canvas units from the bottom
    public float idleTime = 2f;                  // seconds on the pad before ignition
    public float ignitionTime = 1.6f;
    public float acceleration = 140f;            // canvas units per second squared
    public float waitTime = 2.5f;                // seconds after it leaves the screen

    const int SmokeCount = 60;

    RectTransform area;
    RectTransform rocket;
    RectTransform flame;
    RectTransform[] clouds;
    RectTransform[] puffs = new RectTransform[SmokeCount];
    RawImage[] puffImages = new RawImage[SmokeCount];
    Vector2[] puffVelocity = new Vector2[SmokeCount];
    float[] puffAge = new float[SmokeCount];
    float[] puffLife = new float[SmokeCount];
    float[] puffSize = new float[SmokeCount];
    int nextPuff;

    int state;        // 0 idle, 1 ignition, 2 lift-off, 3 waiting
    float stateTime;
    float speed;
    float smokeTimer;
    Vector2 padPosition;

    void Start()
    {
        area = (RectTransform)transform;
        Texture2D soft = SoftCircle(64);
        Rect r = area.rect;

        AddRaw("Sky", SkyTexture(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage("Sun", soft, new Color(1f, 0.85f, 0.6f, 0.9f), new Vector2(r.width * 0.2f, groundHeight + 160f), new Vector2(260, 260));

        clouds = new RectTransform[4];
        for (int i = 0; i < clouds.Length; i++)
        {
            Vector2 pos = new Vector2(r.width * (0.1f + 0.27f * i), r.height * (0.62f + 0.09f * (i % 2)));
            clouds[i] = AddImage("Cloud", soft, new Color(1f, 1f, 1f, 0.55f), pos, new Vector2(420 - 50 * (i % 3), 110));
        }

        // Hills on the horizon and flat ground (same green as the near hills) where the pad stands
        AddRaw("Hills", HillsTexture(), Vector2.zero, new Vector2(1f, 0f), new Vector2(0, groundHeight - 80f), new Vector2(0, groundHeight + 220f));
        AddRaw("Ground", null, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0, groundHeight));

        // Launch tower and pad
        padPosition = new Vector2(r.width * rocketX, groundHeight);
        Color steel = new Color(0.32f, 0.34f, 0.38f);
        AddImage("Tower", null, steel, padPosition + new Vector2(95, 200), new Vector2(26, 400));
        AddImage("TowerArm", null, steel, padPosition + new Vector2(65, 300), new Vector2(60, 10));
        AddImage("TowerArm", null, steel, padPosition + new Vector2(65, 180), new Vector2(60, 10));
        AddImage("Pad", null, new Color(0.2f, 0.21f, 0.24f), padPosition + new Vector2(0, -8), new Vector2(280, 26));

        // Rocket with its flame as a child (the flame hangs from the nozzle)
        rocket = AddImage("Rocket", RocketTexture(), Color.white, Vector2.zero, new Vector2(72, 300));
        rocket.pivot = new Vector2(0.5f, 0f);
        flame = AddImage("Flame", FlameTexture(), Color.white, Vector2.zero, new Vector2(52, 170));
        flame.SetParent(rocket, false);
        flame.anchorMin = flame.anchorMax = new Vector2(0.5f, 0f);
        flame.pivot = new Vector2(0.5f, 1f);
        flame.anchoredPosition = new Vector2(0, 10);

        for (int i = 0; i < SmokeCount; i++)
        {
            puffs[i] = AddImage("Smoke", soft, new Color(0.9f, 0.9f, 0.9f, 0f), Vector2.zero, new Vector2(10, 10));
            puffImages[i] = puffs[i].GetComponent<RawImage>();
            puffLife[i] = 0f;
        }

        ResetRocket();
    }

    void Update()
    {
        if (rocket == null) return;
        float dt = Time.deltaTime;
        stateTime += dt;
        Rect r = area.rect;

        // Clouds drift to the right and wrap around
        for (int i = 0; i < clouds.Length; i++)
        {
            Vector2 p = clouds[i].anchoredPosition;
            p.x += (8f + 4f * i) * dt;
            if (p.x > r.width + 250f) p.x = -250f;
            clouds[i].anchoredPosition = p;
        }

        if (state == 0 && stateTime > idleTime) SetState(1);
        else if (state == 1)
        {
            float k = Mathf.Clamp01(stateTime / ignitionTime);
            flame.localScale = new Vector3(1f, k * Random.Range(0.85f, 1.1f), 1f);
            rocket.anchoredPosition = padPosition + new Vector2(Random.Range(-1.5f, 1.5f) * k, 0f); // engines shake it
            Smoke(dt, 0.03f, true);
            if (stateTime > ignitionTime) SetState(2);
        }
        else if (state == 2)
        {
            speed += acceleration * dt;
            rocket.anchoredPosition += new Vector2(0f, speed * dt);
            flame.localScale = new Vector3(1f, Random.Range(0.95f, 1.25f), 1f);
            if (rocket.anchoredPosition.y < r.height) Smoke(dt, 0.04f, rocket.anchoredPosition.y < groundHeight + 80f);
            if (rocket.anchoredPosition.y > r.height + 400f) SetState(3);
        }
        else if (state == 3 && stateTime > waitTime)
        {
            ResetRocket();
        }

        UpdateSmoke(dt);
    }

    void SetState(int s)
    {
        state = s;
        stateTime = 0f;
    }

    void ResetRocket()
    {
        rocket.anchoredPosition = padPosition;
        flame.localScale = new Vector3(1f, 0f, 1f);
        speed = 0f;
        SetState(0);
    }

    // ---------- Smoke ----------

    // onGround = big clouds that spread to the sides of the pad; otherwise a trail behind the rocket
    void Smoke(float dt, float every, bool onGround)
    {
        smokeTimer += dt;
        while (smokeTimer > every)
        {
            smokeTimer -= every;
            int i = nextPuff;
            nextPuff = (nextPuff + 1) % SmokeCount;
            Vector2 nozzle = rocket.anchoredPosition + new Vector2(Random.Range(-10f, 10f), -40f);
            if (onGround)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                puffVelocity[i] = new Vector2(side * Random.Range(60f, 160f), Random.Range(5f, 35f));
                puffSize[i] = Random.Range(60f, 110f);
                puffLife[i] = Random.Range(2.5f, 4f);
                nozzle.y = groundHeight + 10f;
            }
            else
            {
                puffVelocity[i] = new Vector2(Random.Range(-15f, 15f), Random.Range(-10f, 10f));
                puffSize[i] = Random.Range(30f, 55f);
                puffLife[i] = Random.Range(1.5f, 2.5f);
            }
            puffAge[i] = 0f;
            puffs[i].anchoredPosition = nozzle;
        }
    }

    void UpdateSmoke(float dt)
    {
        for (int i = 0; i < SmokeCount; i++)
        {
            if (puffLife[i] <= 0f) continue;
            puffAge[i] += dt;
            float k = puffAge[i] / puffLife[i];
            if (k >= 1f)
            {
                puffLife[i] = 0f;
                puffImages[i].color = new Color(0.9f, 0.9f, 0.9f, 0f);
                continue;
            }
            puffs[i].anchoredPosition += puffVelocity[i] * dt;
            puffVelocity[i] *= 1f - 0.6f * dt; // air slows it down
            float size = puffSize[i] * (1f + 1.5f * k);
            puffs[i].sizeDelta = new Vector2(size, size);
            puffImages[i].color = new Color(0.92f, 0.92f, 0.92f, 0.75f * (1f - k));
        }
    }

    // ---------- UI helpers ----------

    RectTransform AddImage(string name, Texture2D texture, Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);
        RawImage image = go.GetComponent<RawImage>();
        if (texture != null) image.texture = texture;
        image.color = color;
        image.raycastTarget = false;
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero; // positions measured from the bottom-left corner
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    void AddRaw(string name, Texture2D texture, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);
        RawImage image = go.GetComponent<RawImage>();
        if (texture != null) image.texture = texture;
        else image.color = new Color(0.16f, 0.27f, 0.22f); // plain ground
        image.raycastTarget = false;
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    // ---------- Textures drawn in code ----------

    static Texture2D NewTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    // Deep blue at the top, light blue, warm orange near the horizon
    static Texture2D SkyTexture()
    {
        Texture2D tex = NewTexture(1, 256);
        Color top = new Color(0.1f, 0.22f, 0.5f);
        Color middle = new Color(0.42f, 0.62f, 0.88f);
        Color horizon = new Color(0.98f, 0.76f, 0.56f);
        for (int y = 0; y < 256; y++)
        {
            float v = y / 255f;
            Color c = v < 0.35f ? Color.Lerp(horizon, middle, v / 0.35f) : Color.Lerp(middle, top, (v - 0.35f) / 0.65f);
            tex.SetPixel(0, y, c);
        }
        tex.Apply();
        return tex;
    }

    // Two rows of hills: far ones bluish, near ones dark green
    static Texture2D HillsTexture()
    {
        int w = 512, h = 160;
        Texture2D tex = NewTexture(w, h);
        Color far = new Color(0.38f, 0.48f, 0.62f);
        Color near = new Color(0.16f, 0.27f, 0.22f);
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w;
            float farTop = h * (0.62f + 0.12f * Mathf.Sin(u * 9f) + 0.06f * Mathf.Sin(u * 23f + 1f));
            float nearTop = h * (0.38f + 0.08f * Mathf.Sin(u * 5f + 2f) + 0.04f * Mathf.Sin(u * 17f));
            for (int y = 0; y < h; y++)
            {
                Color c = clear;
                if (y < farTop) c = far;
                if (y < nearTop) c = near;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    // White circle that fades at the edge (clouds, sun, smoke)
    static Texture2D SoftCircle(int size)
    {
        Texture2D tex = NewTexture(size, size);
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
        }
        tex.Apply();
        return tex;
    }

    // Simple rocket: white body, pointed nose, dark band, window, red fins and a nozzle
    static Texture2D RocketTexture()
    {
        int w = 48, h = 200;
        Texture2D tex = NewTexture(w, h);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color body = new Color(0.95f, 0.95f, 0.97f);
        Color shade = new Color(0.78f, 0.8f, 0.85f);
        Color red = new Color(0.85f, 0.2f, 0.18f);
        Color dark = new Color(0.25f, 0.27f, 0.3f);
        Color glass = new Color(0.35f, 0.6f, 0.9f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = clear;
                float cx = Mathf.Abs(x + 0.5f - w / 2f);
                float halfWidth = 10f;
                if (y >= 150) halfWidth = 10f * (1f - (y - 150) / 50f); // nose cone
                if (y >= 12 && cx < halfWidth) c = cx > halfWidth * 0.6f ? shade : body;
                if (y >= 150 && y < 200 && cx < halfWidth) c = y > 185 ? red : c;
                if (y >= 112 && y < 118 && cx < halfWidth) c = dark;
                if (Vector2.Distance(new Vector2(x + 0.5f, y), new Vector2(w / 2f, 132f)) < 4f) c = glass;
                if (y >= 12 && y < 52)
                {
                    float finWidth = 10f + 12f * (1f - (y - 12) / 40f); // fins get wider at the bottom
                    if (cx >= 10f && cx < finWidth) c = red;
                }
                if (y < 12 && cx < 7f - y * 0.15f + 2f) c = dark; // nozzle
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    // Teardrop flame: white-yellow near the nozzle (top), orange, fading at the tip (bottom)
    static Texture2D FlameTexture()
    {
        int w = 32, h = 96;
        Texture2D tex = NewTexture(w, h);
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1); // 1 = top (nozzle)
            float halfWidth = (w / 2f) * Mathf.Sqrt(v);
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Abs(x + 0.5f - w / 2f);
                Color c = new Color(0f, 0f, 0f, 0f);
                if (cx < halfWidth)
                {
                    float edge = 1f - cx / Mathf.Max(halfWidth, 0.01f);
                    Color hot = Color.Lerp(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.95f, 0.7f), edge * v);
                    hot.a = Mathf.Clamp01(edge * 1.5f) * Mathf.Clamp01(v * 1.4f);
                    c = hot;
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
