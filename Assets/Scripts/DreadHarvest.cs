using System;
using UnityEngine;

public enum DreadHarvestSource
{
    AdventurerDeath
}

/// <summary>
/// One request to credit Dread from a qualifying dungeon experience. HarvestId
/// is the idempotency key used by the authoritative currency owner.
/// </summary>
public readonly struct DreadHarvestRequest
{
    public string HarvestId { get; }
    public DreadHarvestSource Source { get; }
    public int Amount { get; }
    public string SourceId { get; }
    public string SourceName { get; }
    public int SourceRuntimeAgentId { get; }
    public int SourceLevel { get; }
    public int DungeonOpenCount { get; }
    public Vector2Int Cell { get; }
    public Vector3 WorldPosition { get; }

    public DreadHarvestRequest(
        string harvestId,
        DreadHarvestSource source,
        int amount,
        string sourceId,
        string sourceName,
        int sourceRuntimeAgentId,
        int sourceLevel,
        int dungeonOpenCount,
        Vector2Int cell,
        Vector3 worldPosition)
    {
        HarvestId = harvestId;
        Source = source;
        Amount = Mathf.Max(0, amount);
        SourceId = sourceId;
        SourceName = sourceName;
        SourceRuntimeAgentId = Mathf.Max(0, sourceRuntimeAgentId);
        SourceLevel = Mathf.Max(0, sourceLevel);
        DungeonOpenCount = Mathf.Max(0, dungeonOpenCount);
        Cell = cell;
        WorldPosition = worldPosition;
    }
}

/// <summary>Auditable result of one accepted Dread harvest.</summary>
[Serializable]
public sealed class DreadHarvestRecord
{
    [SerializeField] string harvestId;
    [SerializeField] DreadHarvestSource source;
    [SerializeField, Min(0)] int amount;
    [SerializeField] string sourceId;
    [SerializeField] string sourceName;
    [SerializeField, Min(0)] int sourceRuntimeAgentId;
    [SerializeField, Min(0)] int sourceLevel;
    [SerializeField, Min(0)] int dungeonOpenCount;
    [SerializeField] Vector2Int cell;
    [SerializeField] Vector3 worldPosition;
    [SerializeField, Min(0)] int duplicateAttempts;

    public string HarvestId => harvestId;
    public DreadHarvestSource Source => source;
    public int Amount => amount;
    public string SourceId => sourceId;
    public string SourceName => sourceName;
    public int SourceRuntimeAgentId => sourceRuntimeAgentId;
    public int SourceLevel => sourceLevel;
    public int DungeonOpenCount => dungeonOpenCount;
    public Vector2Int Cell => cell;
    public Vector3 WorldPosition => worldPosition;
    public int DuplicateAttempts => duplicateAttempts;

    public DreadHarvestRecord(DreadHarvestRequest request)
    {
        harvestId = request.HarvestId;
        source = request.Source;
        amount = request.Amount;
        sourceId = request.SourceId;
        sourceName = request.SourceName;
        sourceRuntimeAgentId = request.SourceRuntimeAgentId;
        sourceLevel = request.SourceLevel;
        dungeonOpenCount = request.DungeonOpenCount;
        cell = request.Cell;
        worldPosition = request.WorldPosition;
    }

    internal void RecordDuplicateAttempt()
    {
        duplicateAttempts++;
    }

    internal DreadHarvestRecord Copy()
    {
        var copy = new DreadHarvestRecord(
            new DreadHarvestRequest(
                harvestId,
                source,
                amount,
                sourceId,
                sourceName,
                sourceRuntimeAgentId,
                sourceLevel,
                dungeonOpenCount,
                cell,
                worldPosition));
        copy.duplicateAttempts = duplicateAttempts;
        return copy;
    }
}
