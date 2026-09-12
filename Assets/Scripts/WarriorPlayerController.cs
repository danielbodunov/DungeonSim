using System;
using UnityEngine;

/// <summary>
/// Thin input driver for the fixed prototype Warrior. Gameplay validity and
/// resolution remain in RaiderAbilities and the attempt lifecycle.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RaiderAbilities))]
public sealed class WarriorPlayerController : MonoBehaviour
{
    [SerializeField] RaiderAbilities abilities;
    [SerializeField] InputManager inputManager;
    [SerializeField] GameplayLoopController gameplayLoop;
    [SerializeField] CameraFollow raidCamera;

    Vector3 spawnPosition;
    Quaternion spawnRotation;
    Guid observedAttemptId;
    Guid deathSignaledAttemptId;
    bool ownsCameraFollow;

    public bool IsInputEnabled =>
        abilities != null && abilities.IsAlive &&
        gameplayLoop != null && gameplayLoop.Attempt != null &&
        !gameplayLoop.Attempt.IsTerminal;

    void Awake()
    {
        abilities ??= GetComponent<RaiderAbilities>();
        inputManager ??= InputManager.Instance ?? FindAnyObjectByType<InputManager>();
        gameplayLoop ??= GameplayLoopController.Instance ??
            FindAnyObjectByType<GameplayLoopController>();
        if (raidCamera == null)
            raidCamera = FindAnyObjectByType<CameraFollow>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.constraints |= RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotation;
    }

    void OnEnable()
    {
        abilities ??= GetComponent<RaiderAbilities>();
        if (abilities != null)
        {
            abilities.Died -= OnWarriorDied;
            abilities.Died += OnWarriorDied;
        }
        ResolveLoop();
        if (gameplayLoop != null)
        {
            gameplayLoop.StateChanged -= OnLifecycleChanged;
            gameplayLoop.StateChanged += OnLifecycleChanged;
        }
        OnLifecycleChanged();
    }

    void OnDisable()
    {
        if (abilities != null)
        {
            abilities.Died -= OnWarriorDied;
            abilities.RequestMove(0f);
        }
        if (gameplayLoop != null)
            gameplayLoop.StateChanged -= OnLifecycleChanged;
        ReleaseCamera();
    }

    void Update()
    {
        if (inputManager == null)
            inputManager = InputManager.Instance;
        ProcessInput(
            inputManager != null ? inputManager.Move : Vector2.zero,
            inputManager != null && inputManager.JumpPressed,
            inputManager != null && inputManager.AttackPressed,
            inputManager != null && inputManager.InteractPressed);
    }

    public void ProcessInput(
        Vector2 movement,
        bool jumpPressed,
        bool attackPressed,
        bool interactPressed)
    {
        if (!IsInputEnabled)
        {
            if (abilities != null && abilities.IsAlive)
                abilities.RequestMove(0f);
            return;
        }

        abilities.RequestMove(movement.x);
        if (jumpPressed)
            abilities.RequestJump();
        if (attackPressed)
            abilities.RequestAttack();
        if (interactPressed)
            abilities.RequestInteract();
    }

    public bool RequestRestart(out string failure)
    {
        ResolveLoop();
        DungeonAttempt attempt = gameplayLoop != null ? gameplayLoop.Attempt : null;
        if (attempt == null)
        {
            failure = "There is no attempt to restart.";
            return false;
        }
        return gameplayLoop.TryRestartAttempt(attempt.AttemptId, out failure);
    }

    void OnWarriorDied(RaiderAbilities source)
    {
        ResolveLoop();
        DungeonAttempt attempt = gameplayLoop != null ? gameplayLoop.Attempt : null;
        if (attempt == null || attempt.IsTerminal ||
            deathSignaledAttemptId == attempt.AttemptId)
            return;
        deathSignaledAttemptId = attempt.AttemptId;
        gameplayLoop.TryDieInAttempt(attempt.AttemptId, out _);
    }

    void OnLifecycleChanged()
    {
        ResolveLoop();
        DungeonAttempt attempt = gameplayLoop != null ? gameplayLoop.Attempt : null;
        if (attempt == null)
        {
            observedAttemptId = Guid.Empty;
            ReleaseCamera();
            return;
        }

        if (attempt.IsTerminal)
        {
            ReleaseCamera();
            return;
        }

        if (observedAttemptId != attempt.AttemptId)
        {
            observedAttemptId = attempt.AttemptId;
            deathSignaledAttemptId = Guid.Empty;
            abilities?.ResetForAttempt(spawnPosition, spawnRotation);
        }
        AcquireCamera();
    }

    void ResolveLoop()
    {
        if (gameplayLoop == null)
            gameplayLoop = GameplayLoopController.Instance ??
                FindAnyObjectByType<GameplayLoopController>();
    }

    void AcquireCamera()
    {
        if (raidCamera == null)
            raidCamera = FindAnyObjectByType<CameraFollow>();
        if (raidCamera == null)
            return;
        raidCamera.followTarget = transform;
        ownsCameraFollow = true;
    }

    void ReleaseCamera()
    {
        if (ownsCameraFollow && raidCamera != null &&
            raidCamera.followTarget == transform)
            raidCamera.followTarget = null;
        ownsCameraFollow = false;
    }
}

/// <summary>Creates the fixed placeholder Warrior used by the raid prototype.</summary>
public static class PrototypeWarriorFactory
{
    const string PlaceholderResourcePath = "NPCS/Placeholder_NPC";

    public static GameObject Create(Vector3 position, Quaternion rotation)
    {
        GameObject placeholder = Resources.Load<GameObject>(PlaceholderResourcePath);
        GameObject warrior = placeholder != null
            ? UnityEngine.Object.Instantiate(placeholder, position, rotation)
            : new GameObject("Prototype Warrior");
        warrior.name = "Prototype Warrior";
        warrior.transform.SetPositionAndRotation(position, rotation);

        NPCCharacter character = warrior.GetComponent<NPCCharacter>();
        if (character == null)
            character = warrior.AddComponent<NPCCharacter>();
        Rigidbody body = warrior.GetComponent<Rigidbody>();
        if (body == null)
            body = warrior.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.constraints |= RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation;

        CapsuleCollider collider = warrior.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = warrior.AddComponent<CapsuleCollider>();
        collider.direction = 1;
        if (collider.height <= 0.01f)
            collider.height = 1.8f;
        if (collider.radius <= 0.01f)
            collider.radius = 0.35f;

        RaiderAbilities abilities = warrior.GetComponent<RaiderAbilities>();
        if (abilities == null)
            abilities = warrior.AddComponent<RaiderAbilities>();
        if (warrior.GetComponent<WarriorPlayerController>() == null)
            warrior.AddComponent<WarriorPlayerController>();
        abilities.ResetForAttempt(position, rotation);
        character.ResetVisitResources();
        return warrior;
    }
}
