using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Desk station "launch button" (desk part of task 24). Same pattern as FolderStation and RocketModelStation.
// Goes on the 3D launch button of the desk (it needs a collider on it or a child).
// Click -> the camera moves in front of the button (DeskCamera.FocusOn) -> the launch panel (prefab
// BotonLanzamiento: LaunchChecklist + LaunchButton) is shown. The LaunchButton itself stays locked until
// GameManager.CanLaunch() and moves to 02_Lanzamiento when pressed.
// Escape (any return to the desk) or Close() -> the panel hides and the camera returns to the desk.
public class LaunchStation : MonoBehaviour
{
    // Own overlay canvas for the launch panel, so it never depends on a canvas that other stations hide
    public const string LaunchCanvasName = "LaunchCanvas";

    [Tooltip("Launch panel (root of the BotonLanzamiento prefab). Empty = the panel of the LaunchButton in the scene (it can be inactive).")]
    public GameObject panel;

    [Tooltip("Camera pose when focused. Empty = created straight above the button, looking down at it.")]
    public Transform zoomTarget;
    [Tooltip("Height of the automatic zoom pose above the button center (m)")]
    public float zoomDistance = 0.5f;

    [Tooltip("Move the launch panel to the left side of the screen when it opens, so the button in the center stays visible")]
    public bool panelAtSide = true;
    [Tooltip("Gap between the panel and the left edge of the screen (canvas pixels)")]
    public float panelMargin = 30f;
    public float maxClickDistance = 50f;

    public UnityEvent onOpened;
    public UnityEvent onClosed;

    public bool IsOpen { get; private set; }

    DeskCamera subscribed;

    void Awake()
    {
        // A model whose only collider is a BoxCollider on its root (added by hand or by the menu) gets that
        // box fitted to its meshes; with the default 1x1x1 size on a scaled model it can be too small to click.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && GetComponentsInChildren<Collider>().Length == 1) FitBoxCollider(box);

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
        EnsureOwnCanvas(panel);
        panel.SetActive(false);
    }

    void OnEnable()
    {
        Subscribe();
        LaunchButton.Pressed += HandleLaunchPressed;
    }

    void OnDisable()
    {
        LaunchButton.Pressed -= HandleLaunchPressed;
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

    // Launch confirmed: hide the panel so the 3D button press animation is visible (the camera stays)
    void HandleLaunchPressed()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Escape or any other return to the desk
    void HandleReturnStarted()
    {
        if (IsOpen) Finish();
    }

    void ShowPanel()
    {
        if (panelAtSide && panel.transform is RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(panelMargin, 0f);
        }
        panel.SetActive(true);
        onOpened.Invoke();
    }

    void Finish()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
        onClosed.Invoke();
    }

    // Moves the panel under the LaunchCanvas (created if missing) unless it is already there
    public static void EnsureOwnCanvas(GameObject panel)
    {
        Transform parent = panel.transform.parent;
        if (parent != null && parent.name == LaunchCanvasName && parent.GetComponent<Canvas>() != null) return;
        panel.transform.SetParent(GetOrCreateLaunchCanvas().transform, false);
    }

    public static Canvas GetOrCreateLaunchCanvas()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (c.transform.parent == null && c.name == LaunchCanvasName)
            {
                c.gameObject.SetActive(true);
                return c;
            }
        GameObject go = new GameObject(LaunchCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // Resizes a BoxCollider so it covers every renderer of its object and children
    public static void FitBoxCollider(BoxCollider box)
    {
        Renderer[] renderers = box.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Transform t = box.transform;
        Bounds local = new Bounds();
        bool first = true;
        foreach (Renderer r in renderers)
        {
            Bounds wb = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = wb.center + Vector3.Scale(wb.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 p = t.InverseTransformPoint(corner);
                if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                else local.Encapsulate(p);
            }
        }
        box.center = local.center;
        box.size = local.size;
    }

    // Camera pose straight above the button center, looking down. The top of the screen follows the button's
    // own horizontal axis closest to "away from the desk camera", so the button is seen square, not diagonal.
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

        Vector3 screenUp = -toCamera;
        float bestDot = -1f;
        foreach (Vector3 axis in new[] { transform.right, transform.up, transform.forward })
        {
            Vector3 h = Vector3.ProjectOnPlane(axis, Vector3.up);
            if (h.sqrMagnitude < 0.25f) continue; // (nearly) vertical axis
            h.Normalize();
            float d = Vector3.Dot(h, -toCamera);
            if (Mathf.Abs(d) > bestDot)
            {
                bestDot = Mathf.Abs(d);
                screenUp = d >= 0f ? h : -h;
            }
        }

        GameObject go = new GameObject("Launch_ZoomTarget");
        go.transform.SetParent(transform, true);
        go.transform.position = center + Vector3.up * zoomDistance;
        go.transform.rotation = Quaternion.LookRotation(Vector3.down, screenUp);
        zoomTarget = go.transform;
    }
}
