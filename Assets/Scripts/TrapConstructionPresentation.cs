using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derived trap visuals. Authoritative occupancy remains on CellTrap and the
/// grid; this component can be destroyed and reconstructed from that state.
/// </summary>
[DisallowMultipleComponent]
public sealed class TrapConstructionPresentation : MonoBehaviour
{
    readonly struct RendererEnabledState
    {
        public readonly Renderer Renderer;
        public readonly bool WasEnabled;

        public RendererEnabledState(Renderer renderer)
        {
            Renderer = renderer;
            WasEnabled = renderer.enabled;
        }
    }

    static readonly Dictionary<Color32, Material> fallbackMaterials = new();
    readonly List<Renderer> hiddenGroundRenderers = new();
    readonly List<Vector2Int> suppressedGroundCells = new();
    readonly List<RendererEnabledState> suppressedSurfaceRenderers = new();
    readonly List<GameObject> presentationObjects = new();
    TileConstructionSurfaces targetSurfaces;
    string targetSurfaceId;
    string restoredVariantId;
    bool restored;
    TileGridGenerator groundGrid;

    public static void ApplyCommitted(
        TileGridGenerator grid,
        CellTrap trap,
        TrapAttachmentDefinition definition,
        TrapAttachmentPlacement attachment)
    {
        if (grid == null || trap == null || definition == null)
            return;
        TrapConstructionPresentation presentation =
            trap.gameObject.AddComponent<TrapConstructionPresentation>();
        presentation.Build(grid, definition, attachment, true, null);
    }

    public static GameObject CreatePreview(
        TileGridGenerator grid,
        TrapAttachmentDefinition definition,
        TrapAttachmentPlacement attachment,
        Transform parent)
    {
        if (grid == null || definition == null)
            return null;
        var root = new GameObject("Trap Construction Presentation Preview")
        {
            hideFlags = HideFlags.DontSave
        };
        root.transform.SetParent(parent, false);
        var presentation = root.AddComponent<TrapConstructionPresentation>();
        presentation.Build(grid, definition, attachment, false, root.transform);
        return root;
    }

    public void Restore()
    {
        if (restored)
            return;
        restored = true;
        if (targetSurfaces != null && !string.IsNullOrWhiteSpace(targetSurfaceId))
            targetSurfaces.TrySelectVariant(targetSurfaceId, restoredVariantId);
        for (int i = 0; i < suppressedSurfaceRenderers.Count; i++)
        {
            RendererEnabledState state = suppressedSurfaceRenderers[i];
            if (state.Renderer != null)
                state.Renderer.enabled = state.WasEnabled;
        }
        suppressedSurfaceRenderers.Clear();
        for (int i = 0; i < hiddenGroundRenderers.Count; i++)
            if (hiddenGroundRenderers[i] != null)
                hiddenGroundRenderers[i].enabled = true;
        hiddenGroundRenderers.Clear();
        if (groundGrid != null)
            for (int i = 0; i < suppressedGroundCells.Count; i++)
                groundGrid.SetOrdinaryGroundSuppressed(
                    suppressedGroundCells[i], false);
        suppressedGroundCells.Clear();
        groundGrid = null;
    }

    void OnDestroy() => Restore();

    void Build(
        TileGridGenerator grid,
        TrapAttachmentDefinition definition,
        TrapAttachmentPlacement attachment,
        bool committed,
        Transform previewParent)
    {
        Transform targetAnchor = null;
        TileConstructionModuleVariant targetVariant = null;
        GameObject targetTile = grid.GetCellPresentationObject(attachment.TargetCell);
        if (targetTile != null)
        {
            targetSurfaces = targetTile.GetComponent<TileConstructionSurfaces>();
            if (targetSurfaces != null && targetSurfaces.TryGetTrapSurface(
                    attachment.Surface, out TileConstructionSurfaceSlot surface))
            {
                targetAnchor = surface.Anchor;
                bool hasUsableVariant =
                    surface.ModuleImpact ==
                        TileConstructionModuleImpact.VisualOnly &&
                    targetSurfaces.TryGetVariant(
                        surface.Id,
                        definition.TargetSurfaceVariantId,
                        out targetVariant) &&
                    targetVariant.ModuleRoot != null;
                if (hasUsableVariant && !committed)
                {
                    string selectedVariantId = surface.GetSelectedVariantId();
                    if (!string.IsNullOrWhiteSpace(selectedVariantId) &&
                        targetSurfaces.TryGetVariant(
                            surface.Id,
                            selectedVariantId,
                            out TileConstructionModuleVariant selectedVariant) &&
                        selectedVariant.ModuleRoot != null &&
                        selectedVariant.ModuleRoot != targetVariant.ModuleRoot)
                    {
                        SuppressSurfaceRenderers(selectedVariant.ModuleRoot);
                    }
                }
                if (hasUsableVariant && committed)
                {
                    string priorVariantId = surface.GetSelectedVariantId();
                    if (targetSurfaces.TrySelectVariant(
                            surface.Id, definition.TargetSurfaceVariantId))
                    {
                        targetSurfaceId = surface.Id;
                        restoredVariantId = priorVariantId;
                        if (string.IsNullOrWhiteSpace(restoredVariantId))
                            restoredVariantId =
                                definition.RestoredSurfaceVariantId;
                    }
                }
            }
        }

        bool usesCommittedVariant = committed &&
            !string.IsNullOrWhiteSpace(targetSurfaceId);
        bool usesPreviewVariant = !committed && targetVariant?.ModuleRoot != null;
        if (usesPreviewVariant)
            CreateVariantPreview(targetVariant.ModuleRoot, previewParent);
        else if (!usesCommittedVariant)
            CreateFallbackTargetPresentation(
                grid, definition, attachment, targetAnchor, previewParent);

        CreateCellPresentations(
            grid, attachment.MechanismCells,
            definition.MechanismCellPresentationPrefab,
            definition.MechanismCellColor,
            "Trap Mechanism Cell", committed, previewParent,
            definition.CreateFallbackPresentation);
        CreateCellPresentations(
            grid, attachment.InfrastructureCells,
            definition.InfrastructureCellPresentationPrefab,
            definition.InfrastructureCellColor,
            "Trap Infrastructure Cell", committed, previewParent,
            definition.CreateFallbackPresentation);
    }

