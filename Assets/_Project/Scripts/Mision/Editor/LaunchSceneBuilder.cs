using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu de Unity: SpaceApps > Armar escena de lanzamiento
// Arma 02_Lanzamiento (tarea 10) en greybox:
//   - Dos camaras base con viewport partido: CamaraControlMision (mitad izquierda, solo fondo)
//     y CamaraCohete (mitad derecha, vista 3D de Tierra, Luna, cohete y ruta).
//   - Canvas en Screen Space Overlay: control de mision a la izquierda (operadores, pantalla grande,
//     bitacora), titulo de la vista 3D, boton de prueba "Saltar fase" y panel de fin con "Ver resultados".
//   - Objeto Lanzamiento con LaunchScreen y todas las referencias conectadas.
// Se puede volver a correr: borra y recrea solo los objetos que crea (ver Generated). El EventSystem
// se conserva y se asegura que use InputSystemUIInputModule. Los materiales se guardan en
// Assets/_Project/Art/Materiales/Lanzamiento.
public static class LaunchSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/" + Scenes.Launch + ".unity";
    const string MaterialFolder = "Assets/_Project/Art/Materiales/Lanzamiento";

    // Objetos raiz que crea este menu (y los del greybox inicial que reemplaza)
    static readonly string[] Generated =
    {
        "Main Camera", "Directional Light", "Canvas",
        "CamaraControlMision", "CamaraCohete", "Espacio", "CanvasLanzamiento", "Lanzamiento"
    };

    static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.13f);
    static readonly Color ScreenFrame = new Color(0.35f, 0.38f, 0.45f);
    static readonly Color ScreenColor = new Color(0.02f, 0.05f, 0.09f);
    static readonly Color ButtonColor = new Color(0.15f, 0.22f, 0.35f);
    static readonly Color DeskColor = new Color(0.3f, 0.27f, 0.25f);
    static readonly Color MonitorColor = new Color(0.12f, 0.35f, 0.45f);
    static readonly Color SkinColor = new Color(0.85f, 0.68f, 0.55f);
    static readonly Color AccentColor = new Color(1f, 0.82f, 0.3f);
    static readonly Color LogColor = new Color(0.55f, 0.95f, 0.6f);

    [MenuItem("SpaceApps/Armar escena de lanzamiento")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Primero importa los recursos de TextMeshPro: Window > TextMeshPro > Import TMP Essential Resources. Luego vuelve a correr este menú.",
                "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("Escena de lanzamiento",
                "No existe " + ScenePath + ". Corre primero SpaceApps > Crear escenas greybox.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (System.Array.IndexOf(Generated, go.name) >= 0) Object.DestroyImmediate(go);
        }
        EnsureEventSystem(scene);

        // ---------- Luz y camaras ----------
        GameObject lightGo = new GameObject("Directional Light", typeof(Light));
        Light sun = lightGo.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(40f, -60f, 0f);

        Camera leftCam = CreateCamera("CamaraControlMision", new Rect(0f, 0f, 0.5f, 1f), PanelColor, 0f);
        leftCam.cullingMask = 0; // no dibuja objetos 3D; la UI de la izquierda va en el Canvas

        Camera rightCam = CreateCamera("CamaraCohete", new Rect(0.5f, 0f, 0.5f, 1f), Color.black, 1f);
        rightCam.gameObject.tag = "MainCamera";
        rightCam.gameObject.AddComponent<AudioListener>();
        rightCam.fieldOfView = 60f;
        rightCam.farClipPlane = 200f;
        rightCam.transform.position = new Vector3(7f, 14f, -14f);
        rightCam.transform.LookAt(new Vector3(7f, 0f, 0f));

        // ---------- Espacio 3D ----------
        Transform space = new GameObject("Espacio").transform;
        Transform earth = CreateBody(PrimitiveType.Sphere, "Tierra", space, Vector3.zero, Vector3.one * 3f, "Tierra", new Color(0.15f, 0.4f, 0.85f));
        Transform moon = CreateBody(PrimitiveType.Sphere, "Luna", space, new Vector3(14f, 0f, 0f), Vector3.one, "Luna", new Color(0.7f, 0.7f, 0.7f));
        Transform rocket = CreateBody(PrimitiveType.Capsule, "Cohete", space, new Vector3(0f, 0f, -1.6f), new Vector3(0.25f, 0.35f, 0.25f), "Cohete", new Color(1f, 0.55f, 0.15f));

        GameObject pathGo = new GameObject("Ruta", typeof(LineRenderer));
        pathGo.transform.SetParent(space, false);
        LineRenderer path = pathGo.GetComponent<LineRenderer>();
        path.useWorldSpace = true;
        path.widthMultiplier = 0.06f;
        path.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        path.receiveShadows = false;
        // La ruta usa URP/Unlit (si existe) para que se vea igual sin importar la luz
        path.sharedMaterial = MakeMaterial("Ruta", new Color(0.5f, 0.55f, 0.65f), rocket.GetComponent<Renderer>().sharedMaterial,
            Shader.Find("Universal Render Pipeline/Unlit"));

        // ---------- Canvas ----------
        GameObject canvasGo = new GameObject("CanvasLanzamiento", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        // Mitad izquierda: control de mision
        GameObject control = new GameObject("ControlMision", typeof(RectTransform), typeof(Image));
        control.transform.SetParent(root, false);
        RectTransform cr = (RectTransform)control.transform;
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = new Vector2(0.5f, 1f);
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        control.GetComponent<Image>().color = PanelColor;

        // Pantalla grande al fondo
        RectTransform frame = Box(control.transform, "PantallaGrande", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(880f, 500f), ScreenFrame, null);
        RectTransform screen = Box(frame, "Fondo", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ScreenColor, null);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = new Vector2(8f, 8f);
        screen.offsetMax = new Vector2(-8f, -8f);

        TextMeshProUGUI title = CreateText(screen, "Titulo", "CONTROL DE MISIÓN · ARTEMIS", 30, TextAlignmentOptions.Center, Color.white);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(820f, 45f));

        TextMeshProUGUI phase = CreateText(screen, "Fase", "FASE: SALIDA", 60, TextAlignmentOptions.Center, AccentColor);
        Place(phase.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(820f, 80f));

        TextMeshProUGUI data = CreateText(screen, "Datos", "", 34, TextAlignmentOptions.TopLeft, Color.white);
        Place(data.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(760f, 220f));

        RectTransform bar = Box(screen, "BarraProgreso", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(760f, 26f), new Color(0.15f, 0.18f, 0.25f), null);
        RectTransform fill = Box(bar, "Relleno", Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, AccentColor, null);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        TextMeshProUGUI test = CreateText(screen, "AvisoPrueba", "datos de prueba", 20, TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.4f));
        Place(test.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 8f), new Vector2(300f, 30f));

        // Bitacora entre la pantalla y los operadores
        TextMeshProUGUI logText = CreateText(control.transform, "Bitacora", "<b>Bitácora</b>", 26, TextAlignmentOptions.TopLeft, LogColor);
        Place(logText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 290f), new Vector2(860f, 250f));

        // Operadores en sus computadoras (vistos de espaldas, mirando la pantalla grande)
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        CreateOperator(control.transform, "VUELO", -330f, new Color(0.75f, 0.25f, 0.25f), circle);
        CreateOperator(control.transform, "TRAYECTORIA", -110f, new Color(0.25f, 0.55f, 0.3f), circle);
        CreateOperator(control.transform, "SISTEMAS", 110f, new Color(0.3f, 0.35f, 0.75f), circle);
        CreateOperator(control.transform, "CIENCIA", 330f, new Color(0.65f, 0.45f, 0.75f), circle);

        // Mitad derecha: titulo de la vista 3D y boton de prueba
        TextMeshProUGUI viewTitle = CreateText(root, "TituloVista3D", "Ruta: Tierra - Luna - Tierra", 32, TextAlignmentOptions.Center, Color.white);
        Place(viewTitle.rectTransform, new Vector2(0.75f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 50f));
        viewTitle.raycastTarget = false;

        Button skip = CreateButton(root, "BotonSaltarFase", "Saltar fase (prueba)", 28);
        Place((RectTransform)skip.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(320f, 70f));

        // Panel de fin de mision
        GameObject finish = new GameObject("PanelFin", typeof(RectTransform), typeof(Image));
        finish.transform.SetParent(root, false);
        RectTransform fr = (RectTransform)finish.transform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        finish.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform finishBox = Box(finish.transform, "Caja", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 420f), PanelColor, null);
        TextMeshProUGUI finishTitle = CreateText(finishBox, "Titulo", "¡Misión completada!", 60, TextAlignmentOptions.Center, AccentColor);
        Place(finishTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(820f, 90f));
        TextMeshProUGUI finishText = CreateText(finishBox, "Texto", "La tripulación regresó a salvo a la Tierra.", 34, TextAlignmentOptions.Center, Color.white);
        Place(finishText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(820f, 80f));
        Button results = CreateButton(finishBox, "BotonResultados", "Ver resultados", 44);
        Place((RectTransform)results.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(460f, 100f));

        // ---------- LaunchScreen ----------
        LaunchScreen launch = new GameObject("Lanzamiento").AddComponent<LaunchScreen>();
        launch.rocket = rocket;
        launch.earth = earth;
        launch.moon = moon;
        launch.pathLine = path;
        launch.phaseLabel = phase;
        launch.dataLabel = data;
        launch.logLabel = logText;
        launch.progressFill = fill;
        launch.finishPanel = finish;

        UnityEventTools.AddVoidPersistentListener(skip.onClick, launch.SkipPhase);
        UnityEventTools.AddVoidPersistentListener(results.onClick, launch.GoToResults);
        finish.SetActive(false);

        EditorUtility.SetDirty(launch);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Escena de lanzamiento armada en " + Scenes.Launch + ".");
    }

    // ---------- EventSystem ----------

    static void EnsureEventSystem(Scene scene)
    {
        EventSystem es = null;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            es = go.GetComponentInChildren<EventSystem>(true);
            if (es != null) break;
        }
        if (es == null)
        {
            es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        }
        StandaloneInputModule old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);
        InputSystemUIInputModule module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null)
        {
            module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }

    // ---------- 3D ----------

    static Camera CreateCamera(string name, Rect viewport, Color background, float depth)
    {
        Camera cam = new GameObject(name, typeof(Camera)).GetComponent<Camera>();
        cam.rect = viewport;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.depth = depth;
        return cam;
    }

    static Transform CreateBody(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, string materialName, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = MakeMaterial(materialName, color, r.sharedMaterial);
        return go.transform;
    }

    // Copia el material por defecto del pipeline (URP Lit) y le pone color (y otro shader si se pide). Se guarda como asset.
    static Material MakeMaterial(string name, Color color, Material template, Shader shader = null)
    {
        EnsureFolder(MaterialFolder);
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(template);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (shader != null) mat.shader = shader;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        string parent = folder.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
    }

    // ---------- UI ----------

    static void CreateOperator(Transform parent, string role, float x, Color shirt, Sprite circle)
    {
        GameObject go = new GameObject("Operador_" + role, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)go.transform;
        Place(r, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 30f), new Vector2(210f, 240f));

        Vector2 bottom = new Vector2(0.5f, 0f);
        RectTransform monitor = Box(r, "Monitor", bottom, bottom, new Vector2(45f, 95f), new Vector2(110f, 75f), new Color(0.2f, 0.2f, 0.22f), null);
        RectTransform glow = Box(monitor, "Pantalla", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, MonitorColor, null);
        glow.anchorMin = Vector2.zero;
        glow.anchorMax = Vector2.one;
        glow.offsetMin = new Vector2(6f, 6f);
        glow.offsetMax = new Vector2(-6f, -6f);
        Box(r, "Escritorio", bottom, bottom, new Vector2(0f, 60f), new Vector2(210f, 40f), DeskColor, null);
        Box(r, "Cuerpo", bottom, bottom, new Vector2(-35f, 0f), new Vector2(70f, 110f), shirt, null);
        Box(r, "Cabeza", bottom, bottom, new Vector2(-35f, 100f), new Vector2(56f, 56f), SkinColor, circle);

        TextMeshProUGUI label = CreateText(r, "Rol", role, 22, TextAlignmentOptions.Center, Color.white);
        Place(label.rectTransform, bottom, bottom, new Vector2(0f, 190f), new Vector2(210f, 34f));
    }

    static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null) img.sprite = sprite;
        RectTransform r = (RectTransform)go.transform;
        Place(r, anchor, pivot, position, size);
        return r;
    }

    static Button CreateButton(Transform parent, string name, string label, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = ButtonColor;
        TextMeshProUGUI t = CreateText(go.transform, "Texto", label, size, TextAlignmentOptions.Center, Color.white);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }
}
