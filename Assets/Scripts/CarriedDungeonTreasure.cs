using System;
using UnityEngine;

/// <summary>Visit-local custody record for treasure taken from the dungeon.</summary>
[Serializable]
public sealed class CarriedDungeonTreasure
{
    [SerializeField] string treasureId;
    [SerializeField, Min(0)] int value;
    [SerializeField] Vector2Int sourceCell;
    [SerializeField] bool originatedAsDungeonBait;

    public string TreasureId => treasureId;
    public int Value => value;
    public Vector2Int SourceCell => sourceCell;
    public bool OriginatedAsDungeonBait => originatedAsDungeonBait;

    public CarriedDungeonTreasure(
        string treasureId,
        int value,
        Vector2Int sourceCell,
        bool originatedAsDungeonBait)
    {
        this.treasureId = treasureId;
        this.value = Mathf.Max(0, value);
        this.sourceCell = sourceCell;
        this.originatedAsDungeonBait = originatedAsDungeonBait;
    }
}
