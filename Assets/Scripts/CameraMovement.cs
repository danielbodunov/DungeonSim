using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform followTarget; // Reference to the player's Transform
    public float zPosition = 10f; // Fixed Z position for the camera
    public float panSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minZoom = 15f;
    public float maxZoom = 50f;
    public Camera camComponent;
    public float panSmoothTime = 0.12f;
    public float zoomSmoothTime = 0.08f;
    public bool panWithMiddleMouse = true;
    public float panMouseSensitivity = 0.02f;
    public bool invertMiddlePan = false;

    public InputManager inputManager;

    // smoothing state
    private Vector3 targetPosition;
    private Vector3 positionVelocity;
    private float targetZoom;
    private float zoomVelocity;
    private bool prevMiddlePressed = false;

    void Awake()
    {
        followTarget = this.transform; // Set the camera to follow itself by default

        targetPosition = transform.position;
        // find InputManager if not assigned
        if (inputManager == null)
            inputManager = InputManager.Instance ?? Object.FindFirstObjectByType<InputManager>();

        // initialize targetZoom if camera available
        if (camComponent == null)
            camComponent = GetComponent<Camera>() ?? Camera.main;

        if (camComponent != null)
            targetZoom = camComponent.orthographic ? camComponent.orthographicSize : camComponent.fieldOfView;
    }

    // InputManager handles enabling/disabling actions; nothing required here

    void Update()
    {
        Vector2 input = inputManager != null ? inputManager.Move : Vector2.zero;

        // Handle zoom via mouse scroll (Input System)
        if (camComponent == null)
            camComponent = GetComponent<Camera>() ?? Camera.main;

        if (inputManager != null && camComponent != null)
        {
            float scroll = inputManager.Scroll;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float change = scroll * zoomSpeed;
                if (camComponent.orthographic)
                {
                    targetZoom = Mathf.Clamp(targetZoom - change, minZoom, maxZoom);
                }
                else
                {
                    targetZoom = Mathf.Clamp(targetZoom - change * 10f, minZoom, maxZoom);
                }
            }
        }

        // Keyboard / gamepad panning
        bool isPanning = false;
        if (input.sqrMagnitude > 0.0001f)
        {
            Vector3 delta = new Vector3(-input.x, input.y, 0f) * panSpeed * Time.deltaTime;
            targetPosition += delta;
            isPanning = true;
        }

        // Middle-mouse drag panning; toggle inversion on click
        if (panWithMiddleMouse && inputManager != null)
        {
            bool middle = inputManager.MiddlePressed;
            // toggle invert on rising edge
            if (middle && !prevMiddlePressed)
            {
                invertMiddlePan = !invertMiddlePan;
            }
            prevMiddlePressed = middle;

            if (middle)
            {
                Vector2 mouseDelta = inputManager.MouseDelta;
                Vector3 delta = new Vector3(mouseDelta.x, -mouseDelta.y, 0f) * panMouseSensitivity;
                targetPosition += delta;
                isPanning = true;
            }
        }

        // If not actively panning, follow the followTarget's X position
        if (!isPanning && followTarget != null)
        {
            targetPosition.x = followTarget.position.x;
        }

        // Smoothly move camera to the target position and lock Z
        Vector3 smoothTarget = new Vector3(targetPosition.x, targetPosition.y, zPosition);
        transform.position = Vector3.SmoothDamp(transform.position, smoothTarget, ref positionVelocity, panSmoothTime);

        // Smooth zoom
        if (camComponent != null)
        {
            if (camComponent.orthographic)
            {
                camComponent.orthographicSize = Mathf.SmoothDamp(camComponent.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
            }
            else
            {
                camComponent.fieldOfView = Mathf.SmoothDamp(camComponent.fieldOfView, targetZoom, ref zoomVelocity, zoomSmoothTime);
            }
        }
    }
}