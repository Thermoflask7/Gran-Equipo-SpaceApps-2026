using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Estacion "PC" del escritorio. Va en la raiz de computer_old_2 (necesita un collider
/// en ella o en sus hijos). Clic -> la camara se acerca hasta que la pantalla del monitor
/// ocupa todo el alto -> se muestra el ComputerDesktop. Volver / Escape -> regresa.
/// </summary>
public class PcStation : MonoBehaviour
{
    [Tooltip("Pose de la camara al hacer zoom. El builder la crea (PC_ZoomTarget) y se puede mover a mano.")]
    [SerializeField] Transform zoomTarget;

    [Tooltip("Renderer del monitor y submesh de la pantalla (material 'screen').")]
    [SerializeField] Renderer screenRenderer;
    [SerializeField] int screenSubmesh = 1;

    [Tooltip("1 = la pantalla ocupa exactamente el alto. Menor = mas cerca (recorta), mayor = deja margen.")]
    [SerializeField, Range(0.5f, 1.5f)] float heightFill = 1f;

    [SerializeField] ComputerDesktop desktop;
    [SerializeField] float maxClickDistance = 50f;

    [Header("Eventos")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    /// <summary>Separacion (m) entre el frente de la pantalla y el Canvas, para evitar z-fighting.</summary>
    public const float ScreenCanvasOffset = 0.003f;

    public bool IsOpen { get; private set; }
    public Transform ZoomTarget => zoomTarget;

    DeskCamera subscribed;

    void OnEnable() => Subscribe();

    void OnDisable()
    {
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        subscribed = null;
    }

    void Subscribe()
    {
        var dc = DeskCamera.Instance;
        if (dc == null || dc == subscribed) return;
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        dc.ReturnStarted += HandleReturnStarted;
        subscribed = dc;
    }

    void Update()
    {
        if (IsOpen) return;
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        var dc = DeskCamera.Instance;
        if (dc == null || dc.IsFocused || dc.IsMoving) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = dc.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance) && hit.collider.transform.IsChildOf(transform))
            Open();
    }

    /// <summary>Hace zoom a la PC y muestra el escritorio de apps.</summary>
    public void Open()
    {
        var dc = DeskCamera.Instance;
        if (dc == null || IsOpen) return;
        Subscribe();
        EnsureZoomTarget(dc.GetComponent<Camera>());
        IsOpen = true;
        dc.FocusOn(zoomTarget, () =>
        {
            if (!IsOpen) return;
            if (desktop != null) desktop.Show(this);
            onOpened?.Invoke();
        });
    }

    /// <summary>Oculta el escritorio y regresa la camara (lo usan Volver y la app Apagar).</summary>
    public void Close()
    {
        if (!IsOpen) return;
        var dc = DeskCamera.Instance;
        if (dc != null && dc.IsFocused) dc.ReturnToDesk();
        else HandleReturnStarted();
    }

    void HandleReturnStarted()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (desktop != null) desktop.Hide();
        onClosed?.Invoke();
    }

    void EnsureZoomTarget(Camera cam)
    {
        if (zoomTarget != null) return;
        var go = new GameObject("PC_ZoomTarget");
        go.transform.SetParent(transform, false);
        zoomTarget = go.transform;
        PlaceZoomTarget(cam);
    }

    /// <summary>Coloca zoomTarget para que la pantalla llene el alto (lo usa el builder del editor).</summary>
    public void PlaceZoomTarget(Camera cam)
    {
        if (zoomTarget == null || cam == null) return;
        if (TryComputeZoomPose(cam, out Vector3 pos, out Quaternion rot, out _))
            zoomTarget.SetPositionAndRotation(pos, rot);
    }

    public void SetZoomTarget(Transform target) => zoomTarget = target;
    public void SetScreen(Renderer renderer, int submesh) { screenRenderer = renderer; screenSubmesh = submesh; }
    public void SetDesktop(ComputerDesktop value) => desktop = value;

    /// <summary>
    /// Calcula la pose de camara frente al centro de la pantalla, a la distancia en la que
    /// su alto llena el FOV vertical. screenHeight sale en metros (para depurar).
    /// </summary>
    public bool TryComputeZoomPose(Camera cam, out Vector3 position, out Quaternion rotation, out float screenHeight)
    {
        position = default; rotation = Quaternion.identity; screenHeight = 0f;
        if (cam == null) return false;

        Vector3 center, normal, up;
        float depth = 0f;
        if (!TryGetScreenFrame(out center, out normal, out up, out screenHeight, out _, out depth))
        {
            // Respaldo: bounds de todos los renderers de la PC, mirando desde la camara.
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return false;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            center = b.center;
            screenHeight = b.size.y;
            up = Vector3.up;
            normal = Vector3.ProjectOnPlane(cam.transform.position - center, Vector3.up);
            if (normal.sqrMagnitude < 1e-6f) normal = -transform.forward;
            normal.Normalize();
        }

        // Que la normal apunte hacia donde esta la camara.
        if (Vector3.Dot(normal, cam.transform.position - center) < 0f) normal = -normal;
        // Encuadra el frente de la pantalla (donde va el Canvas de SpaceOS), no el centro del mesh.
        center += normal * (depth * 0.5f + ScreenCanvasOffset);

        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = (screenHeight * 0.5f * heightFill) / Mathf.Tan(halfFov);
        position = center + normal * distance;
        rotation = Quaternion.LookRotation(-normal, up);
        return true;
    }

    /// <summary>
    /// Superficie de la pantalla del monitor en mundo: centro del frente (ya adelantado la mitad
    /// del grosor del submesh), normal hacia afuera (del lado de la camara), arriba, y tamano en metros.
    /// La usa el builder para montar el Canvas World Space de SpaceOS sobre la pantalla.
    /// </summary>
    public bool TryGetScreenSurface(Camera cam, out Vector3 frontCenter, out Vector3 outward, out Vector3 up, out Vector2 size)
    {
        frontCenter = outward = up = default; size = default;
        if (!TryGetScreenFrame(out Vector3 center, out outward, out up, out float height, out float width, out float depth)) return false;
        if (cam != null && Vector3.Dot(outward, cam.transform.position - center) < 0f) outward = -outward;
        else if (cam == null && Vector3.Dot(outward, -transform.forward) < 0f) outward = -outward;
        frontCenter = center + outward * (depth * 0.5f);
        size = new Vector2(width, height);
        return true;
    }

    bool TryGetScreenFrame(out Vector3 center, out Vector3 normal, out Vector3 up, out float height, out float width, out float depth)
    {
        center = normal = up = default; height = width = depth = 0f;
        if (screenRenderer == null) return false;
        var mf = screenRenderer.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null || screenSubmesh < 0 || screenSubmesh >= mf.sharedMesh.subMeshCount) return false;

        Bounds lb = mf.sharedMesh.GetSubMesh(screenSubmesh).bounds; // espacio local del mesh
        Transform t = screenRenderer.transform;
        Vector3[] axes = { t.right, t.up, t.forward };
        Vector3 s = t.lossyScale;
        float[] size = { Mathf.Abs(lb.size.x * s.x), Mathf.Abs(lb.size.y * s.y), Mathf.Abs(lb.size.z * s.z) };

        // Eje mas delgado = normal de la pantalla; de los otros dos, el mas vertical = alto.
        int thin = 0;
        for (int i = 1; i < 3; i++) if (size[i] < size[thin]) thin = i;
        int a = (thin + 1) % 3, b2 = (thin + 2) % 3;
        int hAxis = Mathf.Abs(Vector3.Dot(axes[a], Vector3.up)) >= Mathf.Abs(Vector3.Dot(axes[b2], Vector3.up)) ? a : b2;

        center = t.TransformPoint(lb.center);
        normal = axes[thin].normalized;
        up = axes[hAxis].normalized;
        if (Vector3.Dot(up, Vector3.up) < 0f) up = -up;
        height = size[hAxis];
        width = size[hAxis == a ? b2 : a];
        depth = size[thin];
        return height > 0.001f;
    }
}