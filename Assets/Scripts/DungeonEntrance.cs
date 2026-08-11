using UnityEngine;

/// <summary>
/// Semantic entry and exit point hosted by a resolved dungeon tile.
/// The marker's transform is the authored adventurer spawn position.
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonEntrance : MonoBehaviour
{
    TileGridGenerator owningGrid;
    Vector2Int cell;
    bool isBound;

    public Vector3 EntryPosition => transform.position;
    public Quaternion EntryRotation => transform.rotation;
    public Vector2Int Cell => cell;
    public bool IsBound => isBound;
    public TileGridGenerator OwningGrid => owningGrid;

    internal void Bind(TileGridGenerator grid, Vector2Int owningCell)
    {
        owningGrid = grid;
        cell = owningCell;
        isBound = grid != null;
    }
}
