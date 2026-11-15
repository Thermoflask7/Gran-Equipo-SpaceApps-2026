using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Planning wall station (task 6, map + zoom). Click the calendar or the globe
/// to zoom the camera to a framing of the world map and the calendar.
/// Escape or the Back button returns to the desk. The launch-site and calendar
/// panels are not wired yet.
/// </summary>
public class PlanningStation : MonoBehaviour
{
    public const string RootName = "PlanningStation";
    public const string MapName = "WorldMap";
    public const string ZoomTargetName = "PlanningZoomTarget";
    public const string BackCanvasName = "PlanningBackCanvas";
    public const string CalendarName = "Calendar";
    public const string CalendarLegacyName = "tinker";
    public const string GlobeName = "Globe";

    [Header("Scene objects")]
    [SerializeField] Transform calendar;
    [SerializeField] Transform globe;
    [SerializeField] Transform map;
    [SerializeField] Transform zoomTarget;
    [SerializeField] Canvas backCanvas;
    [SerializeField] Button backButton;

    [Header("Map placement")]
    [Tooltip("Width of the WorldMap quad in meters. Height is width / Map Aspect.")]
    [SerializeField] float mapWidth = 1.1f;
    [Tooltip("Width divided by height. The source texture is about 2:1.")]
    [SerializeField] float mapAspect = 2f;
    [Tooltip("Gap in meters between the calendar's left edge and the map's right edge, from the viewer's point of view.")]
    [SerializeField] float mapGap = 0.12f;
    [Tooltip("Vertical nudge in meters (positive raises the map).")]
    [SerializeField] float mapHeightOffset = 0.05f;
    [Tooltip("How far the map sits off the wall toward the camera, in meters.")]
    [SerializeField] float mapWallOffset = 0.01f;
    [Tooltip("Seconds for the map to fade in when zooming and fade out when returning.")]
    [SerializeField, Min(0f)] float mapFadeDuration = 1f;

    [Header("Zoom")]
    [Tooltip("1 = the map and calendar fill the view. Higher leaves a margin.")]
    [SerializeField, Range(0.7f, 1.8f)] float zoomFill = 1.15f;
    [SerializeField] float maxClickDistance = 50f;
    [Tooltip("Log what the mouse ray hits when clicking (to debug clicks).")]
    [SerializeField] bool debugClicks = true;

    public bool IsOpen { get; private set; }
    public Transform ZoomTarget => zoomTarget;
    public Transform Map => map;
    public Transform Calendar => calendar;
    public Transform Globe => globe;

    public float MapWidth { get { return mapWidth; } set { mapWidth = Mathf.Max(0.1f, value); } }
    public float MapAspect { get { return mapAspect; } set { mapAspect = Mathf.Max(0.1f, value); } }
    public float MapGap { get { return mapGap; } set { mapGap = value; } }
    public float MapHeightOffset { get { return mapHeightOffset; } set { mapHeightOffset = value; } }
    public float MapWallOffset { get { return mapWallOffset; } set { mapWallOffset = value; } }
    public float ZoomFill { get { return zoomFill; } set { zoomFill = value; } }

    DeskCamera subscribed;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock mapBlock;
    float mapAlpha = 1f;
    float mapAlphaTarget = 1f;
    LaunchSiteSelector launchSites;
    LaunchSiteInfoPanel infoPanel;
    CalendarWeatherPanel calendarPanel;

    /// <summary>The launch site markers on the map (created in Play mode if missing).</summary>
    public LaunchSiteSelector LaunchSites => launchSites;

    void OnEnable()
    {
        Subscribe();
        BindBackButton();
        HideBack();
    }

    void OnDisable()
    {
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        subscribed = null;
        if (backButton != null) backButton.onClick.RemoveListener(Close);
        HideBack();
    }

    void Subscribe()
    {
        var dc = DeskCamera.Instance;
        if (dc == null || dc == subscribed) return;
        if (subscribed != null) subscribed.ReturnStarted -= HandleReturnStarted;
        dc.ReturnStarted += HandleReturnStarted;
        subscribed = dc;
    }

    void BindBackButton()
    {
        if (backButton == null) return;
        backButton.onClick.RemoveListener(Close);
        backButton.onClick.AddListener(Close);
    }

