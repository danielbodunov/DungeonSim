#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class GeneratedBuildObstacleTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void ExplicitFootprintsSupportOneThroughFourCells(int cellCount)
    {
        var definition = new GeneratedBuildObstacleDefinition
        {
            definitionId = $"Test_{cellCount}",
            footprintOffsets = new List<Vector2Int>()
        };
        for (int i = 0; i < cellCount; i++)
            definition.footprintOffsets.Add(new Vector2Int(i, 0));

        Assert.That(definition.IsValid(out string failure), Is.True, failure);
        Assert.That(definition.ResolveFootprint(new Vector2Int(4, 5), 0),
            Has.Count.EqualTo(cellCount));
    }

    [Test]
    public void NonRectangularFootprintRotatesAroundAnchorWithoutChangingCount()
    {
        var definition = new GeneratedBuildObstacleDefinition
        {
            definitionId = "L3",
            footprintOffsets = new List<Vector2Int>
            {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.up
            }
        };

        IReadOnlyList<Vector2Int> rotated = definition.ResolveFootprint(
            new Vector2Int(10, 10), 1);

        Assert.That(rotated, Is.EquivalentTo(new[]
        {
            new Vector2Int(10, 10),
            new Vector2Int(10, 11),
            new Vector2Int(9, 10)
        }));
    }

    [Test]
    public void VisualVariantDoesNotChangeLogicalFootprint()
    {
        var definition = new GeneratedBuildObstacleDefinition
        {
            definitionId = "SharedShape",
            footprintOffsets = new List<Vector2Int>
            {
                Vector2Int.zero,
                Vector2Int.right
            }
        };

        var boulder = new GeneratedBuildObstacleInstance(
            definition, new Vector2Int(3, 4), 2, "Boulder");
        var relic = new GeneratedBuildObstacleInstance(
            definition, new Vector2Int(3, 4), 2, "Relic");

        Assert.That(boulder.FootprintCells, Is.EqualTo(relic.FootprintCells));
        Assert.That(boulder.VariantId, Is.Not.EqualTo(relic.VariantId));
    }

    [Test]
    public void SavedRecordCopyPreservesExactIdentityAndPose()
    {
        var source = new SavedGeneratedBuildObstacle
        {
            definitionId = "Formation_L3",
            anchorX = 7,
            anchorY = 9,
            rotation = 3,
            variantId = "Ore"
        };

        SavedGeneratedBuildObstacle copy = source.Copy();

        Assert.That(copy, Is.Not.SameAs(source));
        Assert.That(copy.definitionId, Is.EqualTo(source.definitionId));
        Assert.That(copy.anchorX, Is.EqualTo(source.anchorX));
        Assert.That(copy.anchorY, Is.EqualTo(source.anchorY));
        Assert.That(copy.rotation, Is.EqualTo(source.rotation));
        Assert.That(copy.variantId, Is.EqualTo(source.variantId));
        Assert.That(DungeonSaveData.CurrentVersion, Is.GreaterThanOrEqualTo(15));
    }
}
#endif
