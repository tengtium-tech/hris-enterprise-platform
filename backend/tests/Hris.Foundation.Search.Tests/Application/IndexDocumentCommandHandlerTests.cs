using FluentAssertions;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class IndexDocumentCommandHandlerTests
{
    private readonly ISearchIndexDefinitionRepository _definitionRepository = Substitute.For<ISearchIndexDefinitionRepository>();
    private readonly IIndexedDocumentRepository _documentRepository = Substitute.For<IIndexedDocumentRepository>();
    private readonly IndexDocumentCommandHandler _handler;

    public IndexDocumentCommandHandlerTests()
    {
        _handler = new IndexDocumentCommandHandler(_definitionRepository, _documentRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static IndexDocumentCommand ValidCommand() =>
        new("Employee", "employee-0001", TestData.TenantId, "Juan Dela Cruz", "employee.read");

    [Fact]
    public async Task Handle_CreatesANewDocument_WhenNoneExistsForThisSource()
    {
        var definition = TestData.RegisteredDefinition();
        _definitionRepository.GetByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(definition);
        _documentRepository.FindBySourceAsync(Arg.Any<SearchEntityType>(), "employee-0001", TestData.TenantId, Arg.Any<CancellationToken>())
            .Returns((IndexedDocument?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _documentRepository.Received(1).AddAsync(
            Arg.Is<IndexedDocument>(document => document.SearchIndexDefinitionId == definition.Id && document.SearchableContent == "Juan Dela Cruz"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesTheExistingDocument_WhenOneAlreadyExistsForThisSource()
    {
        var definition = TestData.RegisteredDefinition();
        var existing = TestData.IndexedDoc(definition.Id, sourceEntityId: "employee-0001", tenantId: TestData.TenantId);
        _definitionRepository.GetByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(definition);
        _documentRepository.FindBySourceAsync(Arg.Any<SearchEntityType>(), "employee-0001", TestData.TenantId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id.Value);
        existing.SearchableContent.Should().Be("Juan Dela Cruz");
        await _documentRepository.DidNotReceive().AddAsync(Arg.Any<IndexedDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNoDefinitionIsRegisteredForTheEntityType()
    {
        _definitionRepository.GetByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns((SearchIndexDefinition?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchIndexDefinitionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenEntityTypeIsInvalid_WithoutCallingAnyRepository()
    {
        var result = await _handler.Handle(ValidCommand() with { SourceEntityType = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.EntityTypeRequired);
        await _definitionRepository.DidNotReceive().GetByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>());
    }
}
