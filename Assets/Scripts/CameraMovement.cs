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

    [Header("Navigation Bounds")]
    [SerializeField] TileGridGenerator navigationGrid;
    [SerializeField] bool constrainHorizontalNavigation = true;
    [SerializeField] bool constrainVerticalNavigation = true;
    [SerializeField, Tooltip("Inset the navigation target by the camera's " +
        "projected viewport footprint on the dungeon plane.")]
    bool accountForViewportFootprint = true;
    [SerializeField, Tooltip("Additional world-space framing allowed " +
        "outside the playable grid on X and Y.")]
    Vector2 navigationEdgeMargin = new(0.5f, 0.5f);

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

        if (navigationGrid == null)
            navigationGrid = Object.FindAnyObjectByType<TileGridGenerator>();

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

    public static Vector2 CalculateClampedNavigationCenter(
        Vector2 desiredCenter,
        Rect playableBounds,
        Vector2 minimumViewportOffset,
        Vector2 maximumViewportOffset,
        Vector2 edgeMargin,
        bool constrainHorizontal,
        bool constrainVertical)
    {
        Vector2 result = desiredCenter;
        if (constrainHorizontal)
        {
            float boardMinimum = playableBounds.xMin - Mathf.Max(0f, edgeMargin.x);
            float boardMaximum = playableBounds.xMax + Mathf.Max(0f, edgeMargin.x);
            result.x = ClampAxisCenter(
                desiredCenter.x,
                boardMinimum,
                boardMaximum,
                minimumViewportOffset.x,
                maximumViewportOffset.x);
        }
        if (constrainVertical)
        {
            float boardMinimum = playableBounds.yMin - Mathf.Max(0f, edgeMargin.y);
            float boardMaximum = playableBounds.yMax + Mathf.Max(0f, edgeMargin.y);
            result.y = ClampAxisCenter(
                desiredCenter.y,
                boardMinimum,
                boardMaximum,
                minimumViewportOffset.y,
                maximumViewportOffset.y);
        }
        return result;
    }

    static float ClampAxisCenter(
        float desiredCenter,
        float boardMinimum,
        float boardMaximum,
        float minimumViewportOffset,
        float maximumViewportOffset)
    {
        float minimumCenter = boardMinimum - minimumViewportOffset;
        float maximumCenter = boardMaximum - maximumViewportOffset;
        if (minimumCenter <= maximumCenter)
            return Mathf.Clamp(desiredCenter, minimumCenter, maximumCenter);

        // The viewport is larger than the playable span. There is no interval
        // satisfying both edges, so use the one stable center that balances
        // the projected footprint around the authoritative board bounds.
        return (boardMinimum + boardMaximum -
            minimumViewportOffset - maximumViewportOffset) * 0.5f;
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

        ClampOrthographicNavigationTarget();

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

        ClampPerspectiveNavigationTarget();

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

    void ClampOrthographicNavigationTarget()
    {
        if (!TryResolveNavigationBounds(out Rect playableBounds) ||
            !TryGetPlaneIntersection(
                targetPosition, transform.forward, out Vector3 centerOnPlane))
            return;

        GetViewportOffsets(
            new Vector2(centerOnPlane.x, centerOnPlane.y),
            targetZoom,
            out Vector2 minimumOffset,
            out Vector2 maximumOffset);
        Vector2 clamped = CalculateClampedNavigationCenter(
            new Vector2(centerOnPlane.x, centerOnPlane.y),
            playableBounds,
            minimumOffset,
            maximumOffset,
            navigationEdgeMargin,
            constrainHorizontalNavigation,
            constrainVerticalNavigation);
        targetPosition.x += clamped.x - centerOnPlane.x;
        targetPosition.y += clamped.y - centerOnPlane.y;
    }

    void ClampPerspectiveNavigationTarget()
    {
        if (!TryResolveNavigationBounds(out Rect playableBounds))
            return;

        GetViewportOffsets(
            new Vector2(targetFocusPoint.x, targetFocusPoint.y),
            targetZoom,
            out Vector2 minimumOffset,
            out Vector2 maximumOffset);
        Vector2 clamped = CalculateClampedNavigationCenter(
            new Vector2(targetFocusPoint.x, targetFocusPoint.y),
            playableBounds,
            minimumOffset,
            maximumOffset,
            navigationEdgeMargin,
            constrainHorizontalNavigation,
            constrainVerticalNavigation);
        targetFocusPoint.x = clamped.x;
        targetFocusPoint.y = clamped.y;
    }

    bool TryResolveNavigationBounds(out Rect playableBounds)
    {
        playableBounds = default;
        if (!constrainHorizontalNavigation && !constrainVerticalNavigation)
            return false;
        if (navigationGrid == null)
            navigationGrid = Object.FindAnyObjectByType<TileGridGenerator>();
        return navigationGrid != null &&
            navigationGrid.TryGetPlayableWorldRect(out playableBounds);
    }

    void GetViewportOffsets(
        Vector2 center,
        float zoom,
        out Vector2 minimumOffset,
        out Vector2 maximumOffset)
    {
        minimumOffset = Vector2.zero;
        maximumOffset = Vector2.zero;
        if (!accountForViewportFootprint || camComponent == null)
            return;

        float aspect = Mathf.Max(0.0001f, camComponent.aspect);
        float halfHeight = camComponent.orthographic
            ? Mathf.Max(0.0001f, zoom)
            : Mathf.Tan(camComponent.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 centerOnPlane = new(center.x, center.y, zoomFocusPlaneZ);
        Vector3 cameraOrigin = centerOnPlane - transform.forward *
            Mathf.Max(0.01f, zoom);
        bool hasSample = false;

        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        {
            float normalizedX = x * 2f - 1f;
            float normalizedY = y * 2f - 1f;
            Vector3 rayOrigin;
            Vector3 rayDirection;
            if (camComponent.orthographic)
            {
                rayOrigin = cameraOrigin +
                    transform.right * (normalizedX * halfHeight * aspect) +
                    transform.up * (normalizedY * halfHeight);
                rayDirection = transform.forward;
            }
            else
            {
                rayOrigin = cameraOrigin;
                rayDirection = (
                    transform.forward +
                    transform.right * (normalizedX * halfHeight * aspect) +
                    transform.up * (normalizedY * halfHeight)).normalized;
            }

            if (!TryGetPlaneIntersection(
                    rayOrigin, rayDirection, out Vector3 intersection))
                continue;
            Vector2 offset = new(
                intersection.x - center.x,
                intersection.y - center.y);
            if (!hasSample)
            {
                minimumOffset = offset;
                maximumOffset = offset;
                hasSample = true;
            }
            else
            {
                minimumOffset = Vector2.Min(minimumOffset, offset);
                maximumOffset = Vector2.Max(maximumOffset, offset);
            }
        }

        if (!hasSample)
        {
            minimumOffset = Vector2.zero;
            maximumOffset = Vector2.zero;
        }
    }

    bool TryGetPlaneIntersection(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        out Vector3 intersection)
    {
        var plane = new Plane(
            Vector3.forward,
            new Vector3(0f, 0f, zoomFocusPlaneZ));
        var ray = new Ray(rayOrigin, rayDirection);
        if (plane.Raycast(ray, out float distance))
        {
            intersection = ray.GetPoint(distance);
            return true;
        }
        intersection = default;
        return false;
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
        navigationEdgeMargin = Vector2.Max(Vector2.zero, navigationEdgeMargin);
    }
}
