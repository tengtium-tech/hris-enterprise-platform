using Hris.Foundation.FileStorage.Application.Dtos;
using Hris.Foundation.FileStorage.Domain;

namespace Hris.Foundation.FileStorage.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4 framework's own mapper already establishes.
/// </summary>
internal static class FileStorageMapper
{
    public static StoredFileDto ToDto(StoredFile storedFile) => new(
        storedFile.Id.Value,
        storedFile.ContainerName.Value,
        storedFile.OriginalFileName,
        storedFile.Status.ToString(),
        storedFile.CurrentVersion is null ? null : ToDto(storedFile.CurrentVersion),
        storedFile.Versions.Count);

    public static FileVersionDto ToDto(FileVersion fileVersion) => new(
        fileVersion.Id.Value,
        fileVersion.VersionNumber,
        fileVersion.StorageObjectKey.Value,
        fileVersion.Checksum.Algorithm.ToString(),
        fileVersion.Checksum.Value,
        fileVersion.FileSizeBytes,
        fileVersion.MimeType.Value,
        fileVersion.StorageProviderType.ToString(),
        fileVersion.UploadedByUserId.Value,
        fileVersion.UploadedAtUtc);
}
