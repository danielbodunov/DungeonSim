using UnityEngine;
using System.Collections.Generic;

/// <summary>Base contract for a trap affecting one traversable dungeon cell.</summary>
public abstract class CellTrap : MonoBehaviour
{
    public TileGridGenerator Grid { get; private set; }
    public Vector2Int Cell { get; private set; }
    public Vector2Int ServiceCell { get; private set; }
    public TrapAttachmentSurface AttachmentSurface { get; private set; }
    public Vector3 HazardDirection { get; private set; }
    public IReadOnlyList<Vector2Int> MechanismCells { get; private set; }
    public IReadOnlyList<Vector2Int> InfrastructureCells { get; private set; }
    public IReadOnlyList<Vector2Int> HazardCells { get; private set; }

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
        MechanismCells = attachment.MechanismCells;
        InfrastructureCells = attachment.InfrastructureCells;
        HazardCells = attachment.HazardCells;
        HazardDirection = grid != null
            ? (grid.GetWorldPosition(Cell.x, Cell.y) -
                grid.GetWorldPosition(ServiceCell.x, ServiceCell.y)).normalized
            : Vector3.zero;
    }

    public bool ReservesCell(Vector2Int cell)
    {
        if (MechanismCells != null)
            for (int i = 0; i < MechanismCells.Count; i++)
                if (MechanismCells[i] == cell)
                    return true;
        if (InfrastructureCells != null)
            for (int i = 0; i < InfrastructureCells.Count; i++)
                if (InfrastructureCells[i] == cell)
                    return true;
        return false;
    }

    public abstract void OnNpcEntered(NPCCharacter npc);
}
