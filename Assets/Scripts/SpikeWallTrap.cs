using System.Collections;
using UnityEngine;

public enum SpikeWallState
{
    Default,
    Triggered,
    Resetting
}

/// <summary>
/// A cell trap that challenges the first NPC to enter while it is ready, then
/// plays its extension and reset states before becoming available again.
/// </summary>
[DisallowMultipleComponent]
public class SpikeWallTrap : CellTrap
{
    [Header("Trap Settings")]
    [SerializeField, Min(0)] int damage = 3;
    [SerializeField, Min(0f)] float difficulty = 5f;
    [SerializeField] TrapDodgeSettings dodgeSettings = new();
    [SerializeField, Min(0f)] float cooldown = 5f;
    [SerializeField, Min(0f), Tooltip("Delay before damage, for matching spike contact in the animation.")]
    float damageDelay;
    [SerializeField, Min(0f), Tooltip("How long the spikes remain extended before resetting.")]
    float triggeredDuration = 0.35f;
    [SerializeField, Min(0f), Tooltip("Duration of the spike retraction animation.")]
    float resetDuration = 1.25f;

    [Header("Animation States")]
    [SerializeField] Animator animator;
    [SerializeField] string defaultState = "Default";
    [SerializeField] string triggeredState = "Triggered";
    [SerializeField] string resettingState = "Reseting";
    [SerializeField, Min(0f)] float transitionDuration = 0.05f;

    [Header("Prototype Visual")]
    [SerializeField, Tooltip("Creates a simple marker only when the prefab has no renderer yet.")]
    bool createPlaceholderVisual = true;

    bool isCycling;
    float cooldownRemaining;
    float animatorSpeedBeforePause = 1f;
    bool animatorPaused;

    public int Damage => damage;
    public float Difficulty => difficulty;
    public float Cooldown => cooldown;
    public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);
    public SpikeWallState State { get; private set; } = SpikeWallState.Default;
    public bool IsReady => !isCycling && cooldownRemaining <= 0f;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (createPlaceholderVisual && GetComponentInChildren<Renderer>() == null)
            CreatePlaceholderVisual();
    }

    void Start()
    {
        SetState(SpikeWallState.Default, defaultState);
    }

    void OnEnable()
    {
        DungeonSimulationState.PauseChanged += OnSimulationPauseChanged;
        OnSimulationPauseChanged(DungeonSimulationState.IsPaused);
    }

    void OnDisable()
    {
        DungeonSimulationState.PauseChanged -= OnSimulationPauseChanged;
        RestoreAnimatorSpeed();
    }

    void Update()
    {
        if (cooldownRemaining <= 0f)
            return;

        cooldownRemaining = Mathf.Max(
            0f,
            cooldownRemaining - DungeonSimulationState.DeltaTime);
    }

    public override void OnNpcEntered(NPCCharacter npc)
    {
        TryTrigger(npc);
    }

    public bool TryTrigger(NPCCharacter npc)
    {
        if (DungeonSimulationState.IsPaused || npc == null || npc.IsDead || !IsReady)
            return false;

        cooldownRemaining = cooldown;
        isCycling = true;
        StartCoroutine(RunCycle(npc));
        return true;
    }

    IEnumerator RunCycle(NPCCharacter target)
    {
        SetState(SpikeWallState.Triggered, triggeredState);

        float activeDuration = Mathf.Max(triggeredDuration, damageDelay);
        if (damageDelay > 0f)
            yield return DungeonSimulationState.WaitForSimulationSeconds(damageDelay);

        if (DungeonSimulationState.IsPaused)
            yield return DungeonSimulationState.WaitUntilRunning();
        if (target != null && !target.IsDead)
            NPCActionResolver.ResolveTrap(
                target, this, damage, difficulty, dodgeSettings);

        float remainingTriggeredTime = activeDuration - damageDelay;
        if (remainingTriggeredTime > 0f)
        {
            yield return DungeonSimulationState.WaitForSimulationSeconds(
                remainingTriggeredTime);
        }

        SetState(SpikeWallState.Resetting, resettingState);
        if (resetDuration > 0f)
            yield return DungeonSimulationState.WaitForSimulationSeconds(resetDuration);

        if (DungeonSimulationState.IsPaused)
            yield return DungeonSimulationState.WaitUntilRunning();
        SetState(SpikeWallState.Default, defaultState);

        while (cooldownRemaining > 0f)
            yield return null;
        isCycling = false;
    }

    void OnSimulationPauseChanged(bool paused)
    {
        if (animator == null)
            return;

        if (paused)
        {
            if (animatorPaused)
                return;
            animatorSpeedBeforePause = animator.speed;
            animator.speed = 0f;
            animatorPaused = true;
            return;
        }

        RestoreAnimatorSpeed();
    }

    void RestoreAnimatorSpeed()
    {
        if (!animatorPaused || animator == null)
            return;
        animator.speed = animatorSpeedBeforePause;
        animatorPaused = false;
    }

    void SetState(SpikeWallState newState, string animatorState)
    {
        State = newState;
        if (animator == null || animator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(animatorState))
            return;

        animator.CrossFadeInFixedTime(animatorState, transitionDuration, 0);
    }

    /// <summary>Sets the upgraded cooldown without interrupting an active cycle.</summary>
    public void SetCooldown(float seconds)
    {
        cooldown = Mathf.Max(0f, seconds);
    }

    public void SetDamage(int amount)
    {
        damage = Mathf.Max(0, amount);
    }

    public void SetDifficulty(float value)
    {
        difficulty = Mathf.Max(0f, value);
    }

    void CreatePlaceholderVisual()
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Placeholder Back Plate";
        plate.transform.SetParent(transform, false);
        plate.transform.localPosition = new Vector3(0f, 0.3f, -0.08f);
        plate.transform.localScale = new Vector3(0.8f, 0.16f, 0.08f);
        Destroy(plate.GetComponent<Collider>());

        Renderer plateRenderer = plate.GetComponent<Renderer>();
        if (plateRenderer != null)
            plateRenderer.material.color = new Color(0.24f, 0.18f, 0.16f);

        for (int i = 0; i < 3; i++)
        {
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = $"Placeholder Spike {i + 1}";
            spike.transform.SetParent(plate.transform, false);
            spike.transform.localPosition = new Vector3(-0.3f + i * 0.3f, -0.9f, -0.7f);
            spike.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            spike.transform.localScale = new Vector3(0.1f, 1.1f, 0.6f);
            Destroy(spike.GetComponent<Collider>());

            Renderer spikeRenderer = spike.GetComponent<Renderer>();
            if (spikeRenderer != null)
                spikeRenderer.material.color = new Color(0.68f, 0.7f, 0.73f);
        }
    }
}
