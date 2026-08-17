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
    [SerializeField] RecoverableLootContentKind contentKind;
    [SerializeField] PhysicalResourceCategory resourceCategory;
    [SerializeField, Min(0)] int resourceQuantity;

    public string TreasureId => treasureId;
    public int Value => IsPhysicalResource ? UnitValue * ResourceQuantity : value;
    public int UnitValue => IsPhysicalResource ? Mathf.Max(0, value) : 0;
    public Vector2Int SourceCell => sourceCell;
    public bool OriginatedAsDungeonBait => originatedAsDungeonBait;
    public RecoverableLootOrigin Origin => originatedAsDungeonBait
        ? RecoverableLootOrigin.DungeonTreasure
        : RecoverableLootOrigin.AdventurerPossession;
    public bool HasSourceCell => hasSourceCell;
    public RecoverableLootContentKind ContentKind => contentKind;
    public bool IsPhysicalResource =>
        contentKind == RecoverableLootContentKind.PhysicalResource;
    public PhysicalResourceCategory ResourceCategory => resourceCategory;
    public int ResourceQuantity => IsPhysicalResource
        ? Mathf.Max(1, resourceQuantity)
        : 0;

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
            originatedAsDungeonBait,
            RecoverableLootContentKind.Treasure,
            default,
            0)
    {
    }

    public CarriedDungeonTreasure(
        string treasureId,
        int value,
        RecoverableLootOrigin origin,
        Vector2Int sourceCell,
        bool hasSourceCell)
        : this(
            treasureId,
            value,
            origin,
            sourceCell,
            hasSourceCell,
            RecoverableLootContentKind.Treasure,
            default,
            0)
    {
    }

    public CarriedDungeonTreasure(AdventurerResourcePayload resource)
        : this(
            resource?.ResourceId,
            resource?.UnitValue ?? 0,
            RecoverableLootOrigin.AdventurerPossession,
            default,
            false,
            RecoverableLootContentKind.PhysicalResource,
            resource?.Category ?? default,
            resource?.Quantity ?? 1)
    {
    }

    public CarriedDungeonTreasure(
        string treasureId,
        int value,
        RecoverableLootOrigin origin,
        Vector2Int sourceCell,
        bool hasSourceCell,
        RecoverableLootContentKind contentKind,
        PhysicalResourceCategory resourceCategory,
        int resourceQuantity)
    {
        this.treasureId = treasureId;
        this.value = Mathf.Max(0, value);
        this.sourceCell = sourceCell;
        originatedAsDungeonBait = origin == RecoverableLootOrigin.DungeonTreasure;
        this.hasSourceCell = hasSourceCell;
        this.contentKind = contentKind;
        this.resourceCategory = resourceCategory;
        this.resourceQuantity = contentKind == RecoverableLootContentKind.PhysicalResource
            ? Mathf.Max(1, resourceQuantity)
            : 0;
    }
}
