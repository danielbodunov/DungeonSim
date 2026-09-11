using System;
using UnityEngine;

public enum RaiderAbility
{
    Move,
    Jump,
    Land,
    Attack,
    Interact,
    Damage,
    Death
}

public enum RaiderAbilityRejection
{
    None,
    Unavailable,
    Dead,
    Airborne,
    Cooldown,
    NoTarget,
    OutOfRange,
    RejectedByTarget
}

public readonly struct RaiderAbilityResult
{
    public RaiderAbilityResult(
        RaiderAbility ability,
        bool accepted,
        RaiderAbilityRejection rejection,
        UnityEngine.Object target = null,
        int appliedDamage = 0)
    {
        Ability = ability;
        Accepted = accepted;
        Rejection = rejection;
        Target = target;
        AppliedDamage = appliedDamage;
    }

    public RaiderAbility Ability { get; }
    public bool Accepted { get; }
    public RaiderAbilityRejection Rejection { get; }
    public UnityEngine.Object Target { get; }
    public int AppliedDamage { get; }
}

/// <summary>
/// Implemented by world content that can resolve a shared raider interaction.
/// The target, rather than an input or AI driver, remains authoritative for the
/// interaction and any objective state it changes.
/// </summary>
public interface IRaiderInteractable
{
    bool CanInteract(RaiderAbilities raider);
    bool TryInteract(RaiderAbilities raider);
}

