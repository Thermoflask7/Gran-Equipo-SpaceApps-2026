using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Small runtime UI helpers for the research minigame (task 14, claude_c).
// The two games draw their targets, Earth, antenna and envelopes with plain uGUI Images (circles and boxes),
// so the minigame works without any art. Everything is created under the shared play area and destroyed on reset.
public static class ResearchUI
{
    public static RectTransform Box(Transform parent, string name, Vector2 size, Vector2 position, Color color, Sprite sprite = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false; // clicks are read from the mouse, not from the images
        return rt;
    }

    public static RectTransform Circle(Transform parent, string name, float diameter, Vector2 position, Color color, Sprite circle)
    {
        return Box(parent, name, new Vector2(diameter, diameter), position, color, circle);
    }

    public static TMP_Text Label(Transform parent, string name, string text, float size, Vector2 position, Vector2 box, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = position;
        return t;
    }

    // Removes every child created at runtime
    public static void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--) Object.Destroy(parent.GetChild(i).gameObject);
    }

    // Mouse position in the local space of a RectTransform (works for overlay and camera canvases). False = no mouse.
    public static bool MouseLocal(RectTransform area, out Vector2 local)
    {
        local = Vector2.zero;
        Mouse mouse = Mouse.current;
        if (mouse == null || area == null) return false;
        Canvas canvas = area.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, mouse.position.ReadValue(), cam, out local);
    }

    public static bool Clicked()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    public static bool Held()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    // Cartoon alien made of circles: green head, two big eyes, two antennae. Pivot at the bottom of the head.
    public static RectTransform Alien(Transform parent, Sprite circle)
    {
        RectTransform root = Box(parent, "Alien", new Vector2(80f, 80f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
        Color green = new Color(0.45f, 0.9f, 0.35f);
        Box(root, "AntennaL", new Vector2(4f, 30f), new Vector2(-16f, 48f), green).localRotation = Quaternion.Euler(0f, 0f, 20f);
        Box(root, "AntennaR", new Vector2(4f, 30f), new Vector2(16f, 48f), green).localRotation = Quaternion.Euler(0f, 0f, -20f);
        Circle(root, "TipL", 12f, new Vector2(-22f, 62f), new Color(1f, 0.85f, 0.2f), circle);
        Circle(root, "TipR", 12f, new Vector2(22f, 62f), new Color(1f, 0.85f, 0.2f), circle);
        Circle(root, "Head", 72f, Vector2.zero, green, circle);
        Circle(root, "EyeL", 24f, new Vector2(-14f, 6f), Color.white, circle);
        Circle(root, "EyeR", 24f, new Vector2(14f, 6f), Color.white, circle);
        Circle(root, "PupilL", 10f, new Vector2(-12f, 4f), Color.black, circle);
        Circle(root, "PupilR", 10f, new Vector2(16f, 4f), Color.black, circle);
        Box(root, "Smile", new Vector2(22f, 4f), new Vector2(0f, -16f), new Color(0.15f, 0.35f, 0.1f));
        return root;
    }

    public static Color ColorOf(ResearchMath.TargetType type)
    {
        if (type == ResearchMath.TargetType.Spark) return new Color(0.75f, 1f, 0.3f);
        if (type == ResearchMath.TargetType.Ice) return new Color(0.4f, 0.8f, 1f);
        return new Color(0.75f, 0.72f, 0.68f);
    }
}
