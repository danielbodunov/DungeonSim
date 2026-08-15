using System;
using System.Collections.Generic;
using UnityEngine;

public enum DungeonScenarioEntranceMode
{
    // Default is intentionally zero so existing entrance-less scenario assets
    // retain the historical gameplay-default interpretation after migration.
    Default,
    Manual,
    None
}

[Serializable]
public sealed class DungeonScenarioPlacedObject
{
    public int x;
    public int y;
    public int objectId = -1;
    public string prefabName;
    public GameObject prefab;
    public bool isResolved;
}

/// <summary>
/// Reusable authored dungeon state for deterministic Editor testing.
/// Procedural props are represented by their generation seed and rebuilt
/// through PropGenerator after authored content has been restored.
/// </summary>
[CreateAssetMenu(
    fileName = "DungeonTestScenario",
    menuName = "Dungeon/Test Scenario")]
public sealed class DungeonTestScenario : ScriptableObject
{
    [SerializeField] string scenarioName = "New Dungeon Scenario";
    [SerializeField, TextArea(2, 5)] string description;
    [SerializeField, TextArea(2, 5)] string intendedTestPurpose;
    [SerializeField] int gridWidth;
    [SerializeField] int gridHeight;
    [SerializeField] int propGenerationSeed;
    [SerializeField] List<SavedTileCell> tileCells = new();
    [SerializeField] List<SavedConnectionEdge> connectionEdges = new();
    [SerializeField] List<DungeonScenarioPlacedObject> traps = new();
    [SerializeField] List<DungeonScenarioPlacedObject> floorProps = new();
    [SerializeField] DungeonScenarioEntranceMode entranceMode;
    [SerializeField] DungeonScenarioPlacedObject entrance;
    [SerializeField] bool hasDefaultEntranceCell;
    [SerializeField] Vector2Int defaultEntranceCell;

    public string ScenarioName => scenarioName;
    public string Description => description;
    public string IntendedTestPurpose => intendedTestPurpose;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public int PropGenerationSeed => propGenerationSeed;
    public IReadOnlyList<SavedTileCell> TileCells => tileCells;
    public IReadOnlyList<SavedConnectionEdge> ConnectionEdges => connectionEdges;
    public IReadOnlyList<DungeonScenarioPlacedObject> Traps => traps;
    public IReadOnlyList<DungeonScenarioPlacedObject> FloorProps => floorProps;
    public DungeonScenarioEntranceMode EntranceMode => GetEffectiveEntranceMode();
    public DungeonScenarioPlacedObject Entrance => entrance;

    public void SetMetadata(
        string name,
        string scenarioDescription,
        string testPurpose)
    {
        scenarioName = string.IsNullOrWhiteSpace(name)
            ? "New Dungeon Scenario"
            : name.Trim();
        description = scenarioDescription?.Trim() ?? string.Empty;
        intendedTestPurpose = testPurpose?.Trim() ?? string.Empty;
    }

    public bool CaptureFrom(
        TileGridGenerator grid,
        IReadOnlyList<ObjectData> objectCatalog,
        out string report)
    {
        if (grid == null || !grid.IsInitialized)
        {
            report = "No initialized dungeon grid is available to capture.";
            return false;
        }

        gridWidth = grid.GridWidth;
        gridHeight = grid.GridHeight;
        propGenerationSeed = grid.PropGenerationSeed;
        tileCells = CopyTileCells(grid.CaptureTileLayout());
        connectionEdges = CopyConnectionEdges(grid.CaptureConnectionIntents());
        traps = CapturePlacedObjects(
            grid.CaptureTrapLayout(), objectCatalog, ObjectPlacementType.Trap);
        floorProps = CapturePlacedObjects(
            grid.CaptureFloorPropLayout(), objectCatalog,
            ObjectPlacementType.FloorProp);

        SavedEntrance capturedEntrance = grid.CaptureEntranceLayout();
        if (capturedEntrance != null)
        {
            entranceMode = DungeonScenarioEntranceMode.Manual;
            hasDefaultEntranceCell = false;
            defaultEntranceCell = default;
            entrance = CreatePlacedObject(
                capturedEntrance.x,
                capturedEntrance.y,
                capturedEntrance.objectId,
                capturedEntrance.prefabName,
                false,
                objectCatalog,
                ObjectPlacementType.Entrance);
        }
        else if (grid.TryGetDungeonEntrance(out DungeonEntrance effectiveEntrance))
        {
            entranceMode = DungeonScenarioEntranceMode.Default;
            entrance = null;
            hasDefaultEntranceCell = true;
            defaultEntranceCell = effectiveEntrance.Cell;
            if (FindEntranceOwner(grid) == null)
            {
                report = "Capture is incomplete: the gameplay default entrance " +
                    "has no active NPCTraversal owner assigned to this grid.";
                return false;
            }
            if (!grid.TryValidateDefaultEntrance(
                    defaultEntranceCell, out string entranceFailure))
            {
                report = "Capture is incomplete: the gameplay default entrance " +
                    $"contract is invalid. {entranceFailure}";
                return false;
            }
        }
        else if (grid.PlacedCellCount == 0)
        {
            entranceMode = DungeonScenarioEntranceMode.None;
            entrance = null;
            hasDefaultEntranceCell = false;
            defaultEntranceCell = default;
        }
        else
        {
            report = "Capture is incomplete: the built dungeon has no effective " +
                "manual or default entrance.";
            return false;
        }

        if (!ValidatePrefabReferences(out string contentFailure))
        {
            report = $"Capture is incomplete: {contentFailure}";
            return false;
        }

        report = $"Captured {tileCells.Count} cells, {traps.Count} traps, " +
            $"{floorProps.Count} floor props, and " +
            GetEntranceReportDescription() + ".";
        return true;
    }

