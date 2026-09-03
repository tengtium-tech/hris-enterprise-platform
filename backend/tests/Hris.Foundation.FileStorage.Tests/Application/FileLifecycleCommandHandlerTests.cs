using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class ArchiveFileCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly ArchiveFileCommandHandler _handler;

    public ArchiveFileCommandHandlerTests()
    {
        _handler = new ArchiveFileCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAvailable()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new ArchiveFileCommand(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Archived);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new ArchiveFileCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}

public sealed class RestoreFileCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly RestoreFileCommandHandler _handler;

    public RestoreFileCommandHandlerTests()
    {
        _handler = new RestoreFileCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenArchived()
    {
        var storedFile = TestData.ArchivedFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new RestoreFileCommand(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Available);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new RestoreFileCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}

public sealed class DeleteFileCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly DeleteFileCommandHandler _handler;

    public DeleteFileCommandHandlerTests()
    {
        _handler = new DeleteFileCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAvailable()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new DeleteFileCommand(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Deleted);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new DeleteFileCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
