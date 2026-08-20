#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class BuildResourceAuthorityTests
{
    [Test]
    public void RestoreTrustsBalancesAndKeepsOnlyItemizedTreasure()
    {
        GameObject owner = new("Build Resource Authority Test");
        try
        {
            GameplayLoopController loop =
                owner.AddComponent<GameplayLoopController>();
            CreateMixedRecovery(
                out PlayerLootRecoveryRecord recovery,
                out List<DungeonStoredLootItem> legacyStorage);

            loop.RestoreProgress(
                0, 1f, 0, 1, new List<NPCCharacterRecord>(),
                legacyStorage,
                new[] { recovery },
                null,
                2, 5, 5);

            Assert.That(loop.ConstructionMaterials, Is.EqualTo(2));
            Assert.That(loop.GetRecoveredPhysicalResourceQuantity(
                PhysicalResourceCategory.ConstructionMaterials), Is.EqualTo(2));
            Assert.That(loop.RecoveredLootInventory, Has.Count.EqualTo(1));
            Assert.That(loop.RecoveredLootInventory[0].IsPhysicalResource,
                Is.False);
            Assert.That(loop.PlayerLootRecoveries[0].RecoveredItems,
                Has.Count.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void RepeatedScenarioRestoreDoesNotRecreditResourceAudit()
    {
        GameObject owner = new("Build Resource Scenario Test");
        try
        {
            GameplayLoopController loop =
                owner.AddComponent<GameplayLoopController>();
            CreateMixedRecovery(
                out PlayerLootRecoveryRecord recovery,
                out List<DungeonStoredLootItem> legacyStorage);
            var state = new GameplayLoopScenarioState(
                0, 0, 1, 1f,
                new List<NPCCharacterRecord>(),
                null,
                null,
                legacyStorage,
                new[] { recovery },
                null,
                1, 5, 5);

            loop.RestoreScenarioState(state);
            loop.RestoreScenarioState(state);

            Assert.That(loop.ConstructionMaterials, Is.EqualTo(1));
            Assert.That(loop.RecoveredLootInventory, Has.Count.EqualTo(1));
            Assert.That(loop.PlayerLootRecoveries[0].RecoveredItems,
                Has.Count.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    static void CreateMixedRecovery(
        out PlayerLootRecoveryRecord recovery,
        out List<DungeonStoredLootItem> legacyStorage)
    {
        var treasure = new RecoverableLootItem(
            "treasure", 10, RecoverableLootOrigin.DungeonTreasure,
            Vector2Int.zero, true);
        var resource = new RecoverableLootItem(
            "construction-materials", 2,
            RecoverableLootOrigin.AdventurerPossession,
            Vector2Int.zero, true,
            RecoverableLootContentKind.PhysicalResource,
            PhysicalResourceCategory.ConstructionMaterials,
            3);
        var items = new List<RecoverableLootItem> { treasure, resource };
        var drop = new RecoverableLootDrop(
            "drop-mixed", Vector2Int.zero, Vector3.zero, "Tester", items);
        recovery = new PlayerLootRecoveryRecord(drop, 2, 16, 10, 6);
        legacyStorage = new List<DungeonStoredLootItem>
        {
            new(treasure, drop.DropId),
            new(resource, drop.DropId)
        };
    }
}
#endif