/// <summary>
/// Controller-independent prototype ability authority. Player input and future
/// NPC drivers submit requests here; physics, range, cooldown, damage, death,
/// interaction, and objective validity remain outside those drivers.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NPCCharacter), typeof(Rigidbody))]
public sealed class RaiderAbilities : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] float moveSpeed = 6f;
    [SerializeField, Min(0f)] float jumpSpeed = 7f;
    [SerializeField, Min(0.01f)] float groundProbeDistance = 0.15f;
    [SerializeField, Min(0.01f)] float groundProbeRadius = 0.2f;
    [SerializeField] LayerMask groundLayers = Physics.DefaultRaycastLayers;

    [Header("Attack")]
    [SerializeField, Min(0)] int attackDamage = 2;
    [SerializeField, Min(0.01f)] float attackRange = 1.5f;
    [SerializeField, Min(0f)] float attackCooldown = 0.4f;

    [Header("Interaction")]
    [SerializeField, Min(0.01f)] float interactionRange = 1.5f;

    readonly Collider[] overlapResults = new Collider[24];
    NPCCharacter character;
    Rigidbody body;
    float requestedMove;
    float nextAttackTime;
    bool grounded;
    [NonSerialized] internal Func<bool> groundProbeOverride = null;

    public NPCCharacter Character => character;
    public bool IsAlive => character != null && !character.IsDead;
    public bool IsGrounded => grounded;
    public bool CanMove => IsAlive && body != null;
    public bool CanJump => CanMove && grounded;
    public bool CanAttack => IsAlive && Time.time >= nextAttackTime;
    public bool CanInteract => IsAlive;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float InteractionRange => interactionRange;

    public event Action<RaiderAbilityResult> AbilityResolved;
    public event Action<RaiderAbilities> Fell;
    public event Action<RaiderAbilities> Landed;
    public event Action<RaiderAbilities, int> Damaged;
    public event Action<RaiderAbilities> Died;

    void Awake()
    {
        character = GetComponent<NPCCharacter>();
        body = GetComponent<Rigidbody>();
        grounded = ProbeGround();
    }

    void OnEnable()
    {
        if (character == null)
            character = GetComponent<NPCCharacter>();
        if (character != null)
        {
            character.Damaged -= OnCharacterDamaged;
            character.Died -= OnCharacterDied;
            character.Damaged += OnCharacterDamaged;
            character.Died += OnCharacterDied;
        }
    }

    void OnDisable()
    {
        if (character != null)
        {
            character.Damaged -= OnCharacterDamaged;
            character.Died -= OnCharacterDied;
        }
        requestedMove = 0f;
    }

    void FixedUpdate()
    {
        UpdateGroundState();
        if (!CanMove)
            return;

        Vector3 velocity = body.linearVelocity;
        velocity.x = requestedMove * moveSpeed;
        body.linearVelocity = velocity;
    }

    public RaiderAbilityResult RequestMove(float horizontal)
    {
        if (!CanMove)
            return ResolveRejected(RaiderAbility.Move,
                character != null && character.IsDead
                    ? RaiderAbilityRejection.Dead
                    : RaiderAbilityRejection.Unavailable);

        requestedMove = Mathf.Clamp(horizontal, -1f, 1f);
        return ResolveAccepted(RaiderAbility.Move);
    }

    public RaiderAbilityResult RequestJump()
    {
        UpdateGroundState();
        if (!IsAlive)
            return ResolveRejected(RaiderAbility.Jump, RaiderAbilityRejection.Dead);
        if (body == null)
            return ResolveRejected(RaiderAbility.Jump, RaiderAbilityRejection.Unavailable);
        if (!grounded)
            return ResolveRejected(RaiderAbility.Jump, RaiderAbilityRejection.Airborne);

        Vector3 velocity = body.linearVelocity;
        velocity.y = jumpSpeed;
        body.linearVelocity = velocity;
        SetGrounded(false);
        return ResolveAccepted(RaiderAbility.Jump);
    }

    public RaiderAbilityResult RequestAttack()
    {
        if (!IsAlive)
            return ResolveRejected(RaiderAbility.Attack, RaiderAbilityRejection.Dead);
        if (Time.time < nextAttackTime)
            return ResolveRejected(RaiderAbility.Attack, RaiderAbilityRejection.Cooldown);

        NPCCharacter target = FindNearestAttackTarget();
        if (target == null)
            return ResolveRejected(RaiderAbility.Attack, RaiderAbilityRejection.NoTarget);

        nextAttackTime = Time.time + attackCooldown;
        NPCActionResult damage = NPCActionResolver.ResolveDamage(
            target, this, attackDamage, target.transform.position);
        return ResolveAccepted(RaiderAbility.Attack, target, damage.AppliedDamage);
    }

    public RaiderAbilityResult RequestInteract()
    {
        if (!IsAlive)
            return ResolveRejected(RaiderAbility.Interact, RaiderAbilityRejection.Dead);

        MonoBehaviour target = FindNearestInteractionTarget(out IRaiderInteractable interaction);
        if (interaction == null)
            return ResolveRejected(RaiderAbility.Interact, RaiderAbilityRejection.NoTarget);
        if (!interaction.CanInteract(this))
            return ResolveRejected(RaiderAbility.Interact,
                RaiderAbilityRejection.RejectedByTarget, target);
        if (!interaction.TryInteract(this))
            return ResolveRejected(RaiderAbility.Interact,
                RaiderAbilityRejection.RejectedByTarget, target);
        return ResolveAccepted(RaiderAbility.Interact, target);
    }

    public RaiderAbilityResult ReceiveDamage(
        int damage,
        UnityEngine.Object source,
        Vector3 worldPosition)
    {
        if (!IsAlive)
            return ResolveRejected(RaiderAbility.Damage, RaiderAbilityRejection.Dead);

        NPCActionResult result = NPCActionResolver.ResolveDamage(
            character, source, damage, worldPosition);
        return ResolveAccepted(RaiderAbility.Damage, character, result.AppliedDamage);
    }

    public void ResetForAttempt(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        if (body == null)
            body = GetComponent<Rigidbody>();
        body.position = position;
        body.rotation = rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        requestedMove = 0f;
        nextAttackTime = 0f;
        character?.ResetVisitResources();
        grounded = ProbeGround();
    }

    NPCCharacter FindNearestAttackTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, attackRange, overlapResults,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        NPCCharacter nearest = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            NPCCharacter candidate = overlapResults[i] != null
                ? overlapResults[i].GetComponentInParent<NPCCharacter>()
                : null;
            if (candidate == null || candidate == character || candidate.IsDead)
                continue;
            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    MonoBehaviour FindNearestInteractionTarget(out IRaiderInteractable interaction)
    {
        interaction = null;
        MonoBehaviour nearest = null;
        float nearestDistance = float.PositiveInfinity;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, interactionRange, overlapResults,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            MonoBehaviour[] candidates = overlapResults[i] != null
                ? overlapResults[i].GetComponentsInParent<MonoBehaviour>(true)
                : Array.Empty<MonoBehaviour>();
            for (int c = 0; c < candidates.Length; c++)
            {
                if (!(candidates[c] is IRaiderInteractable candidate))
                    continue;
                float distance = (candidates[c].transform.position - transform.position)
                    .sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;
                nearest = candidates[c];
                interaction = candidate;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    void UpdateGroundState() => SetGrounded(ProbeGround());

    bool ProbeGround()
    {
        if (groundProbeOverride != null)
            return groundProbeOverride.Invoke();
        if (body == null)
            return false;
        float bottom = transform.position.y;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null && !colliders[i].isTrigger)
                bottom = Mathf.Min(bottom, colliders[i].bounds.min.y);
        Vector3 center = new(
            body.worldCenterOfMass.x,
            bottom - groundProbeDistance * 0.5f + 0.01f,
            body.worldCenterOfMass.z);
        Vector3 halfExtents = new(
            groundProbeRadius,
            groundProbeDistance * 0.5f + 0.02f,
            groundProbeRadius);
        int count = Physics.OverlapBoxNonAlloc(
            center, halfExtents, overlapResults, Quaternion.identity, groundLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (overlapResults[i] != null &&
                overlapResults[i].transform.root != transform.root)
                return true;
        return false;
    }

    void SetGrounded(bool value)
    {
        if (grounded == value)
            return;
        grounded = value;
        if (grounded)
        {
            Landed?.Invoke(this);
            AbilityResolved?.Invoke(new RaiderAbilityResult(
                RaiderAbility.Land, true, RaiderAbilityRejection.None));
        }
        else
            Fell?.Invoke(this);
    }

    RaiderAbilityResult ResolveAccepted(
        RaiderAbility ability,
        UnityEngine.Object target = null,
        int appliedDamage = 0)
    {
        var result = new RaiderAbilityResult(
            ability, true, RaiderAbilityRejection.None, target, appliedDamage);
        AbilityResolved?.Invoke(result);
        return result;
    }

    RaiderAbilityResult ResolveRejected(
        RaiderAbility ability,
        RaiderAbilityRejection rejection,
        UnityEngine.Object target = null)
    {
        var result = new RaiderAbilityResult(ability, false, rejection, target);
        AbilityResolved?.Invoke(result);
        return result;
    }

    void OnCharacterDamaged(NPCCharacter _, int amount) => Damaged?.Invoke(this, amount);

    void OnCharacterDied(NPCCharacter _)
    {
        requestedMove = 0f;
        Died?.Invoke(this);
        AbilityResolved?.Invoke(new RaiderAbilityResult(
            RaiderAbility.Death, true, RaiderAbilityRejection.None, character));
    }
}
