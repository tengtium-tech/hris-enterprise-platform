using Hris.Foundation.FileStorage.Domain;
using Hris.Foundation.Identity.Domain;

namespace Hris.Foundation.FileStorage.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly UserAccountId UploaderUserId = new(Guid.NewGuid());

    public static ContainerName NewContainerName(string? value = null) =>
        ContainerName.Create(value ?? "employee-documents").Value;

    public static StorageObjectKey NewStorageObjectKey(string? value = null) =>
        StorageObjectKey.Create(value ?? $"employee-documents/{Guid.NewGuid():N}.pdf").Value;

    public static Checksum NewChecksum(string? value = null) =>
        Checksum.Create(ChecksumAlgorithm.Sha256, value ?? new string('a', 64)).Value;

    public static MimeType NewMimeType(string? value = null) =>
        MimeType.Create(value ?? "application/pdf").Value;

    /// <summary>A file with an upload requested, no content facts known yet.</summary>
    public static StoredFile RequestedUpload(string? containerName = null, string? originalFileName = null) =>
        StoredFile.RequestUpload(containerName ?? "employee-documents", originalFileName ?? "resume.pdf").Value;

    /// <summary>A file whose upload has been confirmed (<see cref="FileLifecycleStatus.Uploaded"/>).</summary>
    public static StoredFile UploadedFile(
        Checksum? checksum = null,
        long fileSizeBytes = 2048,
        StorageProviderType storageProviderType = StorageProviderType.AmazonS3,
        DateTimeOffset? nowUtc = null)
    {
        var storedFile = RequestedUpload();
        storedFile.MarkUploaded(
            NewStorageObjectKey(),
            checksum ?? NewChecksum(),
            fileSizeBytes,
            NewMimeType(),
            storageProviderType,
            UploaderUserId,
            nowUtc ?? NowUtc);
        return storedFile;
    }

    /// <summary>A file whose pending version has passed upload-time integrity verification.</summary>
    public static StoredFile ValidatedFile(Checksum? checksum = null, DateTimeOffset? nowUtc = null)
    {
        var actualChecksum = checksum ?? NewChecksum();
        var storedFile = UploadedFile(actualChecksum, nowUtc: nowUtc);
        storedFile.VerifyIntegrity(actualChecksum, nowUtc ?? NowUtc);
        return storedFile;
    }

    /// <summary>A file with a current, <see cref="FileLifecycleStatus.Available"/> version.</summary>
    public static StoredFile AvailableFile(Checksum? checksum = null, DateTimeOffset? nowUtc = null)
    {
        var storedFile = ValidatedFile(checksum, nowUtc);
        storedFile.MarkStored(nowUtc ?? NowUtc);
        return storedFile;
    }

    /// <summary>An <see cref="FileLifecycleStatus.Archived"/> file.</summary>
    public static StoredFile ArchivedFile(DateTimeOffset? nowUtc = null)
    {
        var storedFile = AvailableFile(nowUtc: nowUtc);
        storedFile.Archive(nowUtc ?? NowUtc);
        return storedFile;
    }

    /// <summary>A <see cref="FileLifecycleStatus.Deleted"/> file.</summary>
    public static StoredFile DeletedFile(DateTimeOffset? nowUtc = null)
    {
        var storedFile = AvailableFile(nowUtc: nowUtc);
        storedFile.Delete(nowUtc ?? NowUtc);
        return storedFile;
    }
}
