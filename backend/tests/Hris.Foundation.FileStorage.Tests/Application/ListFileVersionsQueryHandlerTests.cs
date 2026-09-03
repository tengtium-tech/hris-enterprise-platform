using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Queries;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class ListFileVersionsQueryHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly ListFileVersionsQueryHandler _handler;

    public ListFileVersionsQueryHandlerTests()
    {
        _handler = new ListFileVersionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheFullVersionHistory()
    {
        var storedFile = TestData.AvailableFile();
        _repository.GetByIdAsync(storedFile.Id, Arg.Any<CancellationToken>()).Returns(storedFile);

        var result = await _handler.Handle(new ListFileVersionsQuery(storedFile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Fails_WhenStoredFileDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StoredFileId>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var result = await _handler.Handle(new ListFileVersionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.StoredFileNotFound);
    }
}
