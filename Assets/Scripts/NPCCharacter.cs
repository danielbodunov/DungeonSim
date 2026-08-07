using System;
using UnityEngine;

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
    [SerializeField, Min(0)] int health = 10;
    [SerializeField, Min(1), Tooltip("Maximum number of new cells this character can explore per dungeon visit.")]
    int stamina = 10;

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
    public int Health => health;
    public int Stamina => stamina;
    public int Strength => strength;
    public int Dexterity => dexterity;
    public int Luck => luck;
    public int Intelligence => intelligence;
    public int DungeonVisits => dungeonVisits;

    public event Action<NPCCharacter> ProgressChanged;

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
        health = Mathf.Max(0, value);
        ProgressChanged?.Invoke(this);
    }

    public void GainExperience(int amount)
    {
        experience += Mathf.Max(0, amount);
        ProgressChanged?.Invoke(this);
    }
}
