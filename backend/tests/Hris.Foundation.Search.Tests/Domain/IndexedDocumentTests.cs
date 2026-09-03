using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Xunit;

namespace Hris.Foundation.Search.Tests.Domain;

public sealed class IndexedDocumentTests
{
    [Fact]
    public void Index_Succeeds_AndRaisesSearchIndexCreated()
    {
        var definitionId = new SearchIndexDefinitionId(Guid.NewGuid());
        var entityType = TestData.NewEntityType();

        var result = IndexedDocument.Index(
            definitionId, entityType, "employee-0001", TestData.TenantId, "Juan Dela Cruz", "employee.read", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.SearchIndexDefinitionId.Should().Be(definitionId);
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.SourceEntityType.Should().Be(entityType);
        result.Value.SourceEntityId.Should().Be("employee-0001");
        result.Value.SearchableContent.Should().Be("Juan Dela Cruz");
        result.Value.SecurityScopeToken.Should().Be("employee.read");
        result.Value.Status.Should().Be(IndexedDocumentStatus.Indexed);
        result.Value.DomainEvents.OfType<SearchIndexCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Index_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => IndexedDocument.Index(
            new SearchIndexDefinitionId(Guid.NewGuid()), TestData.NewEntityType(), "employee-0001", Guid.Empty, "content", null, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Index_Fails_WhenSourceEntityIdIsMissing()
    {
        var result = IndexedDocument.Index(
            new SearchIndexDefinitionId(Guid.NewGuid()), TestData.NewEntityType(), " ", TestData.TenantId, "content", null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SourceEntityIdRequired);
    }

    [Fact]
    public void Index_Fails_WhenSearchableContentIsMissing()
    {
        var result = IndexedDocument.Index(
            new SearchIndexDefinitionId(Guid.NewGuid()), TestData.NewEntityType(), "employee-0001", TestData.TenantId, " ", null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchableContentRequired);
    }

    [Fact]
    public void UpdateContent_Succeeds_AndRaisesSearchIndexUpdated()
    {
        var document = TestData.IndexedDoc();

        var result = document.UpdateContent("Juan Dela Cruz, Senior Engineer", "employee.read.v2", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.SearchableContent.Should().Be("Juan Dela Cruz, Senior Engineer");
        document.SecurityScopeToken.Should().Be("employee.read.v2");
        document.DomainEvents.OfType<SearchIndexUpdated>().Should().ContainSingle();
    }

    [Fact]
    public void UpdateContent_Fails_AfterRemoval()
    {
        var document = TestData.IndexedDoc();
        document.Remove(TestData.NowUtc);

        var result = document.UpdateContent("New content", null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidIndexedDocumentTransition);
    }

    [Fact]
    public void Remove_Succeeds_AndRaisesNoEvent()
    {
        var document = TestData.IndexedDoc();

        var result = document.Remove(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(IndexedDocumentStatus.Removed);
        document.DomainEvents.Should().ContainSingle("search-framework.md's own Domain Events list names no removal event")
            .Which.Should().BeOfType<SearchIndexCreated>();
    }

    [Fact]
    public void Remove_Fails_WhenAlreadyRemoved()
    {
        var document = TestData.IndexedDoc();
        document.Remove(TestData.NowUtc);

        var result = document.Remove(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidIndexedDocumentTransition);
    }
}
