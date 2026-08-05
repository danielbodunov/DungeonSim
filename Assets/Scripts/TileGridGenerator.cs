using UnityEngine;
using System.Collections.Generic;

public class TileGridGenerator : MonoBehaviour
{
    const string GroundTileName = "Ground_Full_X";

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
        cells = new List<int>[width, height];
        instantiated = new GameObject[width, height];
        placed = new bool[width, height];
        fixedGround = new bool[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            //Get world position cell with respective origin and generation direction
            // Vector3 coord = GetWorldPosition(x, y);
            // int wX = (int)coord.x;
            // int wY = (int)coord.y;
            cells[x, y] = new List<int>();
            if(x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    cells[x, y].Add(groundTileIndex);
                    fixedGround[x, y] = true;
                    continue;
                }

            for (int i = 0; i < prefabs.Count; i++)
                cells[x, y].Add(i);
        }

        // The border cells are pre-collapsed to the ground tile. Their
        // constraints must be applied before the player can collapse a cell.
        for (int x = 0; x < width; x++)
        {
            Propagate(x, 0);
            if (height > 1)
                Propagate(x, height - 1);
        }

        for (int y = 1; y < height - 1; y++)
        {
            Propagate(0, y);
            if (width > 1)
                Propagate(width - 1, y);
        }
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
        Queue<Vector2Int> queue = new();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();

            // Grid rows increase downward in world space, so y + 1 is south.
            ConstrainNeighbor(p.x, p.y, p.x, p.y + 1, south, queue);
            ConstrainNeighbor(p.x, p.y, p.x, p.y - 1, north, queue);
            ConstrainNeighbor(p.x, p.y, p.x + 1, p.y, east,  queue);
            ConstrainNeighbor(p.x, p.y, p.x - 1, p.y, west,  queue);
        }
    }

    //Set neighbor's options to the subset of its current options that are allowed by the source cell's options and the adjacency rules, and if any options were removed, add the neighbor to the queue to propagate from it
    void ConstrainNeighbor(
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
        var sourceTiles = cells[x, y];
        var neighbor = cells[nx, ny];

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

    public void NotifyLayoutChanged()
    {
        if (propGenerator != null)
            propGenerator.GenerateProps();
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
        return upper.southHash == lower.northHash;
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
        if (instantiated[x, y] != null) Destroy(instantiated[x, y]);
        instantiated[x, y] = InstantiateTile(tileIndex, GetWorldPosition(x, y));
    }
    
    public void ClickWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridCoordinates = GetGridCoordinates(worldPosition);
        ClickCell(gridCoordinates.x, gridCoordinates.y);
    }

    public void PlaceGroundWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridCoordinates = GetGridCoordinates(worldPosition);
        PlaceGroundCell(gridCoordinates.x, gridCoordinates.y);
    }

    public void PlaceGroundCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || fixedGround[x, y])
            return;

        List<int>[,] previousCells = CopyCells();
        bool wasPlaced = placed[x, y];
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
        InstantiateCell(x, y, groundTileIndex);

        if (region.Count > 0)
        {
            var regionSet = new HashSet<Vector2Int>(region);
            if (!FindBestLocalAssignment(region, regionSet, out var assignments))
            {
                cells = previousCells;
                placed[x, y] = wasPlaced;
                InstantiateCurrentCell(x, y);
                foreach (var position in region)
                    InstantiateCurrentCell(position.x, position.y);
                Debug.LogWarning($"Ground cannot be placed at ({x},{y}) without disconnecting the surrounding layout.");
                return;
            }

            foreach (var assignment in assignments)
            {
                Vector2Int position = assignment.Key;
                cells[position.x, position.y].Clear();
                cells[position.x, position.y].Add(assignment.Value);
                InstantiateCell(position.x, position.y, assignment.Value);
            }
        }

        Propagate(x, y);
        if (HasContradiction())
        {
            cells = previousCells;
            placed[x, y] = wasPlaced;
            InstantiateCurrentCell(x, y);
            foreach (var position in region)
                InstantiateCurrentCell(position.x, position.y);
            Debug.LogWarning($"Ground placement at ({x},{y}) caused a contradiction and was reverted.");
            return;
        }

        NotifyLayoutChanged();
    }

    public void ClickCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            Debug.LogWarning($"Cell ({x},{y}) is outside the grid bounds [0-{width - 1}, 0-{height - 1}].");
            return;
        }

        if (fixedGround[x, y])
        {
            Debug.LogWarning($"Cell ({x},{y}) is fixed ground and cannot be replaced.");
            return;
        }

        if (!TryResolveLocalPlacement(x, y))
        {
            Debug.LogWarning($"No local tile combination can connect at ({x},{y}) without changing tiles farther away.");
        }
    }

    bool TryResolveLocalPlacement(int x, int y)
    {
        List<int>[,] previousCells = CopyCells();
        var center = new Vector2Int(x, y);
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
            return false;

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
            foreach (var position in region)
                InstantiateCurrentCell(position.x, position.y);
            return false;
        }

        placed[x, y] = true;
        NotifyLayoutChanged();
        return true;
    }

    List<int>[,] CopyCells()
    {
        var copy = new List<int>[width, height];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            copy[x, y] = new List<int>(cells[x, y]);
        return copy;
    }

    bool HasContradiction()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            if (cells[x, y].Count == 0)
                return true;
        return false;
    }

    void InstantiateCurrentCell(int x, int y)
    {
        if (instantiated[x, y] != null)
            Destroy(instantiated[x, y]);

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
        while (candidates.Count > 0)
        {
            int bestCandidateScore = int.MinValue;
            var tiedCandidates = new List<int>();
            foreach (int candidate in candidates)
            {
                int candidateScore = GetCandidateScore(position, candidate, region);
                if (candidateScore > bestCandidateScore)
                {
                    bestCandidateScore = candidateScore;
                    tiedCandidates.Clear();
                    tiedCandidates.Add(candidate);
                }
                else if (candidateScore == bestCandidateScore)
                {
                    tiedCandidates.Add(candidate);
                }
            }

            int selected = tiedCandidates[Random.Range(0, tiedCandidates.Count)];
            destination.Add(selected);
            candidates.Remove(selected);
        }
    }

    int GetCandidateScore(Vector2Int position, int tileIndex, HashSet<Vector2Int> region)
    {
        int cardinalNeighbors = CountOccupiedNeighbors(position, region, false);
        int diagonalNeighbors = CountOccupiedNeighbors(position, region, true);
        bool roomContext = cardinalNeighbors >= 2 && diagonalNeighbors > 0;
        TileCategory category = GetTileCategory(database.tiles[tileIndex]);

        int score = GetOpeningCount(tileIndex);
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
        return CountOccupiedNeighbors(position, region, false) >= 2
            && CountOccupiedNeighbors(position, region, true) > 0;
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

}
