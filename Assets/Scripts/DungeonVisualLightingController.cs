using UnityEngine;

/// <summary>
/// Owns the dungeon's global baseline presentation brightness. This is separate
/// from authored atlas color and physical/local dungeon light sources.
/// </summary>
[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
public sealed class DungeonVisualLightingController : MonoBehaviour
{
    public const string ShaderPropertyName = "_GlobalLightIntensity";
    static readonly int GlobalLightIntensityId = Shader.PropertyToID(ShaderPropertyName);
    static readonly int GlobalLightInitializedId =
        Shader.PropertyToID("_DungeonGlobalLightInitialized");

    [Header("Phase Brightness")]
    [SerializeField, Range(0f, 1.5f)] float defaultBrightness = 1f;
    [SerializeField, Range(0f, 1.5f)] float expansionBrightness = 1f;
    [SerializeField, Range(0f, 1.5f)] float exploringBrightness = 0.55f;
    [SerializeField, Range(0f, 1.5f)] float debugBrightness = 1f;

    [Header("Transition")]
    [SerializeField, Min(0f)] float brightnessTransitionDuration = 0.3f;

    GameplayLoopController gameplayLoop;
    float transitionStart;
    float targetBrightness;
    float transitionElapsed;
    bool debugOverride;

    public static DungeonVisualLightingController Instance { get; private set; }
    public float CurrentBrightness { get; private set; }
    public bool DebugOverride => debugOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetGlobalBrightness()
    {
        Shader.SetGlobalFloat(GlobalLightInitializedId, 1f);
        Shader.SetGlobalFloat(GlobalLightIntensityId, 1f);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(this);
            return;
        }
        Instance = this;
        ResolveGameplayLoop();
        ApplyResolvedBrightness(true);
    }

    void OnEnable()
    {
        ResolveGameplayLoop();
        Subscribe();
        ApplyResolvedBrightness(true);
    }

    void Start()
    {
        ResolveGameplayLoop();
        Subscribe();
        ApplyResolvedBrightness(true);
    }

    void Update()
    {
        if (Mathf.Approximately(CurrentBrightness, targetBrightness))
            return;

        if (brightnessTransitionDuration <= 0f)
        {
            ApplyBrightness(targetBrightness);
            return;
        }

        transitionElapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(transitionElapsed / brightnessTransitionDuration);
        ApplyBrightness(Mathf.Lerp(transitionStart, targetBrightness, progress));
    }

    void OnDisable()
    {
        if (gameplayLoop != null)
            gameplayLoop.StateChanged -= OnGameplayStateChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        defaultBrightness = ClampBrightness(defaultBrightness);
        expansionBrightness = ClampBrightness(expansionBrightness);
        exploringBrightness = ClampBrightness(exploringBrightness);
        debugBrightness = ClampBrightness(debugBrightness);
        brightnessTransitionDuration = Mathf.Max(0f, brightnessTransitionDuration);
        if (Application.isPlaying)
            ApplyResolvedBrightness(false);
    }
#endif

    public void SetBrightness(float value)
    {
        float clamped = ClampBrightness(value);
        targetBrightness = clamped;
        transitionStart = clamped;
        transitionElapsed = brightnessTransitionDuration;
        ApplyBrightness(clamped);
    }

    public void SetDebugOverride(bool enabled)
    {
        if (debugOverride == enabled)
            return;
        debugOverride = enabled;
        DungeonLightingManager lightingManager =
            FindAnyObjectByType<DungeonLightingManager>();
        lightingManager?.SetDebugOverride(enabled);
        ApplyResolvedBrightness(false);
    }

    public void SetDebugBrightness(float value)
    {
        debugBrightness = ClampBrightness(value);
        if (debugOverride)
            ApplyResolvedBrightness(false);
    }

    void ResolveGameplayLoop()
    {
        if (gameplayLoop == null)
            gameplayLoop = GetComponent<GameplayLoopController>();
        if (gameplayLoop == null)
            gameplayLoop = GameplayLoopController.Instance ??
                FindAnyObjectByType<GameplayLoopController>();
    }

    void Subscribe()
    {
        if (gameplayLoop == null)
            return;
        gameplayLoop.StateChanged -= OnGameplayStateChanged;
        gameplayLoop.StateChanged += OnGameplayStateChanged;
    }

    void OnGameplayStateChanged()
    {
        ApplyResolvedBrightness(false);
    }

    void ApplyResolvedBrightness(bool immediate)
    {
        float resolved = debugOverride
            ? debugBrightness
            : gameplayLoop == null
                ? defaultBrightness
                : gameplayLoop.Phase == DungeonPhase.Expansion
                    ? expansionBrightness
                    : exploringBrightness;

        resolved = ClampBrightness(resolved);
        if (immediate || brightnessTransitionDuration <= 0f)
        {
            SetBrightness(resolved);
            return;
        }

        if (Mathf.Approximately(targetBrightness, resolved))
            return;
        transitionStart = CurrentBrightness;
        targetBrightness = resolved;
        transitionElapsed = 0f;
    }

    void ApplyBrightness(float value)
    {
        CurrentBrightness = ClampBrightness(value);
        Shader.SetGlobalFloat(GlobalLightInitializedId, 1f);
        Shader.SetGlobalFloat(GlobalLightIntensityId, CurrentBrightness);
    }

    static float ClampBrightness(float value) => Mathf.Clamp(value, 0f, 1.5f);
}
