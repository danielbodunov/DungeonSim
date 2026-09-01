#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CameraNavigationBoundsTests
{
    [Test]
    public void ClampUsesViewportFootprintOnBothAxes()
    {
        Rect board = Rect.MinMaxRect(10f, 20f, 30f, 40f);
        Vector2 minimumOffset = new(-5f, -4f);
        Vector2 maximumOffset = new(5f, 4f);

        Vector2 below = CameraFollow.CalculateClampedNavigationCenter(
            new Vector2(-100f, -100f), board,
            minimumOffset, maximumOffset, Vector2.zero, true, true);
        Vector2 above = CameraFollow.CalculateClampedNavigationCenter(
            new Vector2(100f, 100f), board,
            minimumOffset, maximumOffset, Vector2.zero, true, true);

        Assert.That(below.x, Is.EqualTo(15f));
        Assert.That(below.y, Is.EqualTo(24f));
        Assert.That(above.x, Is.EqualTo(25f));
        Assert.That(above.y, Is.EqualTo(36f));
    }

    [Test]
    public void OversizedViewportUsesOneStableBoardCenter()
    {
        Rect board = Rect.MinMaxRect(20f, -30f, 30f, -20f);
        Vector2 minimumOffset = new(-8f, -7f);
        Vector2 maximumOffset = new(8f, 7f);

        Vector2 fromMinimum = CameraFollow.CalculateClampedNavigationCenter(
            new Vector2(-100f, -100f), board,
            minimumOffset, maximumOffset, Vector2.zero, true, true);
        Vector2 fromMaximum = CameraFollow.CalculateClampedNavigationCenter(
            new Vector2(100f, 100f), board,
            minimumOffset, maximumOffset, Vector2.zero, true, true);

        Assert.That(fromMinimum, Is.EqualTo(new Vector2(25f, -25f)));
        Assert.That(fromMaximum, Is.EqualTo(fromMinimum));
    }

    [Test]
    public void EdgeMarginExtendsLimitsAndDisabledAxisRemainsUnchanged()
    {
        Rect board = Rect.MinMaxRect(0f, 0f, 10f, 10f);

        Vector2 result = CameraFollow.CalculateClampedNavigationCenter(
            new Vector2(-100f, 42f), board,
            new Vector2(-2f, -2f), new Vector2(2f, 2f),
            new Vector2(1f, 3f), true, false);

        Assert.That(result.x, Is.EqualTo(1f));
        Assert.That(result.y, Is.EqualTo(42f));
    }

    [Test]
    public void GridPlayableRectUsesInteriorCellsAndGridOrigin()
    {
        var gridObject = new GameObject("Navigation Bounds Test Grid");
        try
        {
            TileGridGenerator grid = gridObject.AddComponent<TileGridGenerator>();
            SetGridField(grid, "width", 6);
            SetGridField(grid, "height", 5);
            SetGridField(grid, "origin", new Vector2(10f, 20f));
            SetGridField(grid, "generationDirection", new Vector2(-2f, 3f));

            Assert.That(grid.TryGetPlayableWorldRect(out Rect rect), Is.True);
            Assert.That(rect.xMin, Is.EqualTo(0.5f));
            Assert.That(rect.xMax, Is.EqualTo(8.5f));
            Assert.That(rect.yMin, Is.EqualTo(21f));
            Assert.That(rect.yMax, Is.EqualTo(30f));
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
        }
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
