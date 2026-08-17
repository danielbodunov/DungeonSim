using System;
using UnityEngine;

public enum DreadSpendPurpose
{
    TreasureManifestation
}

/// <summary>
/// One request to spend Dread on a concrete per-dungeon growth effect.
/// SpendId is the idempotency key owned by the caller's transaction.
/// </summary>
public readonly struct DreadSpendRequest
{
    public string SpendId { get; }
    public DreadSpendPurpose Purpose { get; }
    public int Amount { get; }
    public int DungeonOpenCount { get; }
    public Vector2Int Cell { get; }
    public int ObjectId { get; }
    public string PrefabName { get; }

    public DreadSpendRequest(
        string spendId,
        DreadSpendPurpose purpose,
        int amount,
        int dungeonOpenCount,
        Vector2Int cell,
        int objectId,
        string prefabName)
    {
        SpendId = spendId;
        Purpose = purpose;
        Amount = Mathf.Max(0, amount);
        DungeonOpenCount = Mathf.Max(0, dungeonOpenCount);
        Cell = cell;
        ObjectId = objectId;
        PrefabName = prefabName;
    }
}

/// <summary>Persistent audit record for one accepted Dread purchase.</summary>
[Serializable]
public sealed class DreadSpendRecord
{
    [SerializeField] string spendId;
    [SerializeField] DreadSpendPurpose purpose;
    [SerializeField, Min(0)] int amount;
    [SerializeField, Min(0)] int dungeonOpenCount;
    [SerializeField] Vector2Int cell;
    [SerializeField] int objectId;
    [SerializeField] string prefabName;
    [SerializeField, Min(0)] int duplicateAttempts;

    public string SpendId => spendId;
    public DreadSpendPurpose Purpose => purpose;
    public int Amount => amount;
    public int DungeonOpenCount => dungeonOpenCount;
    public Vector2Int Cell => cell;
    public int ObjectId => objectId;
    public string PrefabName => prefabName;
    public int DuplicateAttempts => duplicateAttempts;

    public DreadSpendRecord(DreadSpendRequest request)
    {
        spendId = request.SpendId;
        purpose = request.Purpose;
        amount = request.Amount;
        dungeonOpenCount = request.DungeonOpenCount;
        cell = request.Cell;
        objectId = request.ObjectId;
        prefabName = request.PrefabName;
    }

    internal void RecordDuplicateAttempt()
    {
        duplicateAttempts++;
    }

    internal DreadSpendRecord Copy()
    {
        var copy = new DreadSpendRecord(
            new DreadSpendRequest(
                spendId,
                purpose,
                amount,
                dungeonOpenCount,
                cell,
                objectId,
                prefabName));
        copy.duplicateAttempts = duplicateAttempts;
        return copy;
    }
}
