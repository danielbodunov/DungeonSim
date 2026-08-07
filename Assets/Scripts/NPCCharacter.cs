using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class NPCCharacterRecord
{
    public string id;
    public string characterName;
    public int level = 1;
    public int experience;
    public int maxHealth = 10;
    public float maxStamina = 10f;
    public int strength = 5;
    public int dexterity = 5;
    public int luck = 5;
    public int intelligence = 5;
    public int dungeonVisits;
}

/// <summary>
/// Persistent progression and RPG attributes for one NPC. Put this component on
/// each NPC prefab so every character can have different starting values.
/// </summary>
[DisallowMultipleComponent]
public class NPCCharacter : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] string characterName = "Adventurer";
    [SerializeField, Min(1)] int level = 1;
    [SerializeField, Min(0)] int experience;

    [Header("Resources")]
    [FormerlySerializedAs("health")]
    [SerializeField, Min(0)] int maxHealth = 10;
    [FormerlySerializedAs("stamina")]
    [SerializeField, Min(0.01f)] float maxStamina = 10f;

    [Header("Current Visit Resources")]
    [SerializeField, Min(0)] int currentHealth;
    [SerializeField, Min(0f)] float currentStamina;

    [Header("Attributes")]
    [SerializeField, Min(0)] int strength = 5;
    [SerializeField, Min(0)] int dexterity = 5;
    [SerializeField, Min(0)] int luck = 5;
    [SerializeField, Min(0)] int intelligence = 5;

    [Header("Progression (Runtime / Save Data)")]
    [SerializeField, Min(0)] int dungeonVisits;
    [SerializeField, Min(0)] int experiencePerNewCell = 1;

    public string CharacterName => characterName;
    public int Level => level;
    public int Experience => experience;
    public int MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;
    public int CurrentHealth => currentHealth;
    public float CurrentStamina => currentStamina;
    // Compatibility aliases for systems that already read these values.
    public int Health => MaxHealth;
    public float Stamina => MaxStamina;
    public int Strength => strength;
    public int Dexterity => dexterity;
    public int Luck => luck;
    public int Intelligence => intelligence;
    public int DungeonVisits => dungeonVisits;
    public bool IsDead => currentHealth <= 0;

    public event Action<NPCCharacter> ProgressChanged;
    public event Action<NPCCharacter> Died;

    public void ApplyRecord(NPCCharacterRecord record)
    {
        if (record == null)
            return;
        characterName = record.characterName;
        level = Mathf.Max(1, record.level);
        experience = Mathf.Max(0, record.experience);
        maxHealth = Mathf.Max(0, record.maxHealth);
        maxStamina = Mathf.Max(0.01f, record.maxStamina);
        strength = Mathf.Max(0, record.strength);
        dexterity = Mathf.Max(0, record.dexterity);
        luck = Mathf.Max(0, record.luck);
        intelligence = Mathf.Max(0, record.intelligence);
        dungeonVisits = Mathf.Max(0, record.dungeonVisits);
        ProgressChanged?.Invoke(this);
    }

    public void WriteToRecord(NPCCharacterRecord record)
    {
        if (record == null)
            return;
        record.characterName = characterName;
        record.level = level;
        record.experience = experience;
        record.maxHealth = maxHealth;
        record.maxStamina = maxStamina;
        record.strength = strength;
        record.dexterity = dexterity;
        record.luck = luck;
        record.intelligence = intelligence;
        record.dungeonVisits = dungeonVisits;
    }

    public void ResetVisitResources()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        ProgressChanged?.Invoke(this);
    }

    public void SpendStamina(float amount)
    {
        if (amount <= 0f || currentStamina <= 0f)
            return;
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        ProgressChanged?.Invoke(this);
    }

    public void RecordCellExplored()
    {
        experience += experiencePerNewCell;
        ProgressChanged?.Invoke(this);
    }

    public void RecordDungeonVisitCompleted()
    {
        dungeonVisits++;
        ProgressChanged?.Invoke(this);
    }

    public void SetHealth(int value)
    {
        bool wasAlive = currentHealth > 0;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        ProgressChanged?.Invoke(this);
        if (wasAlive && currentHealth <= 0)
            Died?.Invoke(this);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return;
        SetHealth(currentHealth - amount);
    }

    public void GainExperience(int amount)
    {
        experience += Mathf.Max(0, amount);
        ProgressChanged?.Invoke(this);
    }
}
