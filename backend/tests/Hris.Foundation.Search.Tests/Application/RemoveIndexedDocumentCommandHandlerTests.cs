using FluentAssertions;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class RemoveIndexedDocumentCommandHandlerTests
{
    private readonly IIndexedDocumentRepository _repository = Substitute.For<IIndexedDocumentRepository>();
    private readonly RemoveIndexedDocumentCommandHandler _handler;

    public RemoveIndexedDocumentCommandHandlerTests()
    {
        _handler = new RemoveIndexedDocumentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDocumentExists()
    {
        var document = TestData.IndexedDoc();
        _repository.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.Handle(new RemoveIndexedDocumentCommand(document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(IndexedDocumentStatus.Removed);
    }

    [Fact]
    public async Task Handle_Fails_WhenDocumentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IndexedDocumentId>(), Arg.Any<CancellationToken>()).Returns((IndexedDocument?)null);

        var result = await _handler.Handle(new RemoveIndexedDocumentCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.IndexedDocumentNotFound);
    }
}
