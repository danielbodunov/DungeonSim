using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform followTarget;
    public float panSpeed = 5f;
    public float zoomSpeed = 2f;

    [Tooltip("For a perspective camera, this is the closest dolly distance from the focus plane.")]
    public float minZoom = 3f;

    [Tooltip("For a perspective camera, this is the farthest dolly distance from the focus plane.")]
    public float maxZoom = 30f;

    [Tooltip("World-space Z plane that perspective zoom keeps centered.")]
    public float zoomFocusPlaneZ = 0f;

    public Camera camComponent;
    public float panSmoothTime = 0.12f;
    public float zoomSmoothTime = 0.08f;
    public bool panWithMiddleMouse = true;
    public float panMouseSensitivity = 0.02f;
    [Tooltip("Reverse the default grab-style middle-mouse pan direction.")]
    public bool invertMiddlePan = false;

    [Header("Target Focus")]
    [SerializeField, Min(0.01f), Tooltip("Orthographic size or perspective dolly distance used while focusing a target.")]
    float focusZoom = 5f;
    [SerializeField, Tooltip("World-space offset applied to the target's position when framing it.")]
    Vector3 focusOffset = new(0f, 0.5f, 0f);
    [SerializeField, Min(0.001f), Tooltip("Smoothing time used while transitioning to and following a focus target.")]
    float focusSmoothTime = 0.2f;
    [SerializeField, Tooltip("Keyboard pan or middle-mouse drag releases target focus. Wheel zoom remains available while focused.")]
    bool manualPanCancelsFocus = true;

    public InputManager inputManager;

    private Vector3 targetPosition;
    private Vector3 positionVelocity;
    private float targetZoom;
    private float zoomVelocity;
    // A perspective camera zooms by moving along its forward axis. Keeping a
    // separate focus point lets panning and dolly movement remain independent.
    private bool perspectiveZoomInitialized;
    private Vector3 currentFocusPoint;
    private Vector3 targetFocusPoint;
    private Vector3 focusVelocity;
    private float currentZoomDistance;
    private Transform focusTarget;
    private bool focusActive;

    public Transform FocusedTarget => HasFocus ? focusTarget : null;
    public bool HasFocus => focusActive && focusTarget != null;
    public float FocusZoom => focusZoom;
    public Vector3 FocusOffset => focusOffset;
    public bool ManualPanCancelsFocus => manualPanCancelsFocus;

    public event System.Action<Transform> FocusChanged;

    void Awake()
    {
        targetPosition = transform.position;

        if (inputManager == null)
            inputManager = InputManager.Instance ?? Object.FindAnyObjectByType<InputManager>();

        if (camComponent == null)
            camComponent = GetComponent<Camera>() ?? Camera.main;

        if (camComponent == null)
            return;

        if (camComponent.orthographic)
            targetZoom = camComponent.orthographicSize;
        else
            InitializePerspectiveZoom();
    }

    void Update()
    {
        if (camComponent == null)
            camComponent = GetComponent<Camera>() ?? Camera.main;

        if (camComponent == null)
            return;

        if (!camComponent.orthographic && !perspectiveZoomInitialized)
            InitializePerspectiveZoom();

        if (focusActive && focusTarget == null)
            ClearFocus();

        Vector2 input = inputManager != null ? inputManager.Move : Vector2.zero;
        Vector2 mouseDelta = Vector2.zero;
        bool middle = false;
        if (panWithMiddleMouse && inputManager != null)
        {
            middle = inputManager.MiddlePressed;
            mouseDelta = inputManager.MouseDelta;
        }

        bool hasManualInput = input.sqrMagnitude > 0.0001f ||
            (middle && mouseDelta.sqrMagnitude > 0.0001f);
        if (HasFocus && manualPanCancelsFocus && hasManualInput)
            ClearFocus();

        HandleZoomInput();

        Vector3 panDelta = Vector3.zero;
        bool isPanning = false;

        if (input.sqrMagnitude > 0.0001f)
        {
            panDelta += new Vector3(input.x, input.y, 0f) * panSpeed * Time.deltaTime;
            isPanning = true;
        }

        if (panWithMiddleMouse && inputManager != null)
        {
            if (middle)
            {
                panDelta += CalculateMiddleMousePanDelta(
                    mouseDelta, panMouseSensitivity, invertMiddlePan);
                isPanning = true;
            }
        }

        if (camComponent.orthographic)
            UpdateOrthographicCamera(panDelta, isPanning);
        else
            UpdatePerspectiveCamera(panDelta, isPanning);
    }

    public static Vector3 CalculateMiddleMousePanDelta(
        Vector2 mouseDelta,
        float sensitivity,
        bool invert)
    {
        Vector2 grabDelta = invert ? mouseDelta : -mouseDelta;
        return new Vector3(grabDelta.x, grabDelta.y, 0f) * sensitivity;
    }

    /// <summary>Begins smoothly framing and following a generic world target.</summary>
    public bool FocusTarget(Transform target)
    {
        if (target == null || target == transform)
        {
            ClearFocus();
            return false;
        }

        if (camComponent == null)
            camComponent = GetComponent<Camera>() ?? Camera.main;
        if (camComponent == null)
            return false;

        if (!camComponent.orthographic && !perspectiveZoomInitialized)
            InitializePerspectiveZoom();

        focusTarget = target;
        focusActive = true;
        targetZoom = Mathf.Clamp(focusZoom, minZoom, maxZoom);
        UpdateFocusedFramingTarget();
        FocusChanged?.Invoke(focusTarget);
        return true;
    }

    /// <summary>Releases target tracking and leaves normal control at the current view.</summary>
    public void ClearFocus()
    {
        if (!focusActive)
            return;

        focusActive = false;
        focusTarget = null;
        HoldCurrentViewForManualControl();
        FocusChanged?.Invoke(null);
    }

    private void HandleZoomInput()
    {
        if (inputManager == null)
            return;

        float scroll = inputManager.Scroll;
        if (Mathf.Abs(scroll) <= 0.0001f)
            return;

        // Mouse wheels commonly report 120 per notch, while other devices and
        // bindings report approximately 1. Normalize both into useful steps.
        float scrollSteps = Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
        float change = scrollSteps * zoomSpeed;

        if (camComponent.orthographic)
            targetZoom = Mathf.Clamp(targetZoom - change, minZoom, maxZoom);
        else
            targetZoom = Mathf.Clamp(targetZoom - change, minZoom, maxZoom);
    }

    private void UpdateOrthographicCamera(Vector3 panDelta, bool isPanning)
    {
        targetPosition += panDelta;

        if (HasFocus)
        {
            Vector3 framingPoint = focusTarget.position + focusOffset;
            targetPosition.x = framingPoint.x;
            targetPosition.y = framingPoint.y;
        }
        else if (!isPanning && followTarget != null && followTarget != transform)
            targetPosition.x = followTarget.position.x;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            HasFocus ? focusSmoothTime : panSmoothTime);

        camComponent.orthographicSize = Mathf.SmoothDamp(
            camComponent.orthographicSize,
            targetZoom,
            ref zoomVelocity,
            HasFocus ? focusSmoothTime : zoomSmoothTime);
    }

    private void UpdatePerspectiveCamera(Vector3 panDelta, bool isPanning)
    {
        targetFocusPoint += panDelta;

        if (HasFocus)
            targetFocusPoint = focusTarget.position + focusOffset;
        else if (!isPanning && followTarget != null && followTarget != transform)
            targetFocusPoint.x = followTarget.position.x;

        currentFocusPoint = Vector3.SmoothDamp(
            currentFocusPoint,
            targetFocusPoint,
            ref focusVelocity,
            HasFocus ? focusSmoothTime : panSmoothTime);

        currentZoomDistance = Mathf.SmoothDamp(
            currentZoomDistance,
            targetZoom,
            ref zoomVelocity,
            HasFocus ? focusSmoothTime : zoomSmoothTime);

        // Leave fieldOfView untouched: perspective zoom is entirely camera motion.
        transform.position = currentFocusPoint - transform.forward * currentZoomDistance;
    }

    private void InitializePerspectiveZoom()
    {
        Plane focusPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, zoomFocusPlaneZ));
        Ray viewRay = new Ray(transform.position, transform.forward);

        if (focusPlane.Raycast(viewRay, out float distance) && distance > 0f)
        {
            currentFocusPoint = viewRay.GetPoint(distance);
            currentZoomDistance = distance;
        }
        else
        {
            currentZoomDistance = Mathf.Clamp(10f, minZoom, maxZoom);
            currentFocusPoint = transform.position + transform.forward * currentZoomDistance;
        }

        targetFocusPoint = currentFocusPoint;
        targetZoom = Mathf.Clamp(currentZoomDistance, minZoom, maxZoom);
        perspectiveZoomInitialized = true;
    }

    private void UpdateFocusedFramingTarget()
    {
        if (!HasFocus)
            return;

        Vector3 framingPoint = focusTarget.position + focusOffset;
        if (camComponent.orthographic)
        {
            targetPosition.x = framingPoint.x;
            targetPosition.y = framingPoint.y;
        }
        else
        {
            targetFocusPoint = framingPoint;
        }
    }

    private void HoldCurrentViewForManualControl()
    {
        positionVelocity = Vector3.zero;
        focusVelocity = Vector3.zero;
        zoomVelocity = 0f;

        if (camComponent == null)
            return;

        if (camComponent.orthographic)
        {
            targetPosition = transform.position;
            targetZoom = camComponent.orthographicSize;
            return;
        }

        if (!perspectiveZoomInitialized)
            InitializePerspectiveZoom();
        targetFocusPoint = currentFocusPoint;
        targetZoom = currentZoomDistance;
    }

    void OnValidate()
    {
        minZoom = Mathf.Max(0.01f, minZoom);
        maxZoom = Mathf.Max(minZoom, maxZoom);
        panSmoothTime = Mathf.Max(0.001f, panSmoothTime);
        zoomSmoothTime = Mathf.Max(0.001f, zoomSmoothTime);
        focusZoom = Mathf.Clamp(focusZoom, minZoom, maxZoom);
        focusSmoothTime = Mathf.Max(0.001f, focusSmoothTime);
    }
}
