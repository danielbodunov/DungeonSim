using UnityEngine;

/// <summary>Base contract for one trap occupying one dungeon cell.</summary>
public abstract class CellTrap : MonoBehaviour
{
    public TileGridGenerator Grid { get; private set; }
    public Vector2Int Cell { get; private set; }

    public virtual void Initialize(TileGridGenerator grid, Vector2Int cell)
    {
        Grid = grid;
        Cell = cell;
    }

    public abstract void OnNpcEntered(NPCCharacter npc);
}
