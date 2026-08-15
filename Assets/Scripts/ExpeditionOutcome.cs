using System;
using UnityEngine;

/// <summary>The authoritative reason an adventurer's dungeon visit ended.</summary>
public enum ExpeditionOutcomeType
{
    SuccessfulEscape,
    Retreated,
    Defeated
}

/// <summary>
/// Immutable input used by the gameplay loop to publish one completed visit.
/// Consequence systems remain responsible for loot and Aura; this request only
/// summarizes their accepted results.
/// </summary>
public readonly struct ExpeditionOutcomeRequest
{
    public string ExpeditionId { get; }
    public ExpeditionOutcomeType Outcome { get; }
    public string AdventurerId { get; }
    public string AdventurerName { get; }
    public int RuntimeAgentId { get; }
    public int AdventurerLevel { get; }
    public int DungeonOpenCount { get; }
    public Vector2Int StartCell { get; }
    public Vector2Int CompletionCell { get; }
    public Vector3 WorldPosition { get; }
    public int VisitedCellCount { get; }
    public int CarriedTreasureItemCount { get; }
    public int CarriedTreasureValue { get; }
    public int LostTreasureItemCount { get; }
    public int LostTreasureValue { get; }
    public int RecoveredTreasureItemCount { get; }
    public int RecoveredTreasureValue { get; }
    public string RecoveryDropId { get; }
    public int AuraHarvested { get; }
    public int VisitAuraSettled { get; }
    public string AuraHarvestId { get; }

    public ExpeditionOutcomeRequest(
        string expeditionId,
        ExpeditionOutcomeType outcome,
        string adventurerId,
        string adventurerName,
        int runtimeAgentId,
        int adventurerLevel,
        int dungeonOpenCount,
        Vector2Int startCell,
        Vector2Int completionCell,
        Vector3 worldPosition,
        int visitedCellCount,
        int carriedTreasureItemCount,
        int carriedTreasureValue,
        int lostTreasureItemCount,
        int lostTreasureValue,
        int recoveredTreasureItemCount,
        int recoveredTreasureValue,
        string recoveryDropId,
        int auraHarvested,
        int visitAuraSettled,
        string auraHarvestId)
    {
        ExpeditionId = expeditionId;
        Outcome = outcome;
        AdventurerId = adventurerId;
        AdventurerName = adventurerName;
        RuntimeAgentId = Mathf.Max(0, runtimeAgentId);
        AdventurerLevel = Mathf.Max(0, adventurerLevel);
        DungeonOpenCount = Mathf.Max(0, dungeonOpenCount);
        StartCell = startCell;
        CompletionCell = completionCell;
        WorldPosition = worldPosition;
        VisitedCellCount = Mathf.Max(0, visitedCellCount);
        CarriedTreasureItemCount = Mathf.Max(0, carriedTreasureItemCount);
        CarriedTreasureValue = Mathf.Max(0, carriedTreasureValue);
        LostTreasureItemCount = Mathf.Max(0, lostTreasureItemCount);
        LostTreasureValue = Mathf.Max(0, lostTreasureValue);
        RecoveredTreasureItemCount = Mathf.Max(0, recoveredTreasureItemCount);
        RecoveredTreasureValue = Mathf.Max(0, recoveredTreasureValue);
        RecoveryDropId = recoveryDropId;
        AuraHarvested = Mathf.Max(0, auraHarvested);
        VisitAuraSettled = Mathf.Max(0, visitAuraSettled);
        AuraHarvestId = auraHarvestId;
    }
}

/// <summary>
/// Read-only, auditable summary emitted after all consequences for one visit
/// have been finalized.
/// </summary>
[Serializable]
public sealed class ExpeditionOutcomeRecord
{
    [SerializeField] string expeditionId;
    [SerializeField] ExpeditionOutcomeType outcome;
    [SerializeField] string adventurerId;
    [SerializeField] string adventurerName;
    [SerializeField, Min(0)] int runtimeAgentId;
    [SerializeField, Min(0)] int adventurerLevel;
    [SerializeField, Min(0)] int dungeonOpenCount;
    [SerializeField] Vector2Int startCell;
    [SerializeField] Vector2Int completionCell;
    [SerializeField] Vector3 worldPosition;
    [SerializeField, Min(0)] int visitedCellCount;
    [SerializeField, Min(0)] int carriedTreasureItemCount;
    [SerializeField, Min(0)] int carriedTreasureValue;
    [SerializeField, Min(0)] int lostTreasureItemCount;
    [SerializeField, Min(0)] int lostTreasureValue;
    [SerializeField, Min(0)] int recoveredTreasureItemCount;
    [SerializeField, Min(0)] int recoveredTreasureValue;
    [SerializeField] string recoveryDropId;
    [SerializeField, Min(0)] int auraHarvested;
    [SerializeField, Min(0)] int visitAuraSettled;
    [SerializeField] string auraHarvestId;
    [SerializeField, Min(0)] int duplicateCompletionAttempts;

    public string ExpeditionId => expeditionId;
    public ExpeditionOutcomeType Outcome => outcome;
    public string AdventurerId => adventurerId;
    public string AdventurerName => adventurerName;
    public int RuntimeAgentId => runtimeAgentId;
    public int AdventurerLevel => adventurerLevel;
    public int DungeonOpenCount => dungeonOpenCount;
    public Vector2Int StartCell => startCell;
    public Vector2Int CompletionCell => completionCell;
    public Vector3 WorldPosition => worldPosition;
    public int VisitedCellCount => visitedCellCount;
    public int CarriedTreasureItemCount => carriedTreasureItemCount;
    public int CarriedTreasureValue => carriedTreasureValue;
    public int LostTreasureItemCount => lostTreasureItemCount;
    public int LostTreasureValue => lostTreasureValue;
    public int RecoveredTreasureItemCount => recoveredTreasureItemCount;
    public int RecoveredTreasureValue => recoveredTreasureValue;
    public string RecoveryDropId => recoveryDropId;
    public int AuraHarvested => auraHarvested;
    public int VisitAuraSettled => visitAuraSettled;
    public int TotalAuraAwarded => auraHarvested + visitAuraSettled;
    public string AuraHarvestId => auraHarvestId;
    public int DuplicateCompletionAttempts => duplicateCompletionAttempts;

    public ExpeditionOutcomeRecord(ExpeditionOutcomeRequest request)
    {
        expeditionId = request.ExpeditionId;
        outcome = request.Outcome;
        adventurerId = request.AdventurerId;
        adventurerName = request.AdventurerName;
        runtimeAgentId = request.RuntimeAgentId;
        adventurerLevel = request.AdventurerLevel;
        dungeonOpenCount = request.DungeonOpenCount;
        startCell = request.StartCell;
        completionCell = request.CompletionCell;
        worldPosition = request.WorldPosition;
        visitedCellCount = request.VisitedCellCount;
        carriedTreasureItemCount = request.CarriedTreasureItemCount;
        carriedTreasureValue = request.CarriedTreasureValue;
        lostTreasureItemCount = request.LostTreasureItemCount;
        lostTreasureValue = request.LostTreasureValue;
        recoveredTreasureItemCount = request.RecoveredTreasureItemCount;
        recoveredTreasureValue = request.RecoveredTreasureValue;
        recoveryDropId = request.RecoveryDropId;
        auraHarvested = request.AuraHarvested;
        visitAuraSettled = request.VisitAuraSettled;
        auraHarvestId = request.AuraHarvestId;
    }

    internal void RecordDuplicateCompletionAttempt()
    {
        duplicateCompletionAttempts++;
    }
}
