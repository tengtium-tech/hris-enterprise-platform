using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class ConfirmFileStoredCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly ConfirmFileStoredCommandHandler _handler;

    public ConfirmFileStoredCommandHandlerTests()
    {
        _handler = new ConfirmFileStoredCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenValidated()
    {
        var storedFile = TestData.ValidatedFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new ConfirmFileStoredCommand(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Available);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new ConfirmFileStoredCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
