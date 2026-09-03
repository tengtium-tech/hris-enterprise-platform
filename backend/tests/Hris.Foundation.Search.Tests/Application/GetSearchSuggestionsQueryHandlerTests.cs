using FluentAssertions;
using Hris.Foundation.Search.Application.Queries;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class GetSearchSuggestionsQueryHandlerTests
{
    private readonly ISavedSearchRepository _repository = Substitute.For<ISavedSearchRepository>();
    private readonly GetSearchSuggestionsQueryHandler _handler;

    public GetSearchSuggestionsQueryHandlerTests()
    {
        _handler = new GetSearchSuggestionsQueryHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_RecordsEachReturnedSavedSearchAsSuggested()
    {
        var first = TestData.SavedSearchFor(name: "First");
        var second = TestData.SavedSearchFor(name: "Second");
        _repository.ListByOwnerAsync(TestData.TenantId, TestData.UserId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SavedSearch> { first, second });

        var result = await _handler.Handle(new GetSearchSuggestionsQuery(TestData.TenantId, TestData.UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        first.SuggestedCount.Should().Be(1);
        second.SuggestedCount.Should().Be(1);
        first.DomainEvents.OfType<SearchSuggestionGenerated>().Should().ContainSingle();
        second.DomainEvents.OfType<SearchSuggestionGenerated>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyList_WhenNoSavedSearchesExist()
    {
        _repository.ListByOwnerAsync(TestData.TenantId, TestData.UserId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SavedSearch>());

        var result = await _handler.Handle(new GetSearchSuggestionsQuery(TestData.TenantId, TestData.UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
