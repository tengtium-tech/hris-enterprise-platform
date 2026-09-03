using FluentAssertions;
using Hris.Foundation.Search.Application.Queries;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class GetSearchIndexDefinitionQueryHandlerTests
{
    private readonly ISearchIndexDefinitionRepository _repository = Substitute.For<ISearchIndexDefinitionRepository>();
    private readonly GetSearchIndexDefinitionQueryHandler _handler;

    public GetSearchIndexDefinitionQueryHandlerTests()
    {
        _handler = new GetSearchIndexDefinitionQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDefinitionExists()
    {
        var definition = TestData.RegisteredDefinition(TestData.NewEntityType("Employee"));
        _repository.GetByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(new GetSearchIndexDefinitionQuery("Employee"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityType.Should().Be("EMPLOYEE");
    }

    [Fact]
    public async Task Handle_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns((SearchIndexDefinition?)null);

        var result = await _handler.Handle(new GetSearchIndexDefinitionQuery("Employee"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchIndexDefinitionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenEntityTypeIsInvalid()
    {
        var result = await _handler.Handle(new GetSearchIndexDefinitionQuery(string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.EntityTypeRequired);
    }
}
