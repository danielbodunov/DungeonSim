using UnityEngine;
using System.Collections.Generic;

/// <summary>Authoring contract for a trap's external mechanism footprint.</summary>
[DisallowMultipleComponent]
public sealed class TrapAttachmentDefinition : MonoBehaviour
{
    [SerializeField] TrapAttachmentSurfaceMask allowedSurfaces =
        TrapAttachmentSurfaceMask.Floor;
    [SerializeField] TrapAttachmentSurface preferredSurface =
        TrapAttachmentSurface.Floor;
    [Tooltip("Additional mechanism cells in local coordinates. X is lateral; " +
        "Y points from the primary service cell toward the target corridor.")]
    [SerializeField] Vector2Int[] additionalMechanismCells =
        System.Array.Empty<Vector2Int>();
    [Tooltip("Reserved support/infrastructure cells using the same local axes.")]
    [SerializeField] Vector2Int[] infrastructureCells =
        System.Array.Empty<Vector2Int>();
    [Tooltip("Additional affected cells relative to the target corridor. " +
        "These describe hazard volume and are not service reservations.")]
    [SerializeField] Vector2Int[] additionalHazardCells =
        System.Array.Empty<Vector2Int>();

    [Header("Construction Presentation")]
    [SerializeField] string targetSurfaceVariantId = "TrapOpening";
    [SerializeField] string restoredSurfaceVariantId = "Default";
    [SerializeField] GameObject targetSurfacePresentationPrefab;
    [SerializeField] GameObject mechanismCellPresentationPrefab;
    [SerializeField] GameObject infrastructureCellPresentationPrefab;
    [SerializeField] bool createFallbackPresentation = true;
    [SerializeField] Color targetSurfaceColor = new(0.16f, 0.08f, 0.04f, 1f);
    [SerializeField] Color mechanismCellColor = new(0.48f, 0.2f, 0.06f, 1f);
    [SerializeField] Color infrastructureCellColor = new(0.2f, 0.28f, 0.34f, 1f);

    public TrapAttachmentSurfaceMask AllowedSurfaces => allowedSurfaces;
    public TrapAttachmentSurface PreferredSurface => preferredSurface;
    public string TargetSurfaceVariantId => targetSurfaceVariantId;
    public string RestoredSurfaceVariantId => restoredSurfaceVariantId;
    public GameObject TargetSurfacePresentationPrefab =>
        targetSurfacePresentationPrefab;
    public GameObject MechanismCellPresentationPrefab =>
        mechanismCellPresentationPrefab;
    public GameObject InfrastructureCellPresentationPrefab =>
        infrastructureCellPresentationPrefab;
    public bool CreateFallbackPresentation => createFallbackPresentation;
    public Color TargetSurfaceColor => targetSurfaceColor;
    public Color MechanismCellColor => mechanismCellColor;
    public Color InfrastructureCellColor => infrastructureCellColor;

    public TrapAttachmentPlacement ResolvePlacement(
        TrapAttachmentSurface surface,
        Vector2Int serviceCell,
        Vector2Int targetCell)
    {
        Vector2Int forward = targetCell - serviceCell;
        Vector2Int right = new(forward.y, -forward.x);
        var mechanism = new List<Vector2Int> { serviceCell };
        AppendResolvedOffsets(
            mechanism, serviceCell, right, forward, additionalMechanismCells);
        var infrastructure = new List<Vector2Int>();
        AppendResolvedOffsets(
            infrastructure, serviceCell, right, forward, infrastructureCells);
        var hazard = new List<Vector2Int> { targetCell };
        AppendResolvedOffsets(
            hazard, targetCell, right, forward, additionalHazardCells);
        return new TrapAttachmentPlacement(
            surface, serviceCell, targetCell,
            mechanism.ToArray(), infrastructure.ToArray(), hazard.ToArray());
    }

    static void AppendResolvedOffsets(
        List<Vector2Int> destination,
        Vector2Int origin,
        Vector2Int right,
        Vector2Int forward,
        Vector2Int[] offsets)
    {
        if (offsets == null)
            return;
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int offset = offsets[i];
            Vector2Int cell = origin + right * offset.x + forward * offset.y;
            if (!destination.Contains(cell))
                destination.Add(cell);
        }
    }

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
