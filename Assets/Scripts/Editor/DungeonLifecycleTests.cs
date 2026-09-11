#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;

public sealed class DungeonLifecycleTests
{
    [Test]
    public void EditAfterPublishInvalidatesProofButCanRaidTheOriginalSnapshot()
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Validate(lifecycle, "layout-one");
        Assert.That(lifecycle.TryPublish(out var published, out _), Is.True);
        long validatedRevision = lifecycle.WorkingRevision;

        Assert.That(lifecycle.TryRecordAuthoringEdit(out _), Is.True);
        Assert.That(lifecycle.WorkingRevision, Is.EqualTo(validatedRevision + 1));
        Assert.That(lifecycle.ValidationProof, Is.Null);
        Assert.That(lifecycle.TryPublish(out _, out _), Is.False);
        Assert.That(lifecycle.TryBeginRaid(published.VersionId, out _), Is.True);
        Assert.That(lifecycle.Attempt.Kind, Is.EqualTo(DungeonAttemptKind.Raid));
        Assert.That(lifecycle.Attempt.Snapshot.AuthoredData, Is.EqualTo("layout-one"));
        Assert.That(lifecycle.Attempt.Snapshot.Revision, Is.EqualTo(validatedRevision));
        Assert.That(lifecycle.PublishedVersions[0], Is.SameAs(published));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FailedOrAbandonedValidationCannotGrantProof(bool death)
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Validate(lifecycle, "layout");
        Assert.That(lifecycle.HasCurrentProof, Is.True);
        Assert.That(lifecycle.TryBeginValidation("layout", out _), Is.True);
        Guid id = lifecycle.Attempt.AttemptId;
        Assert.That(lifecycle.ValidationProof, Is.Null);
        Assert.That(death ? lifecycle.TryDie(id, out _) : lifecycle.TryAbandon(id, out _), Is.True);
        Assert.That(lifecycle.TryEscape(id, out _), Is.False);
        Assert.That(lifecycle.CanAuthor, Is.False);
        Assert.That(lifecycle.TryReturnToAuthoring(out _), Is.True);
        Assert.That(lifecycle.ValidationProof, Is.Null);
        Assert.That(lifecycle.TryPublish(out _, out _), Is.False);
    }

    [Test]
    public void ChangedCompatibilityRejectsOldProofAndVersionWithoutMutatingThem()
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Validate(lifecycle, "layout");
        lifecycle.TryPublish(out var version, out _);
        Assert.That(lifecycle.TrySetCompatibility("rules-2", out _), Is.True);
        Assert.That(lifecycle.ValidationProof, Is.Null);
        Assert.That(lifecycle.TryPublish(out _, out _), Is.False);
        Assert.That(lifecycle.TryBeginRaid(version.VersionId, out _), Is.False);
        Assert.That(lifecycle.Attempt, Is.Null);
        Assert.That(version.Snapshot.CompatibilityId, Is.EqualTo("rules-1"));
        Assert.That(lifecycle.PublishedVersions, Has.Count.EqualTo(1));
    }

    [Test]
    public void InvalidStartPreservesAnExistingProofAndRevision()
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Validate(lifecycle, "layout");
        var proof = lifecycle.ValidationProof;
        Assert.That(lifecycle.TryBeginValidation("", out _), Is.False);
        Assert.That(lifecycle.TryBeginRaid(Guid.NewGuid(), out _), Is.False);
        Assert.That(lifecycle.TrySetCompatibility("", out _), Is.False);
        Assert.That(lifecycle.ValidationProof, Is.SameAs(proof));
        Assert.That(lifecycle.WorkingRevision, Is.EqualTo(proof.Snapshot.Revision));
        Assert.That(lifecycle.Attempt, Is.Null);
    }

    [TestCase(DungeonAttemptKind.CreatorValidation)]
    [TestCase(DungeonAttemptKind.Raid)]
    public void ActiveAttemptRejectsEditsPublishingAndModeChangesAtomically(DungeonAttemptKind kind)
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Start(lifecycle, kind);
        var attempt = lifecycle.Attempt;
        var proof = lifecycle.ValidationProof;
        long revision = lifecycle.WorkingRevision;
        int versions = lifecycle.PublishedVersions.Count;

        Assert.That(lifecycle.TryRecordAuthoringEdit(out _), Is.False);
        Assert.That(lifecycle.TryBeginValidation("replacement", out _), Is.False);
        Assert.That(lifecycle.TrySetCompatibility("rules-2", out _), Is.False);
        Assert.That(lifecycle.TryBeginRaid(Guid.NewGuid(), out _), Is.False);
        Assert.That(lifecycle.TryPublish(out _, out _), Is.False);
        Assert.That(lifecycle.TryReturnToAuthoring(out _), Is.False);
        Assert.That(lifecycle.TryEscape(attempt.AttemptId, out _), Is.False);
        Assert.That(lifecycle.Attempt, Is.SameAs(attempt));
        Assert.That(lifecycle.Attempt.State, Is.EqualTo(DungeonAttemptState.SeekingTreasure));
        Assert.That(lifecycle.WorkingRevision, Is.EqualTo(revision));
        Assert.That(lifecycle.ValidationProof, Is.SameAs(proof));
        Assert.That(lifecycle.PublishedVersions, Has.Count.EqualTo(versions));
        Assert.That(lifecycle.CompatibilityId, Is.EqualTo("rules-1"));
    }

    [TestCase(DungeonAttemptKind.CreatorValidation)]
    [TestCase(DungeonAttemptKind.Raid)]
    public void RestartKeepsSnapshotAndRejectsCallbacksFromPreviousAttempt(DungeonAttemptKind kind)
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Start(lifecycle, kind);
        var previous = lifecycle.Attempt;
        lifecycle.TryAcquireTreasure(previous.AttemptId, out _);
        Assert.That(lifecycle.TryRestart(previous.AttemptId, out _), Is.True);
        var retry = lifecycle.Attempt;
        Assert.That(previous.State, Is.EqualTo(DungeonAttemptState.Abandoned));
        Assert.That(retry.AttemptId, Is.Not.EqualTo(previous.AttemptId));
        Assert.That(retry.Snapshot, Is.SameAs(previous.Snapshot));
        Assert.That(retry.PublishedVersionId, Is.EqualTo(previous.PublishedVersionId));
        Assert.That(retry.State, Is.EqualTo(DungeonAttemptState.SeekingTreasure));
        Assert.That(lifecycle.TryEscape(previous.AttemptId, out _), Is.False);
        Assert.That(lifecycle.TryDie(previous.AttemptId, out _), Is.False);
        Assert.That(lifecycle.TryAbandon(previous.AttemptId, out _), Is.False);
        Assert.That(lifecycle.TryRestart(previous.AttemptId, out _), Is.False);
        Assert.That(lifecycle.Attempt, Is.SameAs(retry));
    }

    [TestCase(DungeonAttemptState.Dead)]
    [TestCase(DungeonAttemptState.Abandoned)]
    [TestCase(DungeonAttemptState.Escaped)]
    public void TerminalAttemptHasExplicitRetryAndAuthoringTransitions(DungeonAttemptState terminal)
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        lifecycle.TryBeginValidation("layout", out _);
        Guid id = lifecycle.Attempt.AttemptId;
        if (terminal == DungeonAttemptState.Escaped)
        {
            lifecycle.TryAcquireTreasure(id, out _);
            Assert.That(lifecycle.TryEscape(id, out _), Is.True);
        }
        else if (terminal == DungeonAttemptState.Dead)
            Assert.That(lifecycle.TryDie(id, out _), Is.True);
        else
            Assert.That(lifecycle.TryAbandon(id, out _), Is.True);
        Assert.That(lifecycle.Attempt.State, Is.EqualTo(terminal));
        Assert.That(lifecycle.TryDie(id, out _), Is.False);
        Assert.That(lifecycle.TryAcquireTreasure(id, out _), Is.False);
        Assert.That(lifecycle.TryRestart(id, out _), Is.True);
        Assert.That(lifecycle.ValidationProof, Is.Null);
        Assert.That(lifecycle.TryAbandon(lifecycle.Attempt.AttemptId, out _), Is.True);
        Assert.That(lifecycle.TryReturnToAuthoring(out _), Is.True);
        Assert.That(lifecycle.CanAuthor, Is.True);
    }

    [Test]
    public void RaidSuccessDoesNotValidateAnEditedWorkingCopy()
    {
        var lifecycle = new DungeonLifecycle("rules-1");
        Validate(lifecycle, "layout");
        lifecycle.TryPublish(out var version, out _);
        lifecycle.TryRecordAuthoringEdit(out _);
        lifecycle.TryBeginRaid(version.VersionId, out _);
        Guid id = lifecycle.Attempt.AttemptId;
        lifecycle.TryAcquireTreasure(id, out _);
        Assert.That(lifecycle.TryEscape(id, out _), Is.True);
        lifecycle.TryReturnToAuthoring(out _);
        Assert.That(lifecycle.ValidationProof, Is.Null);
    }

    static void Start(DungeonLifecycle lifecycle, DungeonAttemptKind kind)
    {
        if (kind == DungeonAttemptKind.Raid)
        {
            Validate(lifecycle, "layout");
            lifecycle.TryPublish(out var version, out _);
            Assert.That(lifecycle.TryBeginRaid(version.VersionId, out _), Is.True);
        }
        else
            Assert.That(lifecycle.TryBeginValidation("layout", out _), Is.True);
    }

    static void Validate(DungeonLifecycle lifecycle, string authoredData)
    {
        Assert.That(lifecycle.TryBeginValidation(authoredData, out _), Is.True);
        Guid id = lifecycle.Attempt.AttemptId;
        Assert.That(lifecycle.TryAcquireTreasure(id, out _), Is.True);
        Assert.That(lifecycle.TryEscape(id, out _), Is.True);
        Assert.That(lifecycle.TryReturnToAuthoring(out _), Is.True);
    }
}
#endif
