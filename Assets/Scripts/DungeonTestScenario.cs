using System;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] DungeonScenarioPlacedObject entrance;

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
        entrance = capturedEntrance == null
            ? null
            : CreatePlacedObject(
                capturedEntrance.x,
                capturedEntrance.y,
                capturedEntrance.objectId,
                capturedEntrance.prefabName,
                false,
                objectCatalog,
                ObjectPlacementType.Entrance);

        if (!ValidateAuthoredContent(out string contentFailure))
        {
            report = $"Capture is incomplete: {contentFailure}";
            return false;
        }

        report = $"Captured {tileCells.Count} cells, {traps.Count} traps, " +
            $"{floorProps.Count} floor props, and " +
            (entrance != null ? "an entrance." : "no entrance.");
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
        entrance = CopyPlacedObject(source.entrance);
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
        if (!ValidateAuthoredContent(out report))
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

        bool restoredEntrance = entrance != null;
        if (entrance != null && !grid.PlaceEntranceCell(
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
        report = $"Loaded '{scenarioName}': {tileCells.Count} cells, " +
            $"{restoredTraps} traps, {restoredFloorProps} floor props, and " +
            (restoredEntrance ? "an entrance." : "no entrance.");
        return true;
    }

    bool ValidateAuthoredContent(out string report)
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

        if (entrance != null)
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
