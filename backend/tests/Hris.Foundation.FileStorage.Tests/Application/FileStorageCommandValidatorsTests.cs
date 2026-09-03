using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Application.Queries;
using Hris.Foundation.FileStorage.Application.Validators;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>ExtensionCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class FileStorageCommandValidatorsTests
{
    [Fact]
    public void RequestFileUploadCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyContainerName()
    {
        var validator = new RequestFileUploadCommandValidator();
        var valid = new RequestFileUploadCommand("employee-documents", "resume.pdf");
        var invalid = valid with { ContainerName = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RequestNewFileVersionUploadCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RequestNewFileVersionUploadCommandValidator();

        validator.Validate(new RequestNewFileVersionUploadCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RequestNewFileVersionUploadCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmFileUploadCommandValidator_AcceptsAValidCommand_AndRejectsAnInvalidFileSize()
    {
        var validator = new ConfirmFileUploadCommandValidator();
        var valid = new ConfirmFileUploadCommand(
            Guid.NewGuid(), "employee-documents/file.pdf", new string('a', 64), 2048, "application/pdf",
            StorageProviderType.AmazonS3, Guid.NewGuid());
        var invalid = valid with { FileSizeBytes = 0 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyFileIntegrityCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyChecksum()
    {
        var validator = new VerifyFileIntegrityCommandValidator();
        var valid = new VerifyFileIntegrityCommand(Guid.NewGuid(), new string('a', 64));
        var invalid = valid with { ActualChecksumValue = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReverifyFileIntegrityCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyChecksum()
    {
        var validator = new ReverifyFileIntegrityCommandValidator();
        var valid = new ReverifyFileIntegrityCommand(Guid.NewGuid(), new string('a', 64));
        var invalid = valid with { ActualChecksumValue = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmFileStoredCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ConfirmFileStoredCommandValidator();

        validator.Validate(new ConfirmFileStoredCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ConfirmFileStoredCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveFileCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ArchiveFileCommandValidator();

        validator.Validate(new ArchiveFileCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ArchiveFileCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RestoreFileCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RestoreFileCommandValidator();

        validator.Validate(new RestoreFileCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RestoreFileCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeleteFileCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new DeleteFileCommandValidator();

        validator.Validate(new DeleteFileCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new DeleteFileCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordFileDownloadCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RecordFileDownloadCommandValidator();
        var valid = new RecordFileDownloadCommand(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { StoredFileId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MigrateFileStorageProviderCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyKey()
    {
        var validator = new MigrateFileStorageProviderCommandValidator();
        var valid = new MigrateFileStorageProviderCommand(
            Guid.NewGuid(), StorageProviderType.AzureBlobStorage, "archive/file.pdf", new string('a', 64));
        var invalid = valid with { NewStorageObjectKey = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetStoredFileQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new GetStoredFileQueryValidator();

        validator.Validate(new GetStoredFileQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetStoredFileQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListStoredFilesQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyContainerName()
    {
        var validator = new ListStoredFilesQueryValidator();
        var valid = new ListStoredFilesQuery("employee-documents");
        var invalid = valid with { ContainerName = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListFileVersionsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new ListFileVersionsQueryValidator();

        validator.Validate(new ListFileVersionsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ListFileVersionsQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
