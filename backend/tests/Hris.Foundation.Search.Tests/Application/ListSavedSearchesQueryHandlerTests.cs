using FluentAssertions;
using Hris.Foundation.Search.Application.Queries;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class ListSavedSearchesQueryHandlerTests
{
    private readonly ISavedSearchRepository _repository = Substitute.For<ISavedSearchRepository>();
    private readonly ListSavedSearchesQueryHandler _handler;

    public ListSavedSearchesQueryHandlerTests()
    {
        _handler = new ListSavedSearchesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheCallersOwnSavedSearches()
    {
        var savedSearch = TestData.SavedSearchFor();
        _repository.ListByOwnerAsync(TestData.TenantId, TestData.UserId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SavedSearch> { savedSearch });

        var result = await _handler.Handle(new ListSavedSearchesQuery(TestData.TenantId, TestData.UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.SavedSearchId == savedSearch.Id.Value);
    }
}
