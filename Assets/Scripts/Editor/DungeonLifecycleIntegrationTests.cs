#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
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

    [Test]
    public void RejectedObstacleRestorePreservesWorldRevisionProofAndPublishedSnapshot()
    {
        CreateRestorationFixture(out GameObject owner, out GameplayLoopController loop,
            out TileGridGenerator grid, out GeneratedBuildObstacleGenerator generator,
            out GeneratedBuildObstacleDefinition definition);
        try
        {
            AddObstacle(generator, definition);
            ValidateAndPublish(loop, out DungeonValidationProof proof,
                out PublishedDungeonVersion published);

            bool restored = grid.RestoreBuildObstacleLayout(new[]
            {
                new SavedGeneratedBuildObstacle
                {
                    definitionId = "Missing",
                    anchorX = 1,
                    anchorY = 1,
                    variantId = "Test"
                }
            }, out _);

            Assert.That(restored, Is.False);
            Assert.That(generator.Instances, Has.Count.EqualTo(1));
            Assert.That(loop.WorkingRevision, Is.Zero);
            Assert.That(loop.ValidationProof, Is.SameAs(proof));
            AssertPublishedSnapshotUnchanged(loop, published);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void SuccessfulObstacleRestoreRecordsOneEditAndInvalidatesProof()
    {
        CreateRestorationFixture(out GameObject owner, out GameplayLoopController loop,
            out TileGridGenerator grid, out GeneratedBuildObstacleGenerator generator,
            out GeneratedBuildObstacleDefinition definition);
        try
        {
            AddObstacle(generator, definition);
            ValidateAndPublish(loop, out _, out PublishedDungeonVersion published);

            Assert.That(grid.RestoreBuildObstacleLayout(
                System.Array.Empty<SavedGeneratedBuildObstacle>(), out _), Is.True);

            Assert.That(generator.Instances, Is.Empty);
            Assert.That(loop.WorkingRevision, Is.EqualTo(1));
            Assert.That(loop.ValidationProof, Is.Null);
            AssertPublishedSnapshotUnchanged(loop, published);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void FailedNestedRestoreScopeCannotDiscardRetainedWorldMutation()
    {
        CreateRestorationFixture(out GameObject owner, out GameplayLoopController loop,
            out TileGridGenerator grid, out GeneratedBuildObstacleGenerator generator,
            out GeneratedBuildObstacleDefinition definition);
        try
        {
            AddObstacle(generator, definition);
            ValidateAndPublish(loop, out _, out PublishedDungeonVersion published);

            using (BeginAuthoringBatch(grid))
            using (BeginAuthoringBatch(grid))
            {
                Assert.That(grid.RestoreBuildObstacleLayout(
                    System.Array.Empty<SavedGeneratedBuildObstacle>(), out _), Is.True);
                // Simulate a later scenario/save restore step rejecting after
                // the obstacle restore has already changed the live world.
            }

            Assert.That(generator.Instances, Is.Empty);
            Assert.That(loop.WorkingRevision, Is.EqualTo(1));
            Assert.That(loop.ValidationProof, Is.Null);
            AssertPublishedSnapshotUnchanged(loop, published);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void DebugClickLockDisablesSelectionAndOnlyReleasesHarnessFocus()
    {
        var owner = new GameObject("Debug Harness Lock Test");
        var target = new GameObject("Harness Focus Target");
        var foreignTarget = new GameObject("Foreign Focus Target");
        NPCRuntimeDebugHarnessWindow window = null;
        try
        {
            GameplayLoopController loop = owner.AddComponent<GameplayLoopController>();
            Camera camera = owner.AddComponent<Camera>();
            CameraFollow cameraFollow = owner.AddComponent<CameraFollow>();
            cameraFollow.camComponent = camera;
            window = ScriptableObject.CreateInstance<NPCRuntimeDebugHarnessWindow>();

            Assert.That(cameraFollow.FocusTarget(target.transform), Is.True);
            SetField(window, "selectionMode", true);
            SetField(window, "focusedCamera", cameraFollow);
            SetField(window, "focusedTarget", target.transform);
            SetField(window, "highlightRoot", new GameObject("Harness Highlight"));

            Assert.That(loop.TryBeginValidation("layout", out _), Is.True);
            Invoke(window, "OnGameViewClicked");

            Assert.That(GetField<bool>(window, "selectionMode"), Is.False);
            Assert.That(GetField<InputManager>(window, "subscribedInputManager"), Is.Null);
            Assert.That(GetField<GameObject>(window, "highlightRoot"), Is.Null);
            Assert.That(cameraFollow.HasFocus, Is.False);

            SetField(window, "selectionMode", true);
            Assert.That(cameraFollow.FocusTarget(target.transform), Is.True);
            SetField(window, "focusedCamera", cameraFollow);
            SetField(window, "focusedTarget", target.transform);
            Assert.That(cameraFollow.FocusTarget(foreignTarget.transform), Is.True);
            Invoke(window, "DisableSelectionForDebugLock");

            Assert.That(GetField<bool>(window, "selectionMode"), Is.False);
            Assert.That(cameraFollow.FocusedTarget, Is.EqualTo(foreignTarget.transform));
        }
        finally
        {
            if (window != null)
                Object.DestroyImmediate(window);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(foreignTarget);
            Object.DestroyImmediate(owner);
        }
    }

    static void CreateRestorationFixture(
        out GameObject owner,
        out GameplayLoopController loop,
        out TileGridGenerator grid,
        out GeneratedBuildObstacleGenerator generator,
        out GeneratedBuildObstacleDefinition definition)
    {
        owner = new GameObject("Lifecycle Restore Integration Test");
        loop = owner.AddComponent<GameplayLoopController>();
        grid = owner.AddComponent<TileGridGenerator>();
        generator = owner.AddComponent<GeneratedBuildObstacleGenerator>();
        definition = new GeneratedBuildObstacleDefinition
        {
            definitionId = "TestObstacle",
            footprintOffsets = new List<Vector2Int> { Vector2Int.zero }
        };
        generator.ConfigureForTests(new[] { definition });
        generator.InitializeDefinitionsOnly(grid);
        SetField(grid, "buildObstacleGenerator", generator);
    }

    static void AddObstacle(
        GeneratedBuildObstacleGenerator generator,
        GeneratedBuildObstacleDefinition definition)
    {
        var obstacle = new GeneratedBuildObstacleInstance(
            definition, new Vector2Int(1, 1), 0, "Test");
        GetField<List<GeneratedBuildObstacleInstance>>(generator, "instances")
            .Add(obstacle);
        GetField<Dictionary<Vector2Int, GeneratedBuildObstacleInstance>>(
            generator, "byCell").Add(new Vector2Int(1, 1), obstacle);
    }

    static void ValidateAndPublish(
        GameplayLoopController loop,
        out DungeonValidationProof proof,
        out PublishedDungeonVersion published)
    {
        Assert.That(loop.TryBeginValidation("original-layout", out _), Is.True);
        System.Guid attemptId = loop.Attempt.AttemptId;
        Assert.That(loop.TryAcquireAttemptTreasure(attemptId, out _), Is.True);
        Assert.That(loop.TryEscapeAttempt(attemptId, out _), Is.True);
        Assert.That(loop.TryReturnToAuthoring(out _), Is.True);
        proof = loop.ValidationProof;
        Assert.That(loop.TryPublishDungeon(out published, out _), Is.True);
    }

    static void AssertPublishedSnapshotUnchanged(
        GameplayLoopController loop,
        PublishedDungeonVersion published)
    {
        Assert.That(loop.PublishedVersions, Has.Count.EqualTo(1));
        Assert.That(loop.PublishedVersions[0], Is.SameAs(published));
        Assert.That(published.Snapshot.Revision, Is.Zero);
        Assert.That(published.Snapshot.AuthoredData, Is.EqualTo("original-layout"));
    }

    static T GetField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing {target.GetType().Name}.{name}");
        return (T)field.GetValue(target);
    }

    static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing {target.GetType().Name}.{name}");
        field.SetValue(target, value);
    }

    static void Invoke(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing {target.GetType().Name}.{name}");
        method.Invoke(target, null);
    }

    static System.IDisposable BeginAuthoringBatch(TileGridGenerator grid)
    {
        MethodInfo method = typeof(TileGridGenerator).GetMethod(
            "BeginAuthoringBatch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing TileGridGenerator.BeginAuthoringBatch");
        return (System.IDisposable)method.Invoke(grid, null);
    }
}
#endif
