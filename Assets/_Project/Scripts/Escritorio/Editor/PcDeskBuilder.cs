using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Arma la estación PC del escritorio en 01_Planeacion:
/// collider, zoom de cámara, malla curva del monitor (RenderTexture + filtro CRT),
/// Canvas SpaceOS y EventSystem con Input System.
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
        public GameObject screenOff;
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

            if (!station.TryGetScreenSurface(cam, out _, out _, out _, out _))
                Debug.LogWarning("[PcDeskBuilder] No se pudo calcular la superficie de la pantalla; se usa la colocacion por defecto de SpaceOsScreen.");

            DestroyAllNamed(scene, "PC_Desktop");
            DestroyAllNamed(scene, "PC_Screen");
            DestroyAllNamed(scene, "PC_UICamera");

            SpaceOsScreen spaceOs = CreateCurvedScreen(pc, cam, monitorRenderer, submesh, out ComputerDesktop desktop, out DesktopRefs refs);
            AssignDesktop(desktop, refs);
            station.SetDesktop(desktop);
            if (spaceOs != null) spaceOs.Refresh();
            EditorUtility.SetDirty(station);
            EditorUtility.SetDirty(spaceOs);

            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = pc;
            Debug.Log("[PcDeskBuilder] Listo. SpaceOS en malla curva (RenderTexture + CRT). Ajusta curvatura y offset en PC_Screen.");
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

    const string CrtMatPath = "Assets/_Project/Art/Materiales/Escritorio/SpaceOS_CRT.mat";
    const string PlainMatPath = "Assets/_Project/Art/Materiales/Escritorio/SpaceOS_Screen_Plain.mat";

    static SpaceOsScreen CreateCurvedScreen(GameObject pc, Camera worldCam, Renderer monitorRenderer, int submesh, out ComputerDesktop desktop, out DesktopRefs refs)
    {
        Material crtMat = AssetDatabase.LoadAssetAtPath<Material>(CrtMatPath);
        Material plainMat = AssetDatabase.LoadAssetAtPath<Material>(PlainMatPath);
        if (crtMat == null)
            Debug.LogWarning("[PcDeskBuilder] No se encontro " + CrtMatPath + ".");
        if (plainMat == null)
            Debug.LogWarning("[PcDeskBuilder] No se encontro " + PlainMatPath + ".");

        var screenGo = new GameObject("PC_Screen");
        Undo.RegisterCreatedObjectUndo(screenGo, "Create PC_Screen");
        Undo.SetTransformParent(screenGo.transform, pc.transform, "Parent PC_Screen");

        Undo.AddComponent<MeshFilter>(screenGo);
        var meshRenderer = Undo.AddComponent<MeshRenderer>(screenGo);
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        Undo.AddComponent<MeshCollider>(screenGo);
        var curved = Undo.AddComponent<CurvedScreenMesh>(screenGo);
        curved.Rebuild();

        var spaceOs = Undo.AddComponent<SpaceOsScreen>(screenGo);
        spaceOs.SetMaterialTemplates(crtMat, plainMat);
        spaceOs.SetScreenRenderer(meshRenderer);
        spaceOs.SetPlacement(SpaceOsScreen.DefaultLocalPosition, SpaceOsScreen.DefaultLocalEuler, Vector3.zero);

        HideOriginalScreen(monitorRenderer, submesh, plainMat);

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        var uiCamGo = new GameObject("PC_UICamera");
        Undo.RegisterCreatedObjectUndo(uiCamGo, "Create PC_UICamera");
        uiCamGo.tag = "Untagged";
        uiCamGo.layer = uiLayer;
        uiCamGo.transform.position = new Vector3(0f, -1000f, 0f);
        uiCamGo.transform.rotation = Quaternion.identity;
        var uiCam = Undo.AddComponent<Camera>(uiCamGo);
        SetupUICamera(uiCam, uiLayer);
        spaceOs.SetUICamera(uiCam);

        var meshCollider = screenGo.GetComponent<MeshCollider>();
        desktop = CreateDesktop(uiCam, uiLayer, out refs);
        spaceOs.SetDesktop(desktop);

        var raycaster = desktop.GetComponent<SpaceOsGraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.SetScreenCollider(meshCollider);
            raycaster.SetWorldCamera(worldCam);
            raycaster.SetDesktop(desktop);
            raycaster.SetScreen(spaceOs);
        }

        spaceOs.Refresh();
        return spaceOs;
    }

    static void SetupUICamera(Camera cam, int uiLayer)
    {
        cam.orthographic = true;
        cam.orthographicSize = 1f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 10f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 1 << uiLayer;
        cam.depth = -100f;
        cam.useOcclusionCulling = false;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.enabled = true;
        var listener = cam.GetComponent<AudioListener>();
        if (listener != null)
            Undo.DestroyObjectImmediate(listener);
        var urp = cam.GetUniversalAdditionalCameraData();
        urp.renderType = CameraRenderType.Base;
        urp.renderPostProcessing = false;
        urp.antialiasing = AntialiasingMode.None;
    }

    static void HideOriginalScreen(Renderer renderer, int submesh, Material unlit)
    {
        if (renderer == null) return;
        var mats = renderer.sharedMaterials;
        if (submesh < 0 || submesh >= mats.Length) return;
        Shader shader = unlit != null ? unlit.shader : Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return;
        var black = new Material(shader) { name = "ScreenHidden" };
        black.SetColor("_BaseColor", Color.black);
        black.SetColor("_Color", Color.black);
        black.SetTexture("_BaseMap", null);
        black.SetTexture("_MainTex", null);
        mats[submesh] = black;
        renderer.sharedMaterials = mats;
        EditorUtility.SetDirty(renderer);
    }

    static ComputerDesktop CreateDesktop(Camera uiCam, int uiLayer, out DesktopRefs refs)
    {
        var canvasGo = new GameObject("PC_Desktop", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create PC_Desktop");
        canvasGo.layer = uiLayer;

        var canvas = Undo.AddComponent<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCam;
        canvas.planeDistance = 1f;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = 0;

        var scaler = Undo.AddComponent<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1084f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        scaler.referencePixelsPerUnit = 100f;

        Undo.AddComponent<SpaceOsGraphicRaycaster>(canvasGo);
        Undo.AddComponent<CanvasGroup>(canvasGo);
        var desktop = Undo.AddComponent<ComputerDesktop>(canvasGo);
        canvasGo.SetActive(true);

        RectTransform root = CreateRect("Root", canvasGo.transform);
        Stretch(root, 0f, 0f, 0f, 0f);

        RectTransform pantalla = CreateImage("Pantalla", root, new Color(0.03f, 0.06f, 0.16f, 1f));
        Stretch(pantalla, 0f, 0f, 0f, 0f);

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
        apps.offsetMin = new Vector2(60f, 80f);
        apps.offsetMax = new Vector2(-60f, -110f);
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

        Button back = CreateButton("Volver", pantalla, new Color(0.15f, 0.15f, 0.2f, 0.9f));
        var backRt = (RectTransform)back.transform;
        backRt.anchorMin = new Vector2(0f, 1f);
        backRt.anchorMax = new Vector2(0f, 1f);
        backRt.pivot = new Vector2(0f, 1f);
        backRt.anchoredPosition = new Vector2(24f, -24f);
        backRt.sizeDelta = new Vector2(200f, 64f);
        CreateText("Etiqueta", back.transform, "< Volver", 30f, TextAlignmentOptions.Center);

        RectTransform apagada = CreateImage("Apagada", pantalla, new Color(0f, 0f, 0f, 1f));
        Stretch(apagada, 0f, 0f, 0f, 0f);
        apagada.SetAsLastSibling();
        apagada.gameObject.SetActive(false);

        refs = new DesktopRefs
        {
            root = root.gameObject,
            appGrid = apps,
            backButton = back,
            comingSoonPanel = soon.gameObject,
            comingSoonText = soonText,
            comingSoonCloseButton = close,
            clockText = clock,
            screenOff = apagada.gameObject
        };
        SetLayerRecursively(canvasGo, uiLayer);
        return desktop;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
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
        SetRef(so, "screenOff", refs.screenOff);
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
