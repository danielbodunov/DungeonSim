#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
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
}
#endif
