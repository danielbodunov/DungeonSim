using System;
using UnityEngine;

public enum NPCActionOutcome
{
    Dodged,
    Damaged,
    Defeated
}

[Serializable]
public sealed class TrapDodgeSettings
{
    [Range(0f, 1f)] public float baseChance = 0.15f;
    [Min(0f)] public float dexterityWeight = 0.07f;
    [Min(0f)] public float luckWeight = 0.03f;
    [Min(0f)] public float difficultyWeight = 0.05f;
    [Range(0f, 1f)] public float minimumChance = 0.05f;
    [Range(0f, 1f)] public float maximumChance = 0.95f;
}

public readonly struct NPCActionResult
{
    public NPCActionResult(
        NPCActionOutcome outcome,
        NPCCharacter target,
        UnityEngine.Object source,
        Vector3 worldPosition,
        int attemptedDamage,
        int appliedDamage,
        float successChance,
        float roll)
    {
        Outcome = outcome;
        Target = target;
        Source = source;
        WorldPosition = worldPosition;
        AttemptedDamage = attemptedDamage;
        AppliedDamage = appliedDamage;
        SuccessChance = successChance;
        Roll = roll;
    }

    public NPCActionOutcome Outcome { get; }
    public NPCCharacter Target { get; }
    public UnityEngine.Object Source { get; }
    public Vector3 WorldPosition { get; }
    public int AttemptedDamage { get; }
    public int AppliedDamage { get; }
    public float SuccessChance { get; }
    public float Roll { get; }
}

/// <summary>Produces one authoritative result for NPC trap interactions.</summary>
public static class NPCActionResolver
{
    static readonly TrapDodgeSettings DefaultDodgeSettings = new();

    public static event Action<NPCActionResult> ActionResolved;

    public static float CalculateTrapDodgeChance(
        NPCCharacter target,
        float trapDifficulty,
        TrapDodgeSettings settings = null)
    {
        if (target == null)
            return 0f;

        settings ??= DefaultDodgeSettings;
        float lower = Mathf.Clamp01(Mathf.Min(
            settings.minimumChance, settings.maximumChance));
        float upper = Mathf.Clamp01(Mathf.Max(
            settings.minimumChance, settings.maximumChance));
        float chance = settings.baseChance
            + target.Dexterity * settings.dexterityWeight
            + target.Luck * settings.luckWeight
            - Mathf.Max(0f, trapDifficulty) * settings.difficultyWeight;
        return Mathf.Clamp(chance, lower, upper);
    }

    public static NPCActionResult ResolveTrap(
        NPCCharacter target,
        UnityEngine.Object source,
        int damage,
        float trapDifficulty,
        TrapDodgeSettings settings = null,
        float? forcedRoll = null)
    {
        Vector3 worldPosition = target != null
            ? target.transform.position + Vector3.up * 1.25f
            : Vector3.zero;
        int attemptedDamage = Mathf.Max(0, damage);
        float chance = CalculateTrapDodgeChance(target, trapDifficulty, settings);
        float roll = Mathf.Clamp01(forcedRoll ?? UnityEngine.Random.value);

        if (target == null || target.IsDead || roll < chance)
        {
            var dodged = new NPCActionResult(
                NPCActionOutcome.Dodged,
                target,
                source,
                worldPosition,
                attemptedDamage,
                0,
                chance,
                roll);
            ActionResolved?.Invoke(dodged);
            return dodged;
        }

        int healthBefore = target.CurrentHealth;
        target.TakeDamage(attemptedDamage);
        int appliedDamage = Mathf.Max(0, healthBefore - target.CurrentHealth);
        NPCActionOutcome outcome = target.IsDead
            ? NPCActionOutcome.Defeated
            : NPCActionOutcome.Damaged;
        var result = new NPCActionResult(
            outcome,
            target,
            source,
            worldPosition,
            attemptedDamage,
            appliedDamage,
            chance,
            roll);
        ActionResolved?.Invoke(result);
        return result;
    }

    public static NPCActionResult ResolveDamage(
        NPCCharacter target,
        UnityEngine.Object source,
        int damage,
        Vector3 worldPosition)
    {
        int attemptedDamage = Mathf.Max(0, damage);
        int healthBefore = target != null ? target.CurrentHealth : 0;
        if (target != null && !target.IsDead)
            target.TakeDamage(attemptedDamage);

        int appliedDamage = target != null
            ? Mathf.Max(0, healthBefore - target.CurrentHealth)
            : 0;
        NPCActionOutcome outcome = target != null && target.IsDead
            ? NPCActionOutcome.Defeated
            : NPCActionOutcome.Damaged;
        var result = new NPCActionResult(
            outcome,
            target,
            source,
            worldPosition,
            attemptedDamage,
            appliedDamage,
            0f,
            0f);
        ActionResolved?.Invoke(result);
        return result;
    }
}
