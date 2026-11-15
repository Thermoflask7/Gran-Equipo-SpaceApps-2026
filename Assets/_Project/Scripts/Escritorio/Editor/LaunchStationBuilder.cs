using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Unity menu: SpaceApps > Desk > Add launch button station  (task 24, launch button part).
// In the open scene (01_Planeacion):
//   1. Uses the selected 3D object as the launch button, or creates a simple one ("LaunchButton3D":
//      dark base + red cap) on the desk in front of the desk camera. Move it wherever you like afterwards;
//      running the menu again never moves it.
//   2. Makes sure it has a collider so it can be clicked.
//   3. Adds the launch panel if the scene has none (SpaceApps > Launch button > Add to open scene: overlay
//      canvas), hides it, adds a Back button to it, and adds LaunchStation to the 3D button linked to it.
// Then save the scene. It can be run again: it reuses what is already there.
public static class LaunchStationBuilder
{
    const string ButtonName = "LaunchButton3D";
    const string MaterialFolder = "Assets/_Project/Art/Materiales/Escritorio";
    const string BaseMaterialPath = MaterialFolder + "/LaunchButton_Base.mat";
    const string CapMaterialPath = MaterialFolder + "/LaunchButton_Cap.mat";

    [MenuItem("SpaceApps/Desk/Add launch button station")]
    static void AddLaunchStation()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null || target.scene != SceneManager.GetActiveScene() || target.GetComponentInParent<Canvas>() != null)
            target = FindInScene(ButtonName);
        if (target == null) target = CreateButton();

        if (target.GetComponentInChildren<Collider>() == null)
        {
            Undo.AddComponent<BoxCollider>(target);
            Debug.Log("Launch station: added a BoxCollider to '" + target.name + "' so it can be clicked.");
        }

        LaunchButton button = Object.FindAnyObjectByType<LaunchButton>(FindObjectsInactive.Include);
        if (button == null)
        {
            EditorApplication.ExecuteMenuItem("SpaceApps/Launch button/Add to open scene");
            button = Object.FindAnyObjectByType<LaunchButton>(FindObjectsInactive.Include);
        }
        if (button == null)
        {
            Debug.LogError("Launch station: could not add the launch panel. Run SpaceApps > Launch button > Create prefab and try again.");
            return;
        }
        GameObject panel = button.transform.parent != null ? button.transform.parent.gameObject : button.gameObject;

        LaunchStation station = target.GetComponent<LaunchStation>();
        if (station == null) station = Undo.AddComponent<LaunchStation>(target);
        Undo.RecordObject(station, "Link launch station");
        station.panel = panel;
        EditorUtility.SetDirty(station);

        AddBackButton(panel, station);

        Undo.RecordObject(panel, "Hide launch panel");
        panel.SetActive(false); // opens only from the desk

        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = target;
        Debug.Log("Launch station: '" + target.name + "' opens the launch panel. Place it on the desk and save the scene.");
    }

    [MenuItem("SpaceApps/Desk/Add launch button station", true)]
    static bool AddLaunchStationValid()
    {
        return !Application.isPlaying;
    }

    // Simple big red button: dark base + red cap. Real-world size (about 14 cm wide).
    static GameObject CreateButton()
    {
        GameObject root = new GameObject(ButtonName);
        Undo.RegisterCreatedObjectUndo(root, "Create launch button");
        root.transform.position = DeskSpot();

        GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseGo.name = "Base";
        baseGo.transform.SetParent(root.transform, false);
        baseGo.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        baseGo.transform.localScale = new Vector3(0.14f, 0.015f, 0.14f);
        baseGo.GetComponent<Renderer>().sharedMaterial = GetMaterial(BaseMaterialPath, new Color(0.12f, 0.12f, 0.14f), 0.5f);

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "Cap";
        cap.transform.SetParent(root.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0.032f, 0f);
        cap.transform.localScale = new Vector3(0.09f, 0.04f, 0.09f);
        cap.GetComponent<Renderer>().sharedMaterial = GetMaterial(CapMaterialPath, new Color(0.85f, 0.1f, 0.08f), 0.7f);

        return root;
    }

    // On the desk surface, a bit to the right of the center of the desk camera's view
    static Vector3 DeskSpot()
    {
        Camera cam = null;
        DeskCamera dc = Object.FindAnyObjectByType<DeskCamera>();
        if (dc != null) cam = dc.GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector3.zero;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.65f, 0.3f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 20f)) return hit.point;
        return cam.transform.position + cam.transform.forward;
    }

    // Saved material asset (URP Lit), so the shader is included in the WebGL build
    static Material GetMaterial(string path, Color color, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // "Back" button in the top right corner of the launch panel -> LaunchStation.Close
    static void AddBackButton(GameObject panel, LaunchStation station)
    {
        Transform existing = panel.transform.Find("BackButton");
        Button back;
        if (existing != null) back = existing.GetComponent<Button>();
        else
        {
            GameObject go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Add back button");
            go.transform.SetParent(panel.transform, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(140f, 56f);
            Image img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.28f, 0.35f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI t = textGo.GetComponent<TextMeshProUGUI>();
            t.text = "Back";
            t.fontSize = 28;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            RectTransform trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            back = go.GetComponent<Button>();
        }
        if (back == null) return;

        // Replace any old link so running the menu again does not stack listeners
        for (int i = back.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            if (back.onClick.GetPersistentMethodName(i) == nameof(LaunchStation.Close))
                UnityEventTools.RemovePersistentListener(back.onClick, i);
        UnityEventTools.AddPersistentListener(back.onClick, station.Close);
        EditorUtility.SetDirty(back);
    }

    static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.gameObject.scene == SceneManager.GetActiveScene() && t.name == name) return t.gameObject;
        return null;
    }
}
