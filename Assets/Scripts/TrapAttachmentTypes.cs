using System;
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

    public TrapAttachmentPlacement(
        TrapAttachmentSurface surface,
        Vector2Int serviceCell,
        Vector2Int targetCell)
    {
        Surface = surface;
        ServiceCell = serviceCell;
        TargetCell = targetCell;
    }
}
