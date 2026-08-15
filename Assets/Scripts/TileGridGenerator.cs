using UnityEngine;
using System.Collections.Generic;

public class TileGridGenerator : MonoBehaviour
{
    const string GroundTileName = "Ground_Full_X";
    const string EntranceStructureId = "Entrance";
    static readonly Vector2Int[] CardinalOffsets =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    internal sealed class ValidatedTileLayout
    {
        public readonly Dictionary<Vector2Int, int> assignments = new();
        public readonly HashSet<Vector2Int> placedCells = new();
        public readonly Dictionary<Vector2Int, CellWidthIntent> widthIntents =
            new();
        public readonly List<SavedConnectionEdge> connectionIntents = new();
        public readonly Dictionary<string, ConnectionIntent> intentsByEdge =
            new(System.StringComparer.Ordinal);
        public List<int>[,] cellOptions;
    }

    /// <summary>
    /// Read-only view of either the current grid or a validated incoming
    /// layout. Reservations are local to this context and let callers validate
    /// a sequence of authored placements without changing the dungeon.
    /// </summary>
    public sealed class PlacementValidationContext
    {
        readonly TileGridGenerator owner;
        readonly ValidatedTileLayout layout;
        readonly HashSet<Vector2Int> reservedTraps = new();
        readonly HashSet<Vector2Int> reservedFloorProps = new();
        Vector2Int? reservedEntrance;

        internal PlacementValidationContext(
            TileGridGenerator owningGrid,
            ValidatedTileLayout validatedLayout)
        {
            owner = owningGrid;
            layout = validatedLayout;
        }

        public bool IsPlacedCell(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            return layout != null
                ? layout.placedCells.Contains(cell)
                : owner != null && owner.IsPlacedCell(x, y);
        }

        public IReadOnlyList<BakedPropSocket> GetCellPropSockets(
            int x,
            int y)
        {
            TileSocketProfile profile = GetCellProfile(new Vector2Int(x, y));
            return profile?.propSockets
                ?? (IReadOnlyList<BakedPropSocket>)System.Array.Empty<BakedPropSocket>();
        }

        internal bool BelongsTo(TileGridGenerator grid) => owner == grid;

        internal void ResetReservations()
        {
            reservedTraps.Clear();
            reservedFloorProps.Clear();
            reservedEntrance = null;
        }

        internal bool HasTrap(Vector2Int cell)
        {
            if (reservedTraps.Contains(cell))
                return true;
            return layout == null &&
                owner.placedTraps.TryGetValue(cell, out CellTrap trap) &&
                trap != null;
        }

        internal bool HasFloorProp(Vector2Int cell)
        {
            if (reservedFloorProps.Contains(cell))
                return true;
            return layout == null &&
                owner.placedFloorProps.TryGetValue(cell, out FloorProp floorProp) &&
                floorProp != null;
        }

        internal bool HasEntranceAt(Vector2Int cell)
        {
            if (reservedEntrance.HasValue && reservedEntrance.Value == cell)
                return true;
            return layout == null && owner.placedEntrance != null &&
                owner.placedEntranceCell == cell;
        }

        internal bool HasEntrance => reservedEntrance.HasValue ||
            (layout == null && owner.placedEntrance != null &&
                !owner.placedEntranceIsFallback);

        internal void ReserveTrap(Vector2Int cell) => reservedTraps.Add(cell);
        internal void ReserveFloorProp(Vector2Int cell) =>
            reservedFloorProps.Add(cell);
        internal void ReserveEntrance(Vector2Int cell) => reservedEntrance = cell;

        internal bool HasTopologySensitiveEntrance(Vector2Int cell)
        {
            return GetTopologySensitiveEntranceCount(cell) > 0;
        }

        internal int CountTopologySensitiveEntrances()
        {
            if (owner == null)
                return 0;

            int count = 0;
            for (int x = 0; x < owner.width; x++)
            for (int y = 0; y < owner.height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (IsPlacedCell(x, y))
                    count += GetTopologySensitiveEntranceCount(cell);
            }
            return count;
        }

        internal bool HasAnyPlacedCell()
        {
            if (owner == null)
                return false;

            for (int x = 0; x < owner.width; x++)
            for (int y = 0; y < owner.height; y++)
                if (IsPlacedCell(x, y))
                    return true;
            return false;
        }

        int GetTopologySensitiveEntranceCount(Vector2Int cell)
        {
            if (layout == null && owner.instantiated != null &&
                cell.x >= 0 && cell.y >= 0 &&
                cell.x < owner.instantiated.GetLength(0) &&
                cell.y < owner.instantiated.GetLength(1))
            {
                GameObject instance = owner.instantiated[cell.x, cell.y];
                return instance != null
                    ? instance.GetComponentsInChildren<DungeonEntrance>(true).Length
                    : 0;
            }

            TileSocketProfile profile = GetCellProfile(cell);
            return profile != null && profile.sourcePrefab != null
                ? profile.sourcePrefab
                    .GetComponentsInChildren<DungeonEntrance>(true).Length
                : 0;
        }

        internal bool HasPointOfInterest(Vector2Int cell)
        {
            if (layout == null)
            {
                if (!owner.pointsOfInterest.TryGetValue(cell, out var points))
                    return false;
                for (int i = 0; i < points.Count; i++)
                {
                    DungeonPointOfInterest point = points[i];
                    if (point != null && point.IsBound && point.Cell == cell)
                        return true;
                }
                return false;
            }

            TileSocketProfile profile = GetCellProfile(cell);
            return profile != null && profile.sourcePrefab != null &&
                profile.sourcePrefab
                    .GetComponentInChildren<DungeonPointOfInterest>(true) != null;
        }

        internal bool IsGeneratedPropOccupied(Vector2Int cell)
        {
            return layout == null && owner.propGenerator != null &&
                owner.propGenerator.IsCellOccupiedByGeneratedProp(cell);
        }

        TileSocketProfile GetCellProfile(Vector2Int cell)
        {
            if (owner == null || cell.x < 0 || cell.y < 0 ||
                cell.x >= owner.width || cell.y >= owner.height)
            {
                return null;
            }

            if (layout != null)
            {
                return layout.assignments.TryGetValue(cell, out int tileIndex)
                    ? owner.database.tiles[tileIndex]
                    : null;
            }

            if (owner.cells == null || owner.cells[cell.x, cell.y].Count != 1)
                return null;
            return owner.database.tiles[owner.cells[cell.x, cell.y][0]];
        }
    }

    [SerializeField] TileAdjacencyDatabase database;
    [SerializeField] GameObject placeholderPrefab;
    [SerializeField] PropGenerator propGenerator;
    [SerializeField] int width = 32;
    [SerializeField] int height = 32;
    [SerializeField] Vector2 origin = Vector2.zero;
    [SerializeField] Vector2 generationDirection = new Vector2(1, -1);
    [SerializeField, Min(1000)] int localSearchNodeLimit = 25000;

    List<GameObject> prefabs;
    int groundTileIndex = -1;

    Dictionary<int, HashSet<int>> north;
    Dictionary<int, HashSet<int>> south;
    Dictionary<int, HashSet<int>> east;
    Dictionary<int, HashSet<int>> west;

    List<int>[,] cells;

    GameObject[,] instantiated;
    bool[,] placed;
    bool[,] fixedGround;
    CellWidthIntent[,] widthIntents;
    ConnectionIntent[,] eastConnectionIntents;
    ConnectionIntent[,] southConnectionIntents;
    readonly Dictionary<Vector2Int, CellTrap> placedTraps = new();
    readonly Dictionary<Vector2Int, FloorProp> placedFloorProps = new();
    readonly Dictionary<Vector2Int, List<DungeonPointOfInterest>> pointsOfInterest =
        new();
    readonly Dictionary<Vector2Int, int> placedTrapObjectIds = new();
    readonly Dictionary<Vector2Int, string> placedTrapPrefabNames = new();
    readonly Dictionary<Vector2Int, int> placedFloorPropObjectIds = new();
    readonly Dictionary<Vector2Int, string> placedFloorPropPrefabNames = new();
    Transform trapContainer;
    Transform floorPropContainer;
    DungeonEntrance placedEntrance;
    GameObject placedEntranceInstance;
    Vector2Int placedEntranceCell;
    int placedEntranceObjectId = -1;
    string placedEntrancePrefabName;
    bool placedEntranceIsFallback;
    Transform entranceContainer;
    PlacementValidationContext livePlacementValidationContext;

    public event System.Action LayoutChanged;

    void Start()
    {
        BuildRuntimeDatabase();
        InitializeGrid();
        InstantiateGrid();

        if (propGenerator == null)
            propGenerator = GetComponent<PropGenerator>();
        if (propGenerator == null)
            propGenerator = gameObject.AddComponent<PropGenerator>();

        propGenerator.Initialize(this);
        propGenerator.GenerateProps();
    }

    // ---- 1. Runtime DB ----

    void BuildRuntimeDatabase()
    {
        prefabs = new List<GameObject>();
        north = new();
        south = new();
        east  = new();
        west  = new();

        for (int i = 0; i < database.tiles.Count; i++)
            prefabs.Add(database.tiles[i].sourcePrefab);

        groundTileIndex = FindTileIndex(GroundTileName);

        for (int i = 0; i < database.tiles.Count; i++)
        {
            north[i] = BuildSet(database.tiles[i].northMatches, "north", i );
            south[i] = BuildSet(database.tiles[i].southMatches, "south", i);
            east[i]  = BuildSet(database.tiles[i].eastMatches, "east", i);
            west[i]  = BuildSet(database.tiles[i].westMatches, "west", i);
        }
        Debug.Log($"Runtime database built with {prefabs.Count} tiles.");
    }

    int FindTileIndex(string baseTileName)
    {
        for (int i = 0; i < database.tiles.Count; i++)
            if (database.tiles[i].baseTileName == baseTileName)
                return i;

        Debug.LogError($"Tile database does not contain required tile '{baseTileName}'.");
        return -1;
    }

    //build a set of tile indices that match the given list of side matches
    HashSet<int> BuildSet(List<string> names, string direction = "", int tileIndex = -1)
    {
        var set = new HashSet<int>();

        for (int i = 0; i < database.tiles.Count; i++)
            if (names.Contains(GetProfileId(database.tiles[i])))
            {
                set.Add(i);
            }
                

        Debug.Log($"Built set with {set.Count} tiles.");
        if (set.Count == 0)
            Debug.LogWarning($"Warning: Tile {database.tiles[tileIndex].sourcePrefab.name} has no matches on its {direction} side.");
        return set;
    }

    static string GetProfileId(TileSocketProfile profile)
    {
        return $"{profile.baseTileName}_R{profile.rotation}";
    }

    Quaternion GetTileRotation(int tileIndex)
    {
        // Baked rotation indices progress clockwise when viewed from +Z.
        return Quaternion.Euler(
            0f,
            0f,
            database.tiles[tileIndex].rotation * -90f);
    }

    GameObject InstantiateTile(int tileIndex, Vector3 position)
    {
        return Instantiate(
            prefabs[tileIndex], position, GetTileRotation(tileIndex), transform);
    }

    // ---- 2. Grid ----

