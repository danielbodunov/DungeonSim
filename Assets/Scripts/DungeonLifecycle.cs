using System;
using System.Collections.Generic;

public enum DungeonAttemptKind { CreatorValidation, Raid }
public enum DungeonAttemptState { SeekingTreasure, CarryingTreasure, Escaped, Dead, Abandoned }

/// <summary>Immutable authored data supplied by the snapshot owner, never live runtime state.</summary>
public sealed class DungeonRevisionSnapshot
{
    public long Revision { get; }
    public string CompatibilityId { get; }
    public string AuthoredData { get; }

    internal DungeonRevisionSnapshot(long revision, string compatibilityId, string authoredData)
    {
        Revision = revision;
        CompatibilityId = compatibilityId;
        AuthoredData = authoredData;
    }
}

public sealed class DungeonValidationProof
{
    public Guid AttemptId { get; }
    public DungeonRevisionSnapshot Snapshot { get; }

    internal DungeonValidationProof(Guid attemptId, DungeonRevisionSnapshot snapshot)
    {
        AttemptId = attemptId;
        Snapshot = snapshot;
    }
}

public sealed class PublishedDungeonVersion
{
    public Guid VersionId { get; } = Guid.NewGuid();
    public DungeonRevisionSnapshot Snapshot { get; }

    internal PublishedDungeonVersion(DungeonRevisionSnapshot snapshot) => Snapshot = snapshot;
}

public sealed class DungeonAttempt
{
    public Guid AttemptId { get; } = Guid.NewGuid();
    public DungeonAttemptKind Kind { get; }
    public DungeonRevisionSnapshot Snapshot { get; }
    public Guid? PublishedVersionId { get; }
    public DungeonAttemptState State { get; internal set; } = DungeonAttemptState.SeekingTreasure;
    public bool IsTerminal => State == DungeonAttemptState.Escaped ||
        State == DungeonAttemptState.Dead || State == DungeonAttemptState.Abandoned;

    internal DungeonAttempt(DungeonAttemptKind kind, DungeonRevisionSnapshot snapshot,
        Guid? publishedVersionId = null)
    {
        Kind = kind;
        Snapshot = snapshot;
        PublishedVersionId = publishedVersionId;
    }
}

/// <summary>
/// Session lifecycle coordinated by GameplayLoopController. Snapshot capture/loading,
/// compatibility fingerprints, persistence and world reset belong to their content owners.
/// Failed commands leave all lifecycle state unchanged.
/// </summary>
public sealed class DungeonLifecycle
{
    readonly List<PublishedDungeonVersion> publishedVersions = new();
    public long WorkingRevision { get; private set; }
    public string CompatibilityId { get; private set; }
    public DungeonValidationProof ValidationProof { get; private set; }
    public DungeonAttempt Attempt { get; private set; }
    public IReadOnlyList<PublishedDungeonVersion> PublishedVersions { get; }
    // Terminal attempts retain the command lock until explicitly returned to authoring.
    public bool CanAuthor => Attempt == null;
    public bool HasCurrentProof => ValidationProof != null &&
        ValidationProof.Snapshot.Revision == WorkingRevision &&
        ValidationProof.Snapshot.CompatibilityId == CompatibilityId;

    public DungeonLifecycle(string compatibilityId)
    {
        if (string.IsNullOrWhiteSpace(compatibilityId))
            throw new ArgumentException("A gameplay compatibility identity is required.", nameof(compatibilityId));
        CompatibilityId = compatibilityId;
        PublishedVersions = publishedVersions.AsReadOnly();
    }

    public bool TryRecordAuthoringEdit(out string failure)
    {
        if (!CanAuthor || WorkingRevision == long.MaxValue)
            return Deny("Authoring is unavailable during an attempt or at the revision limit.", out failure);
        WorkingRevision++;
        ValidationProof = null;
        failure = string.Empty;
        return true;
    }

    public bool TrySetCompatibility(string compatibilityId, out string failure)
    {
        if (!CanAuthor || string.IsNullOrWhiteSpace(compatibilityId))
            return Deny("Compatibility can only be changed while authoring, with a nonempty identity.", out failure);
        if (CompatibilityId != compatibilityId)
        {
            CompatibilityId = compatibilityId;
            ValidationProof = null;
        }
        failure = string.Empty;
        return true;
    }

