using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a low-resolution, world-space dungeon light field. Lighting data is
/// stored and refreshed in chunks, then uploaded to one texture for inexpensive
/// sampling by dungeon materials.
/// </summary>
[DisallowMultipleComponent]
public class DungeonLightingManager : MonoBehaviour
{
    public enum LightQualityPreset
    {
        LegacyCell,
        Smooth2x,
        Smooth4x
    }

    static readonly int LightTextureId = Shader.PropertyToID("_DungeonLightTexture");
    static readonly int GridCellZeroId = Shader.PropertyToID("_DungeonGridCellZero");
    static readonly int GridStepId = Shader.PropertyToID("_DungeonGridStep");
    static readonly int GridSizeId = Shader.PropertyToID("_DungeonGridSize");
    static readonly int AmbientColorId = Shader.PropertyToID("_DungeonAmbientColor");

    static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    [Header("References")]
    [SerializeField] TileGridGenerator grid;

    [Header("Light Field")]
    [SerializeField, Tooltip("Optional prototype material applied to placed dungeon tiles. Leave empty to use only the debug gizmos and shader globals.")]
    Material dungeonMaterialOverride;
    [SerializeField, Range(8, 128)] int chunkSize = 32;
    [SerializeField] Color ambientColor = new(0.025f, 0.03f, 0.045f, 1f);
    [SerializeField, Tooltip("Legacy Cell reproduces the original one-sample, graph-distance lighting. Smooth modes use bilinear sub-cell samples and world-distance falloff.")]
    LightQualityPreset qualityPreset = LightQualityPreset.Smooth2x;
    [SerializeField, Tooltip("Texture filtering used by Legacy Cell mode. Smooth modes always use bilinear filtering.")]
    FilterMode textureFilter = FilterMode.Point;
    [SerializeField, Min(0.02f), Tooltip("Moving sources such as NPC torches refresh at this interval, not every frame.")]
    float dynamicUpdateInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] bool drawChunkBounds = true;
    [SerializeField] bool drawLightCells;
    [SerializeField, Range(0f, 1f)] float debugMinimumBrightness = 0.02f;
    [SerializeField, Min(16)] int debugMaximumCells = 4096;

    readonly List<DungeonLightSource> sources = new();
    readonly HashSet<int> previousDynamicChunks = new();
    readonly HashSet<int> chunksToRefresh = new();
    readonly HashSet<int> newlyTouchedChunks = new();

    LightingChunk[] chunks;
    Texture2D lightTexture;
    int activeChunkSize;
    int activeSamplesPerCell;
    LightQualityPreset activeQualityPreset;
    int chunksX;
    int chunksY;
    int[] visitStamps;
    int[] distances;
    int[] propagationQueue;
    int visitStamp;
    float dynamicTimer;
    bool initialized;
    bool rebuildRequested = true;

    sealed class LightingChunk
    {
        public readonly RectInt Bounds;
        public readonly Color32[] StaticLight;
        public readonly Color32[] DynamicLight;
        public readonly Color32[] UploadBuffer;

        public LightingChunk(RectInt bounds, int samplesPerCell)
        {
            Bounds = bounds;
            int length = bounds.width * bounds.height *
                samplesPerCell * samplesPerCell;
            StaticLight = new Color32[length];
            DynamicLight = new Color32[length];
            UploadBuffer = new Color32[length];
        }

        public int GetLocalSampleIndex(
            int x,
            int y,
            int sampleX,
            int sampleY,
            int samplesPerCell)
        {
            int sampleWidth = Bounds.width * samplesPerCell;
            int localSampleX = (x - Bounds.xMin) * samplesPerCell + sampleX;
            int localSampleY = (y - Bounds.yMin) * samplesPerCell + sampleY;
            return localSampleY * sampleWidth + localSampleX;
        }
    }

    public Texture2D LightTexture => lightTexture;
    public int ChunkSize => initialized ? activeChunkSize : chunkSize;
    public int SamplesPerCell => initialized
        ? activeSamplesPerCell
        : GetSamplesPerCell(qualityPreset);
    public LightQualityPreset QualityPreset => qualityPreset;
    public Vector2Int ChunkCount => new(chunksX, chunksY);

    void Awake()
    {
        if (grid == null)
            grid = GetComponent<TileGridGenerator>();
    }

    void OnEnable()
    {
        if (grid == null)
            grid = GetComponent<TileGridGenerator>();
        if (grid != null)
            grid.LayoutChanged += RequestFullRebuild;
        DungeonLightSource.SourcesChanged += RequestFullRebuild;
        rebuildRequested = true;
    }

    void Start()
    {
        TryInitialize();
    }

    void OnDisable()
    {
        if (grid != null)
            grid.LayoutChanged -= RequestFullRebuild;
        DungeonLightSource.SourcesChanged -= RequestFullRebuild;
    }

    void OnDestroy()
    {
        ReleaseLightingData();
    }

    void Update()
    {
        if (initialized && ConfigurationRequiresReinitialization())
            ReleaseLightingData();

        if (!initialized && !TryInitialize())
            return;

        Shader.SetGlobalColor(AmbientColorId, ambientColor);

        if (rebuildRequested)
        {
            RebuildLighting();
            return;
        }

        dynamicTimer += Time.unscaledDeltaTime;
        if (dynamicTimer >= dynamicUpdateInterval)
        {
            dynamicTimer = 0f;
            RefreshDynamicLighting();
        }
    }

    public void RequestFullRebuild()
    {
        rebuildRequested = true;
    }

    [ContextMenu("Rebuild Dungeon Lighting")]
    public void RebuildLighting()
    {
        if (!initialized && !TryInitialize())
            return;

        DungeonLightSource.GetActiveSources(sources);
        grid.ApplyMaterialToPlacedTiles(dungeonMaterialOverride);
        previousDynamicChunks.Clear();
        newlyTouchedChunks.Clear();

        for (int i = 0; i < chunks.Length; i++)
        {
            Array.Clear(chunks[i].StaticLight, 0, chunks[i].StaticLight.Length);
            Array.Clear(chunks[i].DynamicLight, 0, chunks[i].DynamicLight.Length);
        }

        foreach (DungeonLightSource source in sources)
        {
            if (source.IsDynamic)
                Propagate(source, true, newlyTouchedChunks);
            else
                Propagate(source, false, null);
        }

        previousDynamicChunks.UnionWith(newlyTouchedChunks);
        UploadAllChunks();
        rebuildRequested = false;
        dynamicTimer = 0f;
    }

    public Color GetCellLight(int x, int y)
    {
        if (!initialized || x < 0 || y < 0 || x >= grid.GridWidth || y >= grid.GridHeight)
            return Color.black;

        LightingChunk chunk = GetChunk(x, y);
        Color total = Color.black;
        for (int sampleY = 0; sampleY < activeSamplesPerCell; sampleY++)
        for (int sampleX = 0; sampleX < activeSamplesPerCell; sampleX++)
        {
            int localIndex = chunk.GetLocalSampleIndex(
                x, y, sampleX, sampleY, activeSamplesPerCell);
            total += AddColors(
                chunk.StaticLight[localIndex], chunk.DynamicLight[localIndex]);
        }

        return total / (activeSamplesPerCell * activeSamplesPerCell);
    }

    bool TryInitialize()
    {
        if (initialized)
            return true;
        if (grid == null || !grid.IsInitialized || grid.GridWidth <= 0 || grid.GridHeight <= 0)
            return false;

        activeChunkSize = Mathf.Max(8, chunkSize);
        activeQualityPreset = qualityPreset;
        activeSamplesPerCell = GetSamplesPerCell(activeQualityPreset);
        int maximumSamplesPerCell = Mathf.Min(
            SystemInfo.maxTextureSize / grid.GridWidth,
            SystemInfo.maxTextureSize / grid.GridHeight);
        if (activeSamplesPerCell > maximumSamplesPerCell)
        {
            int requestedSamplesPerCell = activeSamplesPerCell;
            activeSamplesPerCell = Mathf.Max(1, maximumSamplesPerCell);
            Debug.LogWarning(
                $"Dungeon lighting requested {requestedSamplesPerCell}x samples " +
                $"per cell, but the grid exceeds this device's " +
                $"{SystemInfo.maxTextureSize}px texture limit. Using " +
                $"{activeSamplesPerCell}x samples instead.",
                this);
        }
        chunksX = Mathf.CeilToInt(grid.GridWidth / (float)activeChunkSize);
        chunksY = Mathf.CeilToInt(grid.GridHeight / (float)activeChunkSize);
        chunks = new LightingChunk[chunksX * chunksY];

        for (int chunkY = 0; chunkY < chunksY; chunkY++)
        for (int chunkX = 0; chunkX < chunksX; chunkX++)
        {
            int x = chunkX * activeChunkSize;
            int y = chunkY * activeChunkSize;
            int width = Mathf.Min(activeChunkSize, grid.GridWidth - x);
            int height = Mathf.Min(activeChunkSize, grid.GridHeight - y);
            chunks[GetChunkIndex(chunkX, chunkY)] = new LightingChunk(
                new RectInt(x, y, width, height), activeSamplesPerCell);
        }

        int cellCount = grid.GridWidth * grid.GridHeight;
        visitStamps = new int[cellCount];
        distances = new int[cellCount];
        propagationQueue = new int[cellCount];

        lightTexture = new Texture2D(
            grid.GridWidth * activeSamplesPerCell,
            grid.GridHeight * activeSamplesPerCell,
            TextureFormat.RGBA32,
            false,
            true)
        {
            name = $"Dungeon Light Grid {grid.GridWidth}x{grid.GridHeight} " +
                $"({activeSamplesPerCell}x samples)",
            filterMode = GetActiveTextureFilter(),
            wrapMode = TextureWrapMode.Clamp
        };

        Vector3 firstCell = grid.GetCellWorldPosition(0, 0);
        Vector2 step = grid.GridGenerationDirection;
        Shader.SetGlobalTexture(LightTextureId, lightTexture);
        Shader.SetGlobalVector(GridCellZeroId, new Vector4(firstCell.x, firstCell.y, 0f, 0f));
        Shader.SetGlobalVector(GridStepId, new Vector4(step.x, step.y, 0f, 0f));
        Shader.SetGlobalVector(GridSizeId, new Vector4(grid.GridWidth, grid.GridHeight, 0f, 0f));
        Shader.SetGlobalColor(AmbientColorId, ambientColor);

        initialized = true;
        rebuildRequested = true;
        return true;
    }

    void RefreshDynamicLighting()
    {
        DungeonLightSource.GetActiveSources(sources);
        chunksToRefresh.Clear();
        chunksToRefresh.UnionWith(previousDynamicChunks);

        foreach (DungeonLightSource source in sources)
            if (source.IsDynamic)
                AddSourceBoundsChunks(source, chunksToRefresh);

        if (chunksToRefresh.Count == 0)
            return;

        foreach (int chunkIndex in chunksToRefresh)
            Array.Clear(
                chunks[chunkIndex].DynamicLight, 0,
                chunks[chunkIndex].DynamicLight.Length);

        newlyTouchedChunks.Clear();
        foreach (DungeonLightSource source in sources)
            if (source.IsDynamic)
                Propagate(source, true, newlyTouchedChunks);

        chunksToRefresh.UnionWith(newlyTouchedChunks);
        UploadChunks(chunksToRefresh);

        previousDynamicChunks.Clear();
        previousDynamicChunks.UnionWith(newlyTouchedChunks);
    }

    void Propagate(
        DungeonLightSource source,
        bool dynamicLayer,
        HashSet<int> touchedChunks)
    {
        if (!grid.TryWorldToCell(source.transform.position, out Vector2Int start) ||
            !grid.IsPlacedCell(start.x, start.y))
        {
            return;
        }

        int maxDistance = Mathf.CeilToInt(source.RadiusInCells);
        int stamp = NextVisitStamp();
        int startIndex = GetCellIndex(start.x, start.y);
        Vector2 sourceGridPosition = WorldToContinuousGrid(source.transform.position);
        int queueHead = 0;
        int queueTail = 0;
        propagationQueue[queueTail++] = startIndex;
        visitStamps[startIndex] = stamp;
        distances[startIndex] = 0;

        while (queueHead < queueTail)
        {
            int cellIndex = propagationQueue[queueHead++];
            int x = cellIndex % grid.GridWidth;
            int y = cellIndex / grid.GridWidth;
            int distance = distances[cellIndex];

            AddCellSamples(
                source,
                x,
                y,
                distance,
                sourceGridPosition,
                dynamicLayer,
                touchedChunks);

            if (distance >= maxDistance)
                continue;

            Vector2Int from = new(x, y);
            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int neighbor = from + direction;
                if (neighbor.x < 0 || neighbor.y < 0 ||
                    neighbor.x >= grid.GridWidth || neighbor.y >= grid.GridHeight)
                {
                    continue;
                }

                int neighborIndex = GetCellIndex(neighbor.x, neighbor.y);
                if (visitStamps[neighborIndex] == stamp ||
                    !grid.CanLightPass(from, neighbor))
                {
                    continue;
                }

                visitStamps[neighborIndex] = stamp;
                distances[neighborIndex] = distance + 1;
                propagationQueue[queueTail++] = neighborIndex;
            }
        }
    }

    void AddCellSamples(
        DungeonLightSource source,
        int x,
        int y,
        int graphDistance,
        Vector2 sourceGridPosition,
        bool dynamicLayer,
        HashSet<int> touchedChunks)
    {
        int chunkIndex = GetChunkIndex(x / activeChunkSize, y / activeChunkSize);
        LightingChunk chunk = chunks[chunkIndex];

        if (activeQualityPreset == LightQualityPreset.LegacyCell)
        {
            float falloff = CalculateFalloff(graphDistance, source.RadiusInCells);
            Color contribution = source.LightColor * (source.Intensity * falloff);
            contribution.a = 1f;
            AddSample(chunk, x, y, 0, 0, contribution, dynamicLayer);
            touchedChunks?.Add(chunkIndex);
            return;
        }

        float topologyMinimumDistance = Mathf.Max(0f, graphDistance - 0.5f);

        for (int sampleY = 0; sampleY < activeSamplesPerCell; sampleY++)
        for (int sampleX = 0; sampleX < activeSamplesPerCell; sampleX++)
        {
            Vector2 sampleGridPosition = new(
                x - 0.5f + (sampleX + 0.5f) / activeSamplesPerCell,
                y - 0.5f + (sampleY + 0.5f) / activeSamplesPerCell);
            float worldDistance = Vector2.Distance(
                sampleGridPosition, sourceGridPosition);

            // Euclidean distance produces smooth circular falloff. The graph
            // lower bound keeps nearby samples from becoming bright through a
            // long route around a closed wall.
            float effectiveDistance = Mathf.Max(
                worldDistance, topologyMinimumDistance);
            float falloff = CalculateFalloff(
                effectiveDistance, source.RadiusInCells);
            Color contribution = source.LightColor * (source.Intensity * falloff);
            contribution.a = 1f;
            AddSample(
                chunk,
                x,
                y,
                sampleX,
                sampleY,
                contribution,
                dynamicLayer);
        }

        touchedChunks?.Add(chunkIndex);
    }

    void AddSample(
        LightingChunk chunk,
        int x,
        int y,
        int sampleX,
        int sampleY,
        Color contribution,
        bool dynamicLayer)
    {
        int localIndex = chunk.GetLocalSampleIndex(
            x, y, sampleX, sampleY, activeSamplesPerCell);
        if (dynamicLayer)
            chunk.DynamicLight[localIndex] = AddColor(
                chunk.DynamicLight[localIndex], contribution);
        else
            chunk.StaticLight[localIndex] = AddColor(
                chunk.StaticLight[localIndex], contribution);
    }

    static Color32 AddColor(Color32 current, Color contribution)
    {
        return new Color32(
            (byte)Mathf.Min(255, current.r +
                Mathf.RoundToInt(Mathf.Clamp01(contribution.r) * 255f)),
            (byte)Mathf.Min(255, current.g +
                Mathf.RoundToInt(Mathf.Clamp01(contribution.g) * 255f)),
            (byte)Mathf.Min(255, current.b +
                Mathf.RoundToInt(Mathf.Clamp01(contribution.b) * 255f)),
            255);
    }

    static Color AddColors(Color32 first, Color32 second)
    {
        return new Color(
            Mathf.Min(255, first.r + second.r) / 255f,
            Mathf.Min(255, first.g + second.g) / 255f,
            Mathf.Min(255, first.b + second.b) / 255f,
            1f);
    }

    static Color32 AddColors32(Color32 first, Color32 second)
    {
        return new Color32(
            (byte)Mathf.Min(255, first.r + second.r),
            (byte)Mathf.Min(255, first.g + second.g),
            (byte)Mathf.Min(255, first.b + second.b),
            255);
    }

    static float CalculateFalloff(float distance, float radiusInCells)
    {
        float falloff = Mathf.Clamp01(1f - distance / (radiusInCells + 1f));
        return falloff * falloff;
    }

    Vector2 WorldToContinuousGrid(Vector3 worldPosition)
    {
        Vector3 firstCell = grid.GetCellWorldPosition(0, 0);
        Vector2 step = grid.GridGenerationDirection;
        float safeStepX = Mathf.Abs(step.x) < 0.0001f ? 1f : step.x;
        float safeStepY = Mathf.Abs(step.y) < 0.0001f ? 1f : step.y;
        return new Vector2(
            (worldPosition.x - firstCell.x) / safeStepX,
            (worldPosition.y - firstCell.y) / safeStepY);
    }

    void AddSourceBoundsChunks(DungeonLightSource source, HashSet<int> results)
    {
        if (!grid.TryWorldToCell(source.transform.position, out Vector2Int cell))
            return;

        int radius = Mathf.CeilToInt(source.RadiusInCells);
        int minChunkX = Mathf.Clamp((cell.x - radius) / activeChunkSize, 0, chunksX - 1);
        int maxChunkX = Mathf.Clamp((cell.x + radius) / activeChunkSize, 0, chunksX - 1);
        int minChunkY = Mathf.Clamp((cell.y - radius) / activeChunkSize, 0, chunksY - 1);
        int maxChunkY = Mathf.Clamp((cell.y + radius) / activeChunkSize, 0, chunksY - 1);

        for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
            results.Add(GetChunkIndex(chunkX, chunkY));
    }

    void UploadAllChunks()
    {
        chunksToRefresh.Clear();
        for (int i = 0; i < chunks.Length; i++)
            chunksToRefresh.Add(i);
        UploadChunks(chunksToRefresh);
    }

    void UploadChunks(IEnumerable<int> chunkIndices)
    {
        foreach (int chunkIndex in chunkIndices)
        {
            LightingChunk chunk = chunks[chunkIndex];
            for (int i = 0; i < chunk.UploadBuffer.Length; i++)
            {
                chunk.UploadBuffer[i] = AddColors32(
                    chunk.StaticLight[i], chunk.DynamicLight[i]);
            }

            lightTexture.SetPixels32(
                chunk.Bounds.xMin * activeSamplesPerCell,
                chunk.Bounds.yMin * activeSamplesPerCell,
                chunk.Bounds.width * activeSamplesPerCell,
                chunk.Bounds.height * activeSamplesPerCell,
                chunk.UploadBuffer);
        }

        lightTexture.Apply(false, false);
    }

    int NextVisitStamp()
    {
        if (visitStamp == int.MaxValue)
        {
            Array.Clear(visitStamps, 0, visitStamps.Length);
            visitStamp = 0;
        }
        return ++visitStamp;
    }

    int GetCellIndex(int x, int y)
    {
        return y * grid.GridWidth + x;
    }

    int GetChunkIndex(int chunkX, int chunkY)
    {
        return chunkY * chunksX + chunkX;
    }

    LightingChunk GetChunk(int x, int y)
    {
        return chunks[GetChunkIndex(x / activeChunkSize, y / activeChunkSize)];
    }

    static int GetSamplesPerCell(LightQualityPreset preset)
    {
        return preset switch
        {
            LightQualityPreset.Smooth2x => 2,
            LightQualityPreset.Smooth4x => 4,
            _ => 1
        };
    }

    FilterMode GetActiveTextureFilter()
    {
        return activeQualityPreset == LightQualityPreset.LegacyCell
            ? textureFilter
            : FilterMode.Bilinear;
    }

    bool ConfigurationRequiresReinitialization()
    {
        return activeChunkSize != Mathf.Max(8, chunkSize) ||
            activeQualityPreset != qualityPreset;
    }

    void ReleaseLightingData()
    {
        if (lightTexture != null)
        {
            if (Application.isPlaying)
                Destroy(lightTexture);
            else
                DestroyImmediate(lightTexture);
        }

        lightTexture = null;
        chunks = null;
        visitStamps = null;
        distances = null;
        propagationQueue = null;
        chunksX = 0;
        chunksY = 0;
        initialized = false;
        rebuildRequested = true;
        previousDynamicChunks.Clear();
    }

    void OnValidate()
    {
        chunkSize = Mathf.Clamp(chunkSize, 8, 128);
        dynamicUpdateInterval = Mathf.Max(0.02f, dynamicUpdateInterval);
        debugMaximumCells = Mathf.Max(16, debugMaximumCells);
        if (lightTexture != null)
            lightTexture.filterMode = GetActiveTextureFilter();
        if (isActiveAndEnabled)
            rebuildRequested = true;
    }

    void OnDrawGizmosSelected()
    {
        if (grid == null)
            grid = GetComponent<TileGridGenerator>();
        if (grid == null)
            return;

        Vector2 step = grid.GridGenerationDirection;
        Vector3 cellSize = new(
            Mathf.Max(0.02f, Mathf.Abs(step.x) * 0.88f),
            Mathf.Max(0.02f, Mathf.Abs(step.y) * 0.88f),
            0.02f);

        if (drawLightCells && initialized)
        {
            int totalCells = grid.GridWidth * grid.GridHeight;
            int stride = Mathf.Max(
                1,
                Mathf.CeilToInt(Mathf.Sqrt(totalCells / (float)debugMaximumCells)));
            for (int y = 0; y < grid.GridHeight; y += stride)
            for (int x = 0; x < grid.GridWidth; x += stride)
            {
                Color light = GetCellLight(x, y);
                float brightness = Mathf.Max(light.r, Mathf.Max(light.g, light.b));
                if (brightness < debugMinimumBrightness)
                    continue;

                light.a = 0.35f;
                Gizmos.color = light;
                Vector3 position = grid.GetCellWorldPosition(x, y);
                position.z -= 0.55f;
                Gizmos.DrawCube(position, cellSize * stride);
            }
        }

        if (!drawChunkBounds)
            return;

        int debugChunksX = initialized
            ? chunksX
            : Mathf.CeilToInt(grid.GridWidth / (float)Mathf.Max(8, chunkSize));
        int debugChunksY = initialized
            ? chunksY
            : Mathf.CeilToInt(grid.GridHeight / (float)Mathf.Max(8, chunkSize));
        int debugChunkSize = initialized ? activeChunkSize : Mathf.Max(8, chunkSize);
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.75f);
        for (int chunkY = 0; chunkY < debugChunksY; chunkY++)
        for (int chunkX = 0; chunkX < debugChunksX; chunkX++)
        {
            int minX = chunkX * debugChunkSize;
            int minY = chunkY * debugChunkSize;
            int width = Mathf.Min(debugChunkSize, grid.GridWidth - minX);
            int height = Mathf.Min(debugChunkSize, grid.GridHeight - minY);
            Vector3 first = grid.GetCellWorldPosition(minX, minY);
            Vector3 last = grid.GetCellWorldPosition(minX + width - 1, minY + height - 1);
            Vector3 center = (first + last) * 0.5f;
            Vector3 size = new(
                Mathf.Abs(step.x) * width,
                Mathf.Abs(step.y) * height,
                0.05f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
