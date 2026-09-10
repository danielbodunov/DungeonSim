using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decorative ground outside the authoritative grid. This surface owns only
/// presentation: it creates no cells, colliders, navigation, or occupancy.
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonExteriorGroundSurface : MonoBehaviour
{
    readonly List<Vector3> translations = new();
    readonly List<Vector3> worldCenters = new();

    TileGridGenerator grid;
    GameObject surfaceRoot;
    MeshFilter visualFilter;
    MeshRenderer visualRenderer;
    DungeonGroundSurfaceAppearance appearance;
    Mesh visualMesh;
    Vector3[] visualCorners;
    int leftPadding;
    int rightPadding;
    int topPadding;
    int bottomPadding;

    public int VisibleCellCount { get; private set; }
    public int RendererCount => visualRenderer != null ? 1 : 0;
    public int ColliderCount
    {
        get
        {
            if (surfaceRoot == null)
                return 0;
            int count = 0;
            Collider[] colliders =
                surfaceRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i].enabled)
                    count++;
            return count;
        }
    }
    public int VisualVertexCount => visualMesh != null
        ? visualMesh.vertexCount
        : 0;
    public int VisualTriangleCount => visualMesh != null
        ? visualMesh.triangles.Length / 3
        : 0;
    public Material SurfaceMaterial => visualRenderer != null
        ? visualRenderer.sharedMaterial
        : null;
    public DungeonGroundSurfaceFamily SurfaceFamily => appearance != null
        ? appearance.Family
        : null;

    public void Initialize(
        TileGridGenerator owner,
        GameObject groundTemplate,
        int left,
        int right,
        int top,
        int bottom,
        Material materialOverride,
        DungeonGroundSurfaceFamily familyOverride)
    {
        ReleaseRuntimeObjects();

        grid = owner;
        leftPadding = Mathf.Max(0, left);
        rightPadding = Mathf.Max(0, right);
        topPadding = Mathf.Max(0, top);
        bottomPadding = Mathf.Max(0, bottom);
        if (grid == null || groundTemplate == null ||
            leftPadding + rightPadding + topPadding + bottomPadding <= 0)
        {
            return;
        }

        surfaceRoot = Instantiate(
            groundTemplate, Vector3.zero, Quaternion.identity, grid.transform);
        surfaceRoot.name = "Consolidated Exterior Ground Surface";
        visualFilter = surfaceRoot.GetComponentInChildren<MeshFilter>(true);
        visualRenderer = visualFilter != null
            ? visualFilter.GetComponent<MeshRenderer>()
            : null;
        if (visualFilter == null || visualRenderer == null ||
            visualFilter.sharedMesh == null)
        {
            Debug.LogError(
                "The exterior ground template needs one MeshFilter/" +
                "MeshRenderer pair with a source mesh.", groundTemplate);
            surfaceRoot.SetActive(false);
            return;
        }

        visualCorners = DungeonConsolidatedGroundSurface.CreateVisualCorners(
            visualFilter.sharedMesh.bounds);
        Collider[] colliders = surfaceRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
            Destroy(colliders[i]);
        }
        MeshRenderer[] renderers =
            surfaceRoot.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != visualRenderer)
                renderers[i].enabled = false;

        visualMesh = new Mesh
        {
            name = "Consolidated Exterior Ground",
            hideFlags = HideFlags.DontSave
        };
        visualFilter.sharedMesh = visualMesh;
        if (materialOverride != null)
            visualRenderer.sharedMaterial = materialOverride;
        appearance =
            surfaceRoot.GetComponentInChildren<DungeonGroundSurfaceAppearance>(true);
        if (familyOverride != null && appearance != null)
            appearance.SetFamily(familyOverride);
        else
            appearance?.Apply();

        RebuildNow();
    }

    public void RebuildNow()
    {
        if (grid == null || visualMesh == null || visualFilter == null)
            return;

        translations.Clear();
        worldCenters.Clear();
        AppendExteriorWorldCenters(
            grid,
            leftPadding,
            rightPadding,
            topPadding,
            bottomPadding,
            worldCenters);
        for (int i = 0; i < worldCenters.Count; i++)
        {
            translations.Add(
                visualFilter.transform.InverseTransformPoint(worldCenters[i]));
        }

        VisibleCellCount = translations.Count;
        DungeonConsolidatedGroundSurface.PopulateQuadMesh(
            visualMesh,
            translations,
            visualCorners,
            Vector3.back,
            includeRenderData: true);
        visualRenderer.enabled = VisibleCellCount > 0;
    }

    public static int AppendExteriorWorldCenters(
        TileGridGenerator owner,
        int left,
        int right,
        int top,
        int bottom,
        List<Vector3> destination)
    {
        if (owner == null || destination == null ||
            owner.GridWidth <= 0 || owner.GridHeight <= 0)
        {
            return 0;
        }

        left = Mathf.Max(0, left);
        right = Mathf.Max(0, right);
        top = Mathf.Max(0, top);
        bottom = Mathf.Max(0, bottom);
        bool xIncreasesRight = owner.GridGenerationDirection.x >= 0f;
        bool yIncreasesUp = owner.GridGenerationDirection.y >= 0f;
        int minimumX = xIncreasesRight ? -left : -right;
        int maximumX = owner.GridWidth - 1 +
            (xIncreasesRight ? right : left);
        int minimumY = yIncreasesUp ? -bottom : -top;
        int maximumY = owner.GridHeight - 1 +
            (yIncreasesUp ? top : bottom);
        int initialCount = destination.Count;

        for (int x = minimumX; x <= maximumX; x++)
        for (int y = minimumY; y <= maximumY; y++)
        {
            bool insideGrid = x >= 0 && x < owner.GridWidth &&
                y >= 0 && y < owner.GridHeight;
            if (!insideGrid)
                destination.Add(owner.GetCellWorldPosition(x, y));
        }

        return destination.Count - initialCount;
    }

    void OnDestroy()
    {
        ReleaseRuntimeMesh();
    }

    void ReleaseRuntimeObjects()
    {
        ReleaseRuntimeMesh();
        if (surfaceRoot != null)
        {
            surfaceRoot.SetActive(false);
            Destroy(surfaceRoot);
        }
        surfaceRoot = null;
        visualFilter = null;
        visualRenderer = null;
        appearance = null;
        visualCorners = null;
        VisibleCellCount = 0;
    }

    void ReleaseRuntimeMesh()
    {
        if (visualMesh != null)
            Destroy(visualMesh);
        visualMesh = null;
    }
}
