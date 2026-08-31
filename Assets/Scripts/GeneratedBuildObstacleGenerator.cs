using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GeneratedBuildObstacleVisualVariant
{
    public string variantId = "Default";
    public GameObject prefab;
    [Tooltip("Instantiate this prefab once in every resolved footprint cell. " +
        "Disable only when the prefab is a composite authored to cover the " +
        "definition's complete footprint from its anchor.")]
    public bool instantiatePerFootprintCell = true;
}

[Serializable]
public sealed class GeneratedBuildObstacleDefinition
{
    public string definitionId = "Obstacle";
    public bool blocksConstruction = true;
    public bool blocksServiceSpace = true;
    public bool allowRotation = true;
    [Tooltip("Lowest world-space Y at which this definition may generate.")]
    public float minimumGenerationHeight = -1000f;
    [Tooltip("Highest world-space Y at which this definition may generate.")]
    public float maximumGenerationHeight = 1000f;
    public List<Vector2Int> footprintOffsets = new() { Vector2Int.zero };
    public List<GeneratedBuildObstacleVisualVariant> visualVariants = new();

    public IReadOnlyList<Vector2Int> ResolveFootprint(
        Vector2Int anchor,
        int quarterTurns)
    {
        var result = new List<Vector2Int>(footprintOffsets?.Count ?? 0);
        int rotation = allowRotation ? NormalizeRotation(quarterTurns) : 0;
        if (footprintOffsets == null)
            return result;
        for (int i = 0; i < footprintOffsets.Count; i++)
            result.Add(anchor + Rotate(footprintOffsets[i], rotation));
        return result;
    }

    public bool IsValid(out string failure)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            failure = "Obstacle definition ID is empty.";
            return false;
        }
        if (footprintOffsets == null || footprintOffsets.Count == 0 ||
            footprintOffsets.Count > 4)
        {
            failure = $"Obstacle '{definitionId}' must contain 1-4 footprint cells.";
            return false;
        }
        var unique = new HashSet<Vector2Int>();
        for (int i = 0; i < footprintOffsets.Count; i++)
            if (!unique.Add(footprintOffsets[i]))
            {
                failure = $"Obstacle '{definitionId}' repeats footprint offset " +
                    $"{footprintOffsets[i]}.";
                return false;
            }
        failure = string.Empty;
        return true;
    }

    public GeneratedBuildObstacleVisualVariant GetVariant(int index)
    {
        return visualVariants != null && index >= 0 && index < visualVariants.Count
            ? visualVariants[index]
            : null;
    }

    public static int NormalizeRotation(int quarterTurns) =>
        ((quarterTurns % 4) + 4) % 4;

    static Vector2Int Rotate(Vector2Int value, int quarterTurns)
    {
        return quarterTurns switch
        {
            1 => new Vector2Int(-value.y, value.x),
            2 => new Vector2Int(-value.x, -value.y),
            3 => new Vector2Int(value.y, -value.x),
            _ => value
        };
    }
}

[Serializable]
public sealed class SavedGeneratedBuildObstacle
{
    public string definitionId;
    public int anchorX;
    public int anchorY;
    public int rotation;
    public string variantId;

    public SavedGeneratedBuildObstacle Copy() => new()
    {
        definitionId = definitionId,
        anchorX = anchorX,
        anchorY = anchorY,
        rotation = rotation,
        variantId = variantId
    };
}

public sealed class GeneratedBuildObstacleInstance
{
    public GeneratedBuildObstacleDefinition Definition { get; }
    public Vector2Int Anchor { get; }
    public int Rotation { get; }
    public string VariantId { get; }
    public IReadOnlyList<Vector2Int> FootprintCells { get; }
    public GameObject Visual { get; internal set; }

