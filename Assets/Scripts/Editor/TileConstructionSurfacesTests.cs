using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TileConstructionSurfacesTests
{
    const string RepresentativeTilePath =
        "Assets/Resources/Dungeon/Narrow_Straight_I.prefab";

    [Test]
    public void RepresentativeTile_ExposesRequiredConstructionSurfaces()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RepresentativeTilePath);
        Assert.That(prefab, Is.Not.Null);

        TileConstructionSurfaces contract =
            prefab.GetComponent<TileConstructionSurfaces>();
        Assert.That(contract, Is.Not.Null);

        TileConstructionSurfaceKind[] required =
        {
            TileConstructionSurfaceKind.Floor,
            TileConstructionSurfaceKind.Ceiling,
            TileConstructionSurfaceKind.NorthWall,
            TileConstructionSurfaceKind.SouthWall,
            TileConstructionSurfaceKind.EastWall,
            TileConstructionSurfaceKind.WestWall,
            TileConstructionSurfaceKind.TrapServiceRegion
        };
        foreach (TileConstructionSurfaceKind kind in required)
            Assert.That(contract.Surfaces.Any(surface =>
                surface.Kind == kind && surface.Anchor != null), Is.True,
                $"Representative tile is missing {kind}.");
    }

    [TestCase(TrapAttachmentSurface.Floor)]
    [TestCase(TrapAttachmentSurface.Ceiling)]
    [TestCase(TrapAttachmentSurface.LeftWall)]
    [TestCase(TrapAttachmentSurface.RightWall)]
    public void RepresentativeTile_ExposesTrapCompatibleAnchor(
        TrapAttachmentSurface attachmentSurface)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            RepresentativeTilePath);
        TileConstructionSurfaces contract =
            prefab.GetComponent<TileConstructionSurfaces>();

        Assert.That(contract.TryGetTrapSurface(
            attachmentSurface, out TileConstructionSurfaceSlot surface),
            Is.True);
        Assert.That(surface.Anchor, Is.Not.Null);
    }
}
