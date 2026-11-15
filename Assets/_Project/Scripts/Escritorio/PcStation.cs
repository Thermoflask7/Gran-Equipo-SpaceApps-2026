using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Desk station "PC". Goes on the root of computer_old_2 (it needs a collider
/// on it or its children). Click -> the camera moves in until the monitor screen
/// fills the whole height -> the ComputerDesktop is shown. Back / Escape -> returns.
/// </summary>
public class PcStation : MonoBehaviour
{
    [Tooltip("Camera pose when zoomed in. The builder creates it (PC_ZoomTarget) and it can be moved by hand.")]
    [SerializeField] Transform zoomTarget;

    [Tooltip("Monitor renderer and screen submesh (material 'screen').")]
    [SerializeField] Renderer screenRenderer;
    [SerializeField] int screenSubmesh = 1;

    [Tooltip("1 = the screen fills exactly the height. Lower = closer (crops), higher = leaves a margin.")]
    [SerializeField, Range(0.5f, 1.5f)] float heightFill = 1f;

    [SerializeField] ComputerDesktop desktop;
    [SerializeField] float maxClickDistance = 50f;

    [Header("Events")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    /// <summary>Gap (m) between the screen front and the Canvas, to avoid z-fighting.</summary>
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

    /// <summary>Zooms to the PC and shows the app desktop.</summary>
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

    /// <summary>Hides the desktop and returns the camera (used by Back and the Shut down app).</summary>
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

    /// <summary>Places zoomTarget so the screen fills the height (used by the editor builder).</summary>
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
    /// Computes the camera pose in front of the screen center, at the distance where
    /// its height fills the vertical FOV. screenHeight is in meters (for debugging).
    /// </summary>
    public bool TryComputeZoomPose(Camera cam, out Vector3 position, out Quaternion rotation, out float screenHeight)
    {
        position = default; rotation = Quaternion.identity; screenHeight = 0f;
        if (cam == null) return false;

        Vector3 center, normal, up;
        float depth = 0f;
        if (!TryGetScreenFrame(out center, out normal, out up, out screenHeight, out _, out depth))
        {
            // Fallback: bounds of all the PC renderers, looking from the camera.
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

        // Make the normal point toward the camera.
        if (Vector3.Dot(normal, cam.transform.position - center) < 0f) normal = -normal;
        // Frames the screen front (where the SpaceOS Canvas goes), not the mesh center.
        center += normal * (depth * 0.5f + ScreenCanvasOffset);

        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = (screenHeight * 0.5f * heightFill) / Mathf.Tan(halfFov);
        position = center + normal * distance;
        rotation = Quaternion.LookRotation(-normal, up);
        return true;
    }

    /// <summary>
    /// Monitor screen surface in world space: front center (already moved forward half
    /// the submesh thickness), outward normal (camera side), up, and size in meters.
    /// The builder uses it to mount the SpaceOS World Space Canvas on the screen.
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

        Bounds lb = mf.sharedMesh.GetSubMesh(screenSubmesh).bounds; // mesh local space
        Transform t = screenRenderer.transform;
        Vector3[] axes = { t.right, t.up, t.forward };
        Vector3 s = t.lossyScale;
        float[] size = { Mathf.Abs(lb.size.x * s.x), Mathf.Abs(lb.size.y * s.y), Mathf.Abs(lb.size.z * s.z) };

        // Thinnest axis = screen normal; of the other two, the most vertical = height.
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