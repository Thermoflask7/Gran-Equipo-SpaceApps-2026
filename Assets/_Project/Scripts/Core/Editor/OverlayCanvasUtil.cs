using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Editor helper for the "Add to open scene" menus (audit F02). Signed: claude_c.
// Full-screen UI (mission folder, launch button, results, maintenance, SpaceMart test) must go on a
// Screen Space Overlay canvas. Never use FindAnyObjectByType<Canvas>(): in 01_Planeacion that can return
// PC_Desktop, the Screen Space Camera canvas that is rendered onto the PC monitor.
// GetOrCreate() returns the root canvas named "OverlayCanvas" (or any other root Screen Space Overlay canvas),
// and creates "OverlayCanvas" if the scene has none. It also makes sure there is an EventSystem
// with InputSystemUIInputModule.
public static class OverlayCanvasUtil
{
    public const string OverlayName = "OverlayCanvas";

    public static Canvas GetOrCreate()
    {
        Canvas found = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (c.gameObject.name == OverlayName) { found = c; break; }
            if (found == null) found = c;
        }

        if (found == null)
        {
            GameObject canvasGo = new GameObject(OverlayName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            found = canvasGo.GetComponent<Canvas>();
            found.renderMode = RenderMode.ScreenSpaceOverlay;
            found.sortingOrder = 10; // above any other overlay UI
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create overlay canvas");
        }

        EnsureEventSystem();
        return found;
    }

    public static void EnsureEventSystem()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            return;
        }
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            InputSystemUIInputModule module = Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
            module.AssignDefaultActions();
        }
    }
}
