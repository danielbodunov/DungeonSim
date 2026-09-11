#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RaiderAbilitiesTests
{
    [Test]
    public void MovementJumpFallAndLandResolveThroughSharedRequests()
    {
        GameObject actor = CreateRaider("Raider", Vector3.zero,
            out RaiderAbilities abilities, out _);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -1.5f, 0f);
        ground.transform.localScale = new Vector3(10f, 1f, 2f);
        try
        {
            Rigidbody body = actor.GetComponent<Rigidbody>();
            body.isKinematic = false;
            Physics.SyncTransforms();
            Invoke(abilities, "FixedUpdate");
            Assert.That(abilities.IsGrounded, Is.True);

            Assert.That(abilities.RequestMove(0.5f).Accepted, Is.True);
            Invoke(abilities, "FixedUpdate");
            Assert.That(body.linearVelocity.x,
                Is.EqualTo(abilities.MoveSpeed * 0.5f).Within(0.001f));

            int falls = 0;
            int lands = 0;
            abilities.Fell += _ => falls++;
            abilities.Landed += _ => lands++;
            Assert.That(abilities.RequestJump().Accepted, Is.True);
            Assert.That(abilities.IsGrounded, Is.False);
            Assert.That(falls, Is.EqualTo(1));

            actor.transform.position = Vector3.up * 3f;
            body.position = actor.transform.position;
            Physics.SyncTransforms();
            Invoke(abilities, "FixedUpdate");
            Assert.That(abilities.IsGrounded, Is.False);

            actor.transform.position = Vector3.zero;
            body.position = actor.transform.position;
            Physics.SyncTransforms();
            Invoke(abilities, "FixedUpdate");
            Assert.That(abilities.IsGrounded, Is.True);
            Assert.That(lands, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(ground);
        }
    }

    [Test]
    public void InputAndTestDriversUseIdenticalAttackValidityAndDamageRules()
    {
        GameObject inputActor = CreateRaider("Input Raider", Vector3.zero,
            out RaiderAbilities inputAbilities, out _);
        GameObject testActor = CreateRaider("Test Raider", new Vector3(10f, 0f, 0f),
            out RaiderAbilities testAbilities, out _);
        GameObject inputTarget = CreateTarget("Input Target", new Vector3(1f, 0f, 0f),
            out NPCCharacter inputCharacter);
        GameObject testTarget = CreateTarget("Test Target", new Vector3(11f, 0f, 0f),
            out NPCCharacter testCharacter);
        try
        {
            Physics.SyncTransforms();
            var inputDriver = new AbilityDriver(inputAbilities);
            var testDriver = new AbilityDriver(testAbilities);

            RaiderAbilityResult inputResult = inputDriver.Attack();
            RaiderAbilityResult testResult = testDriver.Attack();

            Assert.That(inputResult.Accepted, Is.True);
            Assert.That(testResult.Accepted, Is.True);
            Assert.That(inputResult.AppliedDamage, Is.EqualTo(testResult.AppliedDamage));
            Assert.That(inputCharacter.CurrentHealth, Is.EqualTo(testCharacter.CurrentHealth));
            Assert.That(inputCharacter.CurrentHealth, Is.EqualTo(8));
        }
        finally
        {
            Object.DestroyImmediate(inputActor);
            Object.DestroyImmediate(testActor);
            Object.DestroyImmediate(inputTarget);
            Object.DestroyImmediate(testTarget);
        }
    }

    [Test]
    public void AttackAvailabilityRangeAndCooldownAreOwnedByAbilities()
    {
        GameObject actor = CreateRaider("Raider", Vector3.zero,
            out RaiderAbilities abilities, out _);
        GameObject target = CreateTarget("Target", new Vector3(1f, 0f, 0f), out _);
        try
        {
            Physics.SyncTransforms();
            Assert.That(abilities.CanAttack, Is.True);
            Assert.That(abilities.RequestAttack().Accepted, Is.True);

            RaiderAbilityResult cooldown = abilities.RequestAttack();
            Assert.That(cooldown.Accepted, Is.False);
            Assert.That(cooldown.Rejection, Is.EqualTo(RaiderAbilityRejection.Cooldown));

            SetField(abilities, "nextAttackTime", 0f);
            target.transform.position = new Vector3(5f, 0f, 0f);
            Physics.SyncTransforms();
            RaiderAbilityResult outOfRange = abilities.RequestAttack();
            Assert.That(outOfRange.Accepted, Is.False);
            Assert.That(outOfRange.Rejection, Is.EqualTo(RaiderAbilityRejection.NoTarget));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void InteractionTargetOwnsObjectiveValidityAndResolution()
    {
        GameObject actor = CreateRaider("Raider", Vector3.zero,
            out RaiderAbilities abilities, out _);
        var target = new GameObject("Objective");
        target.transform.position = new Vector3(1f, 0f, 0f);
        target.AddComponent<BoxCollider>();
        TestRaiderInteractable interaction = target.AddComponent<TestRaiderInteractable>();
        try
        {
            Physics.SyncTransforms();
            interaction.available = false;
            RaiderAbilityResult rejected = abilities.RequestInteract();
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Rejection,
                Is.EqualTo(RaiderAbilityRejection.RejectedByTarget));
            Assert.That(interaction.resolutionCount, Is.Zero);

            interaction.available = true;
            RaiderAbilityResult accepted = abilities.RequestInteract();
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(interaction.resolutionCount, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void DamageAndDeathReuseNpcCharacterAuthorityAndResolvedEvents()
    {
        GameObject actor = CreateRaider("Raider", Vector3.zero,
            out RaiderAbilities abilities, out NPCCharacter character);
        try
        {
            int damageEvents = 0;
            int deathEvents = 0;
            abilities.Damaged += (_, amount) => damageEvents += amount;
            abilities.Died += _ => deathEvents++;

            RaiderAbilityResult first = abilities.ReceiveDamage(4, abilities, Vector3.zero);
            RaiderAbilityResult lethal = abilities.ReceiveDamage(20, abilities, Vector3.zero);

            Assert.That(first.AppliedDamage, Is.EqualTo(4));
            Assert.That(lethal.AppliedDamage, Is.EqualTo(6));
            Assert.That(character.IsDead, Is.True);
            Assert.That(damageEvents, Is.EqualTo(10));
            Assert.That(deathEvents, Is.EqualTo(1));
            Assert.That(abilities.CanMove, Is.False);
            Assert.That(abilities.CanAttack, Is.False);
            Assert.That(abilities.CanInteract, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(actor);
        }
    }

    static GameObject CreateRaider(
        string name,
        Vector3 position,
        out RaiderAbilities abilities,
        out NPCCharacter character)
    {
        var owner = new GameObject(name);
        owner.transform.position = position;
        owner.AddComponent<CapsuleCollider>();
        Rigidbody body = owner.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        character = owner.AddComponent<NPCCharacter>();
        character.ResetVisitResources();
        abilities = owner.AddComponent<RaiderAbilities>();
        Invoke(abilities, "Awake");
        Invoke(abilities, "OnEnable");
        return owner;
    }

    static GameObject CreateTarget(
        string name,
        Vector3 position,
        out NPCCharacter character)
    {
        var owner = new GameObject(name);
        owner.transform.position = position;
        owner.AddComponent<CapsuleCollider>();
        character = owner.AddComponent<NPCCharacter>();
        character.ResetVisitResources();
        return owner;
    }

    static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    static void Invoke(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }

    sealed class AbilityDriver
    {
        readonly RaiderAbilities abilities;
        public AbilityDriver(RaiderAbilities target) => abilities = target;
        public RaiderAbilityResult Attack() => abilities.RequestAttack();
    }
}

public sealed class TestRaiderInteractable : MonoBehaviour, IRaiderInteractable
{
    public bool available;
    public int resolutionCount;

    public bool CanInteract(RaiderAbilities raider) => available && raider != null;

    public bool TryInteract(RaiderAbilities raider)
    {
        if (!CanInteract(raider))
            return false;
        resolutionCount++;
        return true;
    }
}
#endif
