using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Camara del escritorio (01_Planeacion). Va en la Main Camera.
/// API compartida: cualquier estacion (PC, folder, globo...) y la navegacion
/// entre estaciones (tarea 2) deben mover la camara con FocusOn / ReturnToDesk /
/// SetDeskPose en vez de tocar el transform directamente.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class DeskCamera : MonoBehaviour
{
    static DeskCamera instance;

    /// <summary>La DeskCamera de la escena (se busca si no se ha registrado).</summary>
    public static DeskCamera Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<DeskCamera>();
            return instance;
        }
    }

    [Tooltip("Segundos que tarda en ir o regresar.")]
    [SerializeField] float moveDuration = 0.6f;

    [Tooltip("Near clip mientras esta enfocada (de cerca la pantalla queda a menos de 0.3 m).")]
    [SerializeField] float focusNearClip = 0.01f;

    [Tooltip("Escape regresa al escritorio cuando esta enfocada.")]
    [SerializeField] bool escapeReturns = true;

    [Header("Eventos")]
    public UnityEvent onFocused;   // llego al objetivo
    public UnityEvent onReturned;  // llego de vuelta al escritorio

    /// <summary>Se dispara al EMPEZAR a regresar (boton Volver, Escape o codigo).</summary>
    public event Action ReturnStarted;

    public bool IsFocused => CurrentTarget != null;
    public bool IsMoving { get; private set; }
    public Transform CurrentTarget { get; private set; }

    Camera cam;
    Vector3 deskPosition;
    Quaternion deskRotation;
    float deskNearClip;
    Coroutine moveRoutine;

    void Awake()
    {
        if (instance == null) instance = this;
        cam = GetComponent<Camera>();
        deskNearClip = cam.nearClipPlane;
        SaveCurrentAsDeskPose();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (!escapeReturns || !IsFocused || IsMoving) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ReturnToDesk();
    }

    /// <summary>Cambia la pose base a la que regresa ReturnToDesk (para la tarea 2).</summary>
    public void SetDeskPose(Vector3 position, Quaternion rotation)
    {
        deskPosition = position;
        deskRotation = rotation;
    }

    /// <summary>Toma la pose actual de la camara como pose del escritorio.</summary>
    public void SaveCurrentAsDeskPose() => SetDeskPose(transform.position, transform.rotation);

    /// <summary>Mueve la camara suavemente hasta la pose de target.</summary>
    public void FocusOn(Transform target, Action onArrived = null)
    {
        if (target == null) return;
        CurrentTarget = target;
        cam.nearClipPlane = Mathf.Min(deskNearClip, focusNearClip);
        StartMove(target.position, target.rotation, () =>
        {
            onFocused?.Invoke();
            onArrived?.Invoke();
        });
    }

    /// <summary>Regresa suavemente a la pose del escritorio.</summary>
    public void ReturnToDesk(Action onArrived = null)
    {
        if (!IsFocused && !IsMoving) { onArrived?.Invoke(); return; }
        CurrentTarget = null;
        ReturnStarted?.Invoke();
        StartMove(deskPosition, deskRotation, () =>
        {
            cam.nearClipPlane = deskNearClip;
            onReturned?.Invoke();
            onArrived?.Invoke();
        });
    }

    void StartMove(Vector3 position, Quaternion rotation, Action onDone)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(position, rotation, onDone));
    }

    IEnumerator MoveRoutine(Vector3 toPos, Quaternion toRot, Action onDone)
    {
        IsMoving = true;
        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        float t = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k));
            yield return null;
        }
        transform.SetPositionAndRotation(toPos, toRot);
        IsMoving = false;
        moveRoutine = null;
        onDone?.Invoke();
    }
}