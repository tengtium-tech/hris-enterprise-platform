using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Queries;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class GetStoredFileQueryHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly GetStoredFileQueryHandler _handler;

    public GetStoredFileQueryHandlerTests()
    {
        _handler = new GetStoredFileQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheDto_WhenStoredFileExists()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new GetStoredFileQuery(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StoredFileId.Should().Be(storedFile.Id.Value);
        result.Value.CurrentVersion.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new GetStoredFileQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
