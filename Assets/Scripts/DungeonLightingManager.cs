using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a low-resolution, world-space dungeon light field. Lighting data is
/// stored and refreshed in chunks, then uploaded to shared previous/current
/// textures for inexpensive, temporally interpolated dungeon-material sampling.
/// </summary>
[DisallowMultipleComponent]
public class DungeonLightingManager : MonoBehaviour
{
    public enum PresentationMode
    {
        ExpansionUniform,
        ExploringAtmospheric
    }

    public enum LightQualityPreset
    {
        LegacyCell,
        Smooth2x,
        Smooth4x
    }

    static readonly int LightTextureId = Shader.PropertyToID("_DungeonLightTexture");
    static readonly int PreviousLightTextureId =
        Shader.PropertyToID("_DungeonPreviousLightTexture");
    static readonly int LightTextureBlendId =
        Shader.PropertyToID("_DungeonLightTextureBlend");
    static readonly int VisiblePixelsPerCellId =
        Shader.PropertyToID("_DungeonLightingPixelsPerCell");
    static readonly int PropagationSamplesPerCellId =
        Shader.PropertyToID("_DungeonLightingPropagationSamplesPerCell");
    static readonly int GridCellZeroId = Shader.PropertyToID("_DungeonGridCellZero");
    static readonly int GridStepId = Shader.PropertyToID("_DungeonGridStep");
    static readonly int GridSizeId = Shader.PropertyToID("_DungeonGridSize");
    static readonly int AmbientColorId = Shader.PropertyToID("_DungeonAmbientColor");
    static readonly int LightingInitializedId =
        Shader.PropertyToID("_DungeonLightingInitialized");
    static readonly int PresentationBlendId =
        Shader.PropertyToID("_DungeonLightingModeBlend");

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
    float dynamicUpdateInterval = 0.05f;

    [Header("Presentation")]
    [SerializeField] PresentationMode presentationMode =
        PresentationMode.ExpansionUniform;
    [SerializeField, Min(0f)] float presentationTransitionDuration = 0.3f;
    [SerializeField, Min(1), Tooltip("Visible world-space lighting blocks per dungeon cell. This does not change propagation resolution.")]
    int visibleLightingPixelsPerCell = 2;

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
    Texture2D previousLightTexture;
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
    bool unsupportedHdrFormatLogged;
    float currentPresentationBlend;
    float presentationBlendStart;
    float targetPresentationBlend;
    float presentationTransitionElapsed;
    float lightTextureBlend = 1f;
    bool debugPresentationOverride;

    sealed class LightingChunk
    {
        public readonly RectInt Bounds;
        public readonly Color[] StaticLight;
        public readonly Color[] DynamicLight;
        public readonly Color[] UploadBuffer;

        public LightingChunk(RectInt bounds, int samplesPerCell)
        {
            Bounds = bounds;
            int length = bounds.width * bounds.height *
                samplesPerCell * samplesPerCell;
            StaticLight = new Color[length];
            DynamicLight = new Color[length];
            UploadBuffer = new Color[length];
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
    public Texture2D PreviousLightTexture => previousLightTexture;
    public int ChunkSize => initialized ? activeChunkSize : chunkSize;
    public int SamplesPerCell => initialized
        ? activeSamplesPerCell
        : GetSamplesPerCell(qualityPreset);
    public LightQualityPreset QualityPreset => qualityPreset;
    public Vector2Int ChunkCount => new(chunksX, chunksY);
    public PresentationMode CurrentPresentationMode => presentationMode;
    public float CurrentPresentationBlend => currentPresentationBlend;
    public bool DebugPresentationOverride => debugPresentationOverride;
    public int VisibleLightingPixelsPerCell => visibleLightingPixelsPerCell;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetPresentationGlobals()
    {
        Shader.SetGlobalFloat(PresentationBlendId, 0f);
        Shader.SetGlobalFloat(LightTextureBlendId, 1f);
        Shader.SetGlobalFloat(VisiblePixelsPerCellId, 2f);
        Shader.SetGlobalFloat(PropagationSamplesPerCellId, 1f);
    }

    void Awake()
    {
        if (grid == null)
            grid = GetComponent<TileGridGenerator>();
        ApplyPresentationMode(true);
    }

    void OnEnable()
    {
        Shader.SetGlobalFloat(LightingInitializedId, 0f);
        ApplyPresentationMode(true);
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
        Shader.SetGlobalFloat(LightingInitializedId, 0f);
        Shader.SetGlobalFloat(PresentationBlendId, 0f);
        Shader.SetGlobalFloat(LightTextureBlendId, 1f);
        Shader.SetGlobalFloat(PropagationSamplesPerCellId, 1f);
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
        UpdatePresentationBlend();
        UpdateLightTextureBlend();

        if (initialized && ConfigurationRequiresReinitialization())
            ReleaseLightingData();

        if (!initialized && !TryInitialize())
            return;

        Shader.SetGlobalFloat(LightingInitializedId, 1f);
        Shader.SetGlobalColor(AmbientColorId, ambientColor);
        Shader.SetGlobalFloat(
            VisiblePixelsPerCellId, visibleLightingPixelsPerCell);
        Shader.SetGlobalFloat(
            PropagationSamplesPerCellId, activeSamplesPerCell);

        if (rebuildRequested)
        {
            RebuildLighting();
            return;
        }

        dynamicTimer += Time.unscaledDeltaTime;
        if (dynamicTimer >= dynamicUpdateInterval)
        {
            dynamicTimer = Mathf.Max(0f, dynamicTimer - dynamicUpdateInterval);
            RefreshDynamicLighting();
        }
    }

    public void RequestFullRebuild()
    {
        rebuildRequested = true;
    }

    public void SetPresentationMode(PresentationMode mode, bool immediate = false)
    {
        presentationMode = mode;
        ApplyPresentationMode(immediate);
    }

    public void SetDebugOverride(bool enabled, bool immediate = false)
    {
        if (debugPresentationOverride == enabled)
            return;
        debugPresentationOverride = enabled;
        ApplyPresentationMode(immediate);
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
        SynchronizeLightTextures();
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
            total += chunk.StaticLight[localIndex] + chunk.DynamicLight[localIndex];
        }

        return total / (activeSamplesPerCell * activeSamplesPerCell);
    }