    public GeneratedBuildObstacleInstance(
        GeneratedBuildObstacleDefinition definition,
        Vector2Int anchor,
        int rotation,
        string variantId)
    {
        Definition = definition;
        Anchor = anchor;
        Rotation = GeneratedBuildObstacleDefinition.NormalizeRotation(rotation);
        VariantId = variantId ?? string.Empty;
        FootprintCells = definition.ResolveFootprint(anchor, Rotation);
    }
}

[DisallowMultipleComponent]
public sealed class GeneratedBuildObstacleGenerator : MonoBehaviour
{
    [SerializeField] bool generateOnInitialize = true;
    [SerializeField] int generationSeed = 26026;
    [SerializeField, Min(0)] int additionalObstacleCount = 4;
    [SerializeField, Min(0)] int protectedRadiusAroundBuiltCells = 2;
    [SerializeField, Min(0)] int protectedRadiusAroundGridCenter = 2;
    [SerializeField] List<GeneratedBuildObstacleDefinition> definitions = new();

    readonly List<GeneratedBuildObstacleInstance> instances = new();
    readonly Dictionary<Vector2Int, GeneratedBuildObstacleInstance> byCell = new();
    TileGridGenerator grid;
    Transform visualContainer;

    public int GenerationSeed => generationSeed;
    public IReadOnlyList<GeneratedBuildObstacleInstance> Instances => instances;

    public void Initialize(TileGridGenerator owner)
    {
        grid = owner;
        EnsureDefaultDefinitions();
        if (generateOnInitialize)
            Generate(generationSeed);
    }

    public void InitializeDefinitionsOnly(TileGridGenerator owner)
    {
        grid = owner;
        EnsureDefaultDefinitions();
    }

    public bool TryResolveFootprint(
        SavedGeneratedBuildObstacle saved,
        out IReadOnlyList<Vector2Int> footprint,
        out bool blocksServiceSpace)
    {
        GeneratedBuildObstacleDefinition definition =
            saved != null ? FindDefinition(saved.definitionId) : null;
        if (definition == null)
        {
            footprint = Array.Empty<Vector2Int>();
            blocksServiceSpace = false;
            return false;
        }
        footprint = definition.ResolveFootprint(
            new Vector2Int(saved.anchorX, saved.anchorY), saved.rotation);
        blocksServiceSpace = definition.blocksServiceSpace;
        return true;
    }

    public void ConfigureForTests(
        IReadOnlyList<GeneratedBuildObstacleDefinition> configuredDefinitions,
        int extraCount = 0)
    {
        definitions = configuredDefinitions != null
            ? new List<GeneratedBuildObstacleDefinition>(configuredDefinitions)
            : new List<GeneratedBuildObstacleDefinition>();
        additionalObstacleCount = Mathf.Max(0, extraCount);
    }

    public bool IsConstructionBlocked(Vector2Int cell) =>
        byCell.TryGetValue(cell, out GeneratedBuildObstacleInstance obstacle) &&
        obstacle.Definition.blocksConstruction;

    public bool IsServiceSpaceBlocked(Vector2Int cell) =>
        byCell.TryGetValue(cell, out GeneratedBuildObstacleInstance obstacle) &&
        obstacle.Definition.blocksServiceSpace;

    public bool TryGetObstacle(
        Vector2Int cell,
        out GeneratedBuildObstacleInstance obstacle) => byCell.TryGetValue(cell, out obstacle);

    public string GetBlockReason(Vector2Int cell)
    {
        return byCell.TryGetValue(cell, out GeneratedBuildObstacleInstance obstacle)
            ? $"generated obstacle '{obstacle.Definition.definitionId}'/" +
              $"'{obstacle.VariantId}' anchored at {obstacle.Anchor}"
            : string.Empty;
    }

