#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public sealed class DungeonLifecycleIntegrationTests
{
    [Test]
    public void AttemptLocksControllerBuildDebugAndResourceCommandsUntilReturn()
    {
        var owner = new GameObject("Dungeon Lifecycle Integration Test");
        try
        {
            GameplayLoopController loop = owner.AddComponent<GameplayLoopController>();
            int materials = loop.ConstructionMaterials;

            Assert.That(loop.CanBuild, Is.True);
            Assert.That(loop.CanUseDebugActions, Is.True);
            Assert.That(loop.TryBeginValidation("authored-layout", out _), Is.True);
            Assert.That(loop.CanBuild, Is.False);
            Assert.That(loop.CanUseDebugActions, Is.False);
            Assert.That(loop.TryRecordAuthoringEdit(out _), Is.False);
            Assert.That(loop.TrySpendBuildCost(
                new BuildCost(PhysicalResourceCategory.ConstructionMaterials, 1),
                out _), Is.False);
            Assert.That(loop.ConstructionMaterials, Is.EqualTo(materials));
            Assert.That(loop.WorkingRevision, Is.Zero);

            Assert.That(loop.TryAbandonAttempt(loop.Attempt.AttemptId, out _), Is.True);
            Assert.That(loop.CanBuild, Is.False);
            Assert.That(loop.TryReturnToAuthoring(out _), Is.True);
            Assert.That(loop.CanBuild, Is.True);
            Assert.That(loop.CanUseDebugActions, Is.True);
            Assert.That(loop.TryRecordAuthoringEdit(out _), Is.True);
            Assert.That(loop.WorkingRevision, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
#endif
