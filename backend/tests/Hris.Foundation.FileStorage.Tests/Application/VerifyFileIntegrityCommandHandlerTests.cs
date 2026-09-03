using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Commands;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class VerifyFileIntegrityCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly VerifyFileIntegrityCommandHandler _handler;

    public VerifyFileIntegrityCommandHandlerTests()
    {
        _handler = new VerifyFileIntegrityCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenChecksumMatches()
    {
        var checksum = new string('a', 64);
        var storedFile = TestData.UploadedFile(TestData.NewChecksum(checksum));
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new VerifyFileIntegrityCommand(storedFile.Id.Value, checksum), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storedFile.Status.Should().Be(FileLifecycleStatus.Validated);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new VerifyFileIntegrityCommand(Guid.NewGuid(), new string('a', 64)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenActualChecksumValueIsMalformed()
    {
        var storedFile = TestData.UploadedFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new VerifyFileIntegrityCommand(storedFile.Id.Value, "not-hex"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueInvalidLength);
    }
}

public sealed class ReverifyFileIntegrityCommandHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly ReverifyFileIntegrityCommandHandler _handler;

    public ReverifyFileIntegrityCommandHandlerTests()
    {
        _handler = new ReverifyFileIntegrityCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCurrentVersionChecksumStillMatches()
    {
        var checksum = new string('a', 64);
        var storedFile = TestData.AvailableFile(TestData.NewChecksum(checksum));
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new ReverifyFileIntegrityCommand(storedFile.Id.Value, checksum), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenCorruptionDetected()
    {
        var storedFile = TestData.AvailableFile(TestData.NewChecksum(new string('a', 64)));
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(
            new ReverifyFileIntegrityCommand(storedFile.Id.Value, new string('b', 64)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.IntegrityCheckFailed);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new ReverifyFileIntegrityCommand(Guid.NewGuid(), new string('a', 64)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenActualChecksumValueIsMalformed()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new ReverifyFileIntegrityCommand(storedFile.Id.Value, "not-hex"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ChecksumValueInvalidLength);
    }
}
