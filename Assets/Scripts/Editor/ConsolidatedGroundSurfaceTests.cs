#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ConsolidatedGroundSurfaceTests
{
    [Test]
    public void QuadBuilderCombinesCellsIntoOneMesh()
    {
        var mesh = new Mesh();
        try
        {
            var translations = new List<Vector3>
            {
                Vector3.zero,
                new(2f, 0f, 0f)
            };
            Vector3[] corners =
            {
                new(-0.5f, -0.5f, 0f),
                new(-0.5f, 0.5f, 0f),
                new(0.5f, 0.5f, 0f),
                new(0.5f, -0.5f, 0f)
            };

            DungeonConsolidatedGroundSurface.PopulateQuadMesh(
                mesh, translations, corners, Vector3.back, true);

            Assert.That(mesh.vertexCount, Is.EqualTo(8));
            Assert.That(mesh.triangles.Length / 3, Is.EqualTo(4));
            Assert.That(mesh.bounds.min.x, Is.EqualTo(-0.5f).Within(0.0001f));
            Assert.That(mesh.bounds.max.x, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(mesh.normals, Has.All.EqualTo(Vector3.back));
            Assert.That(mesh.colors, Has.All.EqualTo(Color.white));
            Assert.That(mesh.uv, Has.Length.EqualTo(8));
        }
        finally
        {
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void RebuildKeepsGeometryAtWorldCellWhenHierarchyIsTranslated()
    {
        var gridRoot = new GameObject("Translated Consolidated Ground Test");
        var template = new GameObject("Translated Ground Template");
        var sourceMesh = new Mesh { name = "Translated Ground Source" };
        try
        {
            TileGridGenerator grid = gridRoot.AddComponent<TileGridGenerator>();
            var cells = new List<int>[1, 1];
            cells[0, 0] = new List<int> { 0 };
            SetField(grid, "width", 1);
            SetField(grid, "height", 1);
            SetField(grid, "origin", new Vector2(12f, -7f));
            SetField(grid, "generationDirection", new Vector2(1f, -1f));
            SetField(grid, "groundTileIndex", 0);
            SetField(grid, "cells", cells);
            SetField(grid, "instantiated", new GameObject[1, 1]);
            SetField(grid, "placed", new bool[1, 1]);

            var visualObject = new GameObject("Offset Visual");
            visualObject.transform.SetParent(template.transform, false);
            visualObject.transform.localPosition = new Vector3(2.5f, -1.5f, 0.75f);
            MeshFilter sourceFilter = visualObject.AddComponent<MeshFilter>();
            visualObject.AddComponent<MeshRenderer>();
            sourceMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f)
            };
            sourceMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            sourceFilter.sharedMesh = sourceMesh;
            template.AddComponent<BoxCollider>().size = Vector3.one;

            DungeonConsolidatedGroundSurface surface =
                gridRoot.AddComponent<DungeonConsolidatedGroundSurface>();
            surface.Initialize(grid, template, createCollider: true);
            Transform generatedRoot = gridRoot.transform.Find(
                "Consolidated Ground Surface");
            Assert.That(generatedRoot, Is.Not.Null);

            generatedRoot.position = new Vector3(5f, 6f, 7f);
            surface.RebuildNow();

            Vector3 cellCenter = grid.GetCellWorldPosition(0, 0);
            MeshFilter generatedVisual =
                generatedRoot.GetComponentInChildren<MeshFilter>(true);
            MeshCollider generatedCollider =
                generatedRoot.GetComponent<MeshCollider>();
            Assert.That(surface.VisibleCellCount, Is.EqualTo(1));
            Assert.That(generatedVisual, Is.Not.Null);
            Assert.That(generatedCollider, Is.Not.Null);

            AssertWorldPosition(
                GetWorldVertexCenter(
                    generatedVisual.sharedMesh, generatedVisual.transform),
                cellCenter);
            AssertWorldPosition(
                GetWorldVertexCenter(
                    generatedCollider.sharedMesh, generatedCollider.transform),
                cellCenter + Vector3.up * 0.5f);
        }
        finally
        {
            Object.DestroyImmediate(gridRoot);
            Object.DestroyImmediate(template);
            Object.DestroyImmediate(sourceMesh);
        }
    }

    [Test]
    public void GridGroundVisibilityUsesOccupancyAndReferenceCountedSuppression()
    {
        var root = new GameObject("Consolidated Ground Visibility Test");
        try
        {
            TileGridGenerator grid = root.AddComponent<TileGridGenerator>();
            DungeonConsolidatedGroundSurface surface =
                root.AddComponent<DungeonConsolidatedGroundSurface>();
            var cells = new List<int>[3, 3];
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                cells[x, y] = new List<int> { 0 };
            var placed = new bool[3, 3];

            SetField(grid, "width", 3);
            SetField(grid, "height", 3);
            SetField(grid, "groundTileIndex", 0);
            SetField(grid, "cells", cells);
            SetField(grid, "instantiated", new GameObject[3, 3]);
            SetField(grid, "placed", placed);
            SetField(grid, "consolidatedGroundSurface", surface);

            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.True);

            placed[1, 1] = true;
            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.False);
            placed[1, 1] = false;

            cells[1, 1] = new List<int> { 1 };
            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.False);
            cells[1, 1] = new List<int> { 0, 1 };
            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.True);

            var cell = new Vector2Int(1, 1);
            Assert.That(grid.SetOrdinaryGroundSuppressed(cell, true), Is.True);
            Assert.That(grid.SetOrdinaryGroundSuppressed(cell, true), Is.True);
            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.False);
            grid.SetOrdinaryGroundSuppressed(cell, false);
            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.False);
            grid.SetOrdinaryGroundSuppressed(cell, false);
            Assert.That(grid.ShouldRenderOrdinaryGround(1, 1), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PointerSelectionIntersectsConfiguredGridPlane()
    {
        var ray = new Ray(new Vector3(2f, 3f, 10f), Vector3.back);

        bool hit = InputManager.TryGetMapPlanePosition(
            ray, -2f, out Vector3 position);

        Assert.That(hit, Is.True);
        Assert.That(position, Is.EqualTo(new Vector3(2f, 3f, -2f)));
        Assert.That(InputManager.TryGetMapPlanePosition(
            new Ray(Vector3.zero, Vector3.right), 0f, out _), Is.False);
    }

    [Test]
    public void WorldMappingExcludesRightBorderAndCellsBeyondIt()
    {
        var root = new GameObject("Playable Boundary Test Grid");
        try
        {
            TileGridGenerator grid = root.AddComponent<TileGridGenerator>();
            SetField(grid, "width", 17);
            SetField(grid, "height", 100);
            SetField(grid, "origin", Vector2.zero);
            SetField(grid, "generationDirection", new Vector2(1f, -1f));

            Vector3 lastInterior = grid.GetCellWorldPosition(15, 50);
            Vector3 rightBorder = grid.GetCellWorldPosition(16, 50);
            Vector3 beyondRightBorder = grid.GetCellWorldPosition(17, 50);

            Assert.That(grid.TryWorldToPlayableCell(
                lastInterior, out Vector2Int interiorCell), Is.True);
            Assert.That(interiorCell, Is.EqualTo(new Vector2Int(15, 50)));

            Assert.That(grid.TryWorldToCell(
                rightBorder, out Vector2Int borderCell), Is.True);
            Assert.That(borderCell, Is.EqualTo(new Vector2Int(16, 50)));
            Assert.That(grid.TryWorldToPlayableCell(
                rightBorder, out _), Is.False);

            Assert.That(grid.TryWorldToCell(
                beyondRightBorder, out _), Is.False);
            Assert.That(grid.TryWorldToPlayableCell(
                beyondRightBorder, out _), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase(0, 50)]
    [TestCase(16, 50)]
    [TestCase(8, 0)]
    [TestCase(8, 99)]
    public void FixedOuterRingIsNeverPlayable(int x, int y)
    {
        var root = new GameObject("Playable Ring Test Grid");
        try
        {
            TileGridGenerator grid = root.AddComponent<TileGridGenerator>();
            SetField(grid, "width", 17);
            SetField(grid, "height", 100);

            Assert.That(grid.IsPlayableCell(x, y), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void SetField<T>(TileGridGenerator grid, string name, T value)
    {
        FieldInfo field = typeof(TileGridGenerator).GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing TileGridGenerator.{name}");
        field.SetValue(grid, value);
    }

    static Vector3 GetWorldVertexCenter(Mesh mesh, Transform transform)
    {
        Assert.That(mesh, Is.Not.Null);
        Vector3[] vertices = mesh.vertices;
        Assert.That(vertices, Is.Not.Empty);
        Vector3 total = Vector3.zero;
        for (int i = 0; i < vertices.Length; i++)
            total += transform.TransformPoint(vertices[i]);
        return total / vertices.Length;
    }

    static void AssertWorldPosition(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
    }
}
#endif
