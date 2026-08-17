using System;
using System.Collections.Generic;
using UnityEngine;

public enum RecoverableLootOrigin
{
    DungeonTreasure,
    AdventurerPossession
}

/// <summary>Immutable item snapshot finalized by a successful dungeon escape.</summary>
[Serializable]
public sealed class EscapedLootItem
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

    public EscapedLootItem(
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

/// <summary>Auditable result of finalizing loot at a successful dungeon exit.</summary>
[Serializable]
public sealed class AdventurerEscapeLootOutcome
{
    [SerializeField] int sourceRuntimeAgentId;
    [SerializeField] string sourceAdventurerName;
    [SerializeField] Vector2Int exitCell;
    [SerializeField] Vector3 worldPosition;
    [SerializeField] int carriedItemCountBefore;
    [SerializeField] int carriedValueBefore;
    [SerializeField] List<EscapedLootItem> escapedItems = new();
    [SerializeField] int carriedItemCountAfter;
    [SerializeField] int carriedValueAfter;
    [SerializeField] bool escapeProcessed;
    [SerializeField, Min(0)] int duplicateProcessingAttempts;

    public int SourceRuntimeAgentId => sourceRuntimeAgentId;
    public string SourceAdventurerName => sourceAdventurerName;
    public Vector2Int ExitCell => exitCell;
    public Vector3 WorldPosition => worldPosition;
    public int CarriedItemCountBefore => carriedItemCountBefore;
    public int CarriedValueBefore => carriedValueBefore;
    public IReadOnlyList<EscapedLootItem> EscapedItems => escapedItems;
    public int EscapedItemCount => escapedItems.Count;
    public int EscapedValue
    {
        get
        {
            int total = 0;
            for (int i = 0; i < escapedItems.Count; i++)
                if (escapedItems[i] != null)
                    total += escapedItems[i].Value;
            return total;
        }
    }
    public int CarriedItemCountAfter => carriedItemCountAfter;
    public int CarriedValueAfter => carriedValueAfter;
    public bool EscapeProcessed => escapeProcessed;
    public int DuplicateProcessingAttempts => duplicateProcessingAttempts;
    public bool ProducedLoss => EscapedItemCount > 0;
    public bool CustodyCleared =>
        carriedItemCountAfter == 0 && carriedValueAfter == 0;

    public AdventurerEscapeLootOutcome(
        int sourceRuntimeAgentId,
        string sourceAdventurerName,
        Vector2Int exitCell,
        Vector3 worldPosition,
        int carriedItemCountBefore,
        int carriedValueBefore,
        List<EscapedLootItem> escapedItems,
        int carriedItemCountAfter,
        int carriedValueAfter)
    {
        this.sourceRuntimeAgentId = sourceRuntimeAgentId;
        this.sourceAdventurerName = sourceAdventurerName;
        this.exitCell = exitCell;
        this.worldPosition = worldPosition;
        this.carriedItemCountBefore = Mathf.Max(0, carriedItemCountBefore);
        this.carriedValueBefore = Mathf.Max(0, carriedValueBefore);
        this.escapedItems = escapedItems != null
            ? new List<EscapedLootItem>(escapedItems)
            : new List<EscapedLootItem>();
        this.carriedItemCountAfter = Mathf.Max(0, carriedItemCountAfter);
        this.carriedValueAfter = Mathf.Max(0, carriedValueAfter);
        escapeProcessed = true;
    }

    internal void RecordDuplicateProcessingAttempt()
    {
        duplicateProcessingAttempts++;
    }

    internal AdventurerEscapeLootOutcome Copy()
    {
        var copiedItems = new List<EscapedLootItem>(escapedItems.Count);
        for (int i = 0; i < escapedItems.Count; i++)
        {
            EscapedLootItem item = escapedItems[i];
            if (item == null)
                continue;
            copiedItems.Add(new EscapedLootItem(
                item.ItemId,
                item.Value,
                item.Origin,
                item.SourceCell,
                item.HasSourceCell));
        }

        var copy = new AdventurerEscapeLootOutcome(
            sourceRuntimeAgentId,
            sourceAdventurerName,
            exitCell,
            worldPosition,
            carriedItemCountBefore,
            carriedValueBefore,
            copiedItems,
            carriedItemCountAfter,
            carriedValueAfter);
        copy.duplicateProcessingAttempts = duplicateProcessingAttempts;
        return copy;
    }
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

    internal AdventurerDeathLootOutcome Copy()
    {
        var copy = new AdventurerDeathLootOutcome(
            sourceRuntimeAgentId,
            sourceAdventurerName,
            deathCell,
            worldPosition,
            carriedItemCountBefore,
            carriedValueBefore,
            recoveredItemCount,
            recoveredValue,
            recoveryDropId,
            carriedItemCountAfter,
            carriedValueAfter);
        copy.duplicateProcessingAttempts = duplicateProcessingAttempts;
        return copy;
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

    internal RecoverableLootItem Copy()
    {
        return new RecoverableLootItem(
            itemId,
            value,
            origin,
            sourceCell,
            hasSourceCell);
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
    public IReadOnlyList<RecoverableLootItem> Items =>
        items ?? (IReadOnlyList<RecoverableLootItem>)Array.Empty<RecoverableLootItem>();
    public int ItemCount => items?.Count ?? 0;
    public int TotalValue
    {
        get
        {
            int total = 0;
            if (items == null)
                return 0;
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

    internal RecoverableLootDrop Copy()
    {
        var copiedItems = new List<RecoverableLootItem>(ItemCount);
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null)
                    copiedItems.Add(items[i].Copy());
        }

        return new RecoverableLootDrop(
            dropId,
            dropCell,
            worldPosition,
            sourceAdventurerName,
            copiedItems);
    }
}

/// <summary>
/// One physical item deliberately recovered into dungeon storage by the
/// player. This is inventory state, not an Aura balance.
/// </summary>
[Serializable]
public sealed class DungeonStoredLootItem
{
    [SerializeField] string itemId;
    [SerializeField, Min(0)] int value;
    [SerializeField] RecoverableLootOrigin origin;
    [SerializeField] Vector2Int sourceCell;
    [SerializeField] bool hasSourceCell;
    [SerializeField] string recoveryDropId;

    public string ItemId => itemId;
    public int Value => value;
    public RecoverableLootOrigin Origin => origin;
    public Vector2Int SourceCell => sourceCell;
    public bool HasSourceCell => hasSourceCell;
    public string RecoveryDropId => recoveryDropId;

    public DungeonStoredLootItem(
        RecoverableLootItem source,
        string sourceRecoveryDropId)
    {
        itemId = source?.ItemId ?? string.Empty;
        value = source != null ? source.Value : 0;
        origin = source != null
            ? source.Origin
            : RecoverableLootOrigin.AdventurerPossession;
        sourceCell = source != null ? source.SourceCell : default;
        hasSourceCell = source != null && source.HasSourceCell;
        recoveryDropId = sourceRecoveryDropId;
    }

    internal DungeonStoredLootItem Copy()
    {
        return new DungeonStoredLootItem(
            new RecoverableLootItem(
                itemId,
                value,
                origin,
                sourceCell,
                hasSourceCell),
            recoveryDropId);
    }
}

/// <summary>Auditable result of one accepted player recovery action.</summary>
[Serializable]
public sealed class PlayerLootRecoveryRecord
{
    [SerializeField] string sourceDropId;
    [SerializeField] Vector2Int recoveryCell;
    [SerializeField] Vector3 worldPosition;
    [SerializeField] string sourceAdventurerName;
    [SerializeField, Min(0)] int recoveredItemCount;
    [SerializeField, Min(0)] int recoveredValue;
    [SerializeField, Min(0)] int dungeonTreasureValue;
    [SerializeField, Min(0)] int adventurerLootValue;

    public string SourceDropId => sourceDropId;
    public Vector2Int RecoveryCell => recoveryCell;
    public Vector3 WorldPosition => worldPosition;
    public string SourceAdventurerName => sourceAdventurerName;
    public int RecoveredItemCount => recoveredItemCount;
    public int RecoveredValue => recoveredValue;
    public int DungeonTreasureValue => dungeonTreasureValue;
    public int AdventurerLootValue => adventurerLootValue;

    public PlayerLootRecoveryRecord(
        RecoverableLootDrop sourceDrop,
        int itemCount,
        int totalValue,
        int dungeonOriginValue,
        int adventurerOriginValue)
    {
        sourceDropId = sourceDrop?.DropId ?? string.Empty;
        recoveryCell = sourceDrop != null ? sourceDrop.DropCell : default;
        worldPosition = sourceDrop != null ? sourceDrop.WorldPosition : default;
        sourceAdventurerName = sourceDrop?.SourceAdventurerName ?? string.Empty;
        recoveredItemCount = Mathf.Max(0, itemCount);
        recoveredValue = Mathf.Max(0, totalValue);
        dungeonTreasureValue = Mathf.Max(0, dungeonOriginValue);
        adventurerLootValue = Mathf.Max(0, adventurerOriginValue);
    }

    internal PlayerLootRecoveryRecord Copy()
    {
        var source = new RecoverableLootDrop(
            sourceDropId,
            recoveryCell,
            worldPosition,
            sourceAdventurerName,
            null);
        return new PlayerLootRecoveryRecord(
            source,
            recoveredItemCount,
            recoveredValue,
            dungeonTreasureValue,
            adventurerLootValue);
    }
}

/// <summary>
/// Scenario-owned snapshot of the traversal service's persistent loot and
/// outcome state. Runtime world objects remain derived from recovery records.
/// </summary>
[Serializable]
public sealed class NPCTraversalScenarioState
{
    [SerializeField] List<RecoverableLootDrop> recoverableLootDrops = new();
    [SerializeField] List<AdventurerDeathLootOutcome> deathLootOutcomes = new();
    [SerializeField] List<AdventurerEscapeLootOutcome> escapeLootOutcomes = new();
    [SerializeField, Min(1)] int nextRecoverableLootDropNumber = 1;
    [SerializeField, Min(1)] int nextRuntimeAgentId = 1;

    public IReadOnlyList<RecoverableLootDrop> RecoverableLootDrops =>
        recoverableLootDrops ??
        (IReadOnlyList<RecoverableLootDrop>)Array.Empty<RecoverableLootDrop>();
    public IReadOnlyList<AdventurerDeathLootOutcome> DeathLootOutcomes =>
        deathLootOutcomes ??
        (IReadOnlyList<AdventurerDeathLootOutcome>)Array.Empty<AdventurerDeathLootOutcome>();
    public IReadOnlyList<AdventurerEscapeLootOutcome> EscapeLootOutcomes =>
        escapeLootOutcomes ??
        (IReadOnlyList<AdventurerEscapeLootOutcome>)Array.Empty<AdventurerEscapeLootOutcome>();
    public int NextRecoverableLootDropNumber =>
        Mathf.Max(1, nextRecoverableLootDropNumber);
    public int NextRuntimeAgentId => Mathf.Max(1, nextRuntimeAgentId);

    public NPCTraversalScenarioState(
        IReadOnlyList<RecoverableLootDrop> drops,
        IReadOnlyList<AdventurerDeathLootOutcome> deathOutcomes,
        IReadOnlyList<AdventurerEscapeLootOutcome> escapeOutcomes,
        int nextDropNumber,
        int nextAgentId)
    {
        recoverableLootDrops = CopyDrops(drops);
        deathLootOutcomes = CopyDeathOutcomes(deathOutcomes);
        escapeLootOutcomes = CopyEscapeOutcomes(escapeOutcomes);
        nextRecoverableLootDropNumber = Mathf.Max(1, nextDropNumber);
        nextRuntimeAgentId = Mathf.Max(1, nextAgentId);
    }

    internal NPCTraversalScenarioState Copy()
    {
        return new NPCTraversalScenarioState(
            RecoverableLootDrops,
            DeathLootOutcomes,
            EscapeLootOutcomes,
            NextRecoverableLootDropNumber,
            NextRuntimeAgentId);
    }

    static List<RecoverableLootDrop> CopyDrops(
        IReadOnlyList<RecoverableLootDrop> source)
    {
        var result = new List<RecoverableLootDrop>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }

    static List<AdventurerDeathLootOutcome> CopyDeathOutcomes(
        IReadOnlyList<AdventurerDeathLootOutcome> source)
    {
        var result = new List<AdventurerDeathLootOutcome>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }

    static List<AdventurerEscapeLootOutcome> CopyEscapeOutcomes(
        IReadOnlyList<AdventurerEscapeLootOutcome> source)
    {
        var result = new List<AdventurerEscapeLootOutcome>(source?.Count ?? 0);
        if (source == null)
            return result;
        for (int i = 0; i < source.Count; i++)
            if (source[i] != null)
                result.Add(source[i].Copy());
        return result;
    }
}
