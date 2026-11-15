using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Desk camera (01_Planeacion). Goes on the Main Camera.
/// Shared API: every station (PC, folder, globe...) and the navigation
/// between stations (task 2) must move the camera with FocusOn / ReturnToDesk /
/// SetDeskPose instead of touching the transform directly.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class DeskCamera : MonoBehaviour
{
    static DeskCamera instance;

    /// <summary>The scene's DeskCamera (searched for if it has not registered yet).</summary>
    public static DeskCamera Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<DeskCamera>();
            return instance;
        }
    }

    [Tooltip("Seconds it takes to go or come back.")]
    [SerializeField] float moveDuration = 0.6f;

    [Tooltip("Near clip while focused (up close the screen is less than 0.3 m away).")]
    [SerializeField] float focusNearClip = 0.01f;

    [Tooltip("Escape returns to the desk while focused.")]
    [SerializeField] bool escapeReturns = true;
    public bool EscapeReturns { get { return escapeReturns; } set { escapeReturns = value; } } // TutorialPopup turns it off while open

    [Header("Eventos")]
    public UnityEvent onFocused;   // arrived at the target
    public UnityEvent onReturned;  // arrived back at the desk

    /// <summary>Fires when the return STARTS (Back button, Escape or code).</summary>
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

    /// <summary>Changes the base pose ReturnToDesk goes back to (for task 2).</summary>
    public void SetDeskPose(Vector3 position, Quaternion rotation)
    {
        deskPosition = position;
        deskRotation = rotation;
    }

    /// <summary>Takes the current camera pose as the desk pose.</summary>
    public void SaveCurrentAsDeskPose() => SetDeskPose(transform.position, transform.rotation);

    /// <summary>Moves the camera smoothly to the target pose.</summary>
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

    /// <summary>Smoothly returns to the desk pose.</summary>
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