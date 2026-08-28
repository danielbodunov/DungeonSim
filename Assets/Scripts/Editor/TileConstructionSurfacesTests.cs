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

    [Test]
    public void UnknownVisualVariant_DoesNotMutateCurrentPresentation()
    {
        GameObject root = CreateSurfaceContract(
            TileConstructionModuleImpact.VisualOnly,
            out TileConstructionSurfaces contract,
            out GameObject defaultModule,
            out GameObject trapModule);
        try
        {
            Assert.That(contract.TrySelectVariant("Floor", "Missing"), Is.False);
            Assert.That(defaultModule.activeSelf, Is.True);
            Assert.That(trapModule.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TopologySensitiveVariant_DoesNotMutateCurrentPresentation()
    {
        GameObject root = CreateSurfaceContract(
            TileConstructionModuleImpact.RequiresTopologyResolution,
            out TileConstructionSurfaces contract,
            out GameObject defaultModule,
            out GameObject trapModule);
        try
        {
            Assert.That(contract.TrySelectVariant(
                "Floor", "TrapOpening"), Is.False);
            Assert.That(defaultModule.activeSelf, Is.True);
            Assert.That(trapModule.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static GameObject CreateSurfaceContract(
        TileConstructionModuleImpact impact,
        out TileConstructionSurfaces contract,
        out GameObject defaultModule,
        out GameObject trapModule)
    {
        var root = new GameObject("Construction Surface Test");
        defaultModule = new GameObject("Default");
        defaultModule.transform.SetParent(root.transform);
        trapModule = new GameObject("TrapOpening");
        trapModule.transform.SetParent(root.transform);
        trapModule.SetActive(false);
        contract = root.AddComponent<TileConstructionSurfaces>();

        var serialized = new SerializedObject(contract);
        SerializedProperty surfaces = serialized.FindProperty("surfaces");
        surfaces.arraySize = 1;
        SerializedProperty surface = surfaces.GetArrayElementAtIndex(0);
        surface.FindPropertyRelative("id").stringValue = "Floor";
        surface.FindPropertyRelative("kind").enumValueIndex =
            (int)TileConstructionSurfaceKind.Floor;
        surface.FindPropertyRelative("anchor").objectReferenceValue =
            root.transform;
        surface.FindPropertyRelative("moduleImpact").enumValueIndex =
            (int)impact;

        SerializedProperty variants =
            surface.FindPropertyRelative("variants");
        variants.arraySize = 2;
        SetVariant(variants.GetArrayElementAtIndex(0),
            "Default", defaultModule);
        SetVariant(variants.GetArrayElementAtIndex(1),
            "TrapOpening", trapModule);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    static void SetVariant(
        SerializedProperty variant,
        string id,
        GameObject moduleRoot)
    {
        variant.FindPropertyRelative("id").stringValue = id;
        variant.FindPropertyRelative("moduleRoot").objectReferenceValue =
            moduleRoot;
    }
}
