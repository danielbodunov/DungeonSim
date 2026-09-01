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

    [SerializeField, Tooltip("World-space Z plane used for gameplay grid selection.")]
    float placementPlaneZ;

    public event Action OnClicked, OnRightClicked, OnExit;
    public static InputManager Instance { get; private set; }
    static Func<GameplayInputOwnership> inputOwnershipResolver;

    public float Scroll { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public Vector2 Move { get; private set; }
    public bool MiddlePressed { get; private set; }
    public bool LeftClick { get; private set; }
    public bool EscapePressed { get; private set; }
    public bool TrapCandidateCyclePressed { get; private set; }
    public bool PointerInputOwned { get; private set; } = true;
    public bool KeyboardInputOwned { get; private set; } = true;

    private InputAction clickLeftAction;
    private InputAction clickRightAction;
    private InputAction moveAction;
    private InputAction zoomAction;
    private InputAction mouseDeltaAction;
    private InputAction middleButtonAction;
    private InputAction escapeAction;
    private InputAction trapCandidateCycleAction;

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
        trapCandidateCycleAction = new InputAction(
            "CycleTrapCandidate", InputActionType.Button, "<Keyboard>/r");
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
        trapCandidateCycleAction?.Enable();

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
        trapCandidateCycleAction?.Disable();

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
        TrapCandidateCyclePressed = KeyboardInputOwned &&
            trapCandidateCycleAction != null &&
            trapCandidateCycleAction.WasPressedThisFrame();
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
        trapCandidateCycleAction?.Dispose();
    }

    public bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    public bool TryGetPointerRay(out Ray ray)
    {
        ray = default;
        if (!ResolveInputOwnership().PointerInputOwned ||
            sceneCamera == null || Mouse.current == null)
        {
            return false;
        }

        ray = sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        return true;
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
        TrapCandidateCyclePressed = false;
    }

    public Vector3 GetSelectedMapPosition()
    {
        if (TryGetPointerRay(out Ray ray) &&
            TryGetMapPlanePosition(ray, placementPlaneZ, out Vector3 position))
            lastPosition = position;
        return lastPosition;
    }

    public static bool TryGetMapPlanePosition(
        Ray ray,
        float planeZ,
        out Vector3 position)
    {
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
        if (plane.Raycast(ray, out float distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }
        position = default;
        return false;
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