    public void CopyFrom(DungeonTestScenario source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        scenarioName = source.scenarioName;
        description = source.description;
        intendedTestPurpose = source.intendedTestPurpose;
        gridWidth = source.gridWidth;
        gridHeight = source.gridHeight;
        propGenerationSeed = source.propGenerationSeed;
        tileCells = CopyTileCells(source.tileCells);
        connectionEdges = CopyConnectionEdges(source.connectionEdges);
        traps = CopyPlacedObjects(source.traps);
        floorProps = CopyPlacedObjects(source.floorProps);
        entranceMode = source.GetEffectiveEntranceMode();
        entrance = CopyPlacedObject(source.entrance);
        hasDefaultEntranceCell = source.hasDefaultEntranceCell;
        defaultEntranceCell = source.defaultEntranceCell;
    }

    public bool TryApplyTo(TileGridGenerator grid, out string report)
    {
        if (grid == null || !grid.IsInitialized)
        {
            report = "No initialized dungeon grid is available to load.";
            return false;
        }
        if (grid.GridWidth != gridWidth || grid.GridHeight != gridHeight)
        {
            report = $"Scenario grid is {gridWidth}x{gridHeight}, but the " +
                $"current scene uses {grid.GridWidth}x{grid.GridHeight}.";
            return false;
        }

        // This entire phase operates on copied solver state and local
        // occupancy reservations. Do not move any production restore or
        // placement call above the authored-content validation boundary.
        if (!grid.TryValidateTileLayout(
                tileCells,
                connectionEdges,
                out TileGridGenerator.PlacementValidationContext
                    placementContext,
                out report))
        {
            report = $"Scenario layout is invalid: {report}";
            return false;
        }
        DungeonScenarioEntranceMode effectiveEntranceMode =
            GetEffectiveEntranceMode();
        if (!ValidateAuthoredContent(
                grid, placementContext, effectiveEntranceMode, out report))
            return false;
        if (!grid.RestoreTileLayout(
                CopyTileCells(tileCells),
                CopyConnectionEdges(connectionEdges)))
        {
            report = "The scenario layout is incompatible with the current tile database.";
            return false;
        }

        int restoredTraps = 0;
        for (int i = 0; i < traps.Count; i++)
        {
            DungeonScenarioPlacedObject trap = traps[i];
            if (!grid.PlaceTrapCell(
                    trap.x, trap.y, ResolvePrefab(trap, ObjectPlacementType.Trap),
                    trap.objectId))
            {
                report = $"Could not restore trap '{trap.prefabName}' at " +
                    $"({trap.x},{trap.y}).";
                return false;
            }
            restoredTraps++;
        }

        if (effectiveEntranceMode == DungeonScenarioEntranceMode.Manual &&
            !grid.PlaceEntranceCell(
                entrance.x,
                entrance.y,
                ResolvePrefab(entrance, ObjectPlacementType.Entrance),
                entrance.objectId))
        {
            report = $"Could not restore entrance '{entrance.prefabName}' at " +
                $"({entrance.x},{entrance.y}).";
            return false;
        }

        int restoredFloorProps = 0;
        for (int i = 0; i < floorProps.Count; i++)
        {
            DungeonScenarioPlacedObject floorProp = floorProps[i];
            if (!grid.PlaceFloorPropCell(
                    floorProp.x,
                    floorProp.y,
                    ResolvePrefab(floorProp, ObjectPlacementType.FloorProp),
                    floorProp.objectId,
                    floorProp.isResolved))
            {
                report = $"Could not restore floor prop '{floorProp.prefabName}' " +
                    $"at ({floorProp.x},{floorProp.y}).";
                return false;
            }
            restoredFloorProps++;
        }

        grid.RegenerateProps(propGenerationSeed);
        if (effectiveEntranceMode == DungeonScenarioEntranceMode.Default)
        {
            NPCTraversal traversal = FindEntranceOwner(grid);
            if (traversal == null || !traversal.EnsureDefaultEntrance())
            {
                report = "The validated layout loaded, but its production " +
                    "default entrance could not be recreated.";
                return false;
            }
        }

        report = $"Loaded '{scenarioName}': {tileCells.Count} cells, " +
            $"{restoredTraps} traps, {restoredFloorProps} floor props, and " +
            GetEntranceReportDescription() + ".";
        return true;
    }

