using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public readonly struct GameplayInputOwnership
{
    public static GameplayInputOwnership Full => new(true, true);
    public static GameplayInputOwnership None => new(false, false);

    public bool PointerInputOwned { get; }
    public bool KeyboardInputOwned { get; }

    public GameplayInputOwnership(
        bool pointerInputOwned,
        bool keyboardInputOwned)
    {
        PointerInputOwned = pointerInputOwned;
        KeyboardInputOwned = keyboardInputOwned;
    }
}

[DefaultExecutionOrder(-1000)]
public class InputManager : MonoBehaviour
{
    [SerializeField]
    private Camera sceneCamera;
    private Vector3 lastPosition;

    [SerializeField]
    private LayerMask placementLayerMask;

    public event Action OnClicked, OnRightClicked, OnExit;
    public static InputManager Instance { get; private set; }
    static Func<GameplayInputOwnership> inputOwnershipResolver;

    public float Scroll { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public Vector2 Move { get; private set; }
    public bool MiddlePressed { get; private set; }
    public bool LeftClick { get; private set; }
    public bool EscapePressed { get; private set; }
    public bool PointerInputOwned { get; private set; } = true;
    public bool KeyboardInputOwned { get; private set; } = true;

    private InputAction clickLeftAction;
    private InputAction clickRightAction;
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

        clickRightAction = new InputAction("RightClick", InputActionType.Button);
        clickRightAction.AddBinding("<Mouse>/rightButton");

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
        clickRightAction?.Enable();
        moveAction?.Enable();
        zoomAction?.Enable();
        mouseDeltaAction?.Enable();
        middleButtonAction?.Enable();
        escapeAction?.Enable();

        clickLeftAction.performed += OnClickPerformed;
        clickRightAction.performed += OnRightClickPerformed;
        escapeAction.performed += OnEscapePerformed;
    }

    void OnDisable()
    {
        clickLeftAction?.Disable();
        clickRightAction?.Disable();
        moveAction?.Disable();
        zoomAction?.Disable();
        mouseDeltaAction?.Disable();
        middleButtonAction?.Disable();
        escapeAction?.Disable();

        clickLeftAction.performed -= OnClickPerformed;
        clickRightAction.performed -= OnRightClickPerformed;
        escapeAction.performed -= OnEscapePerformed;
        ClearInputState();
    }

    void Update()
    {
        GameplayInputOwnership ownership = ResolveInputOwnership();
        PointerInputOwned = ownership.PointerInputOwned;
        KeyboardInputOwned = ownership.KeyboardInputOwned;

        // Use Input System action values and callback events for discrete events
        LeftClick = PointerInputOwned && clickLeftAction != null &&
            clickLeftAction.ReadValue<float>() > 0.5f;
        Move = KeyboardInputOwned && moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : Vector2.zero;
        Scroll = PointerInputOwned && zoomAction != null
            ? zoomAction.ReadValue<float>()
            : 0f;
        MouseDelta = PointerInputOwned && mouseDeltaAction != null
            ? mouseDeltaAction.ReadValue<Vector2>()
            : Vector2.zero;
        MiddlePressed = PointerInputOwned && middleButtonAction != null &&
            middleButtonAction.ReadValue<float>() > 0.5f;
        EscapePressed = KeyboardInputOwned && escapeAction != null &&
            escapeAction.WasPressedThisFrame();
    }

    void OnDestroy()
    {
        clickLeftAction?.Dispose();
        clickRightAction?.Dispose();
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

    /// <summary>
    /// Installs the environment-specific authority used to decide which input
    /// surface owns gameplay pointer and keyboard actions.
    /// </summary>
    public static void RegisterInputOwnershipResolver(
        Func<GameplayInputOwnership> resolver)
    {
        inputOwnershipResolver = resolver;
    }

    public static void UnregisterInputOwnershipResolver(
        Func<GameplayInputOwnership> resolver)
    {
        if (inputOwnershipResolver == resolver)
            inputOwnershipResolver = null;
    }

    static GameplayInputOwnership ResolveInputOwnership()
    {
        return inputOwnershipResolver != null
            ? inputOwnershipResolver.Invoke()
            : GameplayInputOwnership.Full;
    }

    void ClearInputState()
    {
        Scroll = 0f;
        MouseDelta = Vector2.zero;
        Move = Vector2.zero;
        MiddlePressed = false;
        LeftClick = false;
        EscapePressed = false;
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
        if (ResolveInputOwnership().PointerInputOwned)
            OnClicked?.Invoke();
    }

    private void OnRightClickPerformed(InputAction.CallbackContext ctx)
    {
        if (ResolveInputOwnership().PointerInputOwned)
            OnRightClicked?.Invoke();
    }

    private void OnEscapePerformed(InputAction.CallbackContext ctx)
    {
        if (ResolveInputOwnership().KeyboardInputOwned)
            OnExit?.Invoke();
    }
}