    void Update()
    {
        UpdateMapFade();
        if (IsOpen) return;
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        var dc = DeskCamera.Instance;
        // Same gate as PcStation: only one station can zoom at a time.
        if (dc == null || dc.IsFocused || dc.IsMoving) return;
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        var cam = dc.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var hits = Physics.RaycastAll(ray, maxClickDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool planningHit = false;
        float firstOther = float.PositiveInfinity;
        foreach (var h in hits)
        {
            if (map != null && (h.collider.transform == map || h.collider.transform.IsChildOf(map))) continue;
            if (IsPlanningHit(h.collider.transform)) { planningHit = true; break; }
            firstOther = Mathf.Min(firstOther, h.distance);
        }
        // The calendar's white sheet may have no collider: also accept clicks inside
        // the calendar's (or globe's) visual bounds when nothing closer blocks them.
        if (!planningHit)
            planningHit = RayHitsBounds(calendar, ray, firstOther) || RayHitsBounds(globe, ray, firstOther);

        if (debugClicks)
        {
            string first = hits.Length > 0 ? hits[0].collider.name : "nothing";
            Debug.Log("[PlanningStation] Click: overUI=" + overUi + ", first hit=" + first + ", calendar/globe hit=" + planningHit);
        }

        if (planningHit && !overUi)
            Open();
    }

    static bool RayHitsBounds(Transform root, Ray ray, float maxDistance)
    {
        if (root == null) return false;
        Bounds b = Encapsulate(root);
        if (b.size.sqrMagnitude < 1e-8f) return false;
        return b.IntersectRay(ray, out float d) && d <= maxDistance + 0.05f;
    }

    void Start()
    {
        // The map is only shown while zoomed into the planning wall.
        if (!Application.isPlaying || map == null) return;
        launchSites = map.GetComponent<LaunchSiteSelector>();
        if (launchSites == null) launchSites = map.gameObject.AddComponent<LaunchSiteSelector>();
        if (calendar != null)
        {
            var dc = DeskCamera.Instance;
            Camera eventCam = dc != null ? dc.GetComponent<Camera>() : Camera.main;
            infoPanel = LaunchSiteInfoPanel.Create(transform, map, Encapsulate(calendar), eventCam);
            launchSites.SelectionChanged += infoPanel.ShowSite;
            calendarPanel = CalendarWeatherPanel.Create(transform, map, Encapsulate(calendar), eventCam);
            launchSites.SelectionChanged += calendarPanel.SetSite;
        }
        mapAlpha = 0f;
        mapAlphaTarget = 0f;
        ApplyMapAlpha();
        map.gameObject.SetActive(false);
    }

    /// <summary>Fade the map in (activating it first) or out (deactivating it at the end).</summary>
    void SetMapVisible(bool visible)
    {
        if (map == null) return;
        mapAlphaTarget = visible ? 1f : 0f;
        if (visible && !map.gameObject.activeSelf)
        {
            map.gameObject.SetActive(true);
            ApplyMapAlpha();
        }
        if (mapFadeDuration <= 0f)
        {
            mapAlpha = mapAlphaTarget;
            ApplyMapAlpha();
            if (!visible) map.gameObject.SetActive(false);
        }
    }

    void UpdateMapFade()
    {
        if (map == null || Mathf.Approximately(mapAlpha, mapAlphaTarget)) return;
        float step = mapFadeDuration > 0f ? Time.deltaTime / mapFadeDuration : 1f;
        mapAlpha = Mathf.MoveTowards(mapAlpha, mapAlphaTarget, step);
        ApplyMapAlpha();
        if (mapAlpha <= 0f && mapAlphaTarget <= 0f) map.gameObject.SetActive(false);
    }

    void ApplyMapAlpha()
    {
        var r = map != null ? map.GetComponent<Renderer>() : null;
        if (r == null) return;
        if (mapBlock == null) mapBlock = new MaterialPropertyBlock();
        r.GetPropertyBlock(mapBlock);
        mapBlock.SetColor(BaseColorId, new Color(1f, 1f, 1f, mapAlpha));
        r.SetPropertyBlock(mapBlock);
        if (launchSites != null) launchSites.SetAlpha(mapAlpha);
        if (infoPanel != null) infoPanel.SetAlpha(mapAlpha);
        if (calendarPanel != null) calendarPanel.SetAlpha(mapAlpha);
    }

    bool IsPlanningHit(Transform hit)
    {
        if (hit == null) return false;
        if (calendar != null && (hit == calendar || hit.IsChildOf(calendar))) return true;
        if (globe != null && (hit == globe || hit.IsChildOf(globe))) return true;
        return false;
    }

    /// <summary>Zoom the camera to the map and calendar wall.</summary>
    public void Open()
    {
        var dc = DeskCamera.Instance;
        if (dc == null || IsOpen) return;
        Subscribe();
        SetMapVisible(true);
        EnsureZoomTarget(dc.GetComponent<Camera>());
        PlaceZoomTarget(dc.GetComponent<Camera>());
        IsOpen = true;
        dc.FocusOn(zoomTarget, () =>
        {
            if (!IsOpen) return;
            ShowBack();
        });
    }

    /// <summary>Return to the desk (Back button or a code call). Escape is handled by DeskCamera.</summary>
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
        HideBack();
        SetMapVisible(false);
    }

    void ShowBack()
    {
        if (backCanvas != null) backCanvas.gameObject.SetActive(true);
    }

    void HideBack()
    {
        if (backCanvas != null) backCanvas.gameObject.SetActive(false);
    }

    public void SetCalendar(Transform value) => calendar = value;
    public void SetGlobe(Transform value) => globe = value;
    public void SetMap(Transform value) => map = value;
    public void SetZoomTarget(Transform target) => zoomTarget = target;
    public void SetBack(Canvas canvas, Button button)
    {
        if (backButton != null) backButton.onClick.RemoveListener(Close);
        backCanvas = canvas;
        backButton = button;
        BindBackButton();
    }

    /// <summary>Place the WorldMap quad to the left of the calendar, on the wall, keeping the aspect.</summary>
    public void ApplyMapPlacement()
    {
        if (map == null || calendar == null) return;

        Bounds cal = Encapsulate(calendar);
        if (cal.size.sqrMagnitude < 1e-8f)
            cal = new Bounds(calendar.position, new Vector3(0.46f, 0.42f, 0.08f));

        Vector3 normal = WallNormal();
        Vector3 calHalf = new Vector3(Mathf.Abs(normal.z) * cal.extents.x + Mathf.Abs(normal.x) * cal.extents.z, 0f, 0f);
        float width = Mathf.Max(0.1f, mapWidth);
        float aspect = Mathf.Max(0.1f, mapAspect);
        float height = width / aspect;

        // Viewer's right when looking at the wall (normal points toward the camera).
        Vector3 wallRight = Vector3.Cross(normal, Vector3.up);
        if (wallRight.sqrMagnitude < 1e-6f) wallRight = Vector3.right;
        wallRight.Normalize();

        Vector3 calLeft = cal.center - wallRight * calHalf.x;
        Vector3 center = calLeft - wallRight * (mapGap + width * 0.5f);
        center += Vector3.up * mapHeightOffset;
        // Keep the map on the wall plane, then push it toward the camera.
        // The calendar hangs on the wall, so its back face marks the wall plane.
        float depth = Mathf.Abs(normal.x) * cal.extents.x + Mathf.Abs(normal.z) * cal.extents.z;
        Vector3 wallPoint = cal.center - normal * depth;
        center -= normal * Vector3.Dot(center - wallPoint, normal);
        center += normal * mapWallOffset;

        // Unity's Quad shows its front face along -Z, so -Z must point at the camera.
        map.SetPositionAndRotation(center, Quaternion.LookRotation(-normal, Vector3.up));
        map.localScale = new Vector3(width, height, 1f);
    }

    /// <summary>Place the zoom target so the map and the calendar fill the camera view.</summary>
    public void PlaceZoomTarget(Camera cam)
    {
        if (zoomTarget == null || cam == null) return;
        if (!TryComputeZoomPose(cam, out Vector3 pos, out Quaternion rot)) return;
        zoomTarget.SetPositionAndRotation(pos, rot);
    }

    public bool TryComputeZoomPose(Camera cam, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;
        if (cam == null) return false;

        Bounds frame = default;
        bool has = false;
        if (map != null)
        {
            frame = Encapsulate(map);
            has = frame.size.sqrMagnitude > 1e-8f;
        }
        if (calendar != null)
        {
            Bounds cal = Encapsulate(calendar);
            if (has) frame.Encapsulate(cal);
            else { frame = cal; has = frame.size.sqrMagnitude > 1e-8f; }
        }
        if (has && infoPanel != null) frame.Encapsulate(infoPanel.WorldBounds());
        if (!has) return false;

        Vector3 normal = WallNormal();
        if (Vector3.Dot(normal, cam.transform.position - frame.center) < 0f) normal = -normal;

        float halfFovV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float halfFovH = Mathf.Atan(Mathf.Tan(halfFovV) * cam.aspect);
        float fill = Mathf.Max(0.5f, zoomFill);
        float distH = (frame.size.y * 0.5f * fill) / Mathf.Tan(halfFovV);
        float distW = (frame.size.x * 0.5f * fill) / Mathf.Tan(halfFovH);
        float distance = Mathf.Max(distH, distW, 0.3f);

        position = frame.center + normal * distance;
        rotation = Quaternion.LookRotation(-normal, Vector3.up);
        return true;
    }

    void EnsureZoomTarget(Camera cam)
    {
        if (zoomTarget != null) return;
        var go = new GameObject(ZoomTargetName);
        go.transform.SetParent(transform, false);
        zoomTarget = go.transform;
        PlaceZoomTarget(cam);
    }

    Vector3 WallNormal()
    {
        var dc = DeskCamera.Instance;
        Transform cam = dc != null ? dc.transform : (Camera.main != null ? Camera.main.transform : null);
        Vector3 from = calendar != null ? calendar.position : (map != null ? map.position : transform.position);
        if (cam != null)
        {
            Vector3 toCam = cam.position - from;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 1e-6f)
            {
                // The room walls are axis-aligned: snap to the closest world axis
                // so the map lies flat on the wall instead of facing the camera at an angle.
                if (Mathf.Abs(toCam.x) > Mathf.Abs(toCam.z))
                    return new Vector3(Mathf.Sign(toCam.x), 0f, 0f);
                return new Vector3(0f, 0f, Mathf.Sign(toCam.z));
            }
        }
        return Vector3.back;
    }

    static Bounds Encapsulate(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // No OnValidate auto-placement: the map keeps the pose set by hand in the scene.
    // Re-run the Build planning wall menu to place it automatically.
}