    bool ValidateAuthoredContent(
        TileGridGenerator grid,
        TileGridGenerator.PlacementValidationContext placementContext,
        DungeonScenarioEntranceMode effectiveEntranceMode,
        out string report)
    {
        if (traps == null)
        {
            report = "The scenario trap collection is missing.";
            return false;
        }
        if (floorProps == null)
        {
            report = "The scenario floor-prop collection is missing.";
            return false;
        }

        for (int i = 0; i < traps.Count; i++)
        {
            DungeonScenarioPlacedObject trap = traps[i];
            if (trap == null)
            {
                report = $"Trap record {i + 1} is empty.";
                return false;
            }

            GameObject prefab = ResolvePrefab(trap, ObjectPlacementType.Trap);
            var cell = new Vector2Int(trap.x, trap.y);
            if (!grid.TryValidateTrapPlacement(
                    placementContext, cell, prefab, out string failure))
            {
                report = $"Trap '{trap.prefabName}' at ({trap.x},{trap.y}) " +
                    $"is invalid: {failure}";
                return false;
            }
            placementContext.ReserveTrap(cell);
        }

        if (effectiveEntranceMode == DungeonScenarioEntranceMode.Manual)
        {
            if (!HasManualEntranceRecord())
            {
                report = "The scenario requires a manual entrance, but its " +
                    "entrance record has no prefab identity.";
                return false;
            }
            if (placementContext.CountTopologySensitiveEntrances() > 0)
            {
                report = "The scenario combines a manual entrance with a " +
                    "built-in layout entrance.";
                return false;
            }

            GameObject prefab = ResolvePrefab(
                entrance, ObjectPlacementType.Entrance);
            var cell = new Vector2Int(entrance.x, entrance.y);
            if (!grid.TryValidateEntrancePlacement(
                    placementContext, cell, prefab, out string failure))
            {
                report = $"Entrance '{entrance.prefabName}' at " +
                    $"({entrance.x},{entrance.y}) is invalid: {failure}";
                return false;
            }
            placementContext.ReserveEntrance(cell);
        }
        else if (effectiveEntranceMode == DungeonScenarioEntranceMode.Default)
        {
            if (FindEntranceOwner(grid) == null)
            {
                report = "The scenario requires the gameplay default entrance, " +
                    "but no active NPCTraversal owner is assigned to this grid.";
                return false;
            }
            if (!grid.TryValidateDefaultEntrance(
                    placementContext,
                    hasDefaultEntranceCell
                        ? defaultEntranceCell
                        : (Vector2Int?)null,
                    out string failure))
            {
                report = $"The scenario default entrance is invalid: {failure}";
                return false;
            }
            if (hasDefaultEntranceCell)
                placementContext.ReserveEntrance(defaultEntranceCell);
        }
        else if (placementContext.HasAnyPlacedCell() ||
                 placementContext.CountTopologySensitiveEntrances() > 0)
        {
            report = "A no-entrance scenario cannot contain built dungeon tiles; " +
                "normal gameplay would create a default entrance for them.";
            return false;
        }

        for (int i = 0; i < floorProps.Count; i++)
        {
            DungeonScenarioPlacedObject floorProp = floorProps[i];
            if (floorProp == null)
            {
                report = $"Floor-prop record {i + 1} is empty.";
                return false;
            }

            GameObject prefab = ResolvePrefab(
                floorProp, ObjectPlacementType.FloorProp);
            var cell = new Vector2Int(floorProp.x, floorProp.y);
            if (!grid.TryValidateFloorPropPlacement(
                    placementContext, cell, prefab, out string failure))
            {
                report = $"Floor prop '{floorProp.prefabName}' at " +
                    $"({floorProp.x},{floorProp.y}) is invalid: {failure}";
                return false;
            }
            placementContext.ReserveFloorProp(cell);
        }

        report = string.Empty;
        return true;
    }

