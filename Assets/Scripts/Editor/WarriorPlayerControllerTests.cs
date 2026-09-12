#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class WarriorPlayerControllerTests
{
    [Test]
    public void PrototypeFactoryCreatesFixedWarriorCompositionAndPlaneConstraint()
    {
        GameObject warrior = PrototypeWarriorFactory.Create(
            new Vector3(2f, 3f, 0f), Quaternion.identity);
        try
        {
            Assert.That(warrior.GetComponent<NPCCharacter>(), Is.Not.Null);
            Assert.That(warrior.GetComponent<RaiderAbilities>(), Is.Not.Null);
            Assert.That(warrior.GetComponent<WarriorPlayerController>(), Is.Not.Null);
            Assert.That(warrior.GetComponent<CapsuleCollider>(), Is.Not.Null);
            Transform visual = warrior.transform.Find("Knight_0");
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one * 3f));
            Rigidbody body = warrior.GetComponent<Rigidbody>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.useGravity, Is.True);
            Assert.That(body.interpolation,
                Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(body.constraints & RigidbodyConstraints.FreezePositionZ,
                Is.Not.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(warrior);
        }
    }

    [Test]
    public void DriverForwardsMovementJumpAndAttackThroughSharedAbilities()
    {
        CreateFixture(out GameObject loopOwner, out GameplayLoopController previousLoop,
            out GameplayLoopController loop, out GameObject warrior,
            out WarriorPlayerController controller, out RaiderAbilities abilities,
            out NPCCharacter character);
        GameObject target = CreateTarget(warrior.transform.position + Vector3.right,
            out NPCCharacter targetCharacter);
        try
        {
            var resolved = new List<RaiderAbility>();
            abilities.AbilityResolved += result =>
            {
                if (result.Accepted)
                    resolved.Add(result.Ability);
            };
            SetField(abilities, "groundProbeOverride", new Func<bool>(() => true));
            Physics.SyncTransforms();

            controller.ProcessInput(Vector2.right, true, true, false);
            Invoke(abilities, "FixedUpdate");

            Assert.That(resolved, Does.Contain(RaiderAbility.Move));
            Assert.That(resolved, Does.Contain(RaiderAbility.Jump));
            Assert.That(resolved, Does.Contain(RaiderAbility.Attack));
            Assert.That(abilities.CurrentVelocity.x,
                Is.EqualTo(abilities.MoveSpeed).Within(0.001f));
            Assert.That(targetCharacter.CurrentHealth, Is.EqualTo(8));
            Assert.That(character.CurrentHealth, Is.EqualTo(character.MaxHealth));
        }
        finally
        {
            Object.DestroyImmediate(target);
            DestroyFixture(loopOwner, warrior, previousLoop);
        }
    }

    [Test]
    public void DeathEndsAttemptOnceAndRestartRestoresFreshAbilityState()
    {
        CreateFixture(out GameObject loopOwner, out GameplayLoopController previousLoop,
            out GameplayLoopController loop, out GameObject warrior,
            out WarriorPlayerController controller, out RaiderAbilities abilities,
            out NPCCharacter character);
        try
        {
            Vector3 spawn = warrior.transform.position;
            Quaternion rotation = warrior.transform.rotation;
            controller.ProcessInput(Vector2.right, false, false, false);
            Invoke(abilities, "FixedUpdate");
            warrior.transform.position += new Vector3(4f, 2f, 0f);
            warrior.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            warrior.GetComponent<Rigidbody>().linearVelocity = new Vector3(5f, -2f, 1f);
            SetField(abilities, "nextAttackTime", Time.time + 100f);

            Guid firstAttempt = loop.Attempt.AttemptId;
            character.TakeDamage(character.CurrentHealth);
            Assert.That(loop.Attempt.AttemptId, Is.EqualTo(firstAttempt));
            Assert.That(loop.Attempt.State, Is.EqualTo(DungeonAttemptState.Dead));
            character.TakeDamage(1);
            Assert.That(loop.Attempt.State, Is.EqualTo(DungeonAttemptState.Dead));

            controller.ProcessInput(Vector2.left, true, true, true);
            Assert.That(GetField<float>(abilities, "requestedMove"), Is.Zero);
            Assert.That(controller.RequestRestart(out _), Is.True);

            Assert.That(loop.Attempt.AttemptId, Is.Not.EqualTo(firstAttempt));
            Assert.That(loop.Attempt.State, Is.EqualTo(DungeonAttemptState.SeekingTreasure));
            Assert.That(character.CurrentHealth, Is.EqualTo(character.MaxHealth));
            Assert.That(warrior.transform.position, Is.EqualTo(spawn));
            Assert.That(warrior.transform.rotation, Is.EqualTo(rotation));
            Assert.That(abilities.CurrentVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(GetField<float>(abilities, "requestedMove"), Is.Zero);
            Assert.That(abilities.CanAttack, Is.True);

            Guid secondAttempt = loop.Attempt.AttemptId;
            character.TakeDamage(character.CurrentHealth);
            Assert.That(controller.RequestRestart(out _), Is.True);
            Assert.That(loop.Attempt.AttemptId, Is.Not.EqualTo(secondAttempt));
            Assert.That(character.CurrentHealth, Is.EqualTo(character.MaxHealth));
            Assert.That(abilities.CurrentVelocity, Is.EqualTo(Vector3.zero));
        }
        finally
        {
            DestroyFixture(loopOwner, warrior, previousLoop);
        }
    }

    [Test]
    public void RaidCameraFollowIsOwnedOnlyDuringActiveAttempt()
    {
        CreateFixture(out GameObject loopOwner, out GameplayLoopController previousLoop,
            out GameplayLoopController loop, out GameObject warrior,
            out WarriorPlayerController controller, out RaiderAbilities abilities,
            out NPCCharacter character, true, out GameObject cameraOwner,
            out CameraFollow cameraFollow);
        try
        {
            Assert.That(cameraFollow.followTarget, Is.EqualTo(warrior.transform));
            character.TakeDamage(character.CurrentHealth);
            Assert.That(cameraFollow.followTarget, Is.Null);
            Assert.That(controller.RequestRestart(out _), Is.True);
            Assert.That(cameraFollow.followTarget, Is.EqualTo(warrior.transform));
        }
        finally
        {
            Object.DestroyImmediate(cameraOwner);
            DestroyFixture(loopOwner, warrior, previousLoop);
        }
    }

    static void CreateFixture(
        out GameObject loopOwner,
        out GameplayLoopController previousLoop,
        out GameplayLoopController loop,
        out GameObject warrior,
        out WarriorPlayerController controller,
        out RaiderAbilities abilities,
        out NPCCharacter character)
    {
        CreateFixture(out loopOwner, out previousLoop, out loop, out warrior,
            out controller, out abilities, out character, false,
            out _, out _);
    }

    static void CreateFixture(
        out GameObject loopOwner,
        out GameplayLoopController previousLoop,
        out GameplayLoopController loop,
        out GameObject warrior,
        out WarriorPlayerController controller,
        out RaiderAbilities abilities,
        out NPCCharacter character,
        bool createCamera,
        out GameObject cameraOwner,
        out CameraFollow cameraFollow)
    {
        previousLoop = GameplayLoopController.Instance;
        SetLoopInstance(null);
        loopOwner = new GameObject("Warrior Controller Loop");
        loop = loopOwner.AddComponent<GameplayLoopController>();
        SetLoopInstance(loop);
        Assert.That(loop.TryBeginValidation("warrior-test", out _), Is.True);

        cameraOwner = null;
        cameraFollow = null;
        if (createCamera)
        {
            cameraOwner = new GameObject("Raid Camera");
            Camera camera = cameraOwner.AddComponent<Camera>();
            cameraFollow = cameraOwner.AddComponent<CameraFollow>();
            cameraFollow.camComponent = camera;
        }

        warrior = new GameObject("Warrior");
        warrior.transform.position = new Vector3(2f, 3f, 0f);
        warrior.AddComponent<CapsuleCollider>();
        Rigidbody body = warrior.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = false;
        character = warrior.AddComponent<NPCCharacter>();
        character.ResetVisitResources();
        abilities = warrior.AddComponent<RaiderAbilities>();
        controller = warrior.AddComponent<WarriorPlayerController>();
        SetField(controller, "gameplayLoop", loop);
        SetField(controller, "raidCamera", cameraFollow);
        Invoke(abilities, "Awake");
        Invoke(abilities, "OnEnable");
        Invoke(controller, "Awake");
        Invoke(controller, "OnEnable");
    }

    static GameObject CreateTarget(Vector3 position, out NPCCharacter character)
    {
        var target = new GameObject("Attack Target");
        target.transform.position = position;
        target.AddComponent<CapsuleCollider>();
        character = target.AddComponent<NPCCharacter>();
        character.ResetVisitResources();
        return target;
    }

    static void DestroyFixture(
        GameObject loopOwner,
        GameObject warrior,
        GameplayLoopController previousLoop)
    {
        Object.DestroyImmediate(warrior);
        Object.DestroyImmediate(loopOwner);
        SetLoopInstance(previousLoop);
    }

    static void SetLoopInstance(GameplayLoopController value)
    {
        PropertyInfo property = typeof(GameplayLoopController).GetProperty(
            "Instance", BindingFlags.Static | BindingFlags.Public);
        property.SetValue(null, value);
    }

    static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    static T GetField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    static void Invoke(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }
}
#endif
