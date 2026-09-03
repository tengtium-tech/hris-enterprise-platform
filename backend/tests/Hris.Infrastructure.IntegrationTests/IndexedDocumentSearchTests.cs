using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies search-framework.md's own AI Implementation Guidance directly: "Apply
/// tenant filtering to every search, including index queries. A search index is a
/// common isolation gap (CTR-ISO-001)." <see cref="IIndexedDocumentRepository.SearchAsync"/>'s
/// own remarks explain why this is raw SQL, not a LINQ predicate; this is the test that
/// actually proves the tenant filter really holds in that raw <c>WHERE</c> clause under
/// a real PostgreSQL instance -- an in-process fake repository (what
/// <c>Hris.Foundation.Search.Tests</c> uses throughout) cannot exercise this property no
/// matter how it is written, since the whole risk is about what the actual SQL text
/// does, not about anything expressible against a fake. Also covers <c>CTR-DAT-004</c>
/// (soft-deleted exclusion) and the authorization-scope-token structural hook, the two
/// other server-side filtering guarantees the same <c>WHERE</c> clause carries.
/// </summary>
public sealed class IndexedDocumentSearchTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public IndexedDocumentSearchTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SearchAsync_NeverReturnsAnotherTenantsDocuments_EvenWhenItsContentMatchesTheQuery()
    {
        var definitionId = await SeedDefinitionAsync();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await SeedDocumentAsync(definitionId, tenantA, "employee-a-0001", "Juan Dela Cruz Software Engineer");
        await SeedDocumentAsync(definitionId, tenantB, "employee-b-0001", "Juan Dela Cruz Payroll Specialist");

        using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIndexedDocumentRepository>();

        var hits = await repository.SearchAsync(tenantA, "Juan", null, [], maxResults: 50, CancellationToken.None);

        hits.Should().ContainSingle();
        hits[0].SourceEntityId.Should().Be("employee-a-0001");
    }

    [Fact]
    public async Task SearchAsync_ExcludesRemovedDocuments_ByDefault()
    {
        var definitionId = await SeedDefinitionAsync();
        var tenantId = Guid.NewGuid();

        using (var seedScope = _fixture.CreateScope())
        {
            var repository = seedScope.ServiceProvider.GetRequiredService<IIndexedDocumentRepository>();
            var dbContext = seedScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            var document = IndexedDocument.Index(
                definitionId, SearchEntityType.Create("EMPLOYEE").Value, "employee-0001", tenantId, "Maria Santos", null, DateTimeOffset.UtcNow).Value;
            document.Remove(DateTimeOffset.UtcNow);

            await repository.AddAsync(document, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var scope = _fixture.CreateScope();
        var searchRepository = scope.ServiceProvider.GetRequiredService<IIndexedDocumentRepository>();

        var hits = await searchRepository.SearchAsync(tenantId, "Maria", null, [], maxResults: 50, CancellationToken.None);

        hits.Should().BeEmpty("CTR-DAT-004: soft-deleted records are excluded from default queries");
    }

    [Fact]
    public async Task SearchAsync_ExcludesDocumentsRequiringAScopeTheCallerDoesNotHave()
    {
        var definitionId = await SeedDefinitionAsync();
        var tenantId = Guid.NewGuid();

        await SeedDocumentAsync(definitionId, tenantId, "employee-restricted-0001", "Confidential Salary Directory Record", securityScopeToken: "payroll.read");
        await SeedDocumentAsync(definitionId, tenantId, "employee-open-0001", "Public Directory Entry");

        using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIndexedDocumentRepository>();

        // "Directory" appears in both documents' own content, so both are genuine
        // full-text matches -- the only thing that should differ between the two
        // searches below is which ones the caller's own scope tokens let through.
        var withoutScope = await repository.SearchAsync(tenantId, "Directory", null, [], maxResults: 50, CancellationToken.None);
        var withScope = await repository.SearchAsync(tenantId, "Directory", null, ["payroll.read"], maxResults: 50, CancellationToken.None);

        withoutScope.Should().ContainSingle(hit => hit.SourceEntityId == "employee-open-0001");
        withScope.Should().Contain(hit => hit.SourceEntityId == "employee-restricted-0001")
            .And.Contain(hit => hit.SourceEntityId == "employee-open-0001");
    }

    private async Task<SearchIndexDefinitionId> SeedDefinitionAsync()
    {
        using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISearchIndexDefinitionRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrisDbContext>();

        var entityType = SearchEntityType.Create($"EMPLOYEE_{Guid.NewGuid():N}").Value;
        IReadOnlyList<SearchFieldDefinition> fields = [new SearchFieldDefinition("FullName", true, true, false, 10)];

        var definition = SearchIndexDefinition.Register(entityType, fields, null, DateTimeOffset.UtcNow).Value;

        await repository.AddAsync(definition, CancellationToken.None).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return definition.Id;
    }

    private async Task SeedDocumentAsync(
        SearchIndexDefinitionId definitionId, Guid tenantId, string sourceEntityId, string searchableContent, string? securityScopeToken = null)
    {
        using var scope = _fixture.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIndexedDocumentRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrisDbContext>();

        var document = IndexedDocument.Index(
            definitionId, SearchEntityType.Create("EMPLOYEE").Value, sourceEntityId, tenantId, searchableContent, securityScopeToken, DateTimeOffset.UtcNow).Value;

        await repository.AddAsync(document, CancellationToken.None).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
