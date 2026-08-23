using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileConstructionSurfaceKind
{
    Floor,
    Ceiling,
    NorthWall,
    SouthWall,
    EastWall,
    WestWall,
    TrapServiceRegion
}

public enum TileConstructionModuleImpact
{
    VisualOnly,
    RequiresTopologyResolution
}

[Serializable]
public sealed class TileConstructionModuleVariant
{
    [SerializeField] string id = "Default";
    [SerializeField] GameObject moduleRoot;

    public string Id => id;
    public GameObject ModuleRoot => moduleRoot;
}

[Serializable]
public sealed class TileConstructionSurfaceSlot
{
    [SerializeField] string id;
    [SerializeField] TileConstructionSurfaceKind kind;
    [SerializeField] Transform anchor;
    [SerializeField] TileConstructionModuleImpact moduleImpact;
    [SerializeField] TrapAttachmentSurfaceMask trapAttachmentSurfaces;
    [SerializeField] List<TileConstructionModuleVariant> variants = new();

    public string Id => id;
    public TileConstructionSurfaceKind Kind => kind;
    public Transform Anchor => anchor;
    public TileConstructionModuleImpact ModuleImpact => moduleImpact;
    public TrapAttachmentSurfaceMask TrapAttachmentSurfaces =>
        trapAttachmentSurfaces;
    public IReadOnlyList<TileConstructionModuleVariant> Variants => variants;

    public bool SupportsTrapAttachment(TrapAttachmentSurface surface) =>
        (trapAttachmentSurfaces & TrapAttachmentDefinition.ToMask(surface)) != 0;

    internal bool TrySelectVariant(string variantId)
    {
        if (moduleImpact != TileConstructionModuleImpact.VisualOnly)
            return false;
        bool found = false;
        for (int i = 0; i < variants.Count; i++)
        {
            TileConstructionModuleVariant variant = variants[i];
            bool selected = variant != null &&
                string.Equals(variant.Id, variantId, StringComparison.Ordinal);
            if (variant?.ModuleRoot != null)
                variant.ModuleRoot.SetActive(selected);
            found |= selected;
        }
        return found;
    }
}

/// <summary>
/// Authored, topology-neutral construction modules on a resolved dungeon tile.
/// TileSocketProfile remains authoritative for connections and traversal.
/// </summary>
[DisallowMultipleComponent]
public sealed class TileConstructionSurfaces : MonoBehaviour
{
    [SerializeField] List<TileConstructionSurfaceSlot> surfaces = new();

    public IReadOnlyList<TileConstructionSurfaceSlot> Surfaces => surfaces;

    public bool TryGetSurface(
        string surfaceId,
        out TileConstructionSurfaceSlot surface)
    {
        for (int i = 0; i < surfaces.Count; i++)
        {
            TileConstructionSurfaceSlot candidate = surfaces[i];
            if (candidate != null && string.Equals(
                    candidate.Id, surfaceId, StringComparison.Ordinal))
            {
                surface = candidate;
                return true;
            }
        }
        surface = null;
        return false;
    }

    public bool TrySelectVariant(string surfaceId, string variantId) =>
        TryGetSurface(surfaceId, out TileConstructionSurfaceSlot surface) &&
        surface.TrySelectVariant(variantId);

    public bool TryGetTrapSurface(
        TrapAttachmentSurface attachmentSurface,
        out TileConstructionSurfaceSlot surface)
    {
        for (int i = 0; i < surfaces.Count; i++)
        {
            TileConstructionSurfaceSlot candidate = surfaces[i];
            if (candidate != null && candidate.Anchor != null &&
                candidate.SupportsTrapAttachment(attachmentSurface))
            {
                surface = candidate;
                return true;
            }
        }
        surface = null;
        return false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < surfaces.Count; i++)
        {
            TileConstructionSurfaceSlot surface = surfaces[i];
            if (surface == null || string.IsNullOrWhiteSpace(surface.Id) ||
                !ids.Add(surface.Id))
                Debug.LogWarning(
                    $"{name} has a missing or duplicate construction surface ID.",
                    this);
            if (surface?.Anchor == null ||
                (surface.Anchor != transform &&
                 !surface.Anchor.IsChildOf(transform)))
                Debug.LogWarning(
                    $"Construction surface '{surface?.Id}' needs an anchor " +
                    "inside its tile prefab.", this);
        }
    }
#endif
}
