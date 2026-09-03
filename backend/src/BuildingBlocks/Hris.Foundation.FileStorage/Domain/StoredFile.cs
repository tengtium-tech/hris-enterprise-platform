using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Aggregate Root of the File Storage Framework's own physical storage abstraction.
/// Source: docs/03-foundation/file-storage.md, Core Concepts ("A File represents a
/// binary object stored by the platform") and File Lifecycle.
///
/// Third framework built in Sprint 4. <see cref="Status"/> walks the source document's
/// own File Lifecycle diagram (<see cref="FileLifecycleStatus"/>'s own remarks explain
/// why <c>Stored</c> is collapsed into <see cref="FileLifecycleStatus.Available"/>) for
/// whichever version is currently mid-upload; <see cref="Versions"/> is the immutable
/// history the source document's own File Versioning section requires ("Previous
/// versions should remain recoverable when enabled"). A second and later upload against
/// an already-<see cref="FileLifecycleStatus.Available"/> file walks the identical
/// pipeline again via <see cref="RequestNewVersionUpload"/> -- <see cref="Status"/>
/// legitimately cycles back to <see cref="FileLifecycleStatus.UploadRequested"/> while
/// <see cref="CurrentVersion"/> keeps serving the prior version's content until the new
/// one reaches <see cref="FileLifecycleStatus.Available"/> in its turn.
///
/// Deliberately excludes Business Document Metadata, OCR Processing, Image Processing,
/// Workflow Management, and Business Classification -- this document's own Scope
/// section states plainly "Business metadata belongs to the Document Management
/// Framework," confirmed from that framework's own side (document-management.md:
/// "Physical storage is provided by the File Storage Framework"). This aggregate
/// therefore carries no field describing what a stored file *means* to a business
/// module, only what it *is* as a binary object.
/// </summary>
public sealed class StoredFile : AggregateRoot<StoredFileId>
{
    private readonly List<FileVersion> _versions = [];
    private FileVersion? _pendingVersion;

    public ContainerName ContainerName { get; }

    public string OriginalFileName { get; private set; }

    public FileLifecycleStatus Status { get; private set; }

    /// <summary>
    /// The most recently completed version -- deliberately not its own stored field.
    /// EF Core's owned-type model requires every owned instance to have exactly one
    /// owner navigation, and the same <see cref="FileVersion"/> instance cannot be
    /// owned by both <see cref="Versions"/> (an <c>OwnsMany</c>) and a second,
    /// independent reference simultaneously. Since <see cref="_versions"/> is only ever
    /// appended to, in order, by <see cref="MarkStored"/>, its own last element already
    /// <em>is</em> the current version -- computing it avoids the duplicate-ownership
    /// problem entirely rather than working around it.
    /// </summary>
    public FileVersion? CurrentVersion => _versions.Count == 0 ? null : _versions[^1];

    /// <summary>
    /// The version currently mid-upload, between <see cref="MarkUploaded"/> and
    /// <see cref="MarkStored"/> -- exposed publicly (not only as the private backing
    /// field) both because a caller may legitimately want to inspect an in-flight
    /// upload's own recorded facts, and so the Infrastructure layer's own EF Core
    /// configuration has a property to map through <c>OwnsOne</c>, the same reason
    /// <see cref="Versions"/> exists as a public property rather than only a private
    /// field.
    /// </summary>
    public FileVersion? PendingVersion => _pendingVersion;

    public IReadOnlyList<FileVersion> Versions => _versions.AsReadOnly();

    private StoredFile(StoredFileId id, ContainerName containerName, string originalFileName)
        : base(id)
    {
        ContainerName = containerName;
        OriginalFileName = originalFileName;
        Status = FileLifecycleStatus.UploadRequested;
    }

    /// <summary>
    /// Begins the very first upload for a new logical file, in
    /// <see cref="FileLifecycleStatus.UploadRequested"/>. No content facts are known yet
    /// -- <see cref="MimeType"/>, size, and checksum are confirmed only at
    /// <see cref="MarkUploaded"/>, the point real bytes actually exist, matching the AI
    /// Implementation Guidance's "never trust client-supplied metadata" over whatever a
    /// caller might have declared when merely requesting an upload target. Raises no
    /// event -- file-storage.md's own Domain Events section names no event for this
    /// state, only for <see cref="MarkUploaded"/> onward.
    /// </summary>
    public static Result<StoredFile> RequestUpload(string? containerName, string? originalFileName)
    {
        var containerNameResult = ContainerName.Create(containerName);
        if (containerNameResult.IsFailure)
        {
            return Result.Failure<StoredFile>(containerNameResult.Error);
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return Result.Failure<StoredFile>(FileStorageErrors.OriginalFileNameRequired);
        }

        var trimmedFileName = originalFileName.Trim();
        if (trimmedFileName.Length > 260)
        {
            return Result.Failure<StoredFile>(FileStorageErrors.OriginalFileNameTooLong);
        }

        return Result.Success(new StoredFile(new StoredFileId(Guid.NewGuid()), containerNameResult.Value, trimmedFileName));
    }

    /// <summary>
    /// Starts a new version's upload cycle against a file that already has a current,
    /// available version -- <see cref="CurrentVersion"/> keeps serving the prior
    /// version's content throughout, since this only changes <see cref="Status"/>, never
    /// <see cref="CurrentVersion"/> itself.
    /// </summary>
    public Result RequestNewVersionUpload()
    {
        if (Status != FileLifecycleStatus.Available)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        _pendingVersion = null;
        Status = FileLifecycleStatus.UploadRequested;
        return Result.Success();
    }

    /// <summary>
    /// Confirms real content was written to a provider -- the point this file's actual
    /// checksum, size, and MIME type first become known. Valid from
    /// <see cref="FileLifecycleStatus.UploadRequested"/> only, whether this is the
    /// file's first version or a later one (<see cref="RequestNewVersionUpload"/> always
    /// returns here first).
    /// </summary>
    public Result MarkUploaded(
        StorageObjectKey storageObjectKey,
        Checksum checksum,
        long fileSizeBytes,
        MimeType mimeType,
        StorageProviderType storageProviderType,
        UserAccountId uploadedByUserId,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(storageObjectKey, nameof(storageObjectKey));
        Guard.AgainstNull(checksum, nameof(checksum));
        Guard.AgainstNull(mimeType, nameof(mimeType));

        if (Status != FileLifecycleStatus.UploadRequested)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        if (fileSizeBytes <= 0)
        {
            return Result.Failure(FileStorageErrors.FileSizeMustBePositive);
        }

        _pendingVersion = new FileVersion(
            new FileVersionId(Guid.NewGuid()),
            _versions.Count + 1,
            storageObjectKey,
            checksum,
            fileSizeBytes,
            mimeType,
            storageProviderType,
            uploadedByUserId,
            nowUtc);

        Status = FileLifecycleStatus.Uploaded;
        AddDomainEvent(new FileUploaded(
            Guid.NewGuid(), nowUtc, Id, _pendingVersion.Id, _pendingVersion.VersionNumber, storageObjectKey, storageProviderType));

        return Result.Success();
    }

    /// <summary>
    /// The upload-time integrity check -- "is this upload trustworthy enough to
    /// accept." A mismatch is not a terminal failure: <see cref="Status"/> stays
    /// <see cref="FileLifecycleStatus.Uploaded"/> so a caller may re-verify (a transient
    /// read error) or eventually <see cref="Delete"/> to abandon the attempt; no event is
    /// raised for a mismatch here, distinct from <see cref="ReverifyIntegrity"/>, whose
    /// entire purpose is auditing corruption after the fact.
    /// </summary>
    public Result VerifyIntegrity(Checksum actualChecksum, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(actualChecksum, nameof(actualChecksum));

        if (Status != FileLifecycleStatus.Uploaded || _pendingVersion is null)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        if (actualChecksum != _pendingVersion.Checksum)
        {
            return Result.Failure(FileStorageErrors.ChecksumMismatch);
        }

        Status = FileLifecycleStatus.Validated;
        AddDomainEvent(new FileValidated(Guid.NewGuid(), nowUtc, Id, _pendingVersion.Id));
        return Result.Success();
    }

    /// <summary>
    /// Confirms durable storage and, in the same step, availability -- see
    /// <see cref="FileLifecycleStatus"/>'s own remarks for why <c>Stored</c> and
    /// <c>Available</c> are not modeled as two separately-actioned transitions. Promotes
    /// the pending version into <see cref="Versions"/> and <see cref="CurrentVersion"/>,
    /// leaving every prior version exactly as it was, per this framework's own
    /// version-history requirement.
    /// </summary>
    public Result MarkStored(DateTimeOffset nowUtc)
    {
        if (Status != FileLifecycleStatus.Validated || _pendingVersion is null)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        var storedVersion = _pendingVersion;
        _versions.Add(storedVersion);
        _pendingVersion = null;
        Status = FileLifecycleStatus.Available;

        AddDomainEvent(new FileStored(Guid.NewGuid(), nowUtc, Id, storedVersion.Id, storedVersion.VersionNumber));
        return Result.Success();
    }

    /// <summary>
    /// Records that <see cref="CurrentVersion"/>'s content was downloaded -- audit-only,
    /// per file-storage.md's Security Considerations ("Every file operation should be
    /// auditable"). Never changes <see cref="Status"/>.
    /// </summary>
    public Result RecordDownload(UserAccountId downloadedByUserId, DateTimeOffset nowUtc)
    {
        if (Status != FileLifecycleStatus.Available || CurrentVersion is null)
        {
            return Result.Failure(FileStorageErrors.NoCurrentVersion);
        }

        AddDomainEvent(new FileDownloaded(Guid.NewGuid(), nowUtc, Id, CurrentVersion.Id, downloadedByUserId));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status != FileLifecycleStatus.Available)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        Status = FileLifecycleStatus.Archived;
        AddDomainEvent(new FileArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Undoes <see cref="Archive"/>. Valid only from <see cref="FileLifecycleStatus.Archived"/>
    /// -- restoring a genuinely <see cref="FileLifecycleStatus.Deleted"/> file is a
    /// disaster-recovery/backup-restore operation (file-storage.md's own Backup and
    /// Recovery section, "Object Restoration"), re-creating an aggregate from a backup,
    /// not a status flip on a row this method could safely assume still exists intact.
    /// </summary>
    public Result Restore(DateTimeOffset nowUtc)
    {
        if (Status != FileLifecycleStatus.Archived)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        Status = FileLifecycleStatus.Available;
        AddDomainEvent(new FileRestored(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Terminal from any other status, including an abandoned in-flight upload -- an
    /// upload that will never complete still needs cleanup, not only a fully-available
    /// file.
    /// </summary>
    public Result Delete(DateTimeOffset nowUtc)
    {
        if (Status == FileLifecycleStatus.Deleted)
        {
            return Result.Failure(FileStorageErrors.InvalidFileLifecycleTransition);
        }

        Status = FileLifecycleStatus.Deleted;
        AddDomainEvent(new FileDeleted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// The periodic, later re-check of an already-<see cref="FileLifecycleStatus.Available"/>
    /// file's own current version -- "has this already-accepted content since become
    /// corrupted," file-storage.md's File Integrity section ("Corruption Detection...
    /// Integrity validation should occur automatically"). Unlike <see cref="VerifyIntegrity"/>,
    /// this always raises its event, matched or not: detecting and reporting corruption
    /// is this method's entire purpose, and silently swallowing a failed re-check would
    /// defeat it. <see cref="Status"/> is left unchanged either way -- this framework
    /// names no distinct "corrupted" status, so a failed re-check surfaces only through
    /// this method's own <see cref="Result"/> and the event it raised.
    /// </summary>
    public Result ReverifyIntegrity(Checksum actualChecksum, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(actualChecksum, nameof(actualChecksum));

        if (Status != FileLifecycleStatus.Available || CurrentVersion is null)
        {
            return Result.Failure(FileStorageErrors.NoCurrentVersion);
        }

        var matched = actualChecksum == CurrentVersion.Checksum;
        AddDomainEvent(new FileIntegrityVerified(Guid.NewGuid(), nowUtc, Id, CurrentVersion.Id, matched));

        return matched ? Result.Success() : Result.Failure(FileStorageErrors.IntegrityCheckFailed);
    }

    /// <summary>
    /// Relocates <see cref="CurrentVersion"/>'s identical content to a different
    /// provider. <paramref name="migratedContentChecksum"/> is the checksum of the copy
    /// already written to <paramref name="newProviderType"/>/<paramref name="newStorageObjectKey"/>
    /// -- verified against <see cref="CurrentVersion"/>'s own recorded checksum before
    /// anything is updated, proving the migration copied faithfully rather than trusting
    /// the caller's own claim that it did.
    /// </summary>
    public Result MigrateCurrentVersionToProvider(
        StorageProviderType newProviderType,
        StorageObjectKey newStorageObjectKey,
        Checksum migratedContentChecksum,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(newStorageObjectKey, nameof(newStorageObjectKey));
        Guard.AgainstNull(migratedContentChecksum, nameof(migratedContentChecksum));

        if (Status != FileLifecycleStatus.Available || CurrentVersion is null)
        {
            return Result.Failure(FileStorageErrors.NoCurrentVersion);
        }

        if (migratedContentChecksum != CurrentVersion.Checksum)
        {
            return Result.Failure(FileStorageErrors.MigratedChecksumMismatch);
        }

        var fromProvider = CurrentVersion.StorageProviderType;
        var versionId = CurrentVersion.Id;
        CurrentVersion.MigrateStorageLocation(newProviderType, newStorageObjectKey);

        AddDomainEvent(new StorageProviderChanged(Guid.NewGuid(), nowUtc, Id, versionId, fromProvider, newProviderType));
        return Result.Success();
    }
}