    public void Generate(int seed)
    {
        if (grid == null || !grid.IsInitialized)
            return;
        Clear();
        generationSeed = seed;
        EnsureDefaultDefinitions();

        var random = new System.Random(seed);
        var order = new List<Vector2Int>();
        for (int x = 1; x < grid.GridWidth - 1; x++)
        for (int y = 1; y < grid.GridHeight - 1; y++)
            order.Add(new Vector2Int(x, y));
        Shuffle(order, random);

        // Prove every configured footprint when space exists, then add variety.
        for (int i = 0; i < definitions.Count; i++)
            TryPlaceFromCandidates(definitions[i], order, random);
        for (int i = 0; i < additionalObstacleCount && definitions.Count > 0; i++)
            TryPlaceFromCandidates(definitions[random.Next(definitions.Count)], order, random);
    }

    public List<SavedGeneratedBuildObstacle> Capture()
    {
        var result = new List<SavedGeneratedBuildObstacle>(instances.Count);
        for (int i = 0; i < instances.Count; i++)
        {
            GeneratedBuildObstacleInstance instance = instances[i];
            result.Add(new SavedGeneratedBuildObstacle
            {
                definitionId = instance.Definition.definitionId,
                anchorX = instance.Anchor.x,
                anchorY = instance.Anchor.y,
                rotation = instance.Rotation,
                variantId = instance.VariantId
            });
        }
        result.Sort((a, b) =>
        {
            int compare = a.anchorX.CompareTo(b.anchorX);
            return compare != 0 ? compare : a.anchorY.CompareTo(b.anchorY);
        });
        return result;
    }

    public bool Validate(
        IReadOnlyList<SavedGeneratedBuildObstacle> saved,
        TileGridGenerator.PlacementValidationContext context,
        out string failure)
    {
        var occupied = new HashSet<Vector2Int>();
        if (saved == null)
        {
            failure = string.Empty;
            return true;
        }
        for (int i = 0; i < saved.Count; i++)
        {
            SavedGeneratedBuildObstacle record = saved[i];
            GeneratedBuildObstacleDefinition definition = FindDefinition(record?.definitionId);
            if (record == null)
            {
                failure = $"Obstacle record {i + 1} is empty.";
                return false;
            }
            if (definition == null)
            {
                failure = $"Obstacle definition '{record.definitionId}' is unavailable.";
                return false;
            }
            if (!definition.IsValid(out failure))
            {
                return false;
            }
            var anchor = new Vector2Int(record.anchorX, record.anchorY);
            IReadOnlyList<Vector2Int> footprint =
                definition.ResolveFootprint(anchor, record.rotation);
            for (int c = 0; c < footprint.Count; c++)
            {
                Vector2Int cell = footprint[c];
                if (!IsEligible(cell, context) || !occupied.Add(cell))
                {
                    failure = $"Obstacle '{definition.definitionId}' has an invalid or " +
                        $"overlapping footprint cell {cell}.";
                    return false;
                }
            }
        }
        failure = string.Empty;
        return true;
    }

    public bool Restore(
        IReadOnlyList<SavedGeneratedBuildObstacle> saved,
        TileGridGenerator.PlacementValidationContext context,
        out string failure)
    {
        if (!Validate(saved, context, out failure))
            return false;
        Clear();
        if (saved == null)
            return true;
        for (int i = 0; i < saved.Count; i++)
        {
            SavedGeneratedBuildObstacle record = saved[i];
            AddInstance(new GeneratedBuildObstacleInstance(
                FindDefinition(record.definitionId),
                new Vector2Int(record.anchorX, record.anchorY),
                record.rotation,
                record.variantId));
        }
        return true;
    }

    public void Clear()
    {
        for (int i = 0; i < instances.Count; i++)
            if (instances[i].Visual != null)
                Destroy(instances[i].Visual);
        instances.Clear();
        byCell.Clear();
        if (visualContainer != null)
            Destroy(visualContainer.gameObject);
        visualContainer = null;
    }