    public bool TryBeginValidation(string authoredData, out string failure)
    {
        if (!CanAuthor || string.IsNullOrWhiteSpace(authoredData))
            return Deny("Validation requires an authored snapshot and no existing attempt.", out failure);
        var snapshot = new DungeonRevisionSnapshot(WorkingRevision, CompatibilityId, authoredData);
        Attempt = new DungeonAttempt(DungeonAttemptKind.CreatorValidation, snapshot);
        ValidationProof = null;
        failure = string.Empty;
        return true;
    }

    public bool TryPublish(out PublishedDungeonVersion version, out string failure)
    {
        version = null;
        if (!CanAuthor || !HasCurrentProof)
            return Deny("Publishing requires proof for the current revision and gameplay compatibility.", out failure);
        version = new PublishedDungeonVersion(ValidationProof.Snapshot);
        publishedVersions.Add(version);
        failure = string.Empty;
        return true;
    }

    public bool TryBeginRaid(Guid versionId, out string failure)
    {
        PublishedDungeonVersion version = publishedVersions.Find(v => v.VersionId == versionId);
        if (!CanAuthor || version == null || version.Snapshot.CompatibilityId != CompatibilityId)
            return Deny("Raiding requires a compatible published version and no existing attempt.", out failure);
        Attempt = new DungeonAttempt(DungeonAttemptKind.Raid, version.Snapshot, version.VersionId);
        failure = string.Empty;
        return true;
    }

    public bool TryAcquireTreasure(Guid attemptId, out string failure)
    {
        if (!IsActive(attemptId) || Attempt.State != DungeonAttemptState.SeekingTreasure)
            return Deny("Only the current treasure-seeking attempt can acquire treasure.", out failure);
        Attempt.State = DungeonAttemptState.CarryingTreasure;
        failure = string.Empty;
        return true;
    }

    // Called by the objective authority after a living raider returns to its entrance.
    public bool TryEscape(Guid attemptId, out string failure)
    {
        if (!IsActive(attemptId) || Attempt.State != DungeonAttemptState.CarryingTreasure ||
            (Attempt.Kind == DungeonAttemptKind.CreatorValidation &&
             (Attempt.Snapshot.Revision != WorkingRevision ||
              Attempt.Snapshot.CompatibilityId != CompatibilityId)))
            return Deny("Escape requires the current living treasure carrier and a current validation snapshot.", out failure);
        Attempt.State = DungeonAttemptState.Escaped;
        if (Attempt.Kind == DungeonAttemptKind.CreatorValidation)
            ValidationProof = new DungeonValidationProof(Attempt.AttemptId, Attempt.Snapshot);
        failure = string.Empty;
        return true;
    }

    public bool TryDie(Guid attemptId, out string failure) =>
        TryEndUnsuccessfully(attemptId, DungeonAttemptState.Dead, out failure);

    public bool TryAbandon(Guid attemptId, out string failure) =>
        TryEndUnsuccessfully(attemptId, DungeonAttemptState.Abandoned, out failure);

    public bool TryRestart(Guid attemptId, out string failure)
    {
        if (Attempt == null || Attempt.AttemptId != attemptId ||
            Attempt.Snapshot.CompatibilityId != CompatibilityId ||
            (Attempt.Kind == DungeonAttemptKind.CreatorValidation &&
             Attempt.Snapshot.Revision != WorkingRevision))
            return Deny("Only the current compatible attempt can restart.", out failure);
        var previous = Attempt;
        var next = new DungeonAttempt(previous.Kind, previous.Snapshot, previous.PublishedVersionId);
        if (!previous.IsTerminal)
            previous.State = DungeonAttemptState.Abandoned;
        Attempt = next;
        if (next.Kind == DungeonAttemptKind.CreatorValidation)
            ValidationProof = null;
        failure = string.Empty;
        return true;
    }

    public bool TryReturnToAuthoring(out string failure)
    {
        if (Attempt == null || !Attempt.IsTerminal)
            return Deny("End the current attempt before returning to authoring.", out failure);
        Attempt = null;
        failure = string.Empty;
        return true;
    }

    bool IsActive(Guid attemptId) => Attempt != null && Attempt.AttemptId == attemptId && !Attempt.IsTerminal;

    bool TryEndUnsuccessfully(Guid attemptId, DungeonAttemptState state, out string failure)
    {
        if (!IsActive(attemptId))
            return Deny("Only the current active attempt can end.", out failure);
        Attempt.State = state;
        if (Attempt.Kind == DungeonAttemptKind.CreatorValidation)
            ValidationProof = null;
        failure = string.Empty;
        return true;
    }

    static bool Deny(string message, out string failure)
    {
        failure = message;
        return false;
    }
}
