#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ExteriorGroundSurfaceTests
{
    [Test]
    public void PaddingBuildsOneOutOfBoundsRingWithoutChangingPlayableBounds()
    {
        var root = new GameObject("Exterior Ground Extent Test");
        var mesh = new Mesh();
        try
        {
            TileGridGenerator grid = root.AddComponent<TileGridGenerator>();
            SetGridField(grid, "width", 4);
            SetGridField(grid, "height", 3);
            SetGridField(grid, "origin", new Vector2(10f, 20f));
            SetGridField(grid, "generationDirection", new Vector2(-1f, 1f));
            Assert.That(grid.TryGetPlayableWorldRect(out Rect before), Is.True);

            var centers = new List<Vector3>();
            int count = DungeonExteriorGroundSurface.AppendExteriorWorldCenters(
                grid, left: 1, right: 2, top: 4, bottom: 3, centers);

            Assert.That(count, Is.EqualTo(58));
            Assert.That(centers, Has.Count.EqualTo(58));
            for (int i = 0; i < centers.Count; i++)
            {
                Assert.That(grid.TryWorldToCell(centers[i], out _), Is.False,
                    $"Exterior center {centers[i]} resolved inside the grid.");
            }
            Assert.That(Minimum(centers, value => value.x), Is.EqualTo(5.5f));
            Assert.That(Maximum(centers, value => value.x), Is.EqualTo(11.5f));
            Assert.That(Minimum(centers, value => value.y), Is.EqualTo(16.5f));
            Assert.That(Maximum(centers, value => value.y), Is.EqualTo(25.5f));
            Assert.That(grid.TryGetPlayableWorldRect(out Rect after), Is.True);
            Assert.That(after, Is.EqualTo(before));

            DungeonConsolidatedGroundSurface.PopulateQuadMesh(
                mesh,
                centers,
                new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f)
                },
                Vector3.back,
                includeRenderData: true);
            Assert.That(mesh.vertexCount, Is.EqualTo(58 * 4));
            Assert.That(mesh.triangles.Length / 3, Is.EqualTo(58 * 2));
        }
        finally
        {
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GroundAppearanceAcceptsDistinctExteriorFamily()
    {
        var root = new GameObject("Exterior Family Test");
        DungeonGroundSurfaceFamily family =
            ScriptableObject.CreateInstance<DungeonGroundSurfaceFamily>();
        try
        {
            DungeonGroundSurfaceAppearance appearance =
                root.AddComponent<DungeonGroundSurfaceAppearance>();

            appearance.SetFamily(family);

            Assert.That(appearance.Family, Is.SameAs(family));
        }
        finally
        {
            Object.DestroyImmediate(family);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void RuntimeSurfaceUsesOneRendererOverridesAndNoActiveCollider()
    {
        var gridRoot = new GameObject("Exterior Runtime Surface Test");
        var template = new GameObject("Exterior Runtime Template");
        var sourceMesh = new Mesh();
        Material baseMaterial = null;
        Material overrideMaterial = null;
        DungeonGroundSurfaceFamily family =
            ScriptableObject.CreateInstance<DungeonGroundSurfaceFamily>();
        try
        {
            TileGridGenerator grid = gridRoot.AddComponent<TileGridGenerator>();
            SetGridField(grid, "width", 3);
            SetGridField(grid, "height", 3);
            SetGridField(grid, "origin", Vector2.zero);
            SetGridField(grid, "generationDirection", new Vector2(1f, -1f));

            MeshFilter filter = template.AddComponent<MeshFilter>();
            MeshRenderer renderer = template.AddComponent<MeshRenderer>();
            template.AddComponent<BoxCollider>();
            template.AddComponent<DungeonGroundSurfaceAppearance>();
            sourceMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f)
            };
            sourceMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            filter.sharedMesh = sourceMesh;
            Shader shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);
            baseMaterial = new Material(shader);
            overrideMaterial = new Material(shader);
            renderer.sharedMaterial = baseMaterial;

            DungeonExteriorGroundSurface surface =
                gridRoot.AddComponent<DungeonExteriorGroundSurface>();
            surface.Initialize(
                grid,
                template,
                left: 1,
                right: 1,
                top: 1,
                bottom: 1,
                overrideMaterial,
                family);

            Assert.That(surface.VisibleCellCount, Is.EqualTo(16));
            Assert.That(surface.RendererCount, Is.EqualTo(1));
            Assert.That(surface.ColliderCount, Is.Zero);
            Assert.That(surface.VisualVertexCount, Is.EqualTo(16 * 4));
            Assert.That(surface.VisualTriangleCount, Is.EqualTo(16 * 2));
            Assert.That(surface.SurfaceMaterial, Is.SameAs(overrideMaterial));
            Assert.That(surface.SurfaceMaterial, Is.Not.SameAs(baseMaterial));
            Assert.That(surface.SurfaceFamily, Is.SameAs(family));
        }
        finally
        {
            Object.DestroyImmediate(gridRoot);
            Object.DestroyImmediate(template);
            Object.DestroyImmediate(sourceMesh);
            Object.DestroyImmediate(baseMaterial);
            Object.DestroyImmediate(overrideMaterial);
            Object.DestroyImmediate(family);
        }
    }

    static float Minimum(
        IReadOnlyList<Vector3> values,
        System.Func<Vector3, float> selector)
    {
        float result = float.PositiveInfinity;
        for (int i = 0; i < values.Count; i++)
            result = Mathf.Min(result, selector(values[i]));
        return result;
    }

    static float Maximum(
        IReadOnlyList<Vector3> values,
        System.Func<Vector3, float> selector)
    {
        float result = float.NegativeInfinity;
        for (int i = 0; i < values.Count; i++)
            result = Mathf.Max(result, selector(values[i]));
        return result;
    }

    static void SetGridField<T>(TileGridGenerator grid, string name, T value)
    {
        FieldInfo field = typeof(TileGridGenerator).GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing TileGridGenerator.{name}");
        field.SetValue(grid, value);
    }
}
#endif
