using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class ConfirmFileUploadCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly ConfirmFileUploadCommandHandler _handler;

    public ConfirmFileUploadCommandHandlerTests()
    {
        _handler = new ConfirmFileUploadCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static ConfirmFileUploadCommand ValidCommand(Guid storedFileId) => new(
        storedFileId, "employee-documents/file.pdf", new string('a', 64), 2048, "application/pdf",
        StorageProviderType.AmazonS3, TestData.UploaderUserId.Value);

    [Fact]
    public async Task Handle_Succeeds_WhenStoredFileExistsAndAwaitingUpload()
    {
        var storedFile = TestData.RequestedUpload();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(ValidCommand(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Uploaded);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenStorageObjectKeyIsInvalid()
    {
        var storedFile = TestData.RequestedUpload();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var command = ValidCommand(storedFile.Id.Value) with { StorageObjectKey = "../escape.pdf" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StorageObjectKeyContainsTraversal);
    }

    [Fact]
    public async Task Handle_Fails_WhenChecksumIsInvalid()
    {
        var storedFile = TestData.RequestedUpload();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var command = ValidCommand(storedFile.Id.Value) with { ChecksumValue = "not-hex" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueInvalidLength);
    }

    [Fact]
    public async Task Handle_Fails_WhenMimeTypeIsInvalid()
    {
        var storedFile = TestData.RequestedUpload();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var command = ValidCommand(storedFile.Id.Value) with { MimeType = "not-a-mime-type" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.MimeTypeInvalid);
    }
}
