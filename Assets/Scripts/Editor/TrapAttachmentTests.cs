#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TrapAttachmentTests
{
    [TestCase(TrapAttachmentSurface.Floor, 0, 1)]
    [TestCase(TrapAttachmentSurface.Ceiling, 0, -1)]
    [TestCase(TrapAttachmentSurface.LeftWall, -1, 0)]
    [TestCase(TrapAttachmentSurface.RightWall, 1, 0)]
    public void SurfaceMapsToExpectedExternalServiceCell(
        TrapAttachmentSurface surface,
        int expectedX,
        int expectedY)
    {
        Assert.That(
            TrapAttachmentDefinition.GetServiceOffset(surface),
            Is.EqualTo(new Vector2Int(expectedX, expectedY)));
    }

    [Test]
    public void SpikeWallAuthorsEveryAttachmentSurface()
    {
        GameObject prefab = Resources.Load<GameObject>("Traps/SpikeWall");
        Assert.That(prefab, Is.Not.Null);
        TrapAttachmentDefinition definition =
            prefab.GetComponent<TrapAttachmentDefinition>();
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.PreferredSurface,
            Is.EqualTo(TrapAttachmentSurface.Floor));
        Assert.That(definition.Allows(TrapAttachmentSurface.Floor), Is.True);
        Assert.That(definition.Allows(TrapAttachmentSurface.Ceiling), Is.True);
        Assert.That(definition.Allows(TrapAttachmentSurface.LeftWall), Is.True);
        Assert.That(definition.Allows(TrapAttachmentSurface.RightWall), Is.True);
    }

    [Test]
    public void FootprintOffsetsRotateFromServiceTowardTarget()
    {
        var root = new GameObject("Trap Footprint Test");
        try
        {
            TrapAttachmentDefinition definition =
                root.AddComponent<TrapAttachmentDefinition>();
            var serialized = new SerializedObject(definition);
            SetSingleOffset(
                serialized, "additionalMechanismCells", new Vector2Int(1, 0));
            SetSingleOffset(
                serialized, "infrastructureCells", new Vector2Int(0, -1));
            SetSingleOffset(
                serialized, "additionalHazardCells", new Vector2Int(0, 1));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            TrapAttachmentPlacement placement = definition.ResolvePlacement(
                TrapAttachmentSurface.Ceiling,
                new Vector2Int(5, 5),
                new Vector2Int(5, 4));

            Assert.That(placement.MechanismCells,
                Does.Contain(new Vector2Int(4, 5)));
            Assert.That(placement.InfrastructureCells,
                Does.Contain(new Vector2Int(5, 6)));
            Assert.That(placement.HazardCells,
                Does.Contain(new Vector2Int(5, 3)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void DefaultFootprintSeparatesMechanismAndHazardCells()
    {
        var root = new GameObject("Default Trap Footprint Test");
        try
        {
            TrapAttachmentDefinition definition =
                root.AddComponent<TrapAttachmentDefinition>();
            TrapAttachmentPlacement placement = definition.ResolvePlacement(
                TrapAttachmentSurface.Floor,
                new Vector2Int(3, 4),
                new Vector2Int(3, 3));

            Assert.That(placement.MechanismCells,
                Is.EquivalentTo(new[] { new Vector2Int(3, 4) }));
            Assert.That(placement.InfrastructureCells, Is.Empty);
            Assert.That(placement.HazardCells,
                Is.EquivalentTo(new[] { new Vector2Int(3, 3) }));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ReservedCellsExcludeHazardVolume()
    {
        var placement = new TrapAttachmentPlacement(
            TrapAttachmentSurface.LeftWall,
            new Vector2Int(1, 2),
            new Vector2Int(2, 2),
            new[] { new Vector2Int(1, 2), new Vector2Int(1, 3) },
            new[] { new Vector2Int(0, 2) },
            new[] { new Vector2Int(2, 2), new Vector2Int(3, 2) });

        Assert.That(placement.ReservedCells, Is.EquivalentTo(new[]
        {
            new Vector2Int(1, 2),
            new Vector2Int(1, 3),
            new Vector2Int(0, 2)
        }));
        Assert.That(System.Linq.Enumerable.Contains(
            placement.ReservedCells, new Vector2Int(2, 2)), Is.False);
    }

    static void SetSingleOffset(
        SerializedObject serialized,
        string propertyName,
        Vector2Int offset)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        property.arraySize = 1;
        property.GetArrayElementAtIndex(0).vector2IntValue = offset;
    }
}
#endif
