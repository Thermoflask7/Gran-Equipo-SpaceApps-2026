using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lanza un rayo desde la camara del escritorio hacia la malla curva y traduce
/// el UV a coordenadas del Canvas (RenderTexture) para que los botones reciban hover y clic.
/// Aplica la misma barrel distortion del shader CRT, si esta activa.
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

        Vector2 mapped = new Vector2(uv.x * pw, uv.y * ph);
        Vector2 original = eventData.position;
        eventData.position = mapped;
        base.Raycast(eventData, resultAppendList);
        eventData.position = original;
    }
}