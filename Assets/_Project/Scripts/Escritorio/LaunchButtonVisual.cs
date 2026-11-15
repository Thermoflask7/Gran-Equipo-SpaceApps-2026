using UnityEngine;

// Look of the 3D launch button on the desk (task 24, launch button part).
// Goes on the same object as LaunchStation (the 3D button).
//  - Locked (GameManager.CanLaunch() is false): gray, no glow.
//  - Ready (all 4 tasks): red with a slow glowing pulse.
//  - Early launch possible (GameManager.CanLaunchEarly()): orange pulse, a warning that it is risky.
//  - Pressed (LaunchButton.Pressed): the button sinks, flashes and comes back up while
//    LaunchButton waits before changing scene.
public class LaunchButtonVisual : MonoBehaviour
{
    [Tooltip("Renderers that change color. Empty = the child called 'Cap' if there is one, otherwise every renderer of the button.")]
    public Renderer[] tinted;
    [Tooltip("Part that sinks when pressed. Empty = the child called 'Cap' if there is one, otherwise the whole button.")]
    public Transform pressPart;

    public Color lockedColor = new Color(0.35f, 0.35f, 0.37f);
    public Color readyColor = new Color(0.85f, 0.08f, 0.06f);
    public Color earlyColor = new Color(1f, 0.5f, 0.05f);
    [Tooltip("Pulses per second when ready")]
    public float pulseSpeed = 1.2f;
    [Tooltip("Maximum glow intensity of the pulse")]
    public float pulseGlow = 1.5f;

    [Tooltip("How far the button sinks when pressed (m)")]
    public float pressDepth = 0.012f;
    [Tooltip("Duration of the press animation (s)")]
    public float pressDuration = 0.5f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    Material[] materials;
    Vector3 restLocalPosition;
    float pressTime = -1f;

    void Awake()
    {
        Transform cap = FindChild(transform, "Cap");
        if (tinted == null || tinted.Length == 0)
            tinted = cap != null && cap.GetComponent<Renderer>() != null
                ? new[] { cap.GetComponent<Renderer>() }
                : GetComponentsInChildren<Renderer>();
        if (pressPart == null) pressPart = cap != null ? cap : transform;
        restLocalPosition = pressPart.localPosition;

        // Own material instances, so the shared material assets are never changed
        var list = new System.Collections.Generic.List<Material>();
        foreach (Renderer r in tinted)
        {
            if (r == null) continue;
            foreach (Material m in r.materials)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                list.Add(m);
            }
        }
        materials = list.ToArray();
    }

    void OnEnable() { LaunchButton.Pressed += HandlePressed; }
    void OnDisable() { LaunchButton.Pressed -= HandlePressed; }

    void OnDestroy()
    {
        if (materials == null) return;
        foreach (Material m in materials) if (m != null) Destroy(m);
    }

    void HandlePressed() { pressTime = 0f; }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        bool ready = gm != null && gm.CanLaunch();
        bool early = !ready && gm != null && gm.CanLaunchEarly();
        Color active = ready ? readyColor : earlyColor;

        Color color;
        Color glow;
        if (pressTime >= 0f)
        {
            // Pressed: sink fast, bright flash, come back up
            pressTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(pressTime / Mathf.Max(0.05f, pressDuration));
            float down = t < 0.3f ? t / 0.3f : 1f - (t - 0.3f) / 0.7f;
            Vector3 sink = Vector3.up * pressDepth * down; // world space, the button sits on the desk
            pressPart.localPosition = restLocalPosition - (pressPart.parent != null ? pressPart.parent.InverseTransformVector(sink) : sink);
            color = active;
            glow = active * Mathf.Lerp(pulseGlow * 2f, 0.5f, t);
            if (t >= 1f)
            {
                pressTime = -1f;
                pressPart.localPosition = restLocalPosition;
            }
        }
        else if (ready || early)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
            color = Color.Lerp(active * 0.6f, active, pulse);
            color.a = 1f;
            glow = active * (pulse * pulseGlow);
        }
        else
        {
            color = lockedColor;
            glow = Color.black;
        }

        foreach (Material m in materials)
        {
            if (m == null) continue;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
            else if (m.HasProperty(ColorId)) m.SetColor(ColorId, color);
            if (m.HasProperty(EmissionId)) m.SetColor(EmissionId, glow);
        }
    }

    static Transform FindChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && t.name == name) return t;
        return null;
    }
}
