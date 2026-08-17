#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class AdventurerPhysicalResourceTests
{
    [Test]
    public void PrototypeLoadoutsProvideDifferentResourcePayloads()
    {
        var first = AdventurerResourceLoadouts.CreatePrototypeLoadout(0, 1);
        var second = AdventurerResourceLoadouts.CreatePrototypeLoadout(1, 1);

        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].Category,
            Is.EqualTo(PhysicalResourceCategory.ConstructionMaterials));
        Assert.That(second, Has.Count.EqualTo(2));
        Assert.That(second[0].Category,
            Is.EqualTo(PhysicalResourceCategory.TrapComponents));
        Assert.That(second[1].Category,
            Is.EqualTo(PhysicalResourceCategory.ArcaneComponents));
    }

    [Test]
    public void AdventurerResourceMetadataSurvivesRecoveryAndStorage()
    {
        var payload = new AdventurerResourcePayload(
            "trap-components",
            PhysicalResourceCategory.TrapComponents,
            3,
            4);
        var carried = new CarriedDungeonTreasure(payload);
        var recovered = new RecoverableLootItem(
            carried.TreasureId,
            carried.UnitValue,
            carried.Origin,
            carried.SourceCell,
            carried.HasSourceCell,
            carried.ContentKind,
            carried.ResourceCategory,
            carried.ResourceQuantity);
        var drop = new RecoverableLootDrop(
            "drop-1",
            Vector2Int.zero,
            Vector3.zero,
            "Test Adventurer",
            new List<RecoverableLootItem> { recovered });
        var stored = new DungeonStoredLootItem(recovered, "drop-1");

        Assert.That(drop.PhysicalResourceQuantity, Is.EqualTo(3));
        Assert.That(drop.GetPhysicalResourceQuantity(
            PhysicalResourceCategory.TrapComponents), Is.EqualTo(3));
        Assert.That(stored.ContentKind,
            Is.EqualTo(RecoverableLootContentKind.PhysicalResource));
        Assert.That(stored.Origin,
            Is.EqualTo(RecoverableLootOrigin.AdventurerPossession));
        Assert.That(stored.ResourceCategory,
            Is.EqualTo(PhysicalResourceCategory.TrapComponents));
        Assert.That(stored.ResourceQuantity, Is.EqualTo(3));
        Assert.That(stored.UnitValue, Is.EqualTo(4));
        Assert.That(stored.Value, Is.EqualTo(12));
    }

    [Test]
    public void TreasureAndPhysicalResourcesRemainDistinct()
    {
        var treasure = new RecoverableLootItem(
            "treasure",
            10,
            RecoverableLootOrigin.DungeonTreasure,
            Vector2Int.one,
            true);
        var resource = new RecoverableLootItem(
            "construction-materials",
            2,
            RecoverableLootOrigin.AdventurerPossession,
            default,
            false,
            RecoverableLootContentKind.PhysicalResource,
            PhysicalResourceCategory.ConstructionMaterials,
            2);

        Assert.That(treasure.ContentKind,
            Is.EqualTo(RecoverableLootContentKind.Treasure));
        Assert.That(treasure.ResourceQuantity, Is.Zero);
        Assert.That(resource.ContentKind,
            Is.EqualTo(RecoverableLootContentKind.PhysicalResource));
        Assert.That(resource.Origin,
            Is.EqualTo(RecoverableLootOrigin.AdventurerPossession));
    }

    [Test]
    public void SuccessfulEscapeClearsStartingResourcesWithoutCreatingRecovery()
    {
        var traversalObject = new GameObject("Traversal Test");
        var adventurerObject = new GameObject("Adventurer Test");
        try
        {
            NPCTraversal traversal = traversalObject.AddComponent<NPCTraversal>();
            NPCCharacter character = adventurerObject.AddComponent<NPCCharacter>();
            character.ApplyRecord(new NPCCharacterRecord
            {
                id = "adventurer-1",
                characterName = "Test Adventurer",
                startingResources = AdventurerResourceLoadouts.CreatePrototypeLoadout(0, 1)
            });
            NPCTraversalAgent agent =
                adventurerObject.AddComponent<NPCTraversalAgent>();
            agent.Configure(
                traversal,
                Vector2Int.zero,
                Vector3.zero,
                1f,
                1f,
                false,
                0f,
                0f,
                1f,
                0f,
                1);

            Assert.That(agent.BeginDungeonVisit(), Is.True);
            Assert.That(agent.CarriedPhysicalResourceQuantity, Is.EqualTo(2));

            MethodInfo notifyEscape = typeof(NPCTraversal).GetMethod(
                "NotifyAdventurerEscaped",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(notifyEscape, Is.Not.Null);
            notifyEscape.Invoke(traversal, new object[] { agent });

            Assert.That(agent.CarriedDungeonTreasureCount, Is.Zero);
            Assert.That(traversal.RecoverableLootDropCount, Is.Zero);
            Assert.That(traversal.SuccessfulEscapeLootOutcomeCount, Is.EqualTo(1));
            EscapedLootItem escaped =
                traversal.SuccessfulEscapeLootOutcomes[0].EscapedItems[0];
            Assert.That(escaped.IsPhysicalResource, Is.True);
            Assert.That(escaped.Origin,
                Is.EqualTo(RecoverableLootOrigin.AdventurerPossession));
        }
        finally
        {
            Object.DestroyImmediate(adventurerObject);
            Object.DestroyImmediate(traversalObject);
        }
    }
}
#endif
