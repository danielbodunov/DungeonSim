using UnityEngine;

/// <summary>
/// Base contract for ordinary content placed on the floor of one built cell.
/// Unlike entrances, doors, and ladders, floor props do not require a
/// topology-sensitive socket.
/// </summary>
[DisallowMultipleComponent]
public class FloorProp : MonoBehaviour
{
    public TileGridGenerator Grid { get; private set; }
    public Vector2Int Cell { get; private set; }
    public bool IsPlaced => Grid != null;

    /// <summary>Optional resolved state persisted with the placement.</summary>
    public virtual bool IsResolvedForSave => false;

    /// <summary>
    /// Gives a floor-prop type a narrow compatibility hook without putting
    /// consumer-specific rules in the grid placement system.
    /// </summary>
    public virtual bool IsCompatibleWith(
        TileGridGenerator grid,
        Vector2Int cell)
    {
        return grid != null && grid.IsPlacedCell(cell.x, cell.y);
    }

    public virtual void Initialize(TileGridGenerator grid, Vector2Int cell)
    {
        Grid = grid;
        Cell = cell;

        // A floor prop can expose one or more gameplay POIs. Bind them
        // explicitly because the prop is not parented under the tile prefab.
        DungeonPointOfInterest[] points =
            GetComponentsInChildren<DungeonPointOfInterest>(true);
        for (int i = 0; i < points.Length; i++)
            if (points[i] != null && points[i].isActiveAndEnabled)
                points[i].Bind(grid, cell);
    }

    public virtual void RestoreResolvedState(bool resolved)
    {
    }
}
