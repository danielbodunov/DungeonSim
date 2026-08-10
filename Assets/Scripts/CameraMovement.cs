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
    public bool invertMiddlePan = false;

    public InputManager inputManager;

    private Vector3 targetPosition;
    private Vector3 positionVelocity;
    private float targetZoom;
    private float zoomVelocity;
    private bool prevMiddlePressed;

    // A perspective camera zooms by moving along its forward axis. Keeping a
    // separate focus point lets panning and dolly movement remain independent.
    private bool perspectiveZoomInitialized;
    private Vector3 currentFocusPoint;
    private Vector3 targetFocusPoint;
    private Vector3 focusVelocity;
    private float currentZoomDistance;

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
        Vector2 input = inputManager != null ? inputManager.Move : Vector2.zero;

        if (camComponent == null)
            camComponent = GetComponent<Camera>() ?? Camera.main;

        if (camComponent == null)
            return;

        if (!camComponent.orthographic && !perspectiveZoomInitialized)
            InitializePerspectiveZoom();

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
            bool middle = inputManager.MiddlePressed;
            if (middle && !prevMiddlePressed)
                invertMiddlePan = !invertMiddlePan;

            prevMiddlePressed = middle;

            if (middle)
            {
                Vector2 mouseDelta = inputManager.MouseDelta;
                panDelta += new Vector3(mouseDelta.x, -mouseDelta.y, 0f) * panMouseSensitivity;
                isPanning = true;
            }
        }

        if (camComponent.orthographic)
            UpdateOrthographicCamera(panDelta, isPanning);
        else
            UpdatePerspectiveCamera(panDelta, isPanning);
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

        if (!isPanning && followTarget != null && followTarget != transform)
            targetPosition.x = followTarget.position.x;

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition, ref positionVelocity, panSmoothTime);

        camComponent.orthographicSize = Mathf.SmoothDamp(
            camComponent.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
    }

    private void UpdatePerspectiveCamera(Vector3 panDelta, bool isPanning)
    {
        targetFocusPoint += panDelta;

        if (!isPanning && followTarget != null && followTarget != transform)
            targetFocusPoint.x = followTarget.position.x;

        currentFocusPoint = Vector3.SmoothDamp(
            currentFocusPoint, targetFocusPoint, ref focusVelocity, panSmoothTime);

        currentZoomDistance = Mathf.SmoothDamp(
            currentZoomDistance, targetZoom, ref zoomVelocity, zoomSmoothTime);

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

    void OnValidate()
    {
        minZoom = Mathf.Max(0.01f, minZoom);
        maxZoom = Mathf.Max(minZoom, maxZoom);
        panSmoothTime = Mathf.Max(0.001f, panSmoothTime);
        zoomSmoothTime = Mathf.Max(0.001f, zoomSmoothTime);
    }
}
