using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// One version of a <see cref="StoredFile"/>'s actual binary content. Source:
/// docs/03-foundation/file-storage.md, File Versioning ("Version History... Immutable
/// Versions... Previous versions should remain recoverable when enabled"). A child
/// Entity, never an Aggregate Root of its own (aggregate-design-rules.md Rule 7); its
/// constructor and mutating method are <c>internal</c>, reachable only through
/// <see cref="StoredFile"/> -- mirrors <c>RuleVersion</c>'s own shape closely, both
/// frameworks independently specifying a versioned, immutable-once-recorded child.
///
/// "Immutable" applies to content identity (<see cref="StorageObjectKey"/>,
/// <see cref="Checksum"/>, <see cref="FileSizeBytes"/>, <see cref="MimeType"/>) -- a new
/// version is created, never an existing one's content facts rewritten in place. Only
/// <see cref="StorageProviderType"/> and the physical <see cref="StorageObjectKey"/> can
/// change after the fact, and only through <see cref="MigrateStorageLocation"/>, which a
/// caller uses to relocate the identical content to a different provider, never to
/// substitute different content under an existing version's identity.
/// </summary>
public sealed class FileVersion : Entity<FileVersionId>
{
    public int VersionNumber { get; }

    public StorageObjectKey StorageObjectKey { get; private set; }

    public Checksum Checksum { get; }

    public long FileSizeBytes { get; }

    public MimeType MimeType { get; }

    public StorageProviderType StorageProviderType { get; private set; }

    public UserAccountId UploadedByUserId { get; }

    public DateTimeOffset UploadedAtUtc { get; }

    internal FileVersion(
        FileVersionId id,
        int versionNumber,
        StorageObjectKey storageObjectKey,
        Checksum checksum,
        long fileSizeBytes,
        MimeType mimeType,
        StorageProviderType storageProviderType,
        UserAccountId uploadedByUserId,
        DateTimeOffset uploadedAtUtc)
        : base(id)
    {
        VersionNumber = versionNumber;
        StorageObjectKey = storageObjectKey;
        Checksum = checksum;
        FileSizeBytes = fileSizeBytes;
        MimeType = mimeType;
        StorageProviderType = storageProviderType;
        UploadedByUserId = uploadedByUserId;
        UploadedAtUtc = uploadedAtUtc;
    }

    /// <summary>
    /// Relocates this version's identical content to a different provider/key -- the
    /// caller (<see cref="StoredFile.MigrateCurrentVersionToProvider"/>) has already
    /// verified the migrated copy's own checksum matches <see cref="Checksum"/> before
    /// calling this, so this method only ever records a location change, never a content
    /// change.
    /// </summary>
    internal void MigrateStorageLocation(StorageProviderType newProviderType, StorageObjectKey newStorageObjectKey)
    {
        StorageProviderType = newProviderType;
        StorageObjectKey = newStorageObjectKey;
    }
}
