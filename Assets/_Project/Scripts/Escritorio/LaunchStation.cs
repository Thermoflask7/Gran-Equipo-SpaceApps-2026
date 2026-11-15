using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Desk station "launch button" (desk part of task 24). Same pattern as FolderStation and RocketModelStation.
// Goes on the 3D launch button of the desk (it needs a collider on it or a child).
// Click -> the camera moves in front of the button (DeskCamera.FocusOn) -> the launch panel (prefab
// BotonLanzamiento: LaunchChecklist + LaunchButton) is shown. The LaunchButton itself stays locked until
// GameManager.CanLaunch() and moves to 02_Lanzamiento when pressed.
// Escape (any return to the desk) or Close() -> the panel hides and the camera returns to the desk.
public class LaunchStation : MonoBehaviour
{
    [Tooltip("Launch panel (root of the BotonLanzamiento prefab). Empty = the panel of the LaunchButton in the scene (it can be inactive).")]
    public GameObject panel;

    [Tooltip("Camera pose when focused. Empty = created in front of the button, tilted down at it.")]
    public Transform zoomTarget;
    [Tooltip("Distance of the automatic zoom pose from the button center (m)")]
    public float zoomDistance = 0.5f;
    [Tooltip("How much the automatic pose looks down (degrees, 20 to 80)")]
    public float zoomPitch = 45f;
    public float maxClickDistance = 50f;

    public UnityEvent onOpened;
    public UnityEvent onClosed;

    public bool IsOpen { get; private set; }

    DeskCamera subscribed;

    void Awake()
    {
        if (panel == null)
        {
            LaunchButton button = FindAnyObjectByType<LaunchButton>(FindObjectsInactive.Include);
            if (button != null) panel = button.transform.parent != null ? button.transform.parent.gameObject : button.gameObject;
        }
        if (panel == null)
        {
            Debug.LogWarning("LaunchStation: there is no LaunchButton panel in the scene. Run SpaceApps > Launch button > Add to open scene.");
            return;
        }
        panel.SetActive(false);
    }

    void OnEnable() { Subscribe(); }

    void OnDisable()
    {
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        subscribed = null;
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

    // Moves the camera to the button and shows the launch panel
    public void Open()
    {
        if (IsOpen || panel == null) return;
        IsOpen = true;
        DeskCamera dc = DeskCamera.Instance;
        if (dc == null)
        {
            ShowPanel(); // scene without the desk camera: just show the UI
            return;
        }
        Subscribe();
        EnsureZoomTarget(dc.transform.position);
        dc.FocusOn(zoomTarget, () =>
        {
            if (IsOpen) ShowPanel();
        });
    }

    // Hides the panel and returns the camera to the desk (for a Back/X button)
    public void Close()
    {
        if (!IsOpen) return;
        Finish(); // before ReturnToDesk, so HandleReturnStarted does nothing
        DeskCamera dc = DeskCamera.Instance;
        if (dc != null && (dc.IsFocused || dc.IsMoving)) dc.ReturnToDesk();
    }

    // Escape or any other return to the desk
    void HandleReturnStarted()
    {
        if (IsOpen) Finish();
    }

    void ShowPanel()
    {
        panel.SetActive(true);
        onOpened.Invoke();
    }

    void Finish()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
        onClosed.Invoke();
    }

    // Camera pose in front of and above the button (on the desk camera's side), tilted down by zoomPitch,
    // with world up as up so the view never rolls.
    void EnsureZoomTarget(Vector3 cameraPosition)
    {
        if (zoomTarget != null) return;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Vector3 center = transform.position;
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            center = b.center;
        }
        Vector3 toCamera = Vector3.ProjectOnPlane(cameraPosition - center, Vector3.up);
        toCamera = toCamera.sqrMagnitude > 1e-6f ? toCamera.normalized : -transform.forward;

        float pitch = Mathf.Clamp(zoomPitch, 20f, 80f) * Mathf.Deg2Rad;
        Vector3 offset = toCamera * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
        GameObject go = new GameObject("Launch_ZoomTarget");
        go.transform.SetParent(transform, true);
        go.transform.position = center + offset * zoomDistance;
        go.transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        zoomTarget = go.transform;
    }
}
