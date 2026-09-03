using FluentValidation;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Application.Queries;

namespace Hris.Foundation.FileStorage.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (container-name shape,
/// checksum length, lifecycle-state gating) -- the identical separation every other
/// framework's own validators file states for its own set.
/// </summary>
public sealed class RequestFileUploadCommandValidator : AbstractValidator<RequestFileUploadCommand>
{
    public RequestFileUploadCommandValidator()
    {
        RuleFor(c => c.ContainerName).NotEmpty();
        RuleFor(c => c.OriginalFileName).NotEmpty();
    }
}

public sealed class RequestNewFileVersionUploadCommandValidator : AbstractValidator<RequestNewFileVersionUploadCommand>
{
    public RequestNewFileVersionUploadCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
    }
}

public sealed class ConfirmFileUploadCommandValidator : AbstractValidator<ConfirmFileUploadCommand>
{
    public ConfirmFileUploadCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
        RuleFor(c => c.StorageObjectKey).NotEmpty();
        RuleFor(c => c.ChecksumValue).NotEmpty();
        RuleFor(c => c.FileSizeBytes).GreaterThan(0);
        RuleFor(c => c.MimeType).NotEmpty();
        RuleFor(c => c.UploadedByUserId).NotEmpty();
    }
}

public sealed class VerifyFileIntegrityCommandValidator : AbstractValidator<VerifyFileIntegrityCommand>
{
    public VerifyFileIntegrityCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
        RuleFor(c => c.ActualChecksumValue).NotEmpty();
    }
}

public sealed class ReverifyFileIntegrityCommandValidator : AbstractValidator<ReverifyFileIntegrityCommand>
{
    public ReverifyFileIntegrityCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
        RuleFor(c => c.ActualChecksumValue).NotEmpty();
    }
}

public sealed class ConfirmFileStoredCommandValidator : AbstractValidator<ConfirmFileStoredCommand>
{
    public ConfirmFileStoredCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
    }
}

public sealed class ArchiveFileCommandValidator : AbstractValidator<ArchiveFileCommand>
{
    public ArchiveFileCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
    }
}

public sealed class RestoreFileCommandValidator : AbstractValidator<RestoreFileCommand>
{
    public RestoreFileCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
    }
}

public sealed class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
    }
}

public sealed class RecordFileDownloadCommandValidator : AbstractValidator<RecordFileDownloadCommand>
{
    public RecordFileDownloadCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
        RuleFor(c => c.DownloadedByUserId).NotEmpty();
    }
}

public sealed class MigrateFileStorageProviderCommandValidator : AbstractValidator<MigrateFileStorageProviderCommand>
{
    public MigrateFileStorageProviderCommandValidator()
    {
        RuleFor(c => c.StoredFileId).NotEmpty();
        RuleFor(c => c.NewStorageObjectKey).NotEmpty();
        RuleFor(c => c.MigratedContentChecksumValue).NotEmpty();
    }
}

public sealed class GetStoredFileQueryValidator : AbstractValidator<GetStoredFileQuery>
{
    public GetStoredFileQueryValidator()
    {
        RuleFor(q => q.StoredFileId).NotEmpty();
    }
}

public sealed class ListStoredFilesQueryValidator : AbstractValidator<ListStoredFilesQuery>
{
    public ListStoredFilesQueryValidator()
    {
        RuleFor(q => q.ContainerName).NotEmpty();
    }
}

public sealed class ListFileVersionsQueryValidator : AbstractValidator<ListFileVersionsQuery>
{
    public ListFileVersionsQueryValidator()
    {
        RuleFor(q => q.StoredFileId).NotEmpty();
    }
}