    bool TryPlaceFromCandidates(
        GeneratedBuildObstacleDefinition definition,
        List<Vector2Int> candidates,
        System.Random random)
    {
        if (definition == null || !definition.IsValid(out _))
            return false;
        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int anchor = candidates[i];
            int rotation = definition.allowRotation ? random.Next(4) : 0;
            IReadOnlyList<Vector2Int> footprint = definition.ResolveFootprint(anchor, rotation);
            bool valid = true;
            for (int c = 0; c < footprint.Count; c++)
                if (!IsEligible(footprint[c], null) ||
                    !IsWithinGenerationHeight(footprint[c], definition) ||
                    byCell.ContainsKey(footprint[c]))
                {
                    valid = false;
                    break;
                }
            if (!valid)
                continue;

            int variantIndex = definition.visualVariants?.Count > 0
                ? random.Next(definition.visualVariants.Count)
                : -1;
            string variantId = definition.GetVariant(variantIndex)?.variantId ?? "Fallback";
            AddInstance(new GeneratedBuildObstacleInstance(
                definition, anchor, rotation, variantId));
            return true;
        }
        return false;
    }

    bool IsEligible(
        Vector2Int cell,
        TileGridGenerator.PlacementValidationContext context)
    {
        if (grid == null || cell.x <= 0 || cell.y <= 0 ||
            cell.x >= grid.GridWidth - 1 || cell.y >= grid.GridHeight - 1 ||
            grid.IsFixedGround(cell.x, cell.y) ||
            (context != null ? context.IsPlacedCell(cell.x, cell.y) : grid.IsPlacedCell(cell.x, cell.y)))
            return false;
        if (context == null && grid.IsCellOccupiedByGeneratedProp(cell))
            return false;
        // The protected radius is a generation heuristic that preserves an
        // initial expansion area. It must not become a persistence invariant:
        // players are allowed to build beside an existing obstacle, and that
        // later construction must not invalidate save/scenario restoration.
        if (context == null && IsProtected(cell, null))
            return false;
        return true;
    }

    bool IsWithinGenerationHeight(
        Vector2Int cell,
        GeneratedBuildObstacleDefinition definition)
    {
        float worldHeight = grid.GetWorldPosition(cell.x, cell.y).y;
        float lowerHeight = Mathf.Min(
            definition.minimumGenerationHeight,
            definition.maximumGenerationHeight);
        float upperHeight = Mathf.Max(
            definition.minimumGenerationHeight,
            definition.maximumGenerationHeight);
        return worldHeight >= lowerHeight && worldHeight <= upperHeight;
    }

    bool IsProtected(
        Vector2Int cell,
        TileGridGenerator.PlacementValidationContext context)
    {
        var center = new Vector2Int(grid.GridWidth / 2, grid.GridHeight / 2);
        if (ChebyshevDistance(cell, center) <= protectedRadiusAroundGridCenter)
            return true;
        for (int x = 1; x < grid.GridWidth - 1; x++)
        for (int y = 1; y < grid.GridHeight - 1; y++)
            if ((context != null
                    ? context.IsPlacedCell(x, y)
                    : grid.IsPlacedCell(x, y)) &&
                ChebyshevDistance(cell, new Vector2Int(x, y)) <= protectedRadiusAroundBuiltCells)
                return true;
        return false;
    }

    void AddInstance(GeneratedBuildObstacleInstance instance)
    {
        instances.Add(instance);
        for (int i = 0; i < instance.FootprintCells.Count; i++)
            byCell.Add(instance.FootprintCells[i], instance);
        instance.Visual = SpawnVisual(instance);
    }

    GameObject SpawnVisual(GeneratedBuildObstacleInstance instance)
    {
        GeneratedBuildObstacleVisualVariant variant = null;
        if (instance.Definition.visualVariants != null)
            for (int i = 0; i < instance.Definition.visualVariants.Count; i++)
                if (string.Equals(instance.Definition.visualVariants[i]?.variantId,
                        instance.VariantId, StringComparison.Ordinal))
                    variant = instance.Definition.visualVariants[i];

        if (visualContainer == null)
        {
            var container = new GameObject("Generated Build Obstacles");
            container.transform.SetParent(transform, false);
            visualContainer = container.transform;
        }
        Vector3 position = grid.GetWorldPosition(instance.Anchor.x, instance.Anchor.y);
        GameObject visual;
        if (variant?.prefab != null)
        {
            Quaternion rotation =
                Quaternion.Euler(0f, 0f, -90f * instance.Rotation);
            if (variant.instantiatePerFootprintCell)
            {
                visual = new GameObject();
                visual.transform.SetParent(visualContainer, false);
                for (int i = 0; i < instance.FootprintCells.Count; i++)
                {
                    Vector2Int cell = instance.FootprintCells[i];
                    GameObject cellVisual = Instantiate(
                        variant.prefab,
                        grid.GetWorldPosition(cell.x, cell.y),
                        rotation,
                        visual.transform);
                    cellVisual.name = $"{variant.prefab.name} @ {cell}";
                }
            }
            else
            {
                visual = Instantiate(
                    variant.prefab, position, rotation, visualContainer);
            }
        }
        else
        {
            visual = new GameObject();
            visual.transform.SetParent(visualContainer, false);
            for (int i = 0; i < instance.FootprintCells.Count; i++)
            {
                Vector2Int cell = instance.FootprintCells[i];
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Blocked Cell {cell}";
                marker.transform.SetParent(visual.transform, false);
                marker.transform.position = grid.GetWorldPosition(cell.x, cell.y) +
                    Vector3.back * 0.5f;
                marker.transform.localScale = new Vector3(.72f, .72f, .4f);
                Collider collider = marker.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;
            }
        }
        visual.name = $"Obstacle {instance.Definition.definitionId}/" +
            $"{instance.VariantId} @ {instance.Anchor} R{instance.Rotation}";
        if (visual.GetComponentInChildren<DungeonLightReceiver>(true) == null)
            visual.AddComponent<DungeonLightReceiver>();
        return visual;
    }

    GeneratedBuildObstacleDefinition FindDefinition(string id)
    {
        if (definitions == null)
            return null;
        for (int i = 0; i < definitions.Count; i++)
            if (string.Equals(definitions[i]?.definitionId, id,
                    StringComparison.OrdinalIgnoreCase))
                return definitions[i];
        return null;
    }

    void EnsureDefaultDefinitions()
    {
        if (definitions != null && definitions.Count > 0)
            return;
        definitions = new List<GeneratedBuildObstacleDefinition>
        {
            CreateDefault("Rock_1", Vector2Int.zero),
            CreateDefault("Formation_2", Vector2Int.zero, Vector2Int.right),
            CreateDefault("Formation_L3", Vector2Int.zero, Vector2Int.right, Vector2Int.up),
            CreateDefault("Formation_2x2", Vector2Int.zero, Vector2Int.right,
                Vector2Int.up, Vector2Int.one)
        };
    }

    static GeneratedBuildObstacleDefinition CreateDefault(
        string id,
        params Vector2Int[] offsets) => new()
    {
        definitionId = id,
        footprintOffsets = new List<Vector2Int>(offsets),
        visualVariants = new List<GeneratedBuildObstacleVisualVariant>
        {
            new() { variantId = "Boulder" },
            new() { variantId = "Bone" },
            new() { variantId = "Relic" },
            new() { variantId = "Ore" }
        }
    };

    static int ChebyshevDistance(Vector2Int a, Vector2Int b) =>
        Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    static void Shuffle<T>(List<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swap = random.Next(i + 1);
            (values[i], values[swap]) = (values[swap], values[i]);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (grid == null)
            return;
        Gizmos.color = new Color(1f, .25f, .05f, .65f);
        for (int i = 0; i < instances.Count; i++)
        for (int c = 0; c < instances[i].FootprintCells.Count; c++)
            Gizmos.DrawWireCube(
                grid.GetWorldPosition(
                    instances[i].FootprintCells[c].x,
                    instances[i].FootprintCells[c].y),
                new Vector3(.9f, .9f, .2f));
    }
}
