using FluentAssertions;
using Hris.Foundation.FileStorage.Domain;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Domain;

public sealed class StoredFileTests
{
    [Fact]
    public void RequestUpload_Succeeds_WithValidInput()
    {
        var result = StoredFile.RequestUpload("employee-documents", "resume.pdf");

        result.IsSuccess.Should().BeTrue();
        result.Value.ContainerName.Value.Should().Be("employee-documents");
        result.Value.OriginalFileName.Should().Be("resume.pdf");
        result.Value.Status.Should().Be(FileLifecycleStatus.UploadRequested);
        result.Value.CurrentVersion.Should().BeNull();
        result.Value.PendingVersion.Should().BeNull();
        result.Value.Versions.Should().BeEmpty();
        result.Value.DomainEvents.Should().BeEmpty("file-storage.md names no event for a bare upload request");
    }

    [Fact]
    public void RequestUpload_Fails_WhenContainerNameInvalid()
    {
        var result = StoredFile.RequestUpload("Not A Valid Container!", "resume.pdf");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ContainerNameInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestUpload_Fails_WhenOriginalFileNameNullOrWhitespace(string? fileName)
    {
        var result = StoredFile.RequestUpload("employee-documents", fileName);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.OriginalFileNameRequired);
    }

    [Fact]
    public void RequestUpload_Fails_WhenOriginalFileNameTooLong()
    {
        var result = StoredFile.RequestUpload("employee-documents", new string('a', 261));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.OriginalFileNameTooLong);
    }

    [Fact]
    public void MarkUploaded_Succeeds_FromUploadRequested_RaisesFileUploaded()
    {
        var storedFile = TestData.RequestedUpload();
        var key = TestData.NewStorageObjectKey();
        var checksum = TestData.NewChecksum();

        var result = storedFile.MarkUploaded(
            key, checksum, 2048, TestData.NewMimeType(), StorageProviderType.AmazonS3, TestData.UploaderUserId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Uploaded);
        storedFile.PendingVersion.Should().NotBeNull();
        storedFile.PendingVersion!.VersionNumber.Should().Be(1);
        storedFile.PendingVersion.StorageObjectKey.Should().Be(key);
        storedFile.PendingVersion.Checksum.Should().Be(checksum);
        storedFile.DomainEvents.OfType<FileUploaded>().Should().ContainSingle()
            .Which.StoredFileId.Should().Be(storedFile.Id);
    }

    [Fact]
    public void MarkUploaded_Fails_WhenNotUploadRequested()
    {
        var storedFile = TestData.UploadedFile();

        var result = storedFile.MarkUploaded(
            TestData.NewStorageObjectKey(), TestData.NewChecksum(), 2048, TestData.NewMimeType(),
            StorageProviderType.AmazonS3, TestData.UploaderUserId, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MarkUploaded_Fails_WhenFileSizeNotPositive(long fileSizeBytes)
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.MarkUploaded(
            TestData.NewStorageObjectKey(), TestData.NewChecksum(), fileSizeBytes, TestData.NewMimeType(),
            StorageProviderType.AmazonS3, TestData.UploaderUserId, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.FileSizeMustBePositive);
    }

    [Fact]
    public void VerifyIntegrity_Succeeds_WhenChecksumMatches_RaisesFileValidated()
    {
        var checksum = TestData.NewChecksum();
        var storedFile = TestData.UploadedFile(checksum);

        var result = storedFile.VerifyIntegrity(checksum, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Validated);
        storedFile.DomainEvents.OfType<FileValidated>().Should().ContainSingle();
    }

    [Fact]
    public void VerifyIntegrity_Fails_WhenChecksumMismatches_StaysUploaded_RaisesNoEvent()
    {
        var storedFile = TestData.UploadedFile(TestData.NewChecksum(new string('a', 64)));

        var result = storedFile.VerifyIntegrity(TestData.NewChecksum(new string('b', 64)), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumMismatch);
        storedFile.Status.Should().Be(FileLifecycleStatus.Uploaded, "a mismatch permits retry, not automatic abandonment");
        storedFile.DomainEvents.OfType<FileValidated>().Should().BeEmpty();
    }

    [Fact]
    public void VerifyIntegrity_Fails_WhenNoPendingVersion()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.VerifyIntegrity(TestData.NewChecksum(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Fact]
    public void MarkStored_Succeeds_FromValidated_PromotesVersion_RaisesFileStored()
    {
        var storedFile = TestData.ValidatedFile();

        var result = storedFile.MarkStored(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Available);
        storedFile.PendingVersion.Should().BeNull();
        storedFile.CurrentVersion.Should().NotBeNull();
        storedFile.Versions.Should().ContainSingle().Which.Should().BeSameAs(storedFile.CurrentVersion);
        storedFile.DomainEvents.OfType<FileStored>().Should().ContainSingle()
            .Which.VersionNumber.Should().Be(1);
    }

    [Fact]
    public void MarkStored_Fails_WhenNotValidated()
    {
        var storedFile = TestData.UploadedFile();

        var result = storedFile.MarkStored(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Fact]
    public void RequestNewVersionUpload_Succeeds_FromAvailable_KeepsCurrentVersion()
    {
        var storedFile = TestData.AvailableFile();
        var previousCurrent = storedFile.CurrentVersion;

        var result = storedFile.RequestNewVersionUpload();

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.UploadRequested);
        storedFile.CurrentVersion.Should().BeSameAs(previousCurrent, "the prior version keeps serving until the new one completes");
    }

    [Fact]
    public void RequestNewVersionUpload_Fails_WhenNotAvailable()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.RequestNewVersionUpload();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Fact]
    public void SecondVersionUpload_ProducesVersionTwo_PreservesVersionOneInHistory()
    {
        var storedFile = TestData.AvailableFile();
        var firstVersionId = storedFile.CurrentVersion!.Id;

        storedFile.RequestNewVersionUpload();
        var secondChecksum = TestData.NewChecksum(new string('c', 64));
        storedFile.MarkUploaded(
            TestData.NewStorageObjectKey(), secondChecksum, 4096, TestData.NewMimeType(),
            StorageProviderType.AmazonS3, TestData.UploaderUserId, TestData.NowUtc);
        storedFile.VerifyIntegrity(secondChecksum, TestData.NowUtc);
        storedFile.MarkStored(TestData.NowUtc);

        storedFile.Versions.Should().HaveCount(2);
        storedFile.Versions[0].Id.Should().Be(firstVersionId);
        storedFile.CurrentVersion.Should().BeSameAs(storedFile.Versions[1]);
        storedFile.CurrentVersion!.VersionNumber.Should().Be(2);
    }

    [Fact]
    public void RecordDownload_Succeeds_WhenAvailable_RaisesFileDownloaded()
    {
        var storedFile = TestData.AvailableFile();
        var downloaderUserId = new Hris.Foundation.Identity.Domain.UserAccountId(Guid.NewGuid());

        var result = storedFile.RecordDownload(downloaderUserId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Available, "downloading never changes lifecycle status");
        storedFile.DomainEvents.OfType<FileDownloaded>().Should().ContainSingle()
            .Which.DownloadedByUserId.Should().Be(downloaderUserId);
    }

    [Fact]
    public void RecordDownload_Fails_WhenNotAvailable()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.RecordDownload(TestData.UploaderUserId, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.NoCurrentVersion);
    }

    [Fact]
    public void Archive_Succeeds_FromAvailable_RaisesFileArchived()
    {
        var storedFile = TestData.AvailableFile();

        var result = storedFile.Archive(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Archived);
        storedFile.DomainEvents.OfType<FileArchived>().Should().ContainSingle();
    }

    [Fact]
    public void Archive_Fails_WhenNotAvailable()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.Archive(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Fact]
    public void Restore_Succeeds_FromArchived_RaisesFileRestored()
    {
        var storedFile = TestData.ArchivedFile();

        var result = storedFile.Restore(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Available);
        storedFile.DomainEvents.OfType<FileRestored>().Should().ContainSingle();
    }

    [Fact]
    public void Restore_Fails_WhenNotArchived()
    {
        var storedFile = TestData.AvailableFile();

        var result = storedFile.Restore(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Fact]
    public void Delete_Succeeds_FromAvailable()
    {
        var storedFile = TestData.AvailableFile();

        var result = storedFile.Delete(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Deleted);
        storedFile.DomainEvents.OfType<FileDeleted>().Should().ContainSingle();
    }

    [Fact]
    public void Delete_Succeeds_FromArchived()
    {
        var storedFile = TestData.ArchivedFile();

        var result = storedFile.Delete(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Deleted);
    }

    [Fact]
    public void Delete_Succeeds_FromAbandonedInProgressUpload()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.Delete(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue("an upload that will never complete still needs cleanup");
        storedFile.Status.Should().Be(FileLifecycleStatus.Deleted);
    }

    [Fact]
    public void Delete_IsTerminal_AndFailsWhenAlreadyDeleted()
    {
        var storedFile = TestData.DeletedFile();

        var result = storedFile.Delete(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.InvalidFileLifecycleTransition);
    }

    [Fact]
    public void ReverifyIntegrity_Succeeds_WhenMatches_RaisesFileIntegrityVerifiedTrue()
    {
        var checksum = TestData.NewChecksum();
        var storedFile = TestData.AvailableFile(checksum);

        var result = storedFile.ReverifyIntegrity(checksum, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.DomainEvents.OfType<FileIntegrityVerified>().Should().ContainSingle()
            .Which.Matched.Should().BeTrue();
    }

    [Fact]
    public void ReverifyIntegrity_Fails_WhenMismatch_ButStillRaisesEvent()
    {
        var storedFile = TestData.AvailableFile(TestData.NewChecksum(new string('a', 64)));

        var result = storedFile.ReverifyIntegrity(TestData.NewChecksum(new string('b', 64)), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.IntegrityCheckFailed);
        storedFile.Status.Should().Be(FileLifecycleStatus.Available, "this framework names no distinct corrupted status");
        storedFile.DomainEvents.OfType<FileIntegrityVerified>().Should().ContainSingle()
            .Which.Matched.Should().BeFalse("detecting and reporting corruption is this method's entire purpose");
    }

    [Fact]
    public void ReverifyIntegrity_Fails_WhenNoCurrentVersion()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.ReverifyIntegrity(TestData.NewChecksum(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.NoCurrentVersion);
    }

    [Fact]
    public void MigrateCurrentVersionToProvider_Succeeds_WhenChecksumMatches_RaisesStorageProviderChanged()
    {
        var checksum = TestData.NewChecksum();
        var storedFile = TestData.AvailableFile(checksum);
        var newKey = TestData.NewStorageObjectKey("archive/migrated.pdf");

        var result = storedFile.MigrateCurrentVersionToProvider(StorageProviderType.AzureBlobStorage, newKey, checksum, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        storedFile.CurrentVersion!.StorageProviderType.Should().Be(StorageProviderType.AzureBlobStorage);
        storedFile.CurrentVersion.StorageObjectKey.Should().Be(newKey);
        storedFile.DomainEvents.OfType<StorageProviderChanged>().Should().ContainSingle()
            .Which.ToProvider.Should().Be(StorageProviderType.AzureBlobStorage);
    }

    [Fact]
    public void MigrateCurrentVersionToProvider_Fails_WhenMigratedChecksumMismatches()
    {
        var storedFile = TestData.AvailableFile(TestData.NewChecksum(new string('a', 64)));

        var result = storedFile.MigrateCurrentVersionToProvider(
            StorageProviderType.AzureBlobStorage,
            TestData.NewStorageObjectKey(),
            TestData.NewChecksum(new string('b', 64)),
            TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.MigratedChecksumMismatch);
    }

    [Fact]
    public void MigrateCurrentVersionToProvider_Fails_WhenNoCurrentVersion()
    {
        var storedFile = TestData.RequestedUpload();

        var result = storedFile.MigrateCurrentVersionToProvider(
            StorageProviderType.AzureBlobStorage, TestData.NewStorageObjectKey(), TestData.NewChecksum(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.NoCurrentVersion);
    }
}
