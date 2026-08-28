using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Derived trap visuals. Authoritative occupancy remains on CellTrap and the
/// grid; this component can be destroyed and reconstructed from that state.
/// </summary>
[DisallowMultipleComponent]
public sealed class TrapConstructionPresentation : MonoBehaviour
{
    static readonly Dictionary<Color32, Material> fallbackMaterials = new();
    readonly List<Renderer> hiddenGroundRenderers = new();
    readonly List<GameObject> presentationObjects = new();
    TileConstructionSurfaces targetSurfaces;
    string targetSurfaceId;
    string restoredVariantId;
    bool restored;

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
        for (int i = 0; i < hiddenGroundRenderers.Count; i++)
            if (hiddenGroundRenderers[i] != null)
                hiddenGroundRenderers[i].enabled = true;
        hiddenGroundRenderers.Clear();
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
        GameObject targetTile = grid.GetCellPresentationObject(attachment.TargetCell);
        if (targetTile != null)
        {
            targetSurfaces = targetTile.GetComponent<TileConstructionSurfaces>();
            if (targetSurfaces != null && targetSurfaces.TryGetTrapSurface(
                    attachment.Surface, out TileConstructionSurfaceSlot surface))
            {
                targetAnchor = surface.Anchor;
                string priorVariantId = surface.GetSelectedVariantId();
                if (committed &&
                    surface.ModuleImpact == TileConstructionModuleImpact.VisualOnly &&
                    targetSurfaces.TrySelectVariant(
                        surface.Id, definition.TargetSurfaceVariantId))
                {
                    targetSurfaceId = surface.Id;
                    restoredVariantId = priorVariantId;
                    if (string.IsNullOrWhiteSpace(restoredVariantId))
                        restoredVariantId = definition.RestoredSurfaceVariantId;
                }
            }
        }

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
            if (committed)
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
