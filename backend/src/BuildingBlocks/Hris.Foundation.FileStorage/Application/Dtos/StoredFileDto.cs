namespace Hris.Foundation.FileStorage.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetStoredFileQuery</c>/<c>ListStoredFilesQuery</c> return,
/// per dto-design.md's own convention.
/// </summary>
public sealed record StoredFileDto(
    Guid StoredFileId,
    string ContainerName,
    string OriginalFileName,
    string Status,
    FileVersionDto? CurrentVersion,
    int VersionCount);

public sealed record FileVersionDto(
    Guid FileVersionId,
    int VersionNumber,
    string StorageObjectKey,
    string ChecksumAlgorithm,
    string ChecksumValue,
    long FileSizeBytes,
    string MimeType,
    string StorageProviderType,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAtUtc);
