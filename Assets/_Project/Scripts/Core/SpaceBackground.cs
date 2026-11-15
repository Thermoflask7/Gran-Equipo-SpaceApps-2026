using UnityEngine;
using UnityEngine.UI;

// Full-screen space background for UI scenes (Results): dark gradient, two soft nebulas and stars.
// The texture is generated in code (no image file needed) and drifts slowly to the side.
// Put it on a RawImage that fills the canvas, as the first child so it is drawn behind everything.
[RequireComponent(typeof(RawImage))]
public class SpaceBackground : MonoBehaviour
{
    public int starCount = 1400;
    public float driftSpeed = 0.004f; // texture widths per second
    public int seed = 2026;

    RawImage image;

    void Awake()
    {
        image = GetComponent<RawImage>();
        image.texture = MakeTexture(1024, 576);
        image.color = Color.white;
    }

    void Update()
    {
        Rect r = image.uvRect;
        r.x = Mathf.Repeat(r.x + driftSpeed * Time.deltaTime, 1f);
        image.uvRect = r;
    }

    Texture2D MakeTexture(int width, int height)
    {
        System.Random rng = new System.Random(seed);
        Color[] pixels = new Color[width * height];
        Color bottom = new Color(0.01f, 0.01f, 0.04f);
        Color top = new Color(0.05f, 0.06f, 0.16f);

        // Two nebulas: center x, center y, radius, color
        Vector3[] nebulas = { new Vector3(0.25f, 0.65f, 0.22f), new Vector3(0.75f, 0.3f, 0.18f) };
        Color[] nebulaColors = { new Color(0.35f, 0.12f, 0.45f), new Color(0.08f, 0.25f, 0.45f) };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;
                Color c = Color.Lerp(bottom, top, v);
                for (int n = 0; n < nebulas.Length; n++)
                {
                    float dx = Mathf.Abs(u - nebulas[n].x);
                    dx = Mathf.Min(dx, 1f - dx); // wraps around, so the drift has no seam
                    float dy = (v - nebulas[n].y) * height / width;
                    float d2 = (dx * dx + dy * dy) / (nebulas[n].z * nebulas[n].z);
                    c += nebulaColors[n] * Mathf.Exp(-d2 * 2f) * 0.5f;
                }
                pixels[y * width + x] = c;
            }
        }

        for (int i = 0; i < starCount; i++)
        {
            int x = rng.Next(width);
            int y = rng.Next(height);
            float b = (float)rng.NextDouble();
            b = 0.3f + 0.7f * b * b; // most stars are faint
            Color star = new Color(b, b, Mathf.Min(1f, b * 1.1f));
            pixels[y * width + x] = star;
            if (b > 0.85f)
            {
                // A few big stars: a small cross
                Color glow = star * 0.45f;
                pixels[y * width + (x + 1) % width] = Brighter(pixels[y * width + (x + 1) % width], glow);
                pixels[y * width + (x + width - 1) % width] = Brighter(pixels[y * width + (x + width - 1) % width], glow);
                if (y > 0) pixels[(y - 1) * width + x] = Brighter(pixels[(y - 1) * width + x], glow);
                if (y < height - 1) pixels[(y + 1) * width + x] = Brighter(pixels[(y + 1) * width + x], glow);
            }
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Color Brighter(Color a, Color b)
    {
        return new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), 1f);
    }
}
