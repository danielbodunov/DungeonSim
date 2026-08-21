using UnityEngine;

/// <summary>Base contract for a trap affecting one traversable dungeon cell.</summary>
public abstract class CellTrap : MonoBehaviour
{
    public TileGridGenerator Grid { get; private set; }
    public Vector2Int Cell { get; private set; }
    public Vector2Int ServiceCell { get; private set; }
    public TrapAttachmentSurface AttachmentSurface { get; private set; }
    public Vector3 HazardDirection { get; private set; }

    public virtual void Initialize(TileGridGenerator grid, Vector2Int cell)
    {
        Initialize(
            grid,
            new TrapAttachmentPlacement(
                TrapAttachmentSurface.Floor,
                cell,
                cell));
    }

    public virtual void Initialize(
        TileGridGenerator grid,
        TrapAttachmentPlacement attachment)
    {
        Grid = grid;
        Cell = attachment.TargetCell;
        ServiceCell = attachment.ServiceCell;
        AttachmentSurface = attachment.Surface;
        HazardDirection = grid != null
            ? (grid.GetWorldPosition(Cell.x, Cell.y) -
                grid.GetWorldPosition(ServiceCell.x, ServiceCell.y)).normalized
            : Vector3.zero;
    }

    public abstract void OnNpcEntered(NPCCharacter npc);
}
