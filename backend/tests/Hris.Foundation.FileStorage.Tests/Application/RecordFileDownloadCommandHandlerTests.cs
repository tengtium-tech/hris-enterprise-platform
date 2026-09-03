using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class RecordFileDownloadCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly RecordFileDownloadCommandHandler _handler;

    public RecordFileDownloadCommandHandlerTests()
    {
        _handler = new RecordFileDownloadCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAvailable()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new RecordFileDownloadCommand(storedFile.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenNoCurrentVersion()
    {
        var storedFile = TestData.RequestedUpload();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new RecordFileDownloadCommand(storedFile.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.NoCurrentVersion);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new RecordFileDownloadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
