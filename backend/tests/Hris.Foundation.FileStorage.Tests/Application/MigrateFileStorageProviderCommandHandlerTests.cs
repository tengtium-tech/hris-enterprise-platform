using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class MigrateFileStorageProviderCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly MigrateFileStorageProviderCommandHandler _handler;

    public MigrateFileStorageProviderCommandHandlerTests()
    {
        _handler = new MigrateFileStorageProviderCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenMigratedChecksumMatchesCurrentVersion()
    {
        var checksum = new string('a', 64);
        var storedFile = TestData.AvailableFile(TestData.NewChecksum(checksum));
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(
            new MigrateFileStorageProviderCommand(storedFile.Id.Value, StorageProviderType.AzureBlobStorage, "archive/file.pdf", checksum),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.CurrentVersion!.StorageProviderType.Should().Be(StorageProviderType.AzureBlobStorage);
    }

    [Fact]
    public async Task Handle_Fails_WhenMigratedChecksumMismatches()
    {
        var storedFile = TestData.AvailableFile(TestData.NewChecksum(new string('a', 64)));
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(
            new MigrateFileStorageProviderCommand(
                storedFile.Id.Value, StorageProviderType.AzureBlobStorage, "archive/file.pdf", new string('b', 64)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.MigratedChecksumMismatch);
    }

    [Fact]
    public async Task Handle_Fails_WhenNewStorageObjectKeyIsMalformed()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(
            new MigrateFileStorageProviderCommand(storedFile.Id.Value, StorageProviderType.AzureBlobStorage, "../escape.pdf", new string('a', 64)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StorageObjectKeyContainsTraversal);
    }

    [Fact]
    public async Task Handle_Fails_WhenMigratedContentChecksumValueIsMalformed()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(
            new MigrateFileStorageProviderCommand(storedFile.Id.Value, StorageProviderType.AzureBlobStorage, "archive/file.pdf", "not-hex"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueInvalidLength);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(
            new MigrateFileStorageProviderCommand(Guid.NewGuid(), StorageProviderType.AzureBlobStorage, "archive/file.pdf", new string('a', 64)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