    bool TryInitialize()
    {
        if (initialized)
            return true;
        if (grid == null || !grid.IsInitialized || grid.GridWidth <= 0 || grid.GridHeight <= 0)
            return false;
        TextureFormat lightTextureFormat = ResolveHdrTextureFormat();
        if (lightTextureFormat == TextureFormat.RGBA32)
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
            lightTextureFormat,
            false,
            true)
        {
            name = $"Dungeon Light Grid {grid.GridWidth}x{grid.GridHeight} " +
                $"({activeSamplesPerCell}x samples)",
            filterMode = GetActiveTextureFilter(),
            wrapMode = TextureWrapMode.Clamp
        };
        previousLightTexture = new Texture2D(
            grid.GridWidth * activeSamplesPerCell,
            grid.GridHeight * activeSamplesPerCell,
            lightTextureFormat,
            false,
            true)
        {
            name = $"Previous Dungeon Light Grid {grid.GridWidth}x{grid.GridHeight} " +
                $"({activeSamplesPerCell}x samples)",
            filterMode = GetActiveTextureFilter(),
            wrapMode = TextureWrapMode.Clamp
        };

        Vector3 firstCell = grid.GetCellWorldPosition(0, 0);
        Vector2 step = grid.GridGenerationDirection;
        Shader.SetGlobalTexture(LightTextureId, lightTexture);
        Shader.SetGlobalTexture(PreviousLightTextureId, previousLightTexture);
        Shader.SetGlobalVector(GridCellZeroId, new Vector4(firstCell.x, firstCell.y, 0f, 0f));
        Shader.SetGlobalVector(GridStepId, new Vector4(step.x, step.y, 0f, 0f));
        Shader.SetGlobalVector(GridSizeId, new Vector4(grid.GridWidth, grid.GridHeight, 0f, 0f));
        Shader.SetGlobalColor(AmbientColorId, ambientColor);
        Shader.SetGlobalFloat(VisiblePixelsPerCellId, visibleLightingPixelsPerCell);
        Shader.SetGlobalFloat(PropagationSamplesPerCellId, activeSamplesPerCell);
        Shader.SetGlobalFloat(LightTextureBlendId, 1f);
        Shader.SetGlobalFloat(LightingInitializedId, 1f);

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
        Graphics.CopyTexture(lightTexture, previousLightTexture);
        UploadChunks(chunksToRefresh);
        lightTextureBlend = 0f;
        Shader.SetGlobalFloat(LightTextureBlendId, lightTextureBlend);

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
        float sampleTime = Time.unscaledTime;
        Color currentColor = source.GetCurrentColor(sampleTime);
        float currentIntensity = source.GetCurrentIntensity(sampleTime);
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
                currentColor,
                currentIntensity,
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
        Color currentColor,
        float currentIntensity,
        bool dynamicLayer,
        HashSet<int> touchedChunks)
    {
        int chunkIndex = GetChunkIndex(x / activeChunkSize, y / activeChunkSize);
        LightingChunk chunk = chunks[chunkIndex];

        if (activeQualityPreset == LightQualityPreset.LegacyCell)
        {
            float falloff = CalculateFalloff(graphDistance, source);
            Color contribution = currentColor * (currentIntensity * falloff);
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
            float falloff = CalculateFalloff(effectiveDistance, source);
            Color contribution = currentColor * (currentIntensity * falloff);
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

    static Color AddColor(Color current, Color contribution)
    {
        return new Color(
            Mathf.Max(0f, current.r + contribution.r),
            Mathf.Max(0f, current.g + contribution.g),
            Mathf.Max(0f, current.b + contribution.b),
            1f);
    }

    static Color AddColors(Color first, Color second)
    {
        return new Color(
            Mathf.Max(0f, first.r + second.r),
            Mathf.Max(0f, first.g + second.g),
            Mathf.Max(0f, first.b + second.b),
            1f);
    }

    static float CalculateFalloff(float distance, DungeonLightSource source)
    {
        return source.EvaluateSpatialFalloff(distance);
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
                chunk.UploadBuffer[i] = AddColors(
                    chunk.StaticLight[i], chunk.DynamicLight[i]);
            }

            lightTexture.SetPixels(
                chunk.Bounds.xMin * activeSamplesPerCell,
                chunk.Bounds.yMin * activeSamplesPerCell,
                chunk.Bounds.width * activeSamplesPerCell,
                chunk.Bounds.height * activeSamplesPerCell,
                chunk.UploadBuffer);
        }

        lightTexture.Apply(false, false);
    }

