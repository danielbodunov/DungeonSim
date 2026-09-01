using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Derived presentation for ordinary unbuilt ground. TileGridGenerator remains
/// authoritative for cell occupancy; this component only rebuilds meshes from
/// that state when invalidated.
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonConsolidatedGroundSurface : MonoBehaviour
{
    readonly List<Vector3> visualTranslations = new();
    readonly List<Vector3> collisionTranslations = new();

    TileGridGenerator grid;
    GameObject surfaceRoot;
    MeshFilter visualFilter;
    MeshRenderer visualRenderer;
    MeshCollider groundCollider;
    Mesh visualMesh;
    Mesh collisionMesh;
    Vector3[] visualCorners;
    Vector3[] collisionCorners;
    bool rebuildRequested;

    public int VisibleCellCount { get; private set; }
    public int VisualVertexCount => visualMesh != null ? visualMesh.vertexCount : 0;
    public int VisualTriangleCount => visualMesh != null
        ? visualMesh.triangles.Length / 3
        : 0;
    public int ColliderCount => groundCollider != null ? 1 : 0;
    public float LastRebuildMilliseconds { get; private set; }

    public void Initialize(
        TileGridGenerator owner,
        GameObject groundTemplate,
        bool createCollider)
    {
        ReleaseRuntimeObjects();

        grid = owner;
        if (grid == null || groundTemplate == null)
            return;

        surfaceRoot = Instantiate(
            groundTemplate, Vector3.zero, Quaternion.identity, grid.transform);
        surfaceRoot.name = "Consolidated Ground Surface";

        visualFilter = surfaceRoot.GetComponentInChildren<MeshFilter>(true);
        visualRenderer = visualFilter != null
            ? visualFilter.GetComponent<MeshRenderer>()
            : null;
        if (visualFilter == null || visualRenderer == null ||
            visualFilter.sharedMesh == null)
        {
            Debug.LogError(
                "The ordinary ground template needs one MeshFilter/MeshRenderer " +
                "pair with a source mesh.", groundTemplate);
            surfaceRoot.SetActive(false);
            return;
        }

        visualCorners = CreateVisualCorners(visualFilter.sharedMesh.bounds);
        collisionCorners = CreateCollisionCorners(surfaceRoot, visualFilter);

        Collider[] templateColliders =
            surfaceRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < templateColliders.Length; i++)
        {
            templateColliders[i].enabled = false;
            Destroy(templateColliders[i]);
        }

        MeshRenderer[] renderers =
            surfaceRoot.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != visualRenderer)
                renderers[i].enabled = false;

        visualMesh = new Mesh
        {
            name = "Consolidated Ordinary Ground",
            hideFlags = HideFlags.DontSave
        };
        visualMesh.MarkDynamic();
        visualFilter.sharedMesh = visualMesh;

        if (createCollider)
        {
            collisionMesh = new Mesh
            {
                name = "Consolidated Ordinary Ground Collision",
                hideFlags = HideFlags.DontSave
            };
            collisionMesh.MarkDynamic();
            groundCollider = surfaceRoot.AddComponent<MeshCollider>();
        }

        RequestRebuild();
    }

    public void RequestRebuild() => rebuildRequested = true;

    public void RebuildNow()
    {
        if (grid == null || visualMesh == null || visualFilter == null)
            return;

        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        rebuildRequested = false;
        visualTranslations.Clear();
        collisionTranslations.Clear();

        for (int x = 0; x < grid.GridWidth; x++)
        for (int y = 0; y < grid.GridHeight; y++)
        {
            if (!grid.ShouldRenderOrdinaryGround(x, y))
                continue;

            Vector3 worldCenter = grid.GetCellWorldPosition(x, y);
            visualTranslations.Add(
                visualFilter.transform.InverseTransformPoint(worldCenter));
            if (groundCollider != null)
            {
                collisionTranslations.Add(
                    surfaceRoot.transform.InverseTransformPoint(worldCenter));
            }
        }

        VisibleCellCount = visualTranslations.Count;
        PopulateQuadMesh(
            visualMesh,
            visualTranslations,
            visualCorners,
            Vector3.back,
            includeRenderData: true);
        visualRenderer.enabled = VisibleCellCount > 0;

        if (groundCollider != null && collisionMesh != null)
        {
            PopulateQuadMesh(
                collisionMesh,
                collisionTranslations,
                collisionCorners,
                Vector3.up,
                includeRenderData: false);
            groundCollider.sharedMesh = null;
            groundCollider.sharedMesh = collisionMesh;
            groundCollider.enabled = VisibleCellCount > 0;
        }

        long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - started;
        LastRebuildMilliseconds = (float)(elapsed * 1000.0 /
            System.Diagnostics.Stopwatch.Frequency);
    }

    public static void PopulateQuadMesh(
        Mesh mesh,
        IReadOnlyList<Vector3> translations,
        IReadOnlyList<Vector3> corners,
        Vector3 normal,
        bool includeRenderData)
    {
        if (mesh == null)
            return;
        int quadCount = translations?.Count ?? 0;
        if (corners == null || corners.Count != 4)
            quadCount = 0;

        var vertices = new Vector3[quadCount * 4];
        var triangles = new int[quadCount * 6];
        Vector3[] normals = includeRenderData
            ? new Vector3[vertices.Length]
            : null;
        Vector2[] uvs = includeRenderData
            ? new Vector2[vertices.Length]
            : null;
        Color[] colors = includeRenderData
            ? new Color[vertices.Length]
            : null;
        Vector2[] quadUvs =
        {
            new(0f, 0f),
            new(0f, 1f),
            new(1f, 1f),
            new(1f, 0f)
        };

        for (int quad = 0; quad < quadCount; quad++)
        {
            int vertexStart = quad * 4;
            int triangleStart = quad * 6;
            for (int corner = 0; corner < 4; corner++)
            {
                int vertex = vertexStart + corner;
                vertices[vertex] = translations[quad] + corners[corner];
                if (!includeRenderData)
                    continue;
                normals[vertex] = normal;
                uvs[vertex] = quadUvs[corner];
                colors[vertex] = Color.white;
            }

            triangles[triangleStart] = vertexStart;
            triangles[triangleStart + 1] = vertexStart + 1;
            triangles[triangleStart + 2] = vertexStart + 2;
            triangles[triangleStart + 3] = vertexStart;
            triangles[triangleStart + 4] = vertexStart + 2;
            triangles[triangleStart + 5] = vertexStart + 3;
        }

        mesh.Clear();
        mesh.indexFormat = vertices.Length > ushort.MaxValue
            ? IndexFormat.UInt32
            : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        if (includeRenderData)
        {
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors = colors;
        }
        mesh.RecalculateBounds();
    }

    void LateUpdate()
    {
        if (rebuildRequested)
            RebuildNow();
    }

    void OnDestroy()
    {
        ReleaseRuntimeMeshes();
    }

    static Vector3[] CreateVisualCorners(Bounds bounds)
    {
        float minimumX = bounds.size.x > 0.0001f ? bounds.min.x : -0.5f;
        float maximumX = bounds.size.x > 0.0001f ? bounds.max.x : 0.5f;
        float minimumY = bounds.size.y > 0.0001f ? bounds.min.y : -0.5f;
        float maximumY = bounds.size.y > 0.0001f ? bounds.max.y : 0.5f;
        float z = bounds.center.z;
        return new[]
        {
            new Vector3(minimumX, minimumY, z),
            new Vector3(minimumX, maximumY, z),
            new Vector3(maximumX, maximumY, z),
            new Vector3(maximumX, minimumY, z)
        };
    }

    static Vector3[] CreateCollisionCorners(
        GameObject root,
        MeshFilter visualSource)
    {
        BoxCollider source = root.GetComponentInChildren<BoxCollider>(true);
        if (source != null)
        {
            Vector3 half = source.size * 0.5f;
            Vector3 center = source.center;
            Vector3[] boxCorners =
            {
                center + new Vector3(-half.x, half.y, -half.z),
                center + new Vector3(-half.x, half.y, half.z),
                center + new Vector3(half.x, half.y, half.z),
                center + new Vector3(half.x, half.y, -half.z)
            };
            for (int i = 0; i < boxCorners.Length; i++)
            {
                boxCorners[i] = root.transform.InverseTransformPoint(
                    source.transform.TransformPoint(boxCorners[i]));
            }
            return boxCorners;
        }

        Bounds visualBounds = visualSource.sharedMesh.bounds;
        float minimumX = visualBounds.size.x > 0.0001f
            ? visualBounds.min.x
            : -0.5f;
        float maximumX = visualBounds.size.x > 0.0001f
            ? visualBounds.max.x
            : 0.5f;
        float topY = visualBounds.size.y > 0.0001f
            ? visualBounds.max.y
            : 0.5f;
        Vector3[] fallback =
        {
            new(minimumX, topY, -0.5f),
            new(minimumX, topY, 0.5f),
            new(maximumX, topY, 0.5f),
            new(maximumX, topY, -0.5f)
        };
        for (int i = 0; i < fallback.Length; i++)
        {
            fallback[i] = root.transform.InverseTransformPoint(
                visualSource.transform.TransformPoint(fallback[i]));
        }
        return fallback;
    }

    void ReleaseRuntimeObjects()
    {
        ReleaseRuntimeMeshes();
        if (surfaceRoot != null)
        {
            surfaceRoot.SetActive(false);
            Destroy(surfaceRoot);
        }
        surfaceRoot = null;
        visualFilter = null;
        visualRenderer = null;
        groundCollider = null;
    }

    void ReleaseRuntimeMeshes()
    {
        if (visualMesh != null)
            Destroy(visualMesh);
        if (collisionMesh != null)
            Destroy(collisionMesh);
        visualMesh = null;
        collisionMesh = null;
    }
}
