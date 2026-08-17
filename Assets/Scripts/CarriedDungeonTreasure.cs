using System;
using UnityEngine;

/// <summary>
/// Visit-local custody record for physical loot held by an adventurer.
/// The class name is retained for compatibility with the original treasure path.
/// </summary>
[Serializable]
public sealed class CarriedDungeonTreasure
{
    [SerializeField] string treasureId;
    [SerializeField, Min(0)] int value;
    [SerializeField] Vector2Int sourceCell;
    [SerializeField] bool originatedAsDungeonBait;
    [SerializeField] bool hasSourceCell;

    public string TreasureId => treasureId;
    public int Value => value;
    public Vector2Int SourceCell => sourceCell;
    public bool OriginatedAsDungeonBait => originatedAsDungeonBait;
    public RecoverableLootOrigin Origin => originatedAsDungeonBait
        ? RecoverableLootOrigin.DungeonTreasure
        : RecoverableLootOrigin.AdventurerPossession;
    public bool HasSourceCell => hasSourceCell;

    public CarriedDungeonTreasure(
        string treasureId,
        int value,
        Vector2Int sourceCell,
        bool originatedAsDungeonBait)
        : this(
            treasureId,
            value,
            originatedAsDungeonBait
                ? RecoverableLootOrigin.DungeonTreasure
                : RecoverableLootOrigin.AdventurerPossession,
            sourceCell,
            originatedAsDungeonBait)
    {
    }

    public CarriedDungeonTreasure(
        string treasureId,
        int value,
        RecoverableLootOrigin origin,
        Vector2Int sourceCell,
        bool hasSourceCell)
    {
        this.treasureId = treasureId;
        this.value = Mathf.Max(0, value);
        this.sourceCell = sourceCell;
        originatedAsDungeonBait = origin == RecoverableLootOrigin.DungeonTreasure;
        this.hasSourceCell = hasSourceCell;
    }
}
