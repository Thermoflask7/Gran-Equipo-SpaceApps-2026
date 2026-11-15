using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// An "app" on the PC desktop. Tasks 7 (SpaceMart), 8 and 21 (tech tree)
/// connect with ComputerDesktop.RegisterApp(id, ...) or by adding listeners to onOpen.
/// If an app has no listeners, the "Coming soon" panel is shown.
/// </summary>
[Serializable]
public class ComputerApp
{
    public string id;
    public string label;
    public Color color = Color.gray;
    public bool interactable = true;
    [TextArea] public string comingSoonMessage = "Coming soon";
    public UnityEvent onOpen = new UnityEvent();

    [NonSerialized] int runtimeListeners;

    public ComputerApp() { }

    public ComputerApp(string id, string label, Color color, bool interactable = true)
    {
        this.id = id;
        this.label = label;
        this.color = color;
        this.interactable = interactable;
    }

    /// <summary>Adds a listener from code (counted to know if the app is implemented).</summary>
    public void AddListener(UnityAction action)
    {
        if (action == null) return;
        onOpen.AddListener(action);
        runtimeListeners++;
    }

    public bool HasHandlers => runtimeListeners > 0 || onOpen.GetPersistentEventCount() > 0;
}