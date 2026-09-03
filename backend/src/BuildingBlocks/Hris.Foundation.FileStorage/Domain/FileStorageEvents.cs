using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// file-storage.md's own Domain Events section names nine events (FileUploaded,
/// FileValidated, FileStored, FileDownloaded, FileArchived, FileDeleted, FileRestored,
/// FileIntegrityVerified, StorageProviderChanged) -- every one implemented here, one
/// method on <see cref="StoredFile"/> per event. <c>FileValidated</c> is the upload-time
/// checksum check (<see cref="StoredFile.VerifyIntegrity"/>); <c>FileIntegrityVerified</c>
/// is the separate, later, periodic re-check of an already-<see cref="FileLifecycleStatus.Available"/>
/// file (<see cref="StoredFile.ReverifyIntegrity"/>) -- the document lists both because
/// they answer different questions ("is this upload trustworthy enough to accept" versus
/// "has this already-accepted content since become corrupted").
/// </summary>
public sealed record FileUploaded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId,
    FileVersionId FileVersionId,
    int VersionNumber,
    StorageObjectKey StorageObjectKey,
    StorageProviderType StorageProviderType) : IDomainEvent;

public sealed record FileValidated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId,
    FileVersionId FileVersionId) : IDomainEvent;

public sealed record FileStored(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId,
    FileVersionId FileVersionId,
    int VersionNumber) : IDomainEvent;

public sealed record FileDownloaded(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId,
    FileVersionId FileVersionId,
    UserAccountId DownloadedByUserId) : IDomainEvent;

public sealed record FileArchived(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId) : IDomainEvent;

public sealed record FileDeleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId) : IDomainEvent;

public sealed record FileRestored(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId) : IDomainEvent;

public sealed record FileIntegrityVerified(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId,
    FileVersionId FileVersionId,
    bool Matched) : IDomainEvent;

public sealed record StorageProviderChanged(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    StoredFileId StoredFileId,
    FileVersionId FileVersionId,
    StorageProviderType FromProvider,
    StorageProviderType ToProvider) : IDomainEvent;
