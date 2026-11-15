using UnityEngine;

/// <summary>
/// Renders the SpaceOS Canvas to a RenderTexture and shows it on the curved monitor mesh.
/// The default placement is Mateo's manual fit in 01_Planeacion.
/// With crtEnabled it uses the CRT shader; when off, a flat Unlit one.
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
    [Tooltip("Extra offset over the base pose, in the screen's local space.")]
    [SerializeField] Vector3 extraOffset = Vector3.zero;

    [Header("CRT")]
    [Tooltip("When off, the screen uses the flat Unlit material (no scanlines or vignette).")]
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
    MaterialPropertyBlock block;
#if UNITY_EDITOR
    bool editorUpdateHooked;
    bool renderingUi;
#endif

    public Camera UICamera => uiCamera;
    public RenderTexture Texture => rt;
    public ComputerDesktop Desktop => desktop;
    public bool CrtEnabled => crtEnabled;

    /// <summary>Turns the CRT filter on or off (swaps the monitor mesh material).</summary>
    public void SetCrtEnabled(bool value)
    {
        crtEnabled = value;
        if (isActiveAndEnabled) ApplyMaterial();
    }

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

    /// <summary>Applies localPosition + extraOffset and the rotation to the transform.</summary>
    public void ApplyPlacement()
    {
        transform.localPosition = localPosition + extraOffset;
        transform.localRotation = Quaternion.Euler(localEulerAngles);
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Maps the mesh UV to the canvas/RT UV. If the CRT shader applies
    /// barrel distortion, the same formula is used so clicks line up.
    /// </summary>
    public Vector2 MeshUvToCanvasUv(Vector2 uv)
    {
        if (!crtEnabled) return uv;
        Material mat = crtMaterialTemplate;
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
        EnsureTexture();
        ApplyMaterial();
        RenderUiIfEditing();
    }

    void OnEnable()
    {
        EnsureTexture();
        ApplyMaterial();
#if UNITY_EDITOR
        HookEditorUpdate(!Application.isPlaying);
#endif
        RenderUiIfEditing();
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        HookEditorUpdate(false);
#endif
        ReleaseResources();
    }

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

    void HookEditorUpdate(bool on)
    {
        if (on == editorUpdateHooked) return;
        if (on) UnityEditor.EditorApplication.update += EditorRenderUi;
        else UnityEditor.EditorApplication.update -= EditorRenderUi;
        editorUpdateHooked = on;
    }

    void EditorRenderUi()
    {
        if (renderingUi || this == null || Application.isPlaying) return;
        RenderUiIfEditing();
    }
#endif

    void RenderUiIfEditing()
    {
        if (Application.isPlaying || uiCamera == null || rt == null) return;
        if (!uiCamera.isActiveAndEnabled) return;
#if UNITY_EDITOR
        if (renderingUi) return;
        renderingUi = true;
        try
        {
            uiCamera.targetTexture = rt;
            uiCamera.Render();
        }
        finally
        {
            renderingUi = false;
        }
#else
        uiCamera.targetTexture = rt;
#endif
    }

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
        if (screenRenderer == null) screenRenderer = GetComponent<MeshRenderer>();
        if (screenRenderer == null) return;

        // The asset stays assigned in the scene (not a DontSave Material that becomes None when saved).
        Material template = crtEnabled ? crtMaterialTemplate : plainMaterialTemplate;
        if (template == null) template = crtMaterialTemplate != null ? crtMaterialTemplate : plainMaterialTemplate;
        if (template != null) screenRenderer.sharedMaterial = template;

        if (block == null) block = new MaterialPropertyBlock();
        screenRenderer.GetPropertyBlock(block);
        if (rt != null)
        {
            block.SetTexture("_BaseMap", rt);
            block.SetTexture("_MainTex", rt);
        }
        screenRenderer.SetPropertyBlock(block);
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
        if (screenRenderer != null) screenRenderer.SetPropertyBlock(null);
        ReleaseTextureOnly();
    }
}