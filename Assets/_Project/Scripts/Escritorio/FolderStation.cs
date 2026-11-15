using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Desk station "mission folder" (task 24, folder part). Signed: claude_c.
// Goes on the 3D folder of the desk (the `notepad` object of 01_Planeacion; it needs a collider on it or a child).
// Click -> the camera moves above the folder (DeskCamera.FocusOn) -> MissionFolder.Open() shows the full-screen
// folder UI (which sets gm.missionRead and, the first time, rolls a planning event).
// The folder's X button or Escape -> the folder closes and the camera returns to the desk.
// Install it with SpaceApps > Desk > Add folder station (adds the MissionFolder UI too). Same pattern as PcStation.
public class FolderStation : MonoBehaviour
{
    [Tooltip("Full-screen folder UI. Empty = the MissionFolder in the scene (it can be inactive).")]
    public MissionFolder folder;

    [Tooltip("Camera pose when focused. Empty = created in front of the folder, tilted down at it.")]
    public Transform zoomTarget;
    [Tooltip("Distance of the automatic zoom pose from the folder center (m)")]
    public float zoomDistance = 0.6f;
    [Tooltip("How much the automatic pose looks down (degrees, 20 to 80). 90 would be straight down.")]
    public float zoomPitch = 55f;
    public float maxClickDistance = 50f;

    public UnityEvent onOpened;
    public UnityEvent onClosed;

    public bool IsOpen { get; private set; }

    DeskCamera subscribed;

    void Awake()
    {
        if (folder == null) folder = FindAnyObjectByType<MissionFolder>(FindObjectsInactive.Include);
        if (folder == null)
        {
            Debug.LogWarning("FolderStation: there is no MissionFolder in the scene. Run SpaceApps > Desk > Add folder station.");
            return;
        }
        if (folder.gameObject.activeSelf)
            Debug.LogWarning("FolderStation: the MissionFolder starts active; leave it inactive in the scene so it only opens from the desk.");
        folder.gameObject.SetActive(false);
        folder.onClosed.AddListener(HandleFolderClosed);
    }

    void OnEnable() { Subscribe(); }

    void OnDisable()
    {
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        subscribed = null;
    }

    void OnDestroy()
    {
        if (folder != null) folder.onClosed.RemoveListener(HandleFolderClosed);
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
        if (IsOpen || folder == null) return;
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

    // Moves the camera to the folder and opens the folder UI
    public void Open()
    {
        if (IsOpen || folder == null) return;
        IsOpen = true;
        DeskCamera dc = DeskCamera.Instance;
        if (dc == null)
        {
            folder.Open(); // scene without the desk camera: just show the UI
            onOpened.Invoke();
            return;
        }
        Subscribe();
        EnsureZoomTarget(dc.transform.position);
        dc.FocusOn(zoomTarget, () =>
        {
            if (!IsOpen) return;
            folder.Open();
            onOpened.Invoke();
        });
    }

    // Closes the folder UI and returns the camera to the desk
    public void Close()
    {
        if (!IsOpen) return;
        if (folder != null && folder.gameObject.activeSelf) folder.Close(); // -> HandleFolderClosed
        else HandleFolderClosed();
    }

    // The folder's X button (MissionFolder.onClosed)
    void HandleFolderClosed()
    {
        if (!IsOpen) return;
        Finish(); // before ReturnToDesk, so HandleReturnStarted does not close the folder a second time
        DeskCamera dc = DeskCamera.Instance;
        if (dc != null && (dc.IsFocused || dc.IsMoving)) dc.ReturnToDesk();
    }

    // Escape or any other return to the desk
    void HandleReturnStarted()
    {
        if (!IsOpen) return;
        Finish();
        if (folder != null && folder.gameObject.activeSelf) folder.Close(); // its onClosed finds IsOpen == false
    }

    void Finish()
    {
        IsOpen = false;
        onClosed.Invoke();
    }

    // Camera pose in front of and above the folder (on the desk camera's side), tilted down by zoomPitch,
    // with world up as up: the view only moves closer and tilts, it never rolls or flips.
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
        Vector3 offset = toCamera * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch); // unit vector from folder to camera
        GameObject go = new GameObject("Folder_ZoomTarget");
        go.transform.SetParent(transform, true);
        go.transform.position = center + offset * zoomDistance;
        go.transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        zoomTarget = go.transform;
    }
}
