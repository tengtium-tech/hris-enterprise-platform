using Hris.SharedKernel;

namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class FileStorageErrors
{
    public static readonly Error ContainerNameRequired = new(
        "FileStorage.ContainerNameRequired",
        "A container name is required.",
        ErrorCategory.Validation);

    public static readonly Error ContainerNameInvalid = new(
        "FileStorage.ContainerNameInvalid",
        "A container name must be lowercase alphanumeric characters and hyphens only.",
        ErrorCategory.Validation);

    public static readonly Error ContainerNameTooLong = new(
        "FileStorage.ContainerNameTooLong",
        "A container name cannot exceed 63 characters.",
        ErrorCategory.Validation);

    public static readonly Error OriginalFileNameRequired = new(
        "FileStorage.OriginalFileNameRequired",
        "An original file name is required.",
        ErrorCategory.Validation);

    public static readonly Error OriginalFileNameTooLong = new(
        "FileStorage.OriginalFileNameTooLong",
        "An original file name cannot exceed 260 characters.",
        ErrorCategory.Validation);

    public static readonly Error MimeTypeRequired = new(
        "FileStorage.MimeTypeRequired",
        "A MIME type is required.",
        ErrorCategory.Validation);

    public static readonly Error MimeTypeInvalid = new(
        "FileStorage.MimeTypeInvalid",
        "A MIME type must be in the form type/subtype.",
        ErrorCategory.Validation);

    public static readonly Error StorageObjectKeyRequired = new(
        "FileStorage.StorageObjectKeyRequired",
        "A storage object key is required.",
        ErrorCategory.Validation);

    public static readonly Error StorageObjectKeyTooLong = new(
        "FileStorage.StorageObjectKeyTooLong",
        "A storage object key cannot exceed 1024 characters.",
        ErrorCategory.Validation);

    public static readonly Error StorageObjectKeyContainsTraversal = new(
        "FileStorage.StorageObjectKeyContainsTraversal",
        "A storage object key cannot contain a parent-directory traversal segment.",
        ErrorCategory.Validation);

    public static readonly Error ChecksumValueRequired = new(
        "FileStorage.ChecksumValueRequired",
        "A checksum value is required.",
        ErrorCategory.Validation);

    public static readonly Error ChecksumValueInvalidLength = new(
        "FileStorage.ChecksumValueInvalidLength",
        "A checksum value's length does not match its declared algorithm.",
        ErrorCategory.Validation);

    public static readonly Error ChecksumValueNotHexadecimal = new(
        "FileStorage.ChecksumValueNotHexadecimal",
        "A checksum value must be a hexadecimal string.",
        ErrorCategory.Validation);

    public static readonly Error FileSizeMustBePositive = new(
        "FileStorage.FileSizeMustBePositive",
        "A file size must be greater than zero bytes.",
        ErrorCategory.Validation);

    public static readonly Error StoredFileNotFound = new(
        "FileStorage.StoredFileNotFound",
        "No stored file exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidFileLifecycleTransition = new(
        "FileStorage.InvalidFileLifecycleTransition",
        "This transition is not valid from the file's current status.",
        ErrorCategory.Domain);

    public static readonly Error ChecksumMismatch = new(
        "FileStorage.ChecksumMismatch",
        "The uploaded content's checksum does not match the checksum recorded at upload confirmation.",
        ErrorCategory.Domain);

    public static readonly Error IntegrityCheckFailed = new(
        "FileStorage.IntegrityCheckFailed",
        "The current version's stored content no longer matches its recorded checksum.",
        ErrorCategory.Domain);

    public static readonly Error NoPendingVersion = new(
        "FileStorage.NoPendingVersion",
        "This file has no upload in progress to confirm.",
        ErrorCategory.Domain);

    public static readonly Error NoCurrentVersion = new(
        "FileStorage.NoCurrentVersion",
        "This file has no current version available.",
        ErrorCategory.Domain);

    public static readonly Error MigratedChecksumMismatch = new(
        "FileStorage.MigratedChecksumMismatch",
        "The migrated copy's checksum does not match the current version's recorded checksum.",
        ErrorCategory.Domain);
}
