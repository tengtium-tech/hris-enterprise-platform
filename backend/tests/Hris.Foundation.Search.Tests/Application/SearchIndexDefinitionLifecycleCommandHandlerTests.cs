using FluentAssertions;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class SearchIndexDefinitionLifecycleCommandHandlerTests
{
    private readonly ISearchIndexDefinitionRepository _repository = Substitute.For<ISearchIndexDefinitionRepository>();

    [Fact]
    public async Task UpdateSearchIndexFields_Succeeds_WhenDefinitionExists()
    {
        var definition = TestData.RegisteredDefinition();
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new UpdateSearchIndexFieldsCommandHandler(_repository);

        var newFields = new[] { new SearchFieldDefinition("Email", true, false, false, 8) };
        var result = await handler.Handle(
            new UpdateSearchIndexFieldsCommand(definition.Id.Value, newFields, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.Fields.Should().BeEquivalentTo(newFields);
    }

    [Fact]
    public async Task UpdateSearchIndexFields_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<SearchIndexDefinitionId>(), Arg.Any<CancellationToken>()).Returns((SearchIndexDefinition?)null);
        var handler = new UpdateSearchIndexFieldsCommandHandler(_repository);

        var result = await handler.Handle(
            new UpdateSearchIndexFieldsCommand(Guid.NewGuid(), TestData.NewFields(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchIndexDefinitionNotFound);
    }

    [Fact]
    public async Task CompleteIndexRebuild_Succeeds_AndRecordsTheDocumentCount()
    {
        var definition = TestData.RegisteredDefinition();
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new CompleteIndexRebuildCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CompleteIndexRebuildCommand(definition.Id.Value, 500), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.LastRebuiltAtUtc.Should().Be(TestData.NowUtc);
        definition.DomainEvents.OfType<SearchIndexRebuilt>().Should().ContainSingle(e => e.DocumentCount == 500);
    }

    [Fact]
    public async Task CompleteIndexRebuild_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<SearchIndexDefinitionId>(), Arg.Any<CancellationToken>()).Returns((SearchIndexDefinition?)null);
        var handler = new CompleteIndexRebuildCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CompleteIndexRebuildCommand(Guid.NewGuid(), 500), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchIndexDefinitionNotFound);
    }
}
