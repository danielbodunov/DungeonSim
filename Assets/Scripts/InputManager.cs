using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField]
    private Camera sceneCamera;
    private Vector3 lastPosition;

    [SerializeField]
    private LayerMask placementLayerMask;

    public event Action OnClicked, OnExit;
    public static InputManager Instance { get; private set; }

    public float Scroll { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public Vector2 Move { get; private set; }
    public bool MiddlePressed { get; private set; }
    public bool LeftClick { get; private set; }
    public bool EscapePressed { get; private set; }

    private InputAction clickLeftAction;
    private InputAction moveAction;
    private InputAction zoomAction;
    private InputAction mouseDeltaAction;
    private InputAction middleButtonAction;
    private InputAction escapeAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        moveAction = new InputAction("Move", InputActionType.Value, "");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        clickLeftAction = new InputAction("Click", InputActionType.Button);
        clickLeftAction.AddBinding("<Mouse>/leftButton");

        zoomAction = new InputAction("Zoom", InputActionType.Value);
        zoomAction.AddBinding("<Mouse>/scroll/y");

        mouseDeltaAction = new InputAction("MouseDelta", InputActionType.Value);
        mouseDeltaAction.AddBinding("<Mouse>/delta");

        middleButtonAction = new InputAction("MiddleButton", InputActionType.Button);
        middleButtonAction.AddBinding("<Mouse>/middleButton");

        escapeAction = new InputAction("Escape", InputActionType.Button);
        escapeAction.AddBinding("<Keyboard>/escape");
    }

    void OnEnable()
    {
        clickLeftAction?.Enable();  
        moveAction?.Enable();
        zoomAction?.Enable();
        mouseDeltaAction?.Enable();
        middleButtonAction?.Enable();
        escapeAction?.Enable();

        clickLeftAction.performed += OnClickPerformed;
        escapeAction.performed += OnEscapePerformed;
    }

    void OnDisable()
    {
        clickLeftAction?.Disable();
        moveAction?.Disable();
        zoomAction?.Disable();
        mouseDeltaAction?.Disable();
        middleButtonAction?.Disable();
        escapeAction?.Disable();

        clickLeftAction.performed -= OnClickPerformed;
        escapeAction.performed -= OnEscapePerformed;
    }

    void Update()
    {
        // Use Input System action values and callback events for discrete events
        LeftClick = clickLeftAction != null && clickLeftAction.ReadValue<float>() > 0.5f;
        Move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Scroll = zoomAction != null ? zoomAction.ReadValue<float>() : 0f;
        MouseDelta = mouseDeltaAction != null ? mouseDeltaAction.ReadValue<Vector2>() : Vector2.zero;
        MiddlePressed = middleButtonAction != null && middleButtonAction.ReadValue<float>() > 0.5f;
        EscapePressed = escapeAction != null && escapeAction.ReadValue<float>() > 0.5f;
    }

    void OnDestroy()
    {
        clickLeftAction?.Dispose();
        moveAction?.Dispose();
        zoomAction?.Dispose();
        mouseDeltaAction?.Dispose();
        middleButtonAction?.Dispose();
        escapeAction?.Dispose();
    }

    public bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    public Vector3 GetSelectedMapPosition()
    {
        Vector3 mousePos =  UnityEngine.InputSystem.Mouse.current.position.ReadValue();;
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, placementLayerMask))
        {
            lastPosition = hit.point;
        }
        return lastPosition;
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        OnClicked?.Invoke();
    }

    private void OnEscapePerformed(InputAction.CallbackContext ctx)
    {
        OnExit?.Invoke();
    }
}
