using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class RequestFileUploadCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly RequestFileUploadCommandHandler _handler;

    public RequestFileUploadCommandHandlerTests()
    {
        _handler = new RequestFileUploadCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewStoredFile_WhenInputIsValid()
    {
        var result = await _handler.Handle(new RequestFileUploadCommand("employee-documents", "resume.pdf"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<StoredFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenContainerNameIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(new RequestFileUploadCommand(string.Empty, "resume.pdf"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ContainerNameRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<StoredFile>(), Arg.Any<CancellationToken>());
    }
}

public sealed class RequestNewFileVersionUploadCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly RequestNewFileVersionUploadCommandHandler _handler;

    public RequestNewFileVersionUploadCommandHandlerTests()
    {
        _handler = new RequestNewFileVersionUploadCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenStoredFileIsAvailable()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new RequestNewFileVersionUploadCommand(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.UploadRequested);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new RequestNewFileVersionUploadCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