    void InitializeGrid()
    {
        cells = CreateInitialCellOptions();
        instantiated = new GameObject[width, height];
        placed = new bool[width, height];
        fixedGround = new bool[width, height];
        widthIntents = new CellWidthIntent[width, height];
        eastConnectionIntents = new ConnectionIntent[width, height];
        southConnectionIntents = new ConnectionIntent[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if(x == 0 || y == 0 || x == width - 1 || y == height - 1)
                fixedGround[x, y] = true;
        }
    }

    List<int>[,] CreateInitialCellOptions()
    {
        var initialCells = new List<int>[width, height];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            initialCells[x, y] = new List<int>();
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                initialCells[x, y].Add(groundTileIndex);
                continue;
            }

            for (int i = 0; i < prefabs.Count; i++)
                initialCells[x, y].Add(i);
        }

        // The border cells are pre-collapsed to the ground tile. Their
        // constraints must be applied before the player can collapse a cell.
        for (int x = 0; x < width; x++)
        {
            Propagate(initialCells, x, 0);
            if (height > 1)
                Propagate(initialCells, x, height - 1);
        }

        for (int y = 1; y < height - 1; y++)
        {
            Propagate(initialCells, 0, y);
            if (width > 1)
                Propagate(initialCells, width - 1, y);
        }

        return initialCells;
    }

    // ---- 3–5. Collapse + Propagation ----
    //Clear all options for cell except tileIndex, then propagate constraints
    void Collapse(int x, int y, int tileIndex)
    {
        cells[x, y].Clear();
        cells[x, y].Add(tileIndex);
        InstantiateCell(x, y, tileIndex);
        Debug.Log($"Collapsed cell ({x},{y}) to tile {prefabs[tileIndex].name}");
        Propagate(x, y);
    }

    //Propagate constraints from cell (x,y) to neighbors, and recursively propagate if neighbors are constrained
    void Propagate(int startX, int startY)
    {
        Propagate(cells, startX, startY);
    }

    void Propagate(List<int>[,] targetCells, int startX, int startY)
    {
        Queue<Vector2Int> queue = new();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();

            // Grid rows increase downward in world space, so y + 1 is south.
            ConstrainNeighbor(
                targetCells, p.x, p.y, p.x, p.y + 1, south, queue);
            ConstrainNeighbor(
                targetCells, p.x, p.y, p.x, p.y - 1, north, queue);
            ConstrainNeighbor(
                targetCells, p.x, p.y, p.x + 1, p.y, east, queue);
            ConstrainNeighbor(
                targetCells, p.x, p.y, p.x - 1, p.y, west, queue);
        }
    }

    //Set neighbor's options to the subset of its current options that are allowed by the source cell's options and the adjacency rules, and if any options were removed, add the neighbor to the queue to propagate from it
    void ConstrainNeighbor(
        List<int>[,] targetCells,
        int x, int y,
        int nx, int ny,
        Dictionary<int, HashSet<int>> rule,
        Queue<Vector2Int> queue)
    {
        //if neighbor is out of bounds, ignore
        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
        {
            return;
        }
        var sourceTiles = targetCells[x, y];
        var neighbor = targetCells[nx, ny];

        var allowed = new HashSet<int>();

        //build set of allowed tiles for neighbor based on source tiles and adjacency rules
        foreach (var tile in sourceTiles)
        {
            HashSet<int> matches = rule[tile];
            foreach (var match in matches){
                allowed.Add(match);
            }
        }
            

        bool changed = false;

        //remove any tiles from neighbor that aren't in allowed set
        for (int i = neighbor.Count - 1; i >= 0; i--)
        {
            if (!allowed.Contains(neighbor[i]))
            {
                neighbor.RemoveAt(i);
                changed = true;
            }
        }
        
        //if neighbor was changed, add it to the queue to propagate constraints from it
        if (changed)
            queue.Enqueue(new Vector2Int(nx, ny));
    }

    // ---- Solver ----
    //While there are cells with more than 1 option, find the one with the lowest entropy (fewest options), randomly pick one of its options, and collapse it to that option
    void Solve()
    {
        while (true)
        {
            var cell = FindLowestEntropyCell();
            if (!cell.HasValue)
                break;

           Vector2Int coord = cell.Value;
           int x = coord.x;
           int y = coord.y;


            List<int> options = cells[x, y];
            int pick = options[Random.Range(0, options.Count)];

            Collapse(x, y, pick);
            Debug.Log($"Collapsing cell ({x},{y}) with {options.Count} options to tile {prefabs[pick].name}");
        }
    }

    Vector2Int? FindLowestEntropyCell()
    {
        int bestCount = int.MaxValue;
        Vector2Int? best = null;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            int count = cells[x, y].Count;
            if (count > 1 && count < bestCount)
            {
                bestCount = count;
                best = new Vector2Int(x, y);
            }
        }

        return best;
    }

    public int GridWidth => width;
    public int GridHeight => height;
    public Vector2 GridOrigin => origin;
    public Vector2 GridGenerationDirection => generationDirection;
    public bool IsInitialized => cells != null && instantiated != null && placed != null;
    public bool HasManualEntrance => placedEntrance != null && !placedEntranceIsFallback;
    public bool HasFallbackEntrance => placedEntrance != null && placedEntranceIsFallback;
    public int PropGenerationSeed => propGenerator != null
        ? propGenerator.SaveGenerationSeed
        : 0;
    public int PlacedCellCount
    {
        get
        {
            if (placed == null)
                return 0;

            int count = 0;
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (placed[x, y])
                    count++;
            return count;
        }
    }

    public bool IsFixedGround(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        return fixedGround[x, y];
    }

    public bool IsPlacedCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        return placed[x, y];
    }

    public CellWidthIntent GetCellWidthIntent(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || widthIntents == null)
            return CellWidthIntent.Auto;

        return widthIntents[x, y];
    }

    public void NotifyLayoutChanged()
    {
        RefreshPlacedEntrance();
        RefreshPlacedFloorProps();
        if (propGenerator != null)
            propGenerator.GenerateProps();

        LayoutChanged?.Invoke();
    }

    void RefreshPlacedFloorProps()
    {
        var cellsToRemove = new List<Vector2Int>();
        PlacementValidationContext validationContext =
            GetLivePlacementValidationContext();
        foreach (KeyValuePair<Vector2Int, FloorProp> pair in placedFloorProps)
        {
            FloorProp floorProp = pair.Value;
            if (floorProp == null ||
                !IsPlacedCell(pair.Key.x, pair.Key.y) ||
                !floorProp.IsCompatibleWith(validationContext, pair.Key))
            {
                cellsToRemove.Add(pair.Key);
                continue;
            }

            floorProp.transform.SetPositionAndRotation(
                GetCellWorldPosition(pair.Key.x, pair.Key.y),
                Quaternion.identity);
            floorProp.Initialize(this, pair.Key);
        }

        for (int i = 0; i < cellsToRemove.Count; i++)
            RemoveFloorPropAtCell(cellsToRemove[i]);
    }

    void RefreshPlacedEntrance()
    {
        if (placedEntrance == null)
            return;

        BakedPropSocket socket = FindEntranceSocket(
            placedEntranceCell.x, placedEntranceCell.y);
        if (!IsPlacedCell(placedEntranceCell.x, placedEntranceCell.y) ||
            socket == null || !TryGetPropSocketWorldPose(
                placedEntranceCell.x,
                placedEntranceCell.y,
                socket,
                out Vector3 position,
                out Quaternion rotation))
        {
            ClearEntrance();
            return;
        }

        placedEntranceInstance.transform.SetPositionAndRotation(position, rotation);
        placedEntrance.Bind(this, placedEntranceCell);
    }

    public void RegenerateProps(int generationSeed)
    {
        if (propGenerator != null)
            propGenerator.GenerateProps(generationSeed);
    }

    public List<SavedTileCell> CaptureTileLayout()
    {
        var result = new List<SavedTileCell>();
        if (!IsInitialized)
            return result;

        for (int x = 1; x < width - 1; x++)
        for (int y = 1; y < height - 1; y++)
        {
            // Save all visibly resolved cells, including player-cleared ground,
            // plus any placed cell. Unresolved placeholders are reconstructed by
            // propagating constraints from these stable assignments.
            bool isResolved = cells[x, y].Count == 1;
            if (!isResolved && !placed[x, y])
                continue;

            result.Add(new SavedTileCell
            {
                x = x,
                y = y,
                isPlaced = placed[x, y],
                widthIntent = widthIntents[x, y],
                profileId = isResolved
                    ? GetProfileId(database.tiles[cells[x, y][0]])
                    : string.Empty
            });
        }
        return result;
    }

    public List<SavedConnectionEdge> CaptureConnectionIntents()
    {
        var result = new List<SavedConnectionEdge>();
        if (!IsInitialized || eastConnectionIntents == null ||
            southConnectionIntents == null)
        {
            return result;
        }

        for (int x = 1; x < width - 1; x++)
        for (int y = 1; y < height - 1; y++)
        {
            if (x + 1 < width - 1 &&
                eastConnectionIntents[x, y] != ConnectionIntent.Auto)
            {
                result.Add(new SavedConnectionEdge
                {
                    fromX = x,
                    fromY = y,
                    toX = x + 1,
                    toY = y,
                    intent = eastConnectionIntents[x, y]
                });
            }

            if (y + 1 < height - 1 &&
                southConnectionIntents[x, y] != ConnectionIntent.Auto)
            {
                result.Add(new SavedConnectionEdge
                {
                    fromX = x,
                    fromY = y,
                    toX = x,
                    toY = y + 1,
                    intent = southConnectionIntents[x, y]
                });
            }
        }

        return result;
    }

    public bool RestoreTileLayout(
        List<SavedTileCell> savedCells,
        List<SavedConnectionEdge> savedConnections = null)
    {
        if (!TryBuildValidatedTileLayout(
                savedCells,
                savedConnections,
                out ValidatedTileLayout validated,
                out string failure))
        {
            Debug.LogWarning(failure, this);
            return false;
        }

        // No live state changes occur above this point. The validated option
        // grid is applied directly so restoration cannot discover a layout
        // contradiction after outgoing content has been cleared.
        propGenerator?.ClearGeneratedProps();
        ClearTraps();
        ClearFloorProps();
        ClearEntrance();
        DestroyInstantiatedGrid();
        InitializeGrid();
        cells = CopyCellOptions(validated.cellOptions);

        foreach (SavedConnectionEdge edge in validated.connectionIntents)
        {
            SetStoredConnectionIntent(
                new Vector2Int(edge.fromX, edge.fromY),
                new Vector2Int(edge.toX, edge.toY),
                edge.intent);
        }

        foreach (KeyValuePair<Vector2Int, int> assignment in validated.assignments)
        {
            Vector2Int position = assignment.Key;
            placed[position.x, position.y] =
                validated.placedCells.Contains(position);
            widthIntents[position.x, position.y] =
                validated.widthIntents[position];
        }

        InstantiateGrid();
        LayoutChanged?.Invoke();
        return true;
    }

    public bool TryValidateTileLayout(
        List<SavedTileCell> savedCells,
        List<SavedConnectionEdge> savedConnections,
        out PlacementValidationContext placementContext,
        out string failure)
    {
        placementContext = null;
        if (!TryBuildValidatedTileLayout(
                savedCells,
                savedConnections,
                out ValidatedTileLayout validated,
                out failure))
        {
            return false;
        }

        placementContext = new PlacementValidationContext(this, validated);
        return true;
    }

    bool TryBuildValidatedTileLayout(
        List<SavedTileCell> savedCells,
        List<SavedConnectionEdge> savedConnections,
        out ValidatedTileLayout validated,
        out string failure)
    {
        validated = null;
        if (!IsInitialized)
        {
            failure = "The dungeon grid is not initialized.";
            return false;
        }
        if (savedCells == null)
        {
            failure = "The tile layout is missing.";
            return false;
        }

        var tileIndicesById = new Dictionary<string, int>(
            System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < database.tiles.Count; i++)
            tileIndicesById[GetProfileId(database.tiles[i])] = i;

        var result = new ValidatedTileLayout();
        foreach (SavedTileCell savedCell in savedCells)
        {
            if (savedCell == null)
            {
                failure = "The tile layout contains an empty cell record.";
                return false;
            }

            var position = new Vector2Int(savedCell.x, savedCell.y);
            if (savedCell.x <= 0 || savedCell.y <= 0 ||
                savedCell.x >= width - 1 || savedCell.y >= height - 1)
            {
                failure = $"Tile cell ({savedCell.x},{savedCell.y}) is outside " +
                    "the editable grid bounds.";
                return false;
            }
            if (result.assignments.ContainsKey(position))
            {
                failure = $"Tile cell ({savedCell.x},{savedCell.y}) is duplicated.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(savedCell.profileId) ||
                !tileIndicesById.TryGetValue(savedCell.profileId, out int tileIndex))
            {
                failure = $"Tile cell ({savedCell.x},{savedCell.y}) references " +
                    $"unknown profile '{savedCell.profileId}'.";
                return false;
            }

            result.assignments[position] = tileIndex;
            result.widthIntents[position] = System.Enum.IsDefined(
                typeof(CellWidthIntent), savedCell.widthIntent)
                    ? savedCell.widthIntent
                    : CellWidthIntent.Auto;
            if (savedCell.isPlaced)
                result.placedCells.Add(position);
        }

        var savedEdgeKeys = new HashSet<string>();
        if (savedConnections != null)
        {
            foreach (SavedConnectionEdge edge in savedConnections)
            {
                if (edge == null)
                {
                    failure = "The tile layout contains an empty connection record.";
                    return false;
                }
                if (!System.Enum.IsDefined(typeof(ConnectionIntent), edge.intent))
                {
                    failure = $"Connection edge ({edge.fromX},{edge.fromY}) to " +
                        $"({edge.toX},{edge.toY}) has an invalid intent.";
                    return false;
                }
                if (edge.intent == ConnectionIntent.Auto)
                {
                    continue;
                }

                var from = new Vector2Int(edge.fromX, edge.fromY);
                var to = new Vector2Int(edge.toX, edge.toY);
                if (!IsInteriorCell(from) || !IsInteriorCell(to) ||
                    !AreCardinalNeighbors(from, to) ||
                    !result.placedCells.Contains(from) ||
                    !result.placedCells.Contains(to))
                {
                    failure = $"Connection edge {from} to {to} must join two " +
                        "adjacent built cells.";
                    return false;
                }

                string edgeKey = GetCanonicalEdgeKey(from, to);
                if (!savedEdgeKeys.Add(edgeKey))
                {
                    failure = $"Connection edge {from} to {to} is duplicated.";
                    return false;
                }
                result.connectionIntents.Add(new SavedConnectionEdge
                {
                    fromX = edge.fromX,
                    fromY = edge.fromY,
                    toX = edge.toX,
                    toY = edge.toY,
                    intent = edge.intent
                });
                result.intentsByEdge[edgeKey] = edge.intent;
            }
        }

        if (!AssignmentsRespectConnectionIntents(
                result.assignments, result.intentsByEdge))
        {
            failure = "The tile assignments conflict with their connection intents.";
            return false;
        }

        result.cellOptions = CreateInitialCellOptions();
        foreach (KeyValuePair<Vector2Int, int> assignment in result.assignments)
        {
            Vector2Int position = assignment.Key;
            result.cellOptions[position.x, position.y].Clear();
            result.cellOptions[position.x, position.y].Add(assignment.Value);
        }

        foreach (Vector2Int position in result.assignments.Keys)
            Propagate(result.cellOptions, position.x, position.y);

        if (HasContradiction(result.cellOptions))
        {
            failure = "The tile assignments conflict with the current adjacency " +
                "database.";
            return false;
        }

        validated = result;
        failure = string.Empty;
        return true;
    }

    void DestroyInstantiatedGrid()
    {
        if (instantiated == null)
            return;

        for (int x = 0; x < instantiated.GetLength(0); x++)
        for (int y = 0; y < instantiated.GetLength(1); y++)
        {
            GameObject instance = instantiated[x, y];
            if (instance == null)
                continue;
            instance.SetActive(false);
            Destroy(instance);
        }
    }

    public string GetCellProfileId(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || cells[x, y].Count != 1)
            return "Unresolved";

        return GetProfileId(database.tiles[cells[x, y][0]]);
    }

    public IReadOnlyList<BakedPropSocket> GetCellPropSockets(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || cells[x, y].Count != 1)
            return System.Array.Empty<BakedPropSocket>();

        return database.tiles[cells[x, y][0]].propSockets
            ?? (IReadOnlyList<BakedPropSocket>)System.Array.Empty<BakedPropSocket>();
    }

    public bool HasMatchingVerticalEdge(int x, int upperY, int lowerY)
    {
        if (x < 0 || x >= width || upperY < 0 || lowerY < 0 ||
            upperY >= height || lowerY >= height || lowerY != upperY + 1 ||
            cells[x, upperY].Count != 1 || cells[x, lowerY].Count != 1)
            return false;

        TileSocketProfile upper = database.tiles[cells[x, upperY][0]];
        TileSocketProfile lower = database.tiles[cells[x, lowerY][0]];
        return upper.southHash == lower.northHash &&
            HasOpening(upper.southHash);
    }

    public bool HasMatchingHorizontalEdge(int leftX, int rightX, int y)
    {
        if (leftX < 0 || rightX < 0 || y < 0 ||
            leftX >= width || rightX >= width || y >= height ||
            rightX != leftX + 1 ||
            cells[leftX, y].Count != 1 || cells[rightX, y].Count != 1)
            return false;

        TileSocketProfile left = database.tiles[cells[leftX, y][0]];
        TileSocketProfile right = database.tiles[cells[rightX, y][0]];
        return left.eastHash == right.westHash &&
            HasOpening(left.eastHash);
    }

    public bool TryGetPropSocketWorldPose(
        int x,
        int y,
        BakedPropSocket socket,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;
        if (x < 0 || x >= width || y < 0 || y >= height ||
            socket == null || instantiated[x, y] == null)
            return false;

        Transform tileTransform = instantiated[x, y].transform;
        position = tileTransform.TransformPoint(socket.localPosition);
        rotation = tileTransform.rotation * socket.localRotation;
        return true;
    }

    public PropSocketDirection GetRuntimeSocketDirection(BakedPropSocket socket)
    {
        if (socket == null)
            return PropSocketDirection.North;

        return socket.direction;
    }

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        return new Vector3(
            origin.x-.5f + x * generationDirection.x,
            origin.y-.5f + y * generationDirection.y,
            0
        );
    }

    public Vector3 GetWorldPosition(int x, int y) => GetCellWorldPosition(x, y);

    public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int coordinates)
    {
        coordinates = GetGridCoordinates(worldPosition);
        return coordinates.x >= 0 && coordinates.y >= 0 &&
            coordinates.x < width && coordinates.y < height;
    }

    PlacementValidationContext GetLivePlacementValidationContext()
    {
        livePlacementValidationContext ??=
            new PlacementValidationContext(this, null);
        livePlacementValidationContext.ResetReservations();
        return livePlacementValidationContext;
    }

    bool TryValidatePlacementContext(
        PlacementValidationContext context,
        out string failure)
    {
        if (context == null || !context.BelongsTo(this))
        {
            failure = "The placement validation context does not belong to this grid.";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    public bool TryValidateTrapPlacement(
        PlacementValidationContext context,
        Vector2Int cell,
        GameObject trapPrefab,
        out string failure)
    {
        if (!TryValidatePlacementContext(context, out failure))
            return false;
        if (trapPrefab == null)
        {
            failure = "A trap prefab must be assigned before it can be placed.";
            return false;
        }
        if (trapPrefab.GetComponent<CellTrap>() == null)
        {
            failure = $"Trap prefab '{trapPrefab.name}' needs a component " +
                "derived from CellTrap on its root.";
            return false;
        }
        if (!context.IsPlacedCell(cell.x, cell.y))
        {
            failure = $"Traps can only be placed on a built dungeon tile. " +
                $"Cell ({cell.x},{cell.y}) is not available.";
            return false;
        }
        if (context.HasTrap(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains a trap.";
            return false;
        }
        if (context.HasFloorProp(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains a floor prop.";
            return false;
        }
        if (context.HasEntranceAt(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains the dungeon entrance.";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    public bool TryValidateEntrancePlacement(
        PlacementValidationContext context,
        Vector2Int cell,
        GameObject entrancePrefab,
        out string failure)
    {
        if (!TryValidatePlacementContext(context, out failure))
            return false;
        if (entrancePrefab == null)
        {
            failure = "An entrance prefab must be assigned before it can be placed.";
            return false;
        }
        if (entrancePrefab.GetComponentInChildren<DungeonEntrance>(true) == null)
        {
            failure = $"Entrance prefab '{entrancePrefab.name}' needs a " +
                "DungeonEntrance component.";
            return false;
        }
        if (!context.IsPlacedCell(cell.x, cell.y))
        {
            failure = $"Entrances can only be placed on a built dungeon tile. " +
                $"Cell ({cell.x},{cell.y}) is not available.";
            return false;
        }
        if (context.HasEntrance)
        {
            failure = "The dungeon already contains a manually placed entrance.";
            return false;
        }
        if (context.HasTrap(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains a trap.";
            return false;
        }
        if (context.HasFloorProp(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains a floor prop.";
            return false;
        }
        if (FindEntranceSocket(
                context.GetCellPropSockets(cell.x, cell.y)) == null)
        {
            failure = $"Cell ({cell.x},{cell.y}) does not provide a compatible " +
                "entrance socket.";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates the layout-owned portion of the normal default-entrance
    /// contract without instantiating or removing dungeon content.
    /// </summary>
    public bool TryValidateDefaultEntrance(
        PlacementValidationContext context,
        out string failure)
    {
        return TryValidateDefaultEntrance(context, null, out failure);
    }

    public bool TryValidateDefaultEntrance(
        PlacementValidationContext context,
        Vector2Int? expectedCell,
        out string failure)
    {
        if (!TryValidatePlacementContext(context, out failure))
            return false;

        int authoredMarkerCount = context.CountTopologySensitiveEntrances();
        if (authoredMarkerCount > 1)
        {
            failure = "The layout contains multiple built-in dungeon entrances.";
            return false;
        }
        if (expectedCell.HasValue)
        {
            Vector2Int cell = expectedCell.Value;
            if (!context.IsPlacedCell(cell.x, cell.y))
            {
                failure = $"The captured default entrance cell ({cell.x},{cell.y}) " +
                    "is not a built dungeon tile.";
                return false;
            }
            if (context.HasTrap(cell) || context.HasFloorProp(cell))
            {
                failure = $"The captured default entrance cell ({cell.x},{cell.y}) " +
                    "is occupied by other authored content.";
                return false;
            }
            if (authoredMarkerCount == 1 &&
                !context.HasTopologySensitiveEntrance(cell))
            {
                failure = $"The layout's built-in entrance is not at the captured " +
                    $"default entrance cell ({cell.x},{cell.y}).";
                return false;
            }
        }
        if (authoredMarkerCount == 1)
        {
            failure = string.Empty;
            return true;
        }
        if (!context.HasAnyPlacedCell())
        {
            failure = "A default entrance requires at least one built dungeon tile.";
            return false;
        }

        GameObject fallbackPrefab =
            Resources.Load<GameObject>("Props/DungeonEntrance");
        if (fallbackPrefab == null)
        {
            failure = "The default entrance prefab could not be resolved from " +
                "Resources/Props/DungeonEntrance.";
            return false;
        }
        if (fallbackPrefab.GetComponentInChildren<DungeonEntrance>(true) == null)
        {
            failure = "The default entrance prefab has no DungeonEntrance component.";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    public bool TryValidateDefaultEntrance(out string failure)
    {
        return TryValidateDefaultEntrance(
            GetLivePlacementValidationContext(), out failure);
    }

    public bool TryValidateDefaultEntrance(
        Vector2Int expectedCell,
        out string failure)
    {
        return TryValidateDefaultEntrance(
            GetLivePlacementValidationContext(), expectedCell, out failure);
    }

    public bool TryValidateFloorPropPlacement(
        PlacementValidationContext context,
        Vector2Int cell,
        GameObject floorPropPrefab,
        out string failure)
    {
        if (!TryValidatePlacementContext(context, out failure))
            return false;
        if (floorPropPrefab == null)
        {
            failure = "A floor prop prefab must be assigned before it can be placed.";
            return false;
        }

        FloorProp floorProp = floorPropPrefab.GetComponent<FloorProp>();
        if (floorProp == null)
        {
            failure = $"Floor prop prefab '{floorPropPrefab.name}' needs a " +
                "FloorProp component on its root.";
            return false;
        }
        if (!context.IsPlacedCell(cell.x, cell.y))
        {
            failure = $"Floor props can only be placed on a built dungeon tile. " +
                $"Cell ({cell.x},{cell.y}) is not available.";
            return false;
        }
        if (!floorProp.IsCompatibleWith(context, cell))
        {
            failure = $"'{floorPropPrefab.name}' is not compatible with cell " +
                $"({cell.x},{cell.y}).";
            return false;
        }
        if (context.HasFloorProp(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains a floor prop.";
            return false;
        }
        if (context.HasTrap(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains a trap.";
            return false;
        }
        if (context.HasEntranceAt(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains the dungeon entrance.";
            return false;
        }
        if (context.HasTopologySensitiveEntrance(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) contains topology-sensitive " +
                "entrance content.";
            return false;
        }
        if (context.HasPointOfInterest(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) already contains interactive content.";
            return false;
        }
        if (context.IsGeneratedPropOccupied(cell))
        {
            failure = $"Cell ({cell.x},{cell.y}) is occupied by " +
                "topology-sensitive content.";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    public bool HasPlacedFloorProp(Vector2Int cell)
    {
        if (!placedFloorProps.TryGetValue(cell, out FloorProp floorProp))
            return false;
        if (floorProp != null)
            return true;

        placedFloorProps.Remove(cell);
        placedFloorPropObjectIds.Remove(cell);
        placedFloorPropPrefabNames.Remove(cell);
        return false;
    }

    public bool CanPlaceFloorPropWorldPosition(
        Vector3 worldPosition,
        GameObject floorPropPrefab)
    {
        Vector2Int cell = GetGridCoordinates(worldPosition);
        return CanPlaceFloorPropCell(cell.x, cell.y, floorPropPrefab);
    }

    public bool CanPlaceFloorPropCell(
        int x,
        int y,
        GameObject floorPropPrefab)
    {
        return TryValidateFloorPropPlacement(
            new Vector2Int(x, y), floorPropPrefab, out _);
    }

    public bool TryGetFloorPropPreviewPose(
        Vector3 worldPosition,
        GameObject floorPropPrefab,
        out Vector3 position,
        out Quaternion rotation)
    {
        Vector2Int cell = GetGridCoordinates(worldPosition);
        bool inBounds = cell.x >= 0 && cell.y >= 0 &&
            cell.x < width && cell.y < height;
        position = inBounds
            ? GetCellWorldPosition(cell.x, cell.y)
            : worldPosition;
        rotation = Quaternion.identity;
        return CanPlaceFloorPropCell(cell.x, cell.y, floorPropPrefab);
    }

    public bool PlaceFloorPropWorldPosition(
        Vector3 worldPosition,
        GameObject floorPropPrefab,
        int objectId = -1,
        bool resolvedForSave = false)
    {
        Vector2Int cell = GetGridCoordinates(worldPosition);
        return PlaceFloorPropCell(
            cell.x, cell.y, floorPropPrefab, objectId, resolvedForSave);
    }

    public bool PlaceFloorPropCell(
        int x,
        int y,
        GameObject floorPropPrefab,
        int objectId = -1,
        bool resolvedForSave = false)
    {
        var cell = new Vector2Int(x, y);
        if (!TryValidateFloorPropPlacement(
                cell, floorPropPrefab, out string failure))
        {
            Debug.LogWarning(failure, this);
            return false;
        }

        if (placedFloorProps.ContainsKey(cell))
        {
            placedFloorProps.Remove(cell);
            placedFloorPropObjectIds.Remove(cell);
            placedFloorPropPrefabNames.Remove(cell);
        }

        if (floorPropContainer == null)
        {
            var container = new GameObject("Placed Floor Props");
            container.transform.SetParent(transform, false);
            floorPropContainer = container.transform;
        }

        GameObject instance = Instantiate(
            floorPropPrefab,
            GetCellWorldPosition(x, y),
            Quaternion.identity,
            floorPropContainer);
        FloorProp floorProp = instance.GetComponent<FloorProp>();
        if (floorProp == null)
        {
            // The prefab is validated before instantiation, but keep this guard
            // for runtime prefab mutation and clearer diagnostics.
            Debug.LogWarning(
                $"Floor prop prefab '{floorPropPrefab.name}' needs a FloorProp component on its root.",
                floorPropPrefab);
            instance.SetActive(false);
            Destroy(instance);
            return false;
        }

        if (instance.GetComponentInChildren<DungeonLightReceiver>(true) == null)
            instance.AddComponent<DungeonLightReceiver>();

        instance.name = $"{floorPropPrefab.name} [{x},{y}]";
        floorProp.Initialize(this, cell);
        floorProp.RestoreResolvedState(resolvedForSave);
        placedFloorProps.Add(cell, floorProp);
        placedFloorPropObjectIds[cell] = objectId;
        placedFloorPropPrefabNames[cell] = floorPropPrefab.name;
        LayoutChanged?.Invoke();
        return true;
    }

    bool TryValidateFloorPropPlacement(
        Vector2Int cell,
        GameObject floorPropPrefab,
        out string failure)
    {
        return TryValidateFloorPropPlacement(
            GetLivePlacementValidationContext(),
            cell,
            floorPropPrefab,
            out failure);
    }

    public List<SavedFloorPropCell> CaptureFloorPropLayout()
    {
        var result = new List<SavedFloorPropCell>();
        foreach (KeyValuePair<Vector2Int, FloorProp> pair in placedFloorProps)
        {
            if (pair.Value == null)
                continue;

            placedFloorPropObjectIds.TryGetValue(pair.Key, out int objectId);
            placedFloorPropPrefabNames.TryGetValue(pair.Key, out string prefabName);
            result.Add(new SavedFloorPropCell
            {
                x = pair.Key.x,
                y = pair.Key.y,
                objectId = objectId,
                prefabName = prefabName,
                isResolved = pair.Value.IsResolvedForSave
            });
        }
        return result;
    }

    public void ClearFloorProps()
    {
        foreach (FloorProp floorProp in placedFloorProps.Values)
        {
            if (floorProp == null)
                continue;
            floorProp.gameObject.SetActive(false);
            Destroy(floorProp.gameObject);
        }
        placedFloorProps.Clear();
        placedFloorPropObjectIds.Clear();
        placedFloorPropPrefabNames.Clear();
    }

    bool RemoveFloorPropAtCell(Vector2Int cell)
    {
        if (!placedFloorProps.TryGetValue(cell, out FloorProp floorProp))
            return false;

        placedFloorProps.Remove(cell);
        placedFloorPropObjectIds.Remove(cell);
        placedFloorPropPrefabNames.Remove(cell);
        if (floorProp != null)
        {
            floorProp.gameObject.SetActive(false);
            Destroy(floorProp.gameObject);
        }
        return true;
    }

    public IReadOnlyList<DungeonPointOfInterest> GetPointsOfInterest(
        Vector2Int cell,
        bool availableOnly = true)
    {
        if (!pointsOfInterest.TryGetValue(cell, out var registered))
            return System.Array.Empty<DungeonPointOfInterest>();

        var result = new List<DungeonPointOfInterest>(registered.Count);
        for (int i = registered.Count - 1; i >= 0; i--)
        {
            DungeonPointOfInterest point = registered[i];
            if (point == null || !point.IsBound || point.Cell != cell)
            {
                registered.RemoveAt(i);
                continue;
            }

            if (!availableOnly || point.IsAvailable)
                result.Add(point);
        }

        if (registered.Count == 0)
            pointsOfInterest.Remove(cell);
        return result;
    }

    public bool TryGetAvailablePointOfInterest(
        Vector2Int cell,
        out DungeonPointOfInterest pointOfInterest)
    {
        IReadOnlyList<DungeonPointOfInterest> available =
            GetPointsOfInterest(cell, true);
        pointOfInterest = available.Count > 0 ? available[0] : null;
        return pointOfInterest != null;
    }

    internal void RegisterPointOfInterest(DungeonPointOfInterest pointOfInterest)
    {
        if (pointOfInterest == null || pointOfInterest.Grid != this)
            return;

        Vector2Int cell = pointOfInterest.Cell;
        if (!pointsOfInterest.TryGetValue(cell, out var registered))
        {
            registered = new List<DungeonPointOfInterest>();
            pointsOfInterest[cell] = registered;
        }

        if (!registered.Contains(pointOfInterest))
            registered.Add(pointOfInterest);
    }

    internal void UnregisterPointOfInterest(
        DungeonPointOfInterest pointOfInterest)
    {
        if (pointOfInterest == null ||
            !pointsOfInterest.TryGetValue(pointOfInterest.Cell, out var registered))
            return;

        registered.Remove(pointOfInterest);
        if (registered.Count == 0)
            pointsOfInterest.Remove(pointOfInterest.Cell);
    }

    public bool TryGetDungeonEntrance(out DungeonEntrance entrance)
    {
        if (placedEntrance != null)
        {
            entrance = placedEntrance;
            return true;
        }

        entrance = null;
        if (!IsInitialized)
            return false;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (!placed[x, y] || instantiated[x, y] == null)
                continue;

            DungeonEntrance[] markers =
                instantiated[x, y].GetComponentsInChildren<DungeonEntrance>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                DungeonEntrance marker = markers[i];
                marker.Bind(this, new Vector2Int(x, y));
                if (entrance != null)
                {
                    Debug.LogError(
                        $"Dungeon entrance is ambiguous: both cells {entrance.Cell} " +
                        $"and ({x}, {y}) contain entrance markers.", this);
                    entrance = null;
                    return false;
                }

                entrance = marker;
            }
        }

        return entrance != null;
    }

    public bool PlaceEntranceWorldPosition(
        Vector3 worldPosition,
        GameObject entrancePrefab,
        int objectId = -1)
    {
        Vector2Int coordinates = GetGridCoordinates(worldPosition);
        return PlaceEntranceCell(
            coordinates.x, coordinates.y, entrancePrefab, objectId);
    }

    public bool RemoveEntranceWorldPosition(Vector3 worldPosition)
    {
        Vector2Int coordinates = GetGridCoordinates(worldPosition);
        return RemoveEntranceAtCell(coordinates);
    }

    public bool PlaceEntranceCell(
        int x,
        int y,
        GameObject entrancePrefab,
        int objectId = -1)
    {
        var cell = new Vector2Int(x, y);
        if (!TryValidateEntrancePlacement(
                GetLivePlacementValidationContext(),
                cell,
                entrancePrefab,
                out string failure))
        {
            Debug.LogWarning(failure, this);
            return false;
        }

        BakedPropSocket socket = FindEntranceSocket(x, y);
        if (socket == null || !TryGetPropSocketWorldPose(
                x, y, socket, out Vector3 position, out Quaternion rotation))
        {
            Debug.LogWarning(
                $"Cell ({x},{y}) does not provide a compatible entrance socket.", this);
            return false;
        }

        if (placedEntranceIsFallback)
            ClearEntrance();

        if (entranceContainer == null)
        {
            var container = new GameObject("Placed Entrance");
            container.transform.SetParent(transform, false);
            entranceContainer = container.transform;
        }

        GameObject instance = Instantiate(
            entrancePrefab, position, rotation, entranceContainer);
        DungeonEntrance entrance = instance.GetComponentInChildren<DungeonEntrance>(true);
        if (entrance == null)
        {
            Debug.LogWarning(
                $"Entrance prefab '{entrancePrefab.name}' needs a DungeonEntrance component.",
                entrancePrefab);
            Destroy(instance);
            return false;
        }

        placedEntrance = entrance;
        placedEntranceInstance = instance;
        placedEntranceCell = new Vector2Int(x, y);
        placedEntranceObjectId = objectId;
        placedEntrancePrefabName = entrancePrefab.name;
        placedEntranceIsFallback = false;
        placedEntrance.Bind(this, placedEntranceCell);
        LayoutChanged?.Invoke();
        return true;
    }

    public SavedEntrance CaptureEntranceLayout()
    {
        if (placedEntrance == null || placedEntranceIsFallback)
            return null;

        return new SavedEntrance
        {
            x = placedEntranceCell.x,
            y = placedEntranceCell.y,
            objectId = placedEntranceObjectId,
            prefabName = placedEntrancePrefabName
        };
    }

    public void ClearEntrance()
    {
        if (placedEntranceInstance != null)
            Destroy(placedEntranceInstance);
        placedEntrance = null;
        placedEntranceInstance = null;
        placedEntranceCell = default;
        placedEntranceObjectId = -1;
        placedEntrancePrefabName = null;
        placedEntranceIsFallback = false;
    }

    public bool EnsureFallbackEntrance(Vector2Int cell, Vector3 position)
    {
        if (placedEntrance != null && !placedEntranceIsFallback)
            return false;

        if (!IsPlacedCell(cell.x, cell.y))
        {
            if (placedEntranceIsFallback)
                ClearEntrance();
            return false;
        }

        if (placedEntranceIsFallback && placedEntranceCell == cell)
        {
            placedEntranceInstance.transform.SetPositionAndRotation(
                position, Quaternion.identity);
            placedEntrance.Bind(this, cell);
            return true;
        }

        if (placedEntranceIsFallback)
            ClearEntrance();

        GameObject entrancePrefab =
            Resources.Load<GameObject>("Props/DungeonEntrance");
        if (entrancePrefab == null)
        {
            Debug.LogWarning(
                "The fallback entrance prefab could not be loaded from " +
                "Resources/Props/DungeonEntrance.", this);
            return false;
        }

        if (entranceContainer == null)
        {
            var container = new GameObject("Placed Entrance");
            container.transform.SetParent(transform, false);
            entranceContainer = container.transform;
        }

        GameObject instance = Instantiate(
            entrancePrefab, position, Quaternion.identity, entranceContainer);
        DungeonEntrance entrance =
            instance.GetComponentInChildren<DungeonEntrance>(true);
        if (entrance == null)
        {
            Debug.LogWarning(
                "The fallback entrance prefab needs a DungeonEntrance component.",
                entrancePrefab);
            Destroy(instance);
            return false;
        }

        placedEntrance = entrance;
        placedEntranceInstance = instance;
        placedEntranceCell = cell;
        placedEntranceObjectId = -1;
        placedEntrancePrefabName = entrancePrefab.name;
        placedEntranceIsFallback = true;
        placedEntrance.Bind(this, cell);
        return true;
    }

    BakedPropSocket FindEntranceSocket(int x, int y)
    {
        return FindEntranceSocket(GetCellPropSockets(x, y));
    }

    static BakedPropSocket FindEntranceSocket(
        IReadOnlyList<BakedPropSocket> sockets)
    {
        for (int i = 0; i < sockets.Count; i++)
        {
            BakedPropSocket socket = sockets[i];
            if (socket != null && socket.role == PropSocketRole.Single &&
                string.Equals(
                    socket.structureId, EntranceStructureId,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return socket;
            }
        }
        return null;
    }

    bool RemoveEntranceAtCell(Vector2Int cell)
    {
        if (placedEntrance == null || placedEntranceCell != cell)
            return false;

        ClearEntrance();
        LayoutChanged?.Invoke();
        return true;
    }

    public ConnectionIntent GetConnectionIntent(Vector2Int from, Vector2Int to)
    {
        if (!TryGetStoredEdge(from, to, out Vector2Int owner, out bool eastEdge))
            return ConnectionIntent.Auto;

        return eastEdge
            ? eastConnectionIntents[owner.x, owner.y]
            : southConnectionIntents[owner.x, owner.y];
    }

    public bool SetConnectionIntentWorldPositions(
        Vector3 fromWorldPosition,
        Vector3 toWorldPosition,
        ConnectionIntent intent)
    {
        return SetConnectionIntent(
            GetGridCoordinates(fromWorldPosition),
            GetGridCoordinates(toWorldPosition),
            intent);
    }

    public bool ToggleConnectionIntentAtWorldPosition(
        Vector3 worldPosition,
        float edgeThreshold = 0.28f)
    {
        if (!TryGetClosestBuiltEdge(
                worldPosition, edgeThreshold, out Vector2Int from, out Vector2Int to))
        {
            Debug.LogWarning(
                "Click closer to a wall shared by two built cells to toggle it.",
                this);
            return false;
        }

        ConnectionIntent nextIntent = HasActualSharedOpening(from, to)
            ? ConnectionIntent.Closed
            : ConnectionIntent.Open;
        return SetConnectionIntent(from, to, nextIntent);
    }

    public bool SetConnectionIntent(
        Vector2Int from,
        Vector2Int to,
        ConnectionIntent intent)
    {
        if (!IsInitialized ||
            !System.Enum.IsDefined(typeof(ConnectionIntent), intent) ||
            !IsInteriorCell(from) || !IsInteriorCell(to) ||
            !AreCardinalNeighbors(from, to) ||
            !placed[from.x, from.y] || !placed[to.x, to.y])
        {
            Debug.LogWarning(
                "Connection edges can only be edited between two adjacent built cells.",
                this);
            return false;
        }

        ConnectionIntent previousIntent = GetConnectionIntent(from, to);
        if (previousIntent == intent)
            return true;

        List<int>[,] previousCells = CopyCells();
        SetStoredConnectionIntent(from, to, intent);

        List<Vector2Int> region = BuildEdgeEditRegion(from, to);
        var regionSet = new HashSet<Vector2Int>(region);
        if (!FindBestLocalAssignment(region, regionSet, out var assignments))
        {
            SetStoredConnectionIntent(from, to, previousIntent);
            Debug.LogWarning(
                $"No tile combination can make the edge from {from} to {to} {intent}.",
                this);
            return false;
        }

        foreach (KeyValuePair<Vector2Int, int> assignment in assignments)
        {
            Vector2Int position = assignment.Key;
            cells[position.x, position.y].Clear();
            cells[position.x, position.y].Add(assignment.Value);
            InstantiateCell(position.x, position.y, assignment.Value);
        }

        foreach (Vector2Int position in region)
            Propagate(position.x, position.y);

        if (HasContradiction())
        {
            cells = previousCells;
            SetStoredConnectionIntent(from, to, previousIntent);
            foreach (Vector2Int position in region)
                InstantiateCurrentCell(position.x, position.y);
            Debug.LogWarning(
                $"Changing the edge from {from} to {to} caused a contradiction and was reverted.",
                this);
            return false;
        }

        NotifyLayoutChanged();
        return true;
    }

    bool TryGetClosestBuiltEdge(
        Vector3 worldPosition,
        float edgeThreshold,
        out Vector2Int from,
        out Vector2Int to)
    {
        float gridX = (worldPosition.x - origin.x + 0.5f) / generationDirection.x;
        float gridY = (worldPosition.y - origin.y + 0.5f) / generationDirection.y;
        from = new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        to = from;

        float localX = gridX - from.x;
        float localY = gridY - from.y;
        edgeThreshold = Mathf.Clamp(edgeThreshold, 0f, 0.49f);
        if (Mathf.Max(Mathf.Abs(localX), Mathf.Abs(localY)) < edgeThreshold)
            return false;

        if (Mathf.Abs(localX) >= Mathf.Abs(localY))
            to.x += localX >= 0f ? 1 : -1;
        else
            to.y += localY >= 0f ? 1 : -1;

        return IsInteriorCell(from) && IsInteriorCell(to) &&
            placed[from.x, from.y] && placed[to.x, to.y];
    }

    bool HasActualSharedOpening(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        TileSide fromSide;
        TileSide toSide;
        if (delta == Vector2Int.right)
        {
            fromSide = TileSide.East;
            toSide = TileSide.West;
        }
        else if (delta == Vector2Int.left)
        {
            fromSide = TileSide.West;
            toSide = TileSide.East;
        }
        else if (delta == Vector2Int.up)
        {
            fromSide = TileSide.South;
            toSide = TileSide.North;
        }
        else if (delta == Vector2Int.down)
        {
            fromSide = TileSide.North;
            toSide = TileSide.South;
        }
        else
        {
            return false;
        }

        return TryGetCellEdgeMask(from.x, from.y, fromSide, out string fromMask) &&
            TryGetCellEdgeMask(to.x, to.y, toSide, out string toMask) &&
            MasksShareOpening(fromMask, toMask);
    }

    List<Vector2Int> BuildEdgeEditRegion(Vector2Int from, Vector2Int to)
    {
        var region = new List<Vector2Int> { from, to };
        Vector2Int delta = to - from;
        if (delta.x != 0)
        {
            AddPlacedRegionCell(region, from + Vector2Int.up);
            AddPlacedRegionCell(region, to + Vector2Int.up);
            AddPlacedRegionCell(region, from + Vector2Int.down);
            AddPlacedRegionCell(region, to + Vector2Int.down);
        }
        else
        {
            AddPlacedRegionCell(region, from + Vector2Int.left);
            AddPlacedRegionCell(region, to + Vector2Int.left);
            AddPlacedRegionCell(region, from + Vector2Int.right);
            AddPlacedRegionCell(region, to + Vector2Int.right);
        }
        return region;
    }

    void AddPlacedRegionCell(List<Vector2Int> region, Vector2Int position)
    {
        if (IsInteriorCell(position) && placed[position.x, position.y] &&
            !region.Contains(position))
        {
            region.Add(position);
        }
    }

    static bool AreCardinalNeighbors(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;
    }

    bool IsInteriorCell(Vector2Int position)
    {
        return position.x > 0 && position.y > 0 &&
            position.x < width - 1 && position.y < height - 1;
    }

    static string GetCanonicalEdgeKey(Vector2Int from, Vector2Int to)
    {
        if (to.x < from.x || (to.x == from.x && to.y < from.y))
            (from, to) = (to, from);
        return $"{from.x},{from.y}:{to.x},{to.y}";
    }

    bool TryGetStoredEdge(
        Vector2Int from,
        Vector2Int to,
        out Vector2Int owner,
        out bool eastEdge)
    {
        owner = default;
        eastEdge = false;
        if (!AreCardinalNeighbors(from, to) || eastConnectionIntents == null ||
            southConnectionIntents == null)
        {
            return false;
        }

        Vector2Int delta = to - from;
        if (delta == Vector2Int.right)
        {
            owner = from;
            eastEdge = true;
        }
        else if (delta == Vector2Int.left)
        {
            owner = to;
            eastEdge = true;
        }
        else if (delta == Vector2Int.up)
        {
            owner = from;
        }
        else
        {
            owner = to;
        }

        return owner.x >= 0 && owner.y >= 0 &&
            owner.x < width && owner.y < height;
    }

    void SetStoredConnectionIntent(
        Vector2Int from,
        Vector2Int to,
        ConnectionIntent intent)
    {
        if (!TryGetStoredEdge(from, to, out Vector2Int owner, out bool eastEdge))
            return;

        if (eastEdge)
            eastConnectionIntents[owner.x, owner.y] = intent;
        else
            southConnectionIntents[owner.x, owner.y] = intent;
    }

    void ClearIncidentConnectionIntents(Vector2Int position)
    {
        foreach (Vector2Int offset in CardinalOffsets)
            SetStoredConnectionIntent(position, position + offset, ConnectionIntent.Auto);
    }

    bool AssignmentsRespectConnectionIntents(
        Dictionary<Vector2Int, int> assignments,
        Dictionary<string, ConnectionIntent> intentsByEdge)
    {
        foreach (KeyValuePair<Vector2Int, int> assignment in assignments)
        {
            Vector2Int position = assignment.Key;
            var eastNeighbor = position + Vector2Int.right;
            var southNeighbor = position + Vector2Int.up;
            if (!AssignmentEdgeRespectsIntent(
                    position, assignment.Value, TileSide.East,
                    eastNeighbor, assignments, intentsByEdge) ||
                !AssignmentEdgeRespectsIntent(
                    position, assignment.Value, TileSide.South,
                    southNeighbor, assignments, intentsByEdge))
            {
                return false;
            }
        }
        return true;
    }

    bool AssignmentEdgeRespectsIntent(
        Vector2Int position,
        int tileIndex,
        TileSide side,
        Vector2Int neighbor,
        Dictionary<Vector2Int, int> assignments,
        Dictionary<string, ConnectionIntent> intentsByEdge)
    {
        intentsByEdge.TryGetValue(
            GetCanonicalEdgeKey(position, neighbor), out ConnectionIntent intent);
        if (intent == ConnectionIntent.Auto ||
            !assignments.TryGetValue(neighbor, out int neighborTile))
        {
            return true;
        }

        TileSide opposite = side == TileSide.East ? TileSide.West : TileSide.North;
        bool expectedOpen = intent == ConnectionIntent.Open;
        return HasOpening(database.tiles[tileIndex].GetHash(side)) == expectedOpen &&
            HasOpening(database.tiles[neighborTile].GetHash(opposite)) == expectedOpen;
    }

    public bool TryGetCellEdgeMask(int x, int y, TileSide side, out string mask)
    {
        mask = string.Empty;
        if (!IsInitialized || x < 0 || y < 0 || x >= width || y >= height ||
            cells[x, y].Count != 1)
        {
            return false;
        }

        mask = database.tiles[cells[x, y][0]].GetHash(side);
        return !string.IsNullOrEmpty(mask);
    }

    public bool CanLightPass(Vector2Int from, Vector2Int to)
    {
        if (!IsPlacedCell(from.x, from.y) || !IsPlacedCell(to.x, to.y))
            return false;

        Vector2Int delta = to - from;
        TileSide fromSide;
        TileSide toSide;
        if (delta == Vector2Int.right)
        {
            fromSide = TileSide.East;
            toSide = TileSide.West;
        }
        else if (delta == Vector2Int.left)
        {
            fromSide = TileSide.West;
            toSide = TileSide.East;
        }
        else if (delta == Vector2Int.up)
        {
            // Grid Y increases down the dungeon in world space.
            fromSide = TileSide.South;
            toSide = TileSide.North;
        }
        else if (delta == Vector2Int.down)
        {
            fromSide = TileSide.North;
            toSide = TileSide.South;
        }
        else
        {
            return false;
        }

        if (!TryGetCellEdgeMask(from.x, from.y, fromSide, out string fromMask) ||
            !TryGetCellEdgeMask(to.x, to.y, toSide, out string toMask))
        {
            return false;
        }

        return MasksShareOpening(fromMask, toMask);
    }

    static bool MasksShareOpening(string fromMask, string toMask)
    {
        if (string.IsNullOrEmpty(fromMask) || string.IsNullOrEmpty(toMask))
            return false;

        int sampleCount = Mathf.Min(fromMask.Length, toMask.Length);
        for (int i = 0; i < sampleCount; i++)
            if (fromMask[i] == '1' && toMask[i] == '1')
                return true;
        return false;
    }

    public void ApplyMaterialToPlacedTiles(Material material)
    {
        if (material == null || !IsInitialized)
            return;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (!placed[x, y] || instantiated[x, y] == null)
                continue;

            foreach (Renderer tileRenderer in
                instantiated[x, y].GetComponentsInChildren<Renderer>(true))
            {
                tileRenderer.sharedMaterial = material;
            }
        }
    }

    Vector2Int GetGridCoordinates(Vector3 worldPosition)
    {
        float x = (worldPosition.x - origin.x + 0.5f) / generationDirection.x;
        float y = (worldPosition.y - origin.y + 0.5f) / generationDirection.y;
        return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
    }

    void InstantiateGrid()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (cells[x, y].Count == 0)
            {
                InstantiateCell(x, y, groundTileIndex);
                Debug.LogError("Contradiction detected.");
                continue;
            }
            if (cells[x, y].Count == 1)
            {
                int tileIndex = cells[x, y][0];
                instantiated[x, y] = InstantiateTile(tileIndex, GetWorldPosition(x, y));
            }
            else
            {
                instantiated[x, y] = Instantiate(placeholderPrefab, GetWorldPosition(x, y), Quaternion.identity, transform);
                var comp = instantiated[x, y].AddComponent<TilePlaceholder>();
                comp.x = x;
                comp.y = y;
                comp.generator = this;
            }
        }
    }

    void InstantiateCell(
        int x, int y,
        int tileIndex)
    {
        if (instantiated[x, y] != null)
        {
            instantiated[x, y].SetActive(false);
            Destroy(instantiated[x, y]);
        }
        instantiated[x, y] = InstantiateTile(tileIndex, GetWorldPosition(x, y));
    }
    
    public bool ClickWorldPosition(
        Vector3 worldPosition,
        CellWidthIntent widthIntent = CellWidthIntent.Auto)
    {
        Vector2Int gridCoordinates = GetGridCoordinates(worldPosition);
        return ClickCell(
            gridCoordinates.x,
            gridCoordinates.y,
            widthIntent);
    }

    public bool PlaceGroundWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridCoordinates = GetGridCoordinates(worldPosition);
        return PlaceGroundCell(gridCoordinates.x, gridCoordinates.y);
    }

    public bool PlaceGroundCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height ||
            fixedGround[x, y] || !placed[x, y])
        {
            return false;
        }

        List<int>[,] previousCells = CopyCells();
        ConnectionIntent[,] previousEastConnections =
            (ConnectionIntent[,])eastConnectionIntents.Clone();
        ConnectionIntent[,] previousSouthConnections =
            (ConnectionIntent[,])southConnectionIntents.Clone();
        bool wasPlaced = placed[x, y];
        CellWidthIntent previousWidthIntent = widthIntents[x, y];
        var region = new List<Vector2Int>();
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0)
                continue;
            int nx = x + dx;
            int ny = y + dy;
            if (nx > 0 && ny > 0 && nx < width - 1 && ny < height - 1 && placed[nx, ny])
                region.Add(new Vector2Int(nx, ny));
        }

        cells[x, y].Clear();
        cells[x, y].Add(groundTileIndex);
        placed[x, y] = false;
        widthIntents[x, y] = CellWidthIntent.Auto;
        ClearIncidentConnectionIntents(new Vector2Int(x, y));

        if (region.Count > 0)
        {
            var regionSet = new HashSet<Vector2Int>(region);
            if (!FindBestLocalAssignment(region, regionSet, out var assignments))
            {
                cells = previousCells;
                eastConnectionIntents = previousEastConnections;
                southConnectionIntents = previousSouthConnections;
                placed[x, y] = wasPlaced;
                widthIntents[x, y] = previousWidthIntent;
                Debug.LogWarning($"Ground cannot be placed at ({x},{y}) without disconnecting the surrounding layout.");
                return false;
            }

            foreach (var assignment in assignments)
            {
                Vector2Int position = assignment.Key;
                cells[position.x, position.y].Clear();
                cells[position.x, position.y].Add(assignment.Value);
            }
        }

        Propagate(x, y);
        if (HasContradiction())
        {
            cells = previousCells;
            eastConnectionIntents = previousEastConnections;
            southConnectionIntents = previousSouthConnections;
            placed[x, y] = wasPlaced;
            widthIntents[x, y] = previousWidthIntent;
            Debug.LogWarning($"Ground placement at ({x},{y}) caused a contradiction and was reverted.");
            return false;
        }

        // Commit visuals only after the logical solve succeeds. An unbuilt
        // interior cell uses the neutral placeholder; the ground profile is a
        // constraint, not a visible replacement dungeon tile.
        InstantiateCurrentCell(x, y);
        foreach (Vector2Int position in region)
            InstantiateCurrentCell(position.x, position.y);
        RemoveTrapAtCell(new Vector2Int(x, y));
        RemoveFloorPropAtCell(new Vector2Int(x, y));
        if (placedEntrance != null && placedEntranceCell == new Vector2Int(x, y))
            ClearEntrance();
        NotifyLayoutChanged();
        return true;
    }

    public bool ClickCell(
        int x,
        int y,
        CellWidthIntent widthIntent = CellWidthIntent.Auto)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.LogWarning($"Cell ({x},{y}) is outside the grid bounds [0-{width - 1}, 0-{height - 1}].");
            return false;
        }

        if (fixedGround[x, y])
        {
            Debug.LogWarning($"Cell ({x},{y}) is fixed ground and cannot be replaced.");
            return false;
        }

        if (!TryResolveLocalPlacement(x, y, widthIntent))
        {
            Debug.LogWarning($"No local tile combination can connect at ({x},{y}) without changing tiles farther away.");
            return false;
        }
        return true;
    }

    public void PlaceTrapWorldPosition(
        Vector3 worldPosition,
        GameObject trapPrefab,
        int objectId = -1)
    {
        Vector2Int coordinates = GetGridCoordinates(worldPosition);
        PlaceTrapCell(coordinates.x, coordinates.y, trapPrefab, objectId);
    }

    public bool RemoveTrapWorldPosition(Vector3 worldPosition)
    {
        Vector2Int coordinates = GetGridCoordinates(worldPosition);
        return RemoveTrapAtCell(coordinates);
    }

    public bool PlaceTrapCell(
        int x,
        int y,
        GameObject trapPrefab,
        int objectId = -1)
    {
        var cell = new Vector2Int(x, y);
        if (!TryValidateTrapPlacement(
                GetLivePlacementValidationContext(),
                cell,
                trapPrefab,
                out string failure))
        {
            Debug.LogWarning(failure, this);
            return false;
        }

        if (placedTraps.ContainsKey(cell))
            placedTraps.Remove(cell);

        if (trapContainer == null)
        {
            var container = new GameObject("Placed Traps");
            container.transform.SetParent(transform, false);
            trapContainer = container.transform;
        }

        GameObject instance = Instantiate(
            trapPrefab, GetCellWorldPosition(x, y), Quaternion.identity, trapContainer);
        if (instance.GetComponentInChildren<DungeonLightReceiver>(true) == null)
            instance.AddComponent<DungeonLightReceiver>();
        CellTrap trap = instance.GetComponent<CellTrap>();
        if (trap == null)
        {
            Debug.LogWarning(
                $"Trap prefab '{trapPrefab.name}' needs a component derived from CellTrap.",
                trapPrefab);
            Destroy(instance);
            return false;
        }

        trap.Initialize(this, cell);
        placedTraps.Add(cell, trap);
        placedTrapObjectIds[cell] = objectId;
        placedTrapPrefabNames[cell] = trapPrefab.name;
        return true;
    }

    public List<SavedTrapCell> CaptureTrapLayout()
    {
        var result = new List<SavedTrapCell>();
        foreach (KeyValuePair<Vector2Int, CellTrap> pair in placedTraps)
        {
            if (pair.Value == null)
                continue;

            placedTrapObjectIds.TryGetValue(pair.Key, out int objectId);
            placedTrapPrefabNames.TryGetValue(pair.Key, out string prefabName);
            result.Add(new SavedTrapCell
            {
                x = pair.Key.x,
                y = pair.Key.y,
                objectId = objectId,
                prefabName = prefabName
            });
        }
        return result;
    }

    public void ClearTraps()
    {
        foreach (CellTrap trap in placedTraps.Values)
        {
            if (trap == null)
                continue;
            trap.gameObject.SetActive(false);
            Destroy(trap.gameObject);
        }
        placedTraps.Clear();
        placedTrapObjectIds.Clear();
        placedTrapPrefabNames.Clear();
    }

    public void NotifyNpcEnteredCell(NPCCharacter npc, Vector2Int cell)
    {
        if (npc == null)
            return;

        if (placedTraps.TryGetValue(cell, out CellTrap trap))
        {
            if (trap != null)
                trap.OnNpcEntered(npc);
            else
            {
                placedTraps.Remove(cell);
                placedTrapObjectIds.Remove(cell);
                placedTrapPrefabNames.Remove(cell);
            }
        }
    }

    bool RemoveTrapAtCell(Vector2Int cell)
    {
        if (!placedTraps.TryGetValue(cell, out CellTrap trap))
            return false;

        placedTraps.Remove(cell);
        placedTrapObjectIds.Remove(cell);
        placedTrapPrefabNames.Remove(cell);
        if (trap != null)
            Destroy(trap.gameObject);
        return true;
    }

    bool TryResolveLocalPlacement(
        int x,
        int y,
        CellWidthIntent requestedWidthIntent)
    {
        List<int>[,] previousCells = CopyCells();
        ConnectionIntent[,] previousEastConnections =
            (ConnectionIntent[,])eastConnectionIntents.Clone();
        ConnectionIntent[,] previousSouthConnections =
            (ConnectionIntent[,])southConnectionIntents.Clone();
        CellWidthIntent previousWidthIntent = widthIntents[x, y];
        widthIntents[x, y] = requestedWidthIntent;
        var center = new Vector2Int(x, y);
        ApplyConnectedPlacementIntents(center);
        var region = new List<Vector2Int> { center };
        AddCollapsedNeighbor(region, x, y + 1);
        AddCollapsedNeighbor(region, x, y - 1);
        AddCollapsedNeighbor(region, x + 1, y);
        AddCollapsedNeighbor(region, x - 1, y);
        // Include diagonals in the joint solve so patterns such as a 2x2 room
        // can replace all four corners together. Socket checks remain cardinal.
        AddCollapsedNeighbor(region, x + 1, y + 1);
        AddCollapsedNeighbor(region, x + 1, y - 1);
        AddCollapsedNeighbor(region, x - 1, y + 1);
        AddCollapsedNeighbor(region, x - 1, y - 1);

        var regionSet = new HashSet<Vector2Int>(region);
        if (!FindBestLocalAssignment(region, regionSet, out var assignments))
        {
            widthIntents[x, y] = previousWidthIntent;
            eastConnectionIntents = previousEastConnections;
            southConnectionIntents = previousSouthConnections;
            return false;
        }

        foreach (var assignment in assignments)
        {
            Vector2Int position = assignment.Key;
            cells[position.x, position.y].Clear();
            cells[position.x, position.y].Add(assignment.Value);
            InstantiateCell(position.x, position.y, assignment.Value);
        }

        foreach (var position in region)
            Propagate(position.x, position.y);

        if (HasContradiction())
        {
            cells = previousCells;
            widthIntents[x, y] = previousWidthIntent;
            eastConnectionIntents = previousEastConnections;
            southConnectionIntents = previousSouthConnections;
            foreach (var position in region)
                InstantiateCurrentCell(position.x, position.y);
            return false;
        }

        placed[x, y] = true;
        NotifyLayoutChanged();
        return true;
    }

    void ApplyConnectedPlacementIntents(Vector2Int center)
    {
        foreach (Vector2Int offset in CardinalOffsets)
        {
            Vector2Int neighbor = center + offset;
            if (!IsInteriorCell(neighbor) || !placed[neighbor.x, neighbor.y])
                continue;

            SetStoredConnectionIntent(center, neighbor, ConnectionIntent.Open);
        }
    }

    List<int>[,] CopyCells()
    {
        return CopyCellOptions(cells);
    }

    List<int>[,] CopyCellOptions(List<int>[,] source)
    {
        var copy = new List<int>[width, height];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            copy[x, y] = new List<int>(source[x, y]);
        return copy;
    }

    bool HasContradiction()
    {
        return HasContradiction(cells);
    }

    bool HasContradiction(List<int>[,] targetCells)
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            if (targetCells[x, y].Count == 0)
                return true;
        return false;
    }

    void InstantiateCurrentCell(int x, int y)
    {
        if (instantiated[x, y] != null)
        {
            instantiated[x, y].SetActive(false);
            Destroy(instantiated[x, y]);
        }

        if (placed[x, y] && cells[x, y].Count == 1)
        {
            int tileIndex = cells[x, y][0];
            instantiated[x, y] = InstantiateTile(tileIndex, GetWorldPosition(x, y));
            return;
        }

        instantiated[x, y] = Instantiate(
            placeholderPrefab, GetWorldPosition(x, y), Quaternion.identity, transform);
        var placeholder = instantiated[x, y].AddComponent<TilePlaceholder>();
        placeholder.x = x;
        placeholder.y = y;
        placeholder.generator = this;
    }

    void AddCollapsedNeighbor(List<Vector2Int> region, int x, int y)
    {
        if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
            return;

        if (placed[x, y])
            region.Add(new Vector2Int(x, y));
    }

    bool FindBestLocalAssignment(
        List<Vector2Int> region,
        HashSet<Vector2Int> regionSet,
        out Dictionary<Vector2Int, int> bestAssignments)
    {
        var assignments = new Dictionary<Vector2Int, int>();
        bestAssignments = null;
        int bestScore = int.MinValue;
        int visitedNodes = 0;
        SearchLocalAssignments(region, regionSet, assignments, 0,
            ref bestAssignments, ref bestScore, ref visitedNodes);
        if (bestAssignments != null)
        {
            int wideTiles = 0;
            foreach (int tileIndex in bestAssignments.Values)
                if (GetTileCategory(database.tiles[tileIndex]) == TileCategory.Wide)
                    wideTiles++;
            Debug.Log($"Local layout search visited {visitedNodes} nodes; score {bestScore}; wide tiles {wideTiles}/{region.Count}.");
        }
        return bestAssignments != null;
    }

    void SearchLocalAssignments(
        List<Vector2Int> region,
        HashSet<Vector2Int> regionSet,
        Dictionary<Vector2Int, int> assignments,
        int score,
        ref Dictionary<Vector2Int, int> bestAssignments,
        ref int bestScore,
        ref int visitedNodes)
    {
        if (visitedNodes++ >= localSearchNodeLimit)
            return;

        if (assignments.Count >= region.Count)
        {
            if (score > bestScore)
            {
                bestScore = score;
                bestAssignments = new Dictionary<Vector2Int, int>(assignments);
            }
            return;
        }

        Vector2Int position = default;
        List<int> candidates = null;
        foreach (Vector2Int possiblePosition in region)
        {
            if (assignments.ContainsKey(possiblePosition))
                continue;

            var validCandidates = GetOrderedValidCandidates(
                possiblePosition, regionSet, assignments);
            if (candidates == null || validCandidates.Count < candidates.Count)
            {
                position = possiblePosition;
                candidates = validCandidates;
                if (candidates.Count == 0)
                    break;
            }
        }

        foreach (int candidate in candidates)
        {
            assignments[position] = candidate;
            SearchLocalAssignments(region, regionSet, assignments,
                score + GetCandidateScore(position, candidate, regionSet),
                ref bestAssignments, ref bestScore, ref visitedNodes);
            assignments.Remove(position);

            if (visitedNodes >= localSearchNodeLimit)
                break;
        }
    }

    List<int> GetOrderedValidCandidates(
        Vector2Int position,
        HashSet<Vector2Int> region,
        Dictionary<Vector2Int, int> assignments)
    {
        var candidates = new List<int>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (i == groundTileIndex)
                continue;
            if (IsRoomContext(position, region)
                && GetTileCategory(database.tiles[i]) == TileCategory.Starter)
                continue;
            if (!MatchesWidthIntent(position, i, region))
                continue;
            if (FitsLocalNeighbors(position, i, region, assignments))
                candidates.Add(i);
        }

        var ordered = new List<int>();
        AppendCandidatesByScore(ordered, candidates, position, region);
        return ordered;
    }

    void AppendCandidatesByScore(
        List<int> destination,
        List<int> candidates,
        Vector2Int position,
        HashSet<Vector2Int> region)
    {
        candidates.Sort((left, right) =>
        {
            int scoreComparison = GetCandidateScore(position, right, region)
                .CompareTo(GetCandidateScore(position, left, region));
            if (scoreComparison != 0)
                return scoreComparison;

            int profileComparison = string.CompareOrdinal(
                GetProfileId(database.tiles[left]),
                GetProfileId(database.tiles[right]));
            return profileComparison != 0 ? profileComparison : left.CompareTo(right);
        });
        destination.AddRange(candidates);
    }

    bool MatchesWidthIntent(
        Vector2Int position,
        int tileIndex,
        HashSet<Vector2Int> region)
    {
        CellWidthIntent intent = widthIntents[position.x, position.y];
        if (intent == CellWidthIntent.Auto)
            return true;

        TileCategory category = GetTileCategory(database.tiles[tileIndex]);
        if (intent == CellWidthIntent.Narrow)
            return category == TileCategory.Narrow;

        // A wide room requires a multi-cell footprint. Let early cells use a
        // provisional compatible profile until the painted topology actually
        // forms a room; once it does, Wide becomes a hard constraint.
        return !IsCompleteOpenTwoByTwoRoom(position, region) ||
            category == TileCategory.Wide;
    }

    bool IsCompleteOpenTwoByTwoRoom(
        Vector2Int position,
        HashSet<Vector2Int> region)
    {
        int[] directions = { -1, 1 };
        foreach (int dx in directions)
        foreach (int dy in directions)
        {
            Vector2Int horizontal = position + new Vector2Int(dx, 0);
            Vector2Int vertical = position + new Vector2Int(0, dy);
            Vector2Int diagonal = position + new Vector2Int(dx, dy);
            if (!IsOccupiedForLocalSolve(horizontal, region) ||
                !IsOccupiedForLocalSolve(vertical, region) ||
                !IsOccupiedForLocalSolve(diagonal, region))
            {
                continue;
            }

            if (GetConnectionIntent(position, horizontal) == ConnectionIntent.Open &&
                GetConnectionIntent(position, vertical) == ConnectionIntent.Open &&
                GetConnectionIntent(horizontal, diagonal) == ConnectionIntent.Open &&
                GetConnectionIntent(vertical, diagonal) == ConnectionIntent.Open)
            {
                return true;
            }
        }
        return false;
    }

    bool IsOccupiedForLocalSolve(
        Vector2Int position,
        HashSet<Vector2Int> region)
    {
        return IsInteriorCell(position) &&
            (region.Contains(position) || placed[position.x, position.y]);
    }

    int GetCandidateScore(Vector2Int position, int tileIndex, HashSet<Vector2Int> region)
    {
        int cardinalNeighbors = CountOccupiedNeighbors(position, region, false);
        int diagonalNeighbors = CountOccupiedNeighbors(position, region, true);
        bool roomContext = cardinalNeighbors >= 2 && diagonalNeighbors > 0;
        TileCategory category = GetTileCategory(database.tiles[tileIndex]);

        int score = GetOpeningCount(tileIndex);
        if (widthIntents[position.x, position.y] == CellWidthIntent.Wide &&
            category == TileCategory.Wide)
        {
            score += 3000;
        }
        if (roomContext)
        {
            if (category == TileCategory.Wide) score += 1000;
            if (category == TileCategory.Narrow) score -= 1000;
            if (cardinalNeighbors + diagonalNeighbors == 8 && category == TileCategory.Wide)
                score += 1000;
        }
        else
        {
            if (category == TileCategory.Narrow) score += 200;
            if (category == TileCategory.Wide) score -= 200;
            if (category == TileCategory.Starter) score += 100;
        }
        return score;
    }

    bool IsRoomContext(Vector2Int position, HashSet<Vector2Int> region)
    {
        if (TryGetExplicitRoomTopology(position, region, out bool isRoom))
            return isRoom;

        return CountOccupiedNeighbors(position, region, false) >= 2
            && CountOccupiedNeighbors(position, region, true) > 0;
    }

    bool TryGetExplicitRoomTopology(
        Vector2Int position,
        HashSet<Vector2Int> region,
        out bool isRoom)
    {
        bool north = IsExplicitOpenNeighbor(position, Vector2Int.down, region, out bool northSet);
        bool south = IsExplicitOpenNeighbor(position, Vector2Int.up, region, out bool southSet);
        bool east = IsExplicitOpenNeighbor(position, Vector2Int.right, region, out bool eastSet);
        bool west = IsExplicitOpenNeighbor(position, Vector2Int.left, region, out bool westSet);
        bool hasExplicitIntent = northSet || southSet || eastSet || westSet;

        int openCount = (north ? 1 : 0) + (south ? 1 : 0) +
            (east ? 1 : 0) + (west ? 1 : 0);
        bool hasCorner = (north && east) || (east && south) ||
            (south && west) || (west && north);
        isRoom = openCount >= 3 || hasCorner;
        return hasExplicitIntent;
    }

    bool IsExplicitOpenNeighbor(
        Vector2Int position,
        Vector2Int offset,
        HashSet<Vector2Int> region,
        out bool hasExplicitIntent)
    {
        Vector2Int neighbor = position + offset;
        bool occupied = IsInteriorCell(neighbor) &&
            (region.Contains(neighbor) || placed[neighbor.x, neighbor.y]);
        if (!occupied)
        {
            hasExplicitIntent = false;
            return false;
        }

        ConnectionIntent intent = GetConnectionIntent(position, neighbor);
        hasExplicitIntent = intent != ConnectionIntent.Auto;
        return intent == ConnectionIntent.Open;
    }

    int CountOccupiedNeighbors(Vector2Int position, HashSet<Vector2Int> region, bool diagonal)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0)
                continue;
            bool isDiagonal = dx != 0 && dy != 0;
            if (isDiagonal != diagonal)
                continue;

            var neighbor = position + new Vector2Int(dx, dy);
            if (neighbor.x < 0 || neighbor.y < 0 || neighbor.x >= width || neighbor.y >= height)
                continue;
            if (region.Contains(neighbor) || placed[neighbor.x, neighbor.y])
                count++;
        }
        return count;
    }

    static TileCategory GetTileCategory(TileSocketProfile profile)
    {
        if (profile.category != TileCategory.Unspecified)
            return profile.category;
        if (profile.baseTileName.StartsWith("Wide_")) return TileCategory.Wide;
        if (profile.baseTileName.StartsWith("Narrow_")) return TileCategory.Narrow;
        if (profile.baseTileName.StartsWith("Transition_")) return TileCategory.Transition;
        if (profile.baseTileName.StartsWith("Starter_")) return TileCategory.Starter;
        if (profile.baseTileName.StartsWith("Ground_")) return TileCategory.Ground;
        return TileCategory.Unspecified;
    }

    int GetOpeningCount(int tileIndex)
    {
        TileSocketProfile profile = database.tiles[tileIndex];
        return CountOpenings(profile.northHash)
            + CountOpenings(profile.southHash)
            + CountOpenings(profile.eastHash)
            + CountOpenings(profile.westHash);
    }

    static int CountOpenings(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return 0;

        int count = 0;
        foreach (char value in hash)
            if (value == '1')
                count++;
        return count;
    }

    static bool HasOpening(string hash)
    {
        return CountOpenings(hash) > 0;
    }

    bool FitsLocalNeighbors(
        Vector2Int position,
        int candidate,
        HashSet<Vector2Int> region,
        Dictionary<Vector2Int, int> assignments)
    {
        return FitsNeighbor(position, candidate, new Vector2Int(0, 1), south, north, region, assignments)
            && FitsNeighbor(position, candidate, new Vector2Int(0, -1), north, south, region, assignments)
            && FitsNeighbor(position, candidate, new Vector2Int(1, 0), east, west, region, assignments)
            && FitsNeighbor(position, candidate, new Vector2Int(-1, 0), west, east, region, assignments);
    }

    bool FitsNeighbor(
        Vector2Int position,
        int candidate,
        Vector2Int offset,
        Dictionary<int, HashSet<int>> rule,
        Dictionary<int, HashSet<int>> oppositeRule,
        HashSet<Vector2Int> region,
        Dictionary<Vector2Int, int> assignments)
    {
        Vector2Int neighborPosition = position + offset;
        if (neighborPosition.x < 0 || neighborPosition.y < 0
            || neighborPosition.x >= width || neighborPosition.y >= height)
            return true;

        if (!CandidateMatchesConnectionIntent(
                position, neighborPosition, candidate, offset))
        {
            return false;
        }

        if (assignments.TryGetValue(neighborPosition, out int assignedTile))
            return rule[candidate].Contains(assignedTile)
                && oppositeRule[assignedTile].Contains(candidate);

        if (region.Contains(neighborPosition))
            return true;

        // The pre-placed border is permanent ground, not an expandable
        // frontier. Validate it explicitly against the all-zero ground tile.
        if (fixedGround[neighborPosition.x, neighborPosition.y])
            return rule[candidate].Contains(groundTileIndex)
                && oppositeRule[groundTileIndex].Contains(candidate);

        // Unplaced interior cells are rendered and evaluated as ground. Only
        // already placed cells may satisfy a nonzero/open socket.
        if (!placed[neighborPosition.x, neighborPosition.y])
            return rule[candidate].Contains(groundTileIndex)
                && oppositeRule[groundTileIndex].Contains(candidate);

        foreach (int neighborTile in cells[neighborPosition.x, neighborPosition.y])
        {
            if (rule[candidate].Contains(neighborTile)
                && oppositeRule[neighborTile].Contains(candidate))
                return true;
        }

        return false;
    }

    bool CandidateMatchesConnectionIntent(
        Vector2Int position,
        Vector2Int neighborPosition,
        int candidate,
        Vector2Int offset)
    {
        ConnectionIntent intent = GetConnectionIntent(position, neighborPosition);
        if (intent == ConnectionIntent.Auto)
            return true;

        TileSide side;
        if (offset == Vector2Int.right)
            side = TileSide.East;
        else if (offset == Vector2Int.left)
            side = TileSide.West;
        else if (offset == Vector2Int.up)
            side = TileSide.South;
        else
            side = TileSide.North;

        bool isOpen = HasOpening(database.tiles[candidate].GetHash(side));
        return intent == ConnectionIntent.Open ? isOpen : !isOpen;
    }

}
