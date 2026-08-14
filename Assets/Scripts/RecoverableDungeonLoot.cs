using System;
using System.Collections.Generic;
using UnityEngine;

public enum RecoverableLootOrigin
{
    DungeonTreasure,
    AdventurerPossession
}

/// <summary>Auditable result of processing one dead adventurer's loot custody.</summary>
[Serializable]
public sealed class AdventurerDeathLootOutcome
{
    [SerializeField] int sourceRuntimeAgentId;
    [SerializeField] string sourceAdventurerName;
    [SerializeField] Vector2Int deathCell;
    [SerializeField] Vector3 worldPosition;
    [SerializeField] int carriedItemCountBefore;
    [SerializeField] int carriedValueBefore;
    [SerializeField] int recoveredItemCount;
    [SerializeField] int recoveredValue;
    [SerializeField] string recoveryDropId;
    [SerializeField] int carriedItemCountAfter;
    [SerializeField] int carriedValueAfter;
    [SerializeField] bool recoveryProcessed;
    [SerializeField, Min(0)] int duplicateProcessingAttempts;

    public int SourceRuntimeAgentId => sourceRuntimeAgentId;
    public string SourceAdventurerName => sourceAdventurerName;
    public Vector2Int DeathCell => deathCell;
    public Vector3 WorldPosition => worldPosition;
    public int CarriedItemCountBefore => carriedItemCountBefore;
    public int CarriedValueBefore => carriedValueBefore;
    public int RecoveredItemCount => recoveredItemCount;
    public int RecoveredValue => recoveredValue;
    public string RecoveryDropId => recoveryDropId;
    public int CarriedItemCountAfter => carriedItemCountAfter;
    public int CarriedValueAfter => carriedValueAfter;
    public bool RecoveryProcessed => recoveryProcessed;
    public int DuplicateProcessingAttempts => duplicateProcessingAttempts;
    public bool ProducedDrop => !string.IsNullOrEmpty(recoveryDropId);
    public bool CustodyCleared =>
        carriedItemCountAfter == 0 && carriedValueAfter == 0;

    public AdventurerDeathLootOutcome(
        int sourceRuntimeAgentId,
        string sourceAdventurerName,
        Vector2Int deathCell,
        Vector3 worldPosition,
        int carriedItemCountBefore,
        int carriedValueBefore,
        int recoveredItemCount,
        int recoveredValue,
        string recoveryDropId,
        int carriedItemCountAfter,
        int carriedValueAfter)
    {
        this.sourceRuntimeAgentId = sourceRuntimeAgentId;
        this.sourceAdventurerName = sourceAdventurerName;
        this.deathCell = deathCell;
        this.worldPosition = worldPosition;
        this.carriedItemCountBefore = Mathf.Max(0, carriedItemCountBefore);
        this.carriedValueBefore = Mathf.Max(0, carriedValueBefore);
        this.recoveredItemCount = Mathf.Max(0, recoveredItemCount);
        this.recoveredValue = Mathf.Max(0, recoveredValue);
        this.recoveryDropId = recoveryDropId;
        this.carriedItemCountAfter = Mathf.Max(0, carriedItemCountAfter);
        this.carriedValueAfter = Mathf.Max(0, carriedValueAfter);
        recoveryProcessed = true;
    }

    internal void RecordDuplicateProcessingAttempt()
    {
        duplicateProcessingAttempts++;
    }
}

/// <summary>One item held in dungeon-side recovery after an adventurer defeat.</summary>
[Serializable]
public sealed class RecoverableLootItem
{
    [SerializeField] string itemId;
    [SerializeField, Min(0)] int value;
    [SerializeField] RecoverableLootOrigin origin;
    [SerializeField] Vector2Int sourceCell;
    [SerializeField] bool hasSourceCell;

    public string ItemId => itemId;
    public int Value => value;
    public RecoverableLootOrigin Origin => origin;
    public Vector2Int SourceCell => sourceCell;
    public bool HasSourceCell => hasSourceCell;

    public RecoverableLootItem(
        string itemId,
        int value,
        RecoverableLootOrigin origin,
        Vector2Int sourceCell,
        bool hasSourceCell)
    {
        this.itemId = itemId;
        this.value = Mathf.Max(0, value);
        this.origin = origin;
        this.sourceCell = sourceCell;
        this.hasSourceCell = hasSourceCell;
    }
}

/// <summary>Recoverable loot created atomically from one adventurer death.</summary>
[Serializable]
public sealed class RecoverableLootDrop
{
    [SerializeField] string dropId;
    [SerializeField] Vector2Int dropCell;
    [SerializeField] Vector3 worldPosition;
    [SerializeField] string sourceAdventurerName;
    [SerializeField] List<RecoverableLootItem> items = new();

    public string DropId => dropId;
    public Vector2Int DropCell => dropCell;
    public Vector3 WorldPosition => worldPosition;
    public string SourceAdventurerName => sourceAdventurerName;
    public IReadOnlyList<RecoverableLootItem> Items => items;
    public int ItemCount => items.Count;
    public int TotalValue
    {
        get
        {
            int total = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null)
                    total += items[i].Value;
            return total;
        }
    }

    public RecoverableLootDrop(
        string dropId,
        Vector2Int dropCell,
        Vector3 worldPosition,
        string sourceAdventurerName,
        List<RecoverableLootItem> items)
    {
        this.dropId = dropId;
        this.dropCell = dropCell;
        this.worldPosition = worldPosition;
        this.sourceAdventurerName = sourceAdventurerName;
        this.items = items != null
            ? new List<RecoverableLootItem>(items)
            : new List<RecoverableLootItem>();
    }
}