    void CreateFallbackTargetPresentation(
        TileGridGenerator grid,
        TrapAttachmentDefinition definition,
        TrapAttachmentPlacement attachment,
        Transform targetAnchor,
        Transform previewParent)
    {
        Vector3 targetPosition = targetAnchor != null
            ? targetAnchor.position
            : grid.GetCellWorldPosition(
                attachment.TargetCell.x, attachment.TargetCell.y);
        CreatePresentationObject(
            definition.TargetSurfacePresentationPrefab,
            targetPosition,
            targetAnchor != null ? targetAnchor.rotation : Quaternion.identity,
            new Vector3(0.58f, 0.58f, 0.06f),
            definition.TargetSurfaceColor,
            "Trap Target Surface",
            previewParent,
            definition.CreateFallbackPresentation);
    }

    void CreateVariantPreview(GameObject moduleRoot, Transform previewParent)
    {
        GameObject instance = Instantiate(moduleRoot, previewParent, true);
        instance.name = $"{moduleRoot.name} (Trap Preview)";
        PrepareVariantPreview(instance);
        instance.SetActive(true);
        presentationObjects.Add(instance);
    }

    void SuppressSurfaceRenderers(GameObject moduleRoot)
    {
        Renderer[] renderers = moduleRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            suppressedSurfaceRenderers.Add(new RendererEnabledState(renderer));
            renderer.enabled = false;
        }
    }

    static void PrepareVariantPreview(GameObject root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            transforms[i].gameObject.hideFlags = HideFlags.DontSave;

        Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
            behaviours[i].enabled = false;
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
        Collider2D[] colliders2D = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
            colliders2D[i].enabled = false;
        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
            rigidbodies[i].detectCollisions = false;
        }
        Rigidbody2D[] rigidbodies2D =
            root.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < rigidbodies2D.Length; i++)
        {
            rigidbodies2D[i].bodyType = RigidbodyType2D.Kinematic;
            rigidbodies2D[i].simulated = false;
        }
    }

    void CreateCellPresentations(
        TileGridGenerator grid,
        IReadOnlyList<Vector2Int> cells,
        GameObject prefab,
        Color color,
        string objectName,
        bool committed,
        Transform previewParent,
        bool createFallback)
    {
        if (cells == null)
            return;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            if (committed && !SuppressOrdinaryGround(grid, cell))
                HideOrdinaryGround(grid.GetCellPresentationObject(cell));
            CreatePresentationObject(
                prefab,
                grid.GetCellWorldPosition(cell.x, cell.y),
                Quaternion.identity,
                new Vector3(0.82f, 0.82f, 0.12f),
                color,
                objectName,
                previewParent,
                createFallback);
        }
    }

    bool SuppressOrdinaryGround(TileGridGenerator grid, Vector2Int cell)
    {
        if (!grid.SetOrdinaryGroundSuppressed(cell, true))
            return false;
        groundGrid = grid;
        suppressedGroundCells.Add(cell);
        return true;
    }

    void HideOrdinaryGround(GameObject cellObject)
    {
        if (cellObject == null)
            return;
        Renderer[] renderers = cellObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;
            renderer.enabled = false;
            hiddenGroundRenderers.Add(renderer);
        }
    }

    void CreatePresentationObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Vector3 fallbackScale,
        Color color,
        string objectName,
        Transform previewParent,
        bool createFallback)
    {
        if (prefab == null && !createFallback)
            return;
        GameObject instance;
        if (prefab != null)
        {
            instance = Instantiate(prefab, position, rotation);
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = fallbackScale;
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetFallbackMaterial(color);
        }

        instance.name = objectName;
        instance.hideFlags = HideFlags.DontSave;
        instance.transform.SetParent(previewParent != null ? previewParent : transform, true);
        presentationObjects.Add(instance);
    }

    static Material GetFallbackMaterial(Color color)
    {
        Color32 key = color;
        if (fallbackMaterials.TryGetValue(key, out Material material) &&
            material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Sprites/Default");
        material = new Material(shader)
        {
            color = color,
            hideFlags = HideFlags.HideAndDontSave
        };
        fallbackMaterials[key] = material;
        return material;
    }
}
