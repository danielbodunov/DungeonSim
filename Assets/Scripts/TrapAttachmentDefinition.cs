using UnityEngine;

/// <summary>Authoring contract for a trap's external mechanism footprint.</summary>
[DisallowMultipleComponent]
public sealed class TrapAttachmentDefinition : MonoBehaviour
{
    [SerializeField] TrapAttachmentSurfaceMask allowedSurfaces =
        TrapAttachmentSurfaceMask.Floor;
    [SerializeField] TrapAttachmentSurface preferredSurface =
        TrapAttachmentSurface.Floor;

    public TrapAttachmentSurfaceMask AllowedSurfaces => allowedSurfaces;
    public TrapAttachmentSurface PreferredSurface => preferredSurface;

    public bool Allows(TrapAttachmentSurface surface) =>
        (allowedSurfaces & ToMask(surface)) != 0;

    public static TrapAttachmentSurfaceMask ToMask(
        TrapAttachmentSurface surface) => surface switch
        {
            TrapAttachmentSurface.Floor => TrapAttachmentSurfaceMask.Floor,
            TrapAttachmentSurface.Ceiling => TrapAttachmentSurfaceMask.Ceiling,
            TrapAttachmentSurface.LeftWall => TrapAttachmentSurfaceMask.LeftWall,
            TrapAttachmentSurface.RightWall => TrapAttachmentSurfaceMask.RightWall,
            _ => TrapAttachmentSurfaceMask.None
        };

    public static Vector2Int GetServiceOffset(
        TrapAttachmentSurface surface) => surface switch
        {
            // Logical Y grows downward in the current dungeon grid.
            TrapAttachmentSurface.Floor => new Vector2Int(0, 1),
            TrapAttachmentSurface.Ceiling => new Vector2Int(0, -1),
            TrapAttachmentSurface.LeftWall => new Vector2Int(-1, 0),
            TrapAttachmentSurface.RightWall => new Vector2Int(1, 0),
            _ => Vector2Int.zero
        };
}
