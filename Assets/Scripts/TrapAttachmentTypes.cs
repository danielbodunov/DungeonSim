using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrapAttachmentSurface
{
    Floor = 0,
    Ceiling = 1,
    LeftWall = 2,
    RightWall = 3
}

[Flags]
public enum TrapAttachmentSurfaceMask
{
    None = 0,
    Floor = 1 << 0,
    Ceiling = 1 << 1,
    LeftWall = 1 << 2,
    RightWall = 1 << 3,
    All = Floor | Ceiling | LeftWall | RightWall
}

[Serializable]
public readonly struct TrapAttachmentPlacement
{
    public TrapAttachmentSurface Surface { get; }
    public Vector2Int ServiceCell { get; }
    public Vector2Int TargetCell { get; }
    public IReadOnlyList<Vector2Int> MechanismCells { get; }
    public IReadOnlyList<Vector2Int> InfrastructureCells { get; }
    public IReadOnlyList<Vector2Int> HazardCells { get; }

    public TrapAttachmentPlacement(
        TrapAttachmentSurface surface,
        Vector2Int serviceCell,
        Vector2Int targetCell,
        IReadOnlyList<Vector2Int> mechanismCells = null,
        IReadOnlyList<Vector2Int> infrastructureCells = null,
        IReadOnlyList<Vector2Int> hazardCells = null)
    {
        Surface = surface;
        ServiceCell = serviceCell;
        TargetCell = targetCell;
        MechanismCells = mechanismCells ?? new[] { serviceCell };
        InfrastructureCells = infrastructureCells ??
            Array.Empty<Vector2Int>();
        HazardCells = hazardCells ?? new[] { targetCell };
    }

    public IEnumerable<Vector2Int> ReservedCells
    {
        get
        {
            if (MechanismCells != null)
                for (int i = 0; i < MechanismCells.Count; i++)
                    yield return MechanismCells[i];
            if (InfrastructureCells != null)
                for (int i = 0; i < InfrastructureCells.Count; i++)
                    yield return InfrastructureCells[i];
        }
    }
}