    void SynchronizeLightTextures()
    {
        if (lightTexture == null || previousLightTexture == null)
            return;

        Graphics.CopyTexture(lightTexture, previousLightTexture);
        lightTextureBlend = 1f;
        Shader.SetGlobalFloat(LightTextureBlendId, lightTextureBlend);
    }

    void UpdateLightTextureBlend()
    {
        if (lightTextureBlend >= 1f)
            return;

        lightTextureBlend = dynamicUpdateInterval <= 0f
            ? 1f
            : Mathf.Clamp01(
                lightTextureBlend + Time.unscaledDeltaTime / dynamicUpdateInterval);
        Shader.SetGlobalFloat(LightTextureBlendId, lightTextureBlend);
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

    TextureFormat ResolveHdrTextureFormat()
    {
        if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
            return TextureFormat.RGBAHalf;
        if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
            return TextureFormat.RGBAFloat;

        if (!unsupportedHdrFormatLogged)
        {
            Debug.LogError(
                "Dungeon lighting requires RGBAHalf or RGBAFloat texture support " +
                "to preserve HDR propagated values.",
                this);
            unsupportedHdrFormatLogged = true;
        }
        return TextureFormat.RGBA32;
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

        if (previousLightTexture != null)
        {
            if (Application.isPlaying)
                Destroy(previousLightTexture);
            else
                DestroyImmediate(previousLightTexture);
        }

        lightTexture = null;
        previousLightTexture = null;
        Shader.SetGlobalFloat(LightingInitializedId, 0f);
        Shader.SetGlobalFloat(LightTextureBlendId, 1f);
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
        presentationTransitionDuration = Mathf.Max(0f, presentationTransitionDuration);
        visibleLightingPixelsPerCell = Mathf.Max(1, visibleLightingPixelsPerCell);
        debugMaximumCells = Mathf.Max(16, debugMaximumCells);
        if (lightTexture != null)
            lightTexture.filterMode = GetActiveTextureFilter();
        if (previousLightTexture != null)
            previousLightTexture.filterMode = GetActiveTextureFilter();
        if (isActiveAndEnabled)
        {
            rebuildRequested = true;
            if (Application.isPlaying)
                ApplyPresentationMode(false);
        }
    }

    void ApplyPresentationMode(bool immediate)
    {
        float resolved = !debugPresentationOverride &&
            presentationMode == PresentationMode.ExploringAtmospheric
            ? 1f
            : 0f;
        if (immediate || presentationTransitionDuration <= 0f)
        {
            targetPresentationBlend = resolved;
            presentationBlendStart = resolved;
            presentationTransitionElapsed = presentationTransitionDuration;
            ApplyPresentationBlend(resolved);
            return;
        }

        if (Mathf.Approximately(targetPresentationBlend, resolved))
            return;
        presentationBlendStart = currentPresentationBlend;
        targetPresentationBlend = resolved;
        presentationTransitionElapsed = 0f;
    }

    void UpdatePresentationBlend()
    {
        if (Mathf.Approximately(currentPresentationBlend, targetPresentationBlend))
            return;
        if (presentationTransitionDuration <= 0f)
        {
            ApplyPresentationBlend(targetPresentationBlend);
            return;
        }

        presentationTransitionElapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(
            presentationTransitionElapsed / presentationTransitionDuration);
        ApplyPresentationBlend(Mathf.Lerp(
            presentationBlendStart, targetPresentationBlend, progress));
    }

    void ApplyPresentationBlend(float value)
    {
        currentPresentationBlend = Mathf.Clamp01(value);
        Shader.SetGlobalFloat(PresentationBlendId, currentPresentationBlend);
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
