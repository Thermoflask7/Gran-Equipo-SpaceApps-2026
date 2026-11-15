using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Desk station "rocket model" (task 9b; desk part of task 24). Same pattern as FolderStation.
// Goes on the decorative rocket on the desk ("Stylized Rocket"; it needs a collider on it or a child).
// Click -> the camera moves in front of it (DeskCamera.FocusOn) -> RocketModel.Open() shows the full-screen
// rocket model: parts on the left half, the live low poly rocket (RocketView) on the right half.
// The panel's X button or Escape -> the panel closes and the camera returns to the desk.
// Install it with SpaceApps > Desk > Add rocket model station.
public class RocketModelStation : MonoBehaviour
{
    [Tooltip("Rocket model panel. Empty = the RocketModel in the scene (it can be inactive).")]
    public RocketModel panel;

    [Tooltip("Camera pose when focused. Empty = created in front of the model each time it opens.")]
    public Transform zoomTarget;
    [Tooltip("Camera distance as a multiple of the model height")]
    public float distanceFactor = 1.5f;
    [Tooltip("How much the automatic pose looks down (degrees)")]
    public float zoomPitch = 12f;
    public float maxClickDistance = 50f;

    public UnityEvent onOpened;
    public UnityEvent onClosed;

    public bool IsOpen { get; private set; }

    DeskCamera subscribed;
    Transform autoTarget;

    void Awake()
    {
        if (panel == null) panel = FindAnyObjectByType<RocketModel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("RocketModelStation: there is no RocketModel panel in the scene. Run SpaceApps > Desk > Add rocket model station.");
            return;
        }
        panel.gameObject.SetActive(false);
        panel.onClosed.AddListener(HandlePanelClosed);
    }

    void OnEnable() { Subscribe(); }

    void OnDisable()
    {
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        subscribed = null;
    }

    void OnDestroy()
    {
        if (panel != null) panel.onClosed.RemoveListener(HandlePanelClosed);
    }

    void Subscribe()
    {
        DeskCamera dc = DeskCamera.Instance;
        if (dc == null || dc == subscribed) return;
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        dc.ReturnStarted += HandleReturnStarted;
        subscribed = dc;
    }

    void Update()
    {
        if (IsOpen || panel == null) return;
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        DeskCamera dc = DeskCamera.Instance;
        if (dc == null || dc.IsFocused || dc.IsMoving) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Camera cam = dc.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxClickDistance) && hit.collider.transform.IsChildOf(transform))
            Open();
    }

    public void Open()
    {
        if (IsOpen || panel == null) return;
        IsOpen = true;
        DeskCamera dc = DeskCamera.Instance;
        if (dc == null)
        {
            panel.Open(); // scene without the desk camera: just show the panel
            onOpened.Invoke();
            return;
        }
        Subscribe();
        Transform target = zoomTarget != null ? zoomTarget : AutoTarget(dc.transform.position);
        dc.FocusOn(target, () =>
        {
            if (!IsOpen) return;
            panel.Open();
            onOpened.Invoke();
        });
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (panel != null && panel.gameObject.activeSelf) panel.Close(); // -> HandlePanelClosed
        else HandlePanelClosed();
    }

    void HandlePanelClosed()
    {
        if (!IsOpen) return;
        Finish();
        DeskCamera dc = DeskCamera.Instance;
        if (dc != null && (dc.IsFocused || dc.IsMoving)) dc.ReturnToDesk();
    }

    void HandleReturnStarted()
    {
        if (!IsOpen) return;
        Finish();
        if (panel != null && panel.gameObject.activeSelf) panel.Close();
    }

    void Finish()
    {
        IsOpen = false;
        onClosed.Invoke();
    }

    // Pose in front of the model (on the desk camera's side), far enough to see it whole.
    // Recalculated on every open (the desk rocket may have been moved).
    Transform AutoTarget(Vector3 cameraPosition)
    {
        Bounds b = new Bounds(transform.position, Vector3.one * 0.1f);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        }

        Vector3 toCamera = Vector3.ProjectOnPlane(cameraPosition - b.center, Vector3.up);
        toCamera = toCamera.sqrMagnitude > 1e-6f ? toCamera.normalized : -transform.forward;
        float pitch = Mathf.Clamp(zoomPitch, 0f, 60f) * Mathf.Deg2Rad;
        Vector3 offset = toCamera * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
        float distance = Mathf.Max(b.size.y * distanceFactor, 0.3f);

        if (autoTarget == null)
        {
            autoTarget = new GameObject("RocketModel_ZoomTarget").transform;
            autoTarget.SetParent(transform.parent, true);
        }
        autoTarget.position = b.center + offset * distance;
        autoTarget.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        return autoTarget;
    }
}