    DungeonScenarioEntranceMode GetEffectiveEntranceMode()
    {
        // Older manual-entrance assets predate entranceMode. Their existing
        // meaningful record remains authoritative and migrates without asset
        // rewriting. Unity may deserialize null inline classes as empty
        // records, so object existence alone cannot identify manual authoring.
        return HasManualEntranceRecord()
            ? DungeonScenarioEntranceMode.Manual
            : entranceMode;
    }

    bool HasManualEntranceRecord()
    {
        return entrance != null &&
            (entrance.prefab != null ||
             !string.IsNullOrWhiteSpace(entrance.prefabName) ||
             entrance.objectId >= 0);
    }

    string GetEntranceReportDescription()
    {
        return GetEffectiveEntranceMode() switch
        {
            DungeonScenarioEntranceMode.Manual => "a manual entrance",
            DungeonScenarioEntranceMode.Default => "the gameplay default entrance",
            _ => "no entrance"
        };
    }

    static NPCTraversal FindEntranceOwner(TileGridGenerator grid)
    {
        if (grid == null)
            return null;

        NPCTraversal attached = grid.GetComponent<NPCTraversal>();
        if (attached != null && attached.isActiveAndEnabled &&
            attached.DungeonGrid == grid)
        {
            return attached;
        }

        NPCTraversal[] candidates =
            UnityEngine.Object.FindObjectsByType<NPCTraversal>(
                FindObjectsInactive.Exclude);
        for (int i = 0; i < candidates.Length; i++)
        {
            NPCTraversal candidate = candidates[i];
            if (candidate != null && candidate.isActiveAndEnabled &&
                candidate.DungeonGrid == grid)
            {
                return candidate;
            }
        }
        return null;
    }

    bool ValidatePrefabReferences(out string report)
    {
        for (int i = 0; i < traps.Count; i++)
        {
            GameObject prefab = ResolvePrefab(traps[i], ObjectPlacementType.Trap);
            if (prefab == null || prefab.GetComponent<CellTrap>() == null)
            {
                report = $"Trap '{traps[i].prefabName}' has no valid CellTrap prefab.";
                return false;
            }
        }

        if (GetEffectiveEntranceMode() == DungeonScenarioEntranceMode.Manual)
        {
            GameObject prefab = ResolvePrefab(
                entrance, ObjectPlacementType.Entrance);
            if (prefab == null ||
                prefab.GetComponentInChildren<DungeonEntrance>(true) == null)
            {
                report = $"Entrance '{entrance.prefabName}' has no valid DungeonEntrance prefab.";
                return false;
            }
        }

        for (int i = 0; i < floorProps.Count; i++)
        {
            GameObject prefab = ResolvePrefab(
                floorProps[i], ObjectPlacementType.FloorProp);
            if (prefab == null || prefab.GetComponent<FloorProp>() == null)
            {
                report = $"Floor prop '{floorProps[i].prefabName}' has no valid FloorProp prefab.";
                return false;
            }
        }

        report = string.Empty;
        return true;
    }

    static List<DungeonScenarioPlacedObject> CapturePlacedObjects(
        List<SavedTrapCell> saved,
        IReadOnlyList<ObjectData> objectCatalog,
        ObjectPlacementType placementType)
    {
        var result = new List<DungeonScenarioPlacedObject>();
        if (saved == null)
            return result;

        for (int i = 0; i < saved.Count; i++)
        {
            SavedTrapCell item = saved[i];
            result.Add(CreatePlacedObject(
                item.x, item.y, item.objectId, item.prefabName, false,
                objectCatalog, placementType));
        }
        result.Sort(ComparePlacedObjects);
        return result;
    }

