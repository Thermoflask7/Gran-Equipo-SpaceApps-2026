using UnityEngine;
using UnityEngine.EventSystems;

// Mouse input of the wiring board (task 43). Signed: claude_c.
// Created by WiringMinigame on the board area. Uses uGUI pointer events (works with InputSystemUIInputModule):
// left button press / drag / release draws a cable, right click erases the cable under the mouse.
public class WiringBoardInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public WiringMinigame owner;

    public void OnPointerDown(PointerEventData e)
    {
        if (owner == null) return;
        Vector2Int cell;
        if (!ToCell(e, out cell)) return;
        if (e.button == PointerEventData.InputButton.Right) owner.EraseAt(cell);
        else if (e.button == PointerEventData.InputButton.Left) owner.PressCell(cell);
    }

    public void OnDrag(PointerEventData e)
    {
        if (owner == null || e.button != PointerEventData.InputButton.Left) return;
        Vector2Int cell;
        if (ToCell(e, out cell)) owner.DragToCell(cell);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (owner != null && e.button == PointerEventData.InputButton.Left) owner.Release();
    }

    // Screen point -> board cell (x = column, y = row from the top). Points outside are clamped to the border.
    bool ToCell(PointerEventData e, out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        RectTransform rect = (RectTransform)transform;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out local)) return false;
        Rect r = rect.rect;
        int cols = Mathf.Max(1, owner.BoardWidth);
        int rows = Mathf.Max(1, owner.BoardHeight);
        int x = Mathf.FloorToInt((local.x - r.xMin) / r.width * cols);
        int y = Mathf.FloorToInt((r.yMax - local.y) / r.height * rows);
        cell = new Vector2Int(Mathf.Clamp(x, 0, cols - 1), Mathf.Clamp(y, 0, rows - 1));
        return true;
    }
}
