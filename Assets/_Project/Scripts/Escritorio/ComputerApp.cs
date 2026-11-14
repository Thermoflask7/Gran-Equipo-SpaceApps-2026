using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Una "app" del escritorio de la PC. Las tareas 7 (SpaceMart), 8 y 21 (tech tree)
/// se conectan con ComputerDesktop.RegisterApp(id, ...) o agregando listeners a onOpen.
/// Si una app no tiene ningun listener, se muestra el panel "Proximamente".
/// </summary>
[Serializable]
public class ComputerApp
{
    public string id;
    public string label;
    public Color color = Color.gray;
    public bool interactable = true;
    [TextArea] public string comingSoonMessage = "Proximamente";
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

    /// <summary>Agrega un listener desde codigo (cuenta para saber si la app ya esta implementada).</summary>
    public void AddListener(UnityAction action)
    {
        if (action == null) return;
        onOpen.AddListener(action);
        runtimeListeners++;
    }

    public bool HasHandlers => runtimeListeners > 0 || onOpen.GetPersistentEventCount() > 0;
}