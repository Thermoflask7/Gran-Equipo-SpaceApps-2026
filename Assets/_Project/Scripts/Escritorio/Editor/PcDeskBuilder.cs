using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Arma la estación PC del escritorio en 01_Planeacion:
/// collider, zoom de cámara, Canvas SpaceOS y EventSystem con Input System.
/// Menú: SpaceApps / Armar PC del escritorio.
/// </summary>
public static class PcDeskBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/01_Planeacion.unity";
    const string PcName = "computer_old_2";
    const string MonitorName = "Old Monitor 2";

    struct DesktopRefs
    {
        public GameObject root;
        public RectTransform appGrid;
        public Button backButton;
        public GameObject comingSoonPanel;
        public TextMeshProUGUI comingSoonText;
        public Button comingSoonCloseButton;
        public TextMeshProUGUI clockText;
    }

    [MenuItem("SpaceApps/Armar PC del escritorio")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Armar PC del escritorio", "Sal del modo Play antes de armar la PC.", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.name != "01_Planeacion")
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Armar PC del escritorio", "No se encontró la escena " + ScenePath + ".", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        GameObject pc = FindNamed(scene, PcName);
        if (pc == null)
        {
            EditorUtility.DisplayDialog("Armar PC del escritorio", "No se encontró el GameObject '" + PcName + "' en la escena.", "OK");
            return;
        }

        Transform monitor = FindChild(pc.transform, MonitorName);
        if (monitor == null)
        {
            EditorUtility.DisplayDialog("Armar PC del escritorio", "No se encontró el hijo '" + MonitorName + "'.", "OK");
            return;
        }

        var monitorRenderer = monitor.GetComponent<MeshRenderer>();
        if (monitorRenderer == null)
        {
            EditorUtility.DisplayDialog("Armar PC del escritorio", "'" + MonitorName + "' no tiene MeshRenderer.", "OK");
            return;
        }

        Camera cam = FindMainCamera(scene);
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Armar PC del escritorio", "No hay una cámara con tag MainCamera.", "OK");
            return;
        }

        int submesh = FindScreenSubmesh(monitorRenderer);
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Armar PC del escritorio");
        try
        {
            DisableEmptyMeshColliders(pc);
            FitBoxCollider(pc);

            if (cam.GetComponent<DeskCamera>() == null)
                Undo.AddComponent<DeskCamera>(cam.gameObject);

            var station = pc.GetComponent<PcStation>();
            if (station == null)
                station = Undo.AddComponent<PcStation>(pc);

            Transform zoom = FindChild(pc.transform, "PC_ZoomTarget");
            if (zoom == null)
            {
                var zoomGo = new GameObject("PC_ZoomTarget");
                Undo.RegisterCreatedObjectUndo(zoomGo, "Create PC_ZoomTarget");
                Undo.SetTransformParent(zoomGo.transform, pc.transform, "Parent PC_ZoomTarget");
                zoom = zoomGo.transform;
            }

            Undo.RecordObject(station, "Configure PcStation");
            station.SetScreen(monitorRenderer, submesh);
            station.SetZoomTarget(zoom);
            Undo.RecordObject(zoom, "Place PC_ZoomTarget");
            station.PlaceZoomTarget(cam);
            LogZoom(station, cam);

            DestroyAllNamed(scene, "PC_Desktop");
            ComputerDesktop desktop = CreateDesktop(out DesktopRefs refs);
            AssignDesktop(desktop, refs);
            station.SetDesktop(desktop);
            EditorUtility.SetDirty(station);

            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = pc;
            Debug.Log("[PcDeskBuilder] Listo. PC del escritorio armada y escena 01_Planeacion guardada.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static void LogZoom(PcStation station, Camera cam)
    {
        if (!station.TryComputeZoomPose(cam, out Vector3 zoomPos, out Quaternion zoomRot, out float screenHeight))
        {
            Debug.LogWarning("[PcDeskBuilder] No se pudo calcular la pose de zoom de la pantalla.");
            return;
        }

        float heightFill = 1f;
        var fill = new SerializedObject(station).FindProperty("heightFill");
        if (fill != null) heightFill = fill.floatValue;
        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float tan = Mathf.Tan(halfFov);
        float distance = tan > 0.0001f ? (screenHeight * 0.5f * heightFill) / tan : 0f;
        Debug.Log("[PcDeskBuilder] Alto de pantalla " + screenHeight.ToString("F3") + " m, distancia " + distance.ToString("F3") + " m. Pose " + zoomPos + " / " + zoomRot.eulerAngles + ".");
    }

    static int FindScreenSubmesh(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        if (materials == null) return 1;
        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat != null && mat.name.ToLowerInvariant().Contains("screen"))
                return i;
        }
        return 1;
    }

    static void DisableEmptyMeshColliders(GameObject pc)
    {
        var colliders = pc.GetComponents<MeshCollider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].sharedMesh != null) continue;
            Undo.RecordObject(colliders[i], "Disable empty MeshCollider");
            colliders[i].enabled = false;
            EditorUtility.SetDirty(colliders[i]);
        }
    }

    static void FitBoxCollider(GameObject pc)
    {
        var box = pc.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(pc);

        Undo.RecordObject(box, "Fit PC BoxCollider");
        if (!TryEncapsulateRenderers(pc.transform, out Vector3 center, out Vector3 size))
        {
            Debug.LogWarning("[PcDeskBuilder] " + PcName + " no tiene Renderers. El BoxCollider no se ajustó.");
            return;
        }
        box.center = center;
        box.size = size;
        EditorUtility.SetDirty(box);
    }

    /// <summary>AABB local que encierra las 8 esquinas de cada renderer.bounds.</summary>
    static bool TryEncapsulateRenderers(Transform root, out Vector3 center, out Vector3 size)
    {
        center = Vector3.zero;
        size = Vector3.zero;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bool any = false;
        var local = new Bounds();
        for (int r = 0; r < renderers.Length; r++)
        {
            Bounds world = renderers[r].bounds;
            Vector3 c = world.center;
            Vector3 e = world.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z));
                Vector3 p = root.InverseTransformPoint(corner);
                if (!any)
                {
                    local = new Bounds(p, Vector3.zero);
                    any = true;
                }
                else local.Encapsulate(p);
            }
        }
        if (!any) return false;
        center = local.center;
        size = local.size;
        return true;
    }

    static Camera FindMainCamera(Scene scene)
    {
        if (Camera.main != null && Camera.main.gameObject.scene == scene)
            return Camera.main;

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var cameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < cameras.Length; c++)
            {
                if (cameras[c].CompareTag("MainCamera"))
                    return cameras[c];
            }
        }
        return null;
    }

    static ComputerDesktop CreateDesktop(out DesktopRefs refs)
    {
        var canvasGo = new GameObject("PC_Desktop", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create PC_Desktop");

        var canvas = Undo.AddComponent<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = Undo.AddComponent<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        Undo.AddComponent<GraphicRaycaster>(canvasGo);
        var desktop = Undo.AddComponent<ComputerDesktop>(canvasGo);
        canvasGo.SetActive(true);

        RectTransform root = CreateRect("Root", canvasGo.transform);
        Stretch(root, 0f, 0f, 0f, 0f);

        RectTransform pantalla = CreateImage("Pantalla", root, new Color(0.03f, 0.06f, 0.16f, 1f));
        pantalla.anchorMin = new Vector2(0.5f, 0f);
        pantalla.anchorMax = new Vector2(0.5f, 1f);
        pantalla.pivot = new Vector2(0.5f, 0.5f);
        pantalla.anchoredPosition = Vector2.zero;
        pantalla.offsetMin = Vector2.zero;
        pantalla.offsetMax = Vector2.zero;
        pantalla.sizeDelta = new Vector2(1440f, 0f);
        var fitter = Undo.AddComponent<AspectRatioFitter>(pantalla.gameObject);
        fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fitter.aspectRatio = 1.3333f;

        RectTransform barra = CreateImage("Barra", pantalla, new Color(0f, 0f, 0f, 0.6f));
        barra.anchorMin = new Vector2(0f, 0f);
        barra.anchorMax = new Vector2(1f, 0f);
        barra.pivot = new Vector2(0.5f, 0f);
        barra.anchoredPosition = Vector2.zero;
        barra.sizeDelta = new Vector2(0f, 60f);

        TextMeshProUGUI clock = CreateText("Reloj", barra, "SpaceOS", 28f, TextAlignmentOptions.MidlineLeft);
        clock.rectTransform.offsetMin = new Vector2(20f, 0f);
        clock.rectTransform.offsetMax = new Vector2(-20f, 0f);

        RectTransform apps = CreateRect("Apps", pantalla);
        apps.anchorMin = Vector2.zero;
        apps.anchorMax = Vector2.one;
        apps.pivot = new Vector2(0.5f, 0.5f);
        apps.offsetMin = new Vector2(60f, 60f);
        apps.offsetMax = new Vector2(-60f, -60f);
        var grid = Undo.AddComponent<GridLayoutGroup>(apps.gameObject);
        grid.cellSize = new Vector2(180f, 200f);
        grid.spacing = new Vector2(40f, 40f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(20, 20, 20, 20);

        RectTransform soon = CreateImage("Proximamente", pantalla, new Color(0.1f, 0.12f, 0.2f, 0.97f));
        soon.anchorMin = new Vector2(0.5f, 0.5f);
        soon.anchorMax = new Vector2(0.5f, 0.5f);
        soon.pivot = new Vector2(0.5f, 0.5f);
        soon.anchoredPosition = Vector2.zero;
        soon.sizeDelta = new Vector2(700f, 400f);

        TextMeshProUGUI soonText = CreateText("Texto", soon, "Próximamente", 40f, TextAlignmentOptions.Center);
        soonText.rectTransform.offsetMin = new Vector2(24f, 110f);
        soonText.rectTransform.offsetMax = new Vector2(-24f, -24f);

        Button close = CreateButton("Cerrar", soon, new Color(0.15f, 0.15f, 0.2f, 0.9f));
        var closeRt = (RectTransform)close.transform;
        closeRt.anchorMin = new Vector2(0.5f, 0f);
        closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0f, 24f);
        closeRt.sizeDelta = new Vector2(200f, 60f);
        CreateText("Etiqueta", close.transform, "Cerrar", 28f, TextAlignmentOptions.Center);
        soon.gameObject.SetActive(false);

        Button back = CreateButton("Volver", root, new Color(0.15f, 0.15f, 0.2f, 0.9f));
        var backRt = (RectTransform)back.transform;
        backRt.anchorMin = new Vector2(0f, 1f);
        backRt.anchorMax = new Vector2(0f, 1f);
        backRt.pivot = new Vector2(0f, 1f);
        backRt.anchoredPosition = new Vector2(30f, -30f);
        backRt.sizeDelta = new Vector2(220f, 70f);
        CreateText("Etiqueta", back.transform, "< Volver", 32f, TextAlignmentOptions.Center);

        refs = new DesktopRefs
        {
            root = root.gameObject,
            appGrid = apps,
            backButton = back,
            comingSoonPanel = soon.gameObject,
            comingSoonText = soonText,
            comingSoonCloseButton = close,
            clockText = clock
        };
        return desktop;
    }

    static void AssignDesktop(ComputerDesktop desktop, DesktopRefs refs)
    {
        var so = new SerializedObject(desktop);
        SetRef(so, "root", refs.root);
        SetRef(so, "appGrid", refs.appGrid);
        SetRef(so, "backButton", refs.backButton);
        SetRef(so, "comingSoonPanel", refs.comingSoonPanel);
        SetRef(so, "comingSoonText", refs.comingSoonText);
        SetRef(so, "comingSoonCloseButton", refs.comingSoonCloseButton);
        SetRef(so, "clockText", refs.clockText);
        so.ApplyModifiedProperties();
    }

    static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogError("[PcDeskBuilder] Falta el campo serializado '" + propertyName + "'.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    static void EnsureEventSystem()
    {
        var es = UnityEngine.Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Undo.AddComponent<EventSystem>(go);
            var created = Undo.AddComponent<InputSystemUIInputModule>(go);
            created.AssignDefaultActions();
            return;
        }

        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
            Undo.DestroyObjectImmediate(standalone);

        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            var created = Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
            created.AssignDefaultActions();
        }
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        var rt = (RectTransform)go.transform;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.anchoredPosition3D = Vector3.zero;
        return rt;
    }

    static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        var rt = (RectTransform)go.transform;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.anchoredPosition3D = Vector3.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static Button CreateButton(string name, Transform parent, Color color)
    {
        RectTransform rt = CreateImage(name, parent, color);
        var button = Undo.AddComponent<Button>(rt.gameObject);
        button.targetGraphic = rt.GetComponent<Image>();
        return button;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rt = CreateRect(name, parent);
        Stretch(rt, 0f, 0f, 0f, 0f);
        var tmp = Undo.AddComponent<TextMeshProUGUI>(rt.gameObject);
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        else
            Debug.LogWarning("[PcDeskBuilder] TMP_Settings.defaultFontAsset es null. Asigna una fuente en Project Settings > TextMesh Pro.");
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static GameObject FindNamed(Scene scene, string objectName)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
                return roots[i];
            var transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int t = 0; t < transforms.Length; t++)
            {
                if (transforms[t].name == objectName)
                    return transforms[t].gameObject;
            }
        }
        return null;
    }

    static Transform FindChild(Transform root, string childName)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != root && transforms[i].name == childName)
                return transforms[i];
        }
        return null;
    }

    static void DestroyAllNamed(Scene scene, string objectName)
    {
        for (int n = 0; n < 32; n++)
        {
            GameObject found = FindNamed(scene, objectName);
            if (found == null) return;
            Undo.DestroyObjectImmediate(found);
        }
    }
}
