using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds the planning wall in 01_Planeacion: a WorldMap quad to the left of the
/// calendar, click colliders on the calendar and the globe, a zoom target that
/// frames the map and the calendar, and a Back button. Does not touch the desk PC.
/// Menu: SpaceApps / Build planning wall (map + calendar).
/// The launch-site and calendar panels are not created here (pending).
/// </summary>
public static class PlanningWallBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/01_Planeacion.unity";
    const string TexturePath = "Assets/_Project/Art/Texturas/Escritorio/WorldMap.png";
    const string MaterialPath = "Assets/_Project/Art/Materiales/Escritorio/WorldMap.mat";
    const string UnlitShader = "Universal Render Pipeline/Unlit";
    const string LitShader = "Universal Render Pipeline/Lit";

    [MenuItem("SpaceApps/Build planning wall (map + calendar)")]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Build planning wall", "Exit Play Mode before building the planning wall.", "OK");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.path.Replace("\\", "/").EndsWith("01_Planeacion.unity"))
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        Undo.SetCurrentGroupName("Build planning wall");
        int group = Undo.GetCurrentGroup();

        Transform calendar = EnsureCalendar();
        if (calendar == null)
        {
            EditorUtility.DisplayDialog("Build planning wall", "No calendar was found. Expected an object named Calendar or tinker.", "OK");
            return;
        }

        Transform globe = FindNamed(PlanningStation.GlobeName);
        Transform root = EnsureRoot();
        var station = root.GetComponent<PlanningStation>();
        if (station == null) station = Undo.AddComponent<PlanningStation>(root.gameObject);

        Transform map = EnsureMap(root);
        Transform zoom = EnsureChild(root, PlanningStation.ZoomTargetName);
        Canvas back = EnsureBackCanvas();
        Button backButton = back.GetComponentInChildren<Button>(true);

        EnsureCollider(calendar);
        if (globe != null) EnsureCollider(globe);

        station.SetCalendar(calendar);
        station.SetGlobe(globe);
        station.SetMap(map);
        station.SetZoomTarget(zoom);
        station.SetBack(back, backButton);
        station.ApplyMapPlacement();

        Camera cam = Camera.main;
        var desk = Object.FindAnyObjectByType<DeskCamera>();
        if (desk != null) cam = desk.GetComponent<Camera>();
        station.PlaceZoomTarget(cam);

        EnsureEventSystem();

        EditorUtility.SetDirty(station);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Undo.CollapseUndoOperations(group);
        Debug.Log("[PlanningWallBuilder] Planning wall ready. Nudge the map with PlanningStation (Map Width, Map Gap, Map Height Offset, Map Wall Offset, Zoom Fill) and run the menu again, or tweak those fields in the Inspector.");
    }

    static Transform EnsureRoot()
    {
        Transform existing = FindNamed(PlanningStation.RootName);
        if (existing != null) return existing;
        var go = new GameObject(PlanningStation.RootName);
        Undo.RegisterCreatedObjectUndo(go, "Create PlanningStation");
        return go.transform;
    }

    static Transform EnsureCalendar()
    {
        Transform calendar = FindNamed(PlanningStation.CalendarName);
        if (calendar == null) calendar = FindNamed(PlanningStation.CalendarLegacyName);
        if (calendar == null) return null;
        if (calendar.name != PlanningStation.CalendarName)
        {
            Undo.RecordObject(calendar.gameObject, "Rename calendar");
            calendar.name = PlanningStation.CalendarName;
            EditorUtility.SetDirty(calendar.gameObject);
        }
        return calendar;
    }

    static Transform EnsureMap(Transform root)
    {
        Transform map = FindNamed(PlanningStation.MapName);
        if (map == null && root != null) map = root.Find(PlanningStation.MapName);
        if (map == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = PlanningStation.MapName;
            Undo.RegisterCreatedObjectUndo(go, "Create WorldMap");
            map = go.transform;
        }

        if (root != null && map.parent != root)
            Undo.SetTransformParent(map, root, "Parent WorldMap");

        var collider = map.GetComponent<MeshCollider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
        var box = map.GetComponent<BoxCollider>();
        if (box != null) Undo.DestroyObjectImmediate(box);

        var renderer = map.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = LoadOrCreateMaterial();
            if (mat != null && renderer.sharedMaterial != mat)
            {
                Undo.RecordObject(renderer, "Assign WorldMap material");
                renderer.sharedMaterial = mat;
                EditorUtility.SetDirty(renderer);
            }
        }
        return map;
    }

    static Material LoadOrCreateMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find(UnlitShader);
        if (shader == null) shader = Shader.Find(LitShader);
        if (shader == null)
        {
            Debug.LogWarning("[PlanningWallBuilder] Could not find a URP Unlit or Lit shader.");
            return null;
        }

        mat = new Material(shader);
        mat.name = "WorldMap";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    static Transform EnsureChild(Transform root, string name)
    {
        Transform child = root.Find(name);
        if (child != null) return child;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        Undo.SetTransformParent(go.transform, root, "Parent " + name);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    static void EnsureCollider(Transform target)
    {
        if (target == null) return;
        if (target.GetComponentInChildren<Collider>() != null) return;

        var renderer = target.GetComponentInChildren<Renderer>();
        var meshFilter = target.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            var meshCol = Undo.AddComponent<MeshCollider>(meshFilter.gameObject);
            meshCol.sharedMesh = meshFilter.sharedMesh;
            return;
        }
        if (renderer != null)
        {
            var box = Undo.AddComponent<BoxCollider>(renderer.gameObject);
            box.center = renderer.transform.InverseTransformPoint(renderer.bounds.center);
        }
    }

    static Canvas EnsureBackCanvas()
    {
        Transform existing = FindNamed(PlanningStation.BackCanvasName);
        if (existing != null)
        {
            var found = existing.GetComponent<Canvas>();
            if (found != null) return found;
        }

        var go = new GameObject(PlanningStation.BackCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create PlanningBackCanvas");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var buttonRt = CreateImage("Back", go.transform, new Color(0.15f, 0.15f, 0.2f, 0.92f));
        buttonRt.anchorMin = new Vector2(0f, 0f);
        buttonRt.anchorMax = new Vector2(0f, 0f);
        buttonRt.pivot = new Vector2(0f, 0f);
        buttonRt.anchoredPosition = new Vector2(32f, 32f);
        buttonRt.sizeDelta = new Vector2(160f, 52f);
        var button = buttonRt.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRt.GetComponent<Image>();
        var label = CreateText("Label", buttonRt, "Back", 28f);
        Stretch(label.rectTransform);

        go.SetActive(false);
        return canvas;
    }

    static void EnsureEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Undo.AddComponent<EventSystem>(go);
            var module = Undo.AddComponent<InputSystemUIInputModule>(go);
            module.AssignDefaultActions();
            return;
        }

        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null) Undo.DestroyObjectImmediate(standalone);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            var module = Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
            module.AssignDefaultActions();
        }
    }

    static Transform FindNamed(string name)
    {
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == name)
                return transforms[i];
        }
        return null;
    }

    static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
