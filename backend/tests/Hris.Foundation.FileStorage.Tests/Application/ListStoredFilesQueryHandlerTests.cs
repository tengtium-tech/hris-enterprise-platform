using FluentAssertions;
using Hris.Foundation.FileStorage.Application.Queries;
using Hris.Foundation.FileStorage.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.FileStorage.Tests.Application;

public sealed class ListStoredFilesQueryHandlerTests
{
    private readonly IStoredFileRepository _repository = Substitute.For<IStoredFileRepository>();
    private readonly ListStoredFilesQueryHandler _handler;

    public ListStoredFilesQueryHandlerTests()
    {
        _handler = new ListStoredFilesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryStoredFileInTheContainer()
    {
        IReadOnlyCollection<StoredFile> storedFiles = [TestData.RequestedUpload(), TestData.AvailableFile()];
        _repository.GetByContainerAsync(Arg.Any<ContainerName>(), Arg.Any<CancellationToken>()).Returns(storedFiles);

        var result = await _handler.Handle(new ListStoredFilesQuery("employee-documents"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Fails_WhenContainerNameIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(new ListStoredFilesQuery(string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FileStorageErrors.ContainerNameRequired);
        await _repository.DidNotReceive().GetByContainerAsync(Arg.Any<ContainerName>(), Arg.Any<CancellationToken>());
    }
}
