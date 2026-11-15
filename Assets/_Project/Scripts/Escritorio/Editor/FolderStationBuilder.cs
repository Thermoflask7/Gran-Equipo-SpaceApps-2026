using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Unity menu: SpaceApps > Desk > Add folder station  (task 24, folder part). Signed: claude_c.
// In the open scene (01_Planeacion):
//   1. Takes the selected object, or the object named "notepad" (the 3D folder on the desk).
//   2. Makes sure it has a collider (adds a BoxCollider if none of its children has one).
//   3. Adds the MissionFolder UI if the scene has none (SpaceApps > Mission folder > Add to open scene:
//      overlay canvas, starts inactive) and adds FolderStation to the 3D folder, linked to it.
// Then save the scene. It can be run again: it reuses what is already there.
public static class FolderStationBuilder
{
    const string FolderObjectName = "notepad";

    [MenuItem("SpaceApps/Desk/Add folder station")]
    static void AddFolderStation()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null || target.scene != SceneManager.GetActiveScene() || target.GetComponentInParent<Canvas>() != null)
            target = FindRoot(FolderObjectName);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Folder station",
                "No 3D folder found. Select the folder object on the desk (in 01_Planeacion it is called '" + FolderObjectName + "') and run this menu again.",
                "OK");
            return;
        }

        if (target.GetComponentInChildren<Collider>() == null)
        {
            Undo.AddComponent<BoxCollider>(target);
            Debug.Log("Folder station: added a BoxCollider to '" + target.name + "' so it can be clicked.");
        }

        MissionFolder folder = Object.FindAnyObjectByType<MissionFolder>(FindObjectsInactive.Include);
        if (folder == null)
        {
            EditorApplication.ExecuteMenuItem("SpaceApps/Mission folder/Add to open scene");
            folder = Object.FindAnyObjectByType<MissionFolder>(FindObjectsInactive.Include);
        }
        if (folder == null)
        {
            Debug.LogError("Folder station: could not add the MissionFolder UI. Run SpaceApps > Mission folder > Create prefab and try again.");
            return;
        }
        Undo.RecordObject(folder.gameObject, "Hide mission folder");
        folder.gameObject.SetActive(false); // opens only from the desk

        FolderStation station = target.GetComponent<FolderStation>();
        if (station == null) station = Undo.AddComponent<FolderStation>(target);
        Undo.RecordObject(station, "Link folder station");
        station.folder = folder;
        EditorUtility.SetDirty(station);

        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = target;
        Debug.Log("Folder station: '" + target.name + "' opens the mission folder. Save the scene.");
    }

    [MenuItem("SpaceApps/Desk/Add folder station", true)]
    static bool AddFolderStationValid()
    {
        return !Application.isPlaying;
    }

    static GameObject FindRoot(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.gameObject.scene == SceneManager.GetActiveScene() && t.name == name) return t.gameObject;
        return null;
    }
}
