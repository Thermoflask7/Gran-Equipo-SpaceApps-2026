using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Casts a ray from the desk camera to the curved mesh and maps
/// the UV to Canvas (RenderTexture) coordinates so buttons get hover and clicks.
/// Applies the same barrel distortion as the CRT shader, if it is on.
/// </summary>
public class SpaceOsGraphicRaycaster : GraphicRaycaster
{
    [SerializeField] MeshCollider screenCollider;
    [SerializeField] Camera worldCamera;
    [SerializeField] ComputerDesktop desktop;
    [SerializeField] SpaceOsScreen screen;
    [SerializeField] float maxDistance = 50f;

    public void SetScreenCollider(MeshCollider value) => screenCollider = value;
    public void SetWorldCamera(Camera value) => worldCamera = value;
    public void SetDesktop(ComputerDesktop value) => desktop = value;
    public void SetScreen(SpaceOsScreen value) => screen = value;

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (desktop != null && !desktop.IsInteractive) return;
        // A popup over the screen (quit dialog) owns the clicks: remapping eventData.position here
        // would also shift the clicks of that overlay canvas.
        if (QuitToMenuDialog.IsOpen) return;
        if (screenCollider == null || !screenCollider.enabled) return;

        Camera world = worldCamera;
        if (world == null)
        {
            var dc = DeskCamera.Instance;
            world = dc != null ? dc.GetComponent<Camera>() : Camera.main;
        }
        if (world == null) return;

        Ray ray = world.ScreenPointToRay(eventData.position);
        if (!screenCollider.Raycast(ray, out RaycastHit hit, maxDistance)) return;

        Camera ui = eventCamera;
        if (ui == null) return;
        int pw = ui.pixelWidth;
        int ph = ui.pixelHeight;
        if (pw <= 0 || ph <= 0) return;

        Vector2 uv = hit.textureCoord;
        if (screen != null) uv = screen.MeshUvToCanvasUv(uv);

        // We keep the remapped position: ScrollRect (wheel and drag) and the
        // buttons use the canvas/RT coordinates, not the Game view ones.
        eventData.position = new Vector2(uv.x * pw, uv.y * ph);
        base.Raycast(eventData, resultAppendList);
    }
}