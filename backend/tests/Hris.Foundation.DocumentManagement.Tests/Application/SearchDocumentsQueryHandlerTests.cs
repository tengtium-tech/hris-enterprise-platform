using FluentAssertions;
using Hris.Application.Pagination;
using Hris.Foundation.DocumentManagement.Application.Queries;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class SearchDocumentsQueryHandlerTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly SearchDocumentsQueryHandler _handler;

    public SearchDocumentsQueryHandlerTests()
    {
        _handler = new SearchDocumentsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_AndMapsEveryMatchingDocument_ToASummary()
    {
        var document = TestData.DraftDocument();
        _repository.SearchAsync(
            TestData.TenantId, null, null, null, null, 0, 20, Arg.Any<CancellationToken>())
            .Returns(([document], 1));

        var result = await _handler.Handle(
            new SearchDocumentsQuery(TestData.TenantId, null, null, null, null, new PageRequest(1, 20)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items.Should().ContainSingle(dto => dto.DocumentId == document.Id.Value).Subject;
        dto.Title.Should().Be(document.Title);
        dto.Category.Should().Be(document.Category);
        dto.Classification.Should().Be(document.Classification.ToString());
        dto.Status.Should().Be(document.Status.ToString());
        dto.VersionCount.Should().Be(0);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_Fails_WhenPageSizeExceedsTheMaximum_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(
            new SearchDocumentsQuery(TestData.TenantId, null, null, null, null, new PageRequest(1, 500)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().SearchAsync(
            Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<DocumentClassification?>(), Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<string>?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenClassificationIsNotARecognizedValue()
    {
        var result = await _handler.Handle(
            new SearchDocumentsQuery(TestData.TenantId, null, "NotARealClassification", null, null, new PageRequest(1, 20)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidClassification);
    }
}