    static List<DungeonScenarioPlacedObject> CapturePlacedObjects(
        List<SavedFloorPropCell> saved,
        IReadOnlyList<ObjectData> objectCatalog,
        ObjectPlacementType placementType)
    {
        var result = new List<DungeonScenarioPlacedObject>();
        if (saved == null)
            return result;

        for (int i = 0; i < saved.Count; i++)
        {
            SavedFloorPropCell item = saved[i];
            result.Add(CreatePlacedObject(
                item.x, item.y, item.objectId, item.prefabName,
                item.isResolved, objectCatalog, placementType));
        }
        result.Sort(ComparePlacedObjects);
        return result;
    }

    static int ComparePlacedObjects(
        DungeonScenarioPlacedObject left,
        DungeonScenarioPlacedObject right)
    {
        int xComparison = left.x.CompareTo(right.x);
        if (xComparison != 0)
            return xComparison;

        int yComparison = left.y.CompareTo(right.y);
        if (yComparison != 0)
            return yComparison;

        return left.objectId.CompareTo(right.objectId);
    }

    static DungeonScenarioPlacedObject CreatePlacedObject(
        int x,
        int y,
        int objectId,
        string prefabName,
        bool isResolved,
        IReadOnlyList<ObjectData> objectCatalog,
        ObjectPlacementType placementType)
    {
        var result = new DungeonScenarioPlacedObject
        {
            x = x,
            y = y,
            objectId = objectId,
            prefabName = prefabName,
            isResolved = isResolved
        };
        result.prefab = FindCatalogPrefab(objectCatalog, objectId, placementType)
            ?? ResolvePrefab(result, placementType);
        return result;
    }

    static GameObject FindCatalogPrefab(
        IReadOnlyList<ObjectData> objectCatalog,
        int objectId,
        ObjectPlacementType placementType)
    {
        if (objectCatalog == null || objectId < 0)
            return null;

        for (int i = 0; i < objectCatalog.Count; i++)
        {
            ObjectData item = objectCatalog[i];
            if (item != null && item.ID == objectId &&
                item.PlacementType == placementType)
            {
                return item.Prefab;
            }
        }
        return null;
    }

    static GameObject ResolvePrefab(
        DungeonScenarioPlacedObject item,
        ObjectPlacementType placementType)
    {
        if (item == null)
            return null;
        if (item.prefab != null)
            return item.prefab;
        if (string.IsNullOrWhiteSpace(item.prefabName))
            return null;

        string folder = placementType == ObjectPlacementType.Trap
            ? "Traps"
            : "Props";
        return Resources.Load<GameObject>($"{folder}/{item.prefabName}")
            ?? Resources.Load<GameObject>(item.prefabName);
    }

    static List<SavedTileCell> CopyTileCells(
        IReadOnlyList<SavedTileCell> source)
    {
        var result = new List<SavedTileCell>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            SavedTileCell item = source[i];
            if (item == null)
                continue;
            result.Add(new SavedTileCell
            {
                x = item.x,
                y = item.y,
                isPlaced = item.isPlaced,
                profileId = item.profileId,
                widthIntent = item.widthIntent
            });
        }
        return result;
    }

    static List<SavedConnectionEdge> CopyConnectionEdges(
        IReadOnlyList<SavedConnectionEdge> source)
    {
        var result = new List<SavedConnectionEdge>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            SavedConnectionEdge item = source[i];
            if (item == null)
                continue;
            result.Add(new SavedConnectionEdge
            {
                fromX = item.fromX,
                fromY = item.fromY,
                toX = item.toX,
                toY = item.toY,
                intent = item.intent
            });
        }
        return result;
    }

    static List<DungeonScenarioPlacedObject> CopyPlacedObjects(
        IReadOnlyList<DungeonScenarioPlacedObject> source)
    {
        var result = new List<DungeonScenarioPlacedObject>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            DungeonScenarioPlacedObject item = CopyPlacedObject(source[i]);
            if (item != null)
                result.Add(item);
        }
        return result;
    }

    static DungeonScenarioPlacedObject CopyPlacedObject(
        DungeonScenarioPlacedObject source)
    {
        if (source == null)
            return null;
        return new DungeonScenarioPlacedObject
        {
            x = source.x,
            y = source.y,
            objectId = source.objectId,
            prefabName = source.prefabName,
            prefab = source.prefab,
            isResolved = source.isResolved
        };
    }
}
