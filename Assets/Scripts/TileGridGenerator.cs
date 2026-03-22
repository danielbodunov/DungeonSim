using UnityEngine;
using System.Collections.Generic;

public class TileGridGenerator : MonoBehaviour
{
    [SerializeField] TileAdjacencyDatabase database;
    [SerializeField] GameObject placeholderPrefab;
    [SerializeField] int width = 32;
    [SerializeField] int height = 32;

    List<GameObject> prefabs;

    Dictionary<int, HashSet<int>> north;
    Dictionary<int, HashSet<int>> south;
    Dictionary<int, HashSet<int>> east;
    Dictionary<int, HashSet<int>> west;

    List<int>[,] cells;

    GameObject[,] instantiated;

    void Start()
    {
        BuildRuntimeDatabase();
        InitializeGrid();
        InstantiateGrid();
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

        for (int i = 0; i < database.tiles.Count; i++)
        {
            north[i] = BuildSet(database.tiles[i].northMatches, "north", i );
            south[i] = BuildSet(database.tiles[i].southMatches, "south", i);
            east[i]  = BuildSet(database.tiles[i].eastMatches, "east", i);
            west[i]  = BuildSet(database.tiles[i].westMatches, "west", i);
        }
        Debug.Log($"Runtime database built with {prefabs.Count} tiles.");
    }

    //build a set of tile indices that match the given list of side matches
    HashSet<int> BuildSet(List<string> names, string direction = "", int tileIndex = -1)
    {
        var set = new HashSet<int>();

        //strip rotation from names to match against base tile names in database
        List<string> namesStripped = new();
        foreach (var n in names)
        {
            string stripped = n.Split("_R")[0];
            namesStripped.Add(stripped);
        }


        for (int i = 0; i < database.tiles.Count; i++)

            if (namesStripped.Contains(database.tiles[i].sourcePrefab.name))
            {
                set.Add(i);
            }
                

        Debug.Log($"Built set with {set.Count} tiles.");
        if (set.Count == 0)
            Debug.LogWarning($"Warning: Tile {database.tiles[tileIndex].sourcePrefab.name} has no matches on its {direction} side.");
        return set;
    }

    // ---- 2. Grid ----

    void InitializeGrid()
    {
        cells = new List<int>[width, height];
        instantiated = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            cells[x, y] = new List<int>();
            if(x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    cells[x, y].Add(0); //TODO: replace with actual edge tiles
                    continue;
                }

            for (int i = 0; i < prefabs.Count; i++)
                cells[x, y].Add(i);
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

            ConstrainNeighbor(p.x, p.y, p.x, p.y + 1, north, queue);
            ConstrainNeighbor(p.x, p.y, p.x, p.y - 1, south, queue);
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

    void InstantiateGrid()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (cells[x, y].Count == 0)
            {
                InstantiateCell(x, y, 0);
                Debug.LogError("Contradiction detected.");
                continue;
            }
            if (cells[x, y].Count == 1)
            {
                int tileIndex = cells[x, y][0];
                instantiated[x, y] = Instantiate(prefabs[tileIndex], new Vector3(x, y, 0), Quaternion.identity, transform);
            }
            else
            {
                instantiated[x, y] = Instantiate(placeholderPrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
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
        instantiated[x, y] = Instantiate(
            prefabs[tileIndex],
            new Vector3(x, y, 0),
            Quaternion.identity,
            transform
        );
    }
    
    public void ClickCell(int x, int y)
    {
        if (cells[x, y].Count > 1)
        {
            int pick = cells[x, y][Random.Range(0, cells[x, y].Count)];
            Collapse(x, y, pick);
        }
    }
}