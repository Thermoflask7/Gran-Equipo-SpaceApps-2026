using UnityEngine;

/// <summary>
/// Proyecta el Canvas de SpaceOS en una RenderTexture y la muestra en la malla curva del monitor.
/// La colocacion por defecto es el ajuste manual de Mateo en 01_Planeacion.
/// Con crtEnabled se usa el shader CRT; si se apaga, un Unlit plano.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class SpaceOsScreen : MonoBehaviour
{
    public static readonly Vector3 DefaultLocalPosition = new Vector3(0f, 0.28103662f, 0.06188202f);
    public static readonly Vector3 DefaultLocalEuler = new Vector3(0f, 180f, 0f);

    [Header("Colocacion (local a computer_old_2)")]
    [SerializeField] Vector3 localPosition = DefaultLocalPosition;
    [SerializeField] Vector3 localEulerAngles = DefaultLocalEuler;
    [Tooltip("Desplazamiento extra sobre la pose base, en espacio local de la pantalla.")]
    [SerializeField] Vector3 extraOffset = Vector3.zero;

    [Header("CRT")]
    [Tooltip("Si esta apagado, la pantalla usa el material Unlit plano (sin scanlines ni vineta).")]
    [SerializeField] bool crtEnabled = true;
    [SerializeField] Material crtMaterialTemplate;
    [SerializeField] Material plainMaterialTemplate;

    [Header("RenderTexture")]
    [SerializeField] int textureWidth = 1084;
    [SerializeField] int textureHeight = 1080;

    [Header("Referencias")]
    [SerializeField] Camera uiCamera;
    [SerializeField] ComputerDesktop desktop;
    [SerializeField] MeshRenderer screenRenderer;

    RenderTexture rt;
    Material runtimeCrt;
    Material runtimePlain;

    public Camera UICamera => uiCamera;
    public RenderTexture Texture => rt;
    public ComputerDesktop Desktop => desktop;
    public bool CrtEnabled => crtEnabled;

    public void SetUICamera(Camera value) => uiCamera = value;
    public void SetDesktop(ComputerDesktop value) => desktop = value;
    public void SetScreenRenderer(MeshRenderer value) => screenRenderer = value;
    public void SetMaterialTemplates(Material crt, Material plain)
    {
        crtMaterialTemplate = crt;
        plainMaterialTemplate = plain;
    }

    public void SetPlacement(Vector3 localPos, Vector3 localEuler, Vector3 offset)
    {
        localPosition = localPos;
        localEulerAngles = localEuler;
        extraOffset = offset;
        ApplyPlacement();
    }

    /// <summary>Aplica localPosition + extraOffset y la rotacion al transform.</summary>
    public void ApplyPlacement()
    {
        transform.localPosition = localPosition + extraOffset;
        transform.localRotation = Quaternion.Euler(localEulerAngles);
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Traduce el UV de la malla al UV del canvas/RT. Si el shader CRT aplica
    /// barrel distortion, se usa la misma formula para que los clics coincidan.
    /// </summary>
    public Vector2 MeshUvToCanvasUv(Vector2 uv)
    {
        if (!crtEnabled) return uv;
        Material mat = runtimeCrt != null ? runtimeCrt : crtMaterialTemplate;
        if (mat == null || !mat.HasProperty("_BarrelDistortion")) return uv;
        float barrel = mat.GetFloat("_BarrelDistortion");
        if (Mathf.Abs(barrel) < 1e-5f) return uv;
        Vector2 c = uv * 2f - Vector2.one;
        float r2 = Vector2.Dot(c, c);
        c *= 1f + barrel * r2;
        return c * 0.5f + Vector2.one * 0.5f;
    }

    public void Refresh()
    {
        ApplyPlacement();
        EnsureTexture();
        ApplyMaterial();
    }

    void OnEnable()
    {
        ApplyPlacement();
        EnsureTexture();
        ApplyMaterial();
    }

    void OnDisable() => ReleaseResources();

    void OnValidate()
    {
        textureWidth = Mathf.Clamp(textureWidth, 64, 4096);
        textureHeight = Mathf.Clamp(textureHeight, 64, 4096);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
        UnityEditor.EditorApplication.delayCall += DelayedRefresh;
#endif
    }

#if UNITY_EDITOR
    void DelayedRefresh()
    {
        UnityEditor.EditorApplication.delayCall -= DelayedRefresh;
        if (this == null || !isActiveAndEnabled) return;
        Refresh();
    }
#endif

    void EnsureTexture()
    {
        int w = Mathf.Clamp(textureWidth, 64, 4096);
        int h = Mathf.Clamp(textureHeight, 64, 4096);
        if (rt != null && (rt.width != w || rt.height != h))
            ReleaseTextureOnly();

        if (rt == null)
        {
            var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.Default, 16)
            {
                msaaSamples = 1,
                autoGenerateMips = false,
                useMipMap = false
            };
            rt = new RenderTexture(desc)
            {
                name = "SpaceOS_RT",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
        }

        if (uiCamera != null) uiCamera.targetTexture = rt;
        if (screenRenderer == null) screenRenderer = GetComponent<MeshRenderer>();
    }

    void ApplyMaterial()
    {
        EnsureRuntimeMaterials();
        if (runtimeCrt != null)
        {
            runtimeCrt.SetTexture("_BaseMap", rt);
            runtimeCrt.SetTexture("_MainTex", rt);
        }
        if (runtimePlain != null)
        {
            runtimePlain.SetTexture("_BaseMap", rt);
            runtimePlain.SetTexture("_MainTex", rt);
        }

        Material active = crtEnabled
            ? (runtimeCrt != null ? runtimeCrt : runtimePlain)
            : (runtimePlain != null ? runtimePlain : runtimeCrt);
        if (screenRenderer != null && active != null)
            screenRenderer.sharedMaterial = active;
    }

    void EnsureRuntimeMaterials()
    {
        if (runtimeCrt == null && crtMaterialTemplate != null)
        {
            runtimeCrt = new Material(crtMaterialTemplate) { name = "SpaceOS_CRT_Runtime", hideFlags = HideFlags.DontSave };
        }
        if (runtimePlain == null)
        {
            if (plainMaterialTemplate != null)
                runtimePlain = new Material(plainMaterialTemplate) { name = "SpaceOS_Plain_Runtime", hideFlags = HideFlags.DontSave };
            else
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit != null)
                    runtimePlain = new Material(unlit) { name = "SpaceOS_Plain_Runtime", hideFlags = HideFlags.DontSave };
            }
        }
    }

    void ReleaseTextureOnly()
    {
        if (uiCamera != null && uiCamera.targetTexture == rt)
            uiCamera.targetTexture = null;
        if (rt != null)
        {
            rt.Release();
            if (Application.isPlaying) Destroy(rt);
            else DestroyImmediate(rt);
            rt = null;
        }
    }

    void ReleaseResources()
    {
        ReleaseTextureOnly();
        DestroyMat(ref runtimeCrt);
        DestroyMat(ref runtimePlain);
    }

    static void DestroyMat(ref Material mat)
    {
        if (mat == null) return;
        if (Application.isPlaying) Destroy(mat);
        else DestroyImmediate(mat);
        mat = null;
    }
}