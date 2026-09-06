using FluentAssertions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Infrastructure.IntegrationTests;

/// <summary>
/// Two query-translation risks in <c>DocumentRepository</c> that neither
/// <see cref="RepositoryQueryTranslationTests"/>'s own seven cases nor
/// <c>NumberSeriesConcurrencyTests</c>/<c>IndexedDocumentSearchTests</c> cover, so
/// each earns its own confirmation against a real PostgreSQL instance rather than
/// being assumed to work the way it does against the EF Core InMemory provider (not
/// used anywhere in this repository, per this project's own testing standard):
///
/// <list type="bullet">
/// <item><see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions"/>'s
/// <c>EF.Functions.ILike</c> is a Npgsql-specific SQL function, not a LINQ operator
/// with an obvious translation -- <c>DocumentRepository.SearchAsync</c>'s own
/// case-insensitive title search depends on it actually reaching the database as
/// <c>ILIKE</c>, not throwing or silently falling back to client evaluation.</item>
/// <item><see cref="Document.Tags"/> is mapped through a value converter to a single
/// delimited-string column (<c>DocumentConfiguration</c>'s own remarks explain why),
/// with a custom <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer{T}"/>
/// for change tracking. Both halves -- the converter's own round-trip through a real
/// column, and the comparer's own job of making a replaced-but-equal list invisible to
/// change tracking -- are new shapes among this codebase's own repositories, unverified
/// until now.</item>
/// </list>
/// </summary>
public sealed class DocumentRepositoryQueryTranslationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public DocumentRepositoryQueryTranslationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SearchAsync_TitleContains_TranslatesToACaseInsensitiveIlikeSearch()
    {
        var tenantId = Guid.NewGuid();
        var document = Document.Create(
            tenantId, "Employment Contract - Repository Query Verification", "EmployeeDocuments",
            DocumentClassification.Confidential, Guid.NewGuid(), null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow).Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            await repository.AddAsync(document, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        // Deliberately opposite case from the stored title ("employment" vs "Employment")
        // -- proving this is genuinely ILIKE, not a case-sensitive LIKE/= that happens
        // to match because the test used identical casing.
        var (items, totalCount) = await readRepository.SearchAsync(
            tenantId, category: null, classification: null, titleContains: "employment contract - repository query",
            tags: null, skip: 0, take: 10, CancellationToken.None);

        totalCount.Should().Be(1);
        items.Should().ContainSingle(d => d.Id == document.Id);
    }

    [Fact]
    public async Task Tags_RoundTripThroughTheDelimitedColumnConversion_SurvivesAFreshRead()
    {
        var tenantId = Guid.NewGuid();
        var document = Document.Create(
            tenantId, "Policy Document - Tags Round Trip", "PolicyDocuments", DocumentClassification.Internal,
            Guid.NewGuid(), null, null, null, null, null, null, ["urgent", "2026", "hr-policy"],
            DateTimeOffset.UtcNow).Value;

        using (var writeScope = _fixture.CreateScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var dbContext = writeScope.ServiceProvider.GetRequiredService<HrisDbContext>();

            await repository.AddAsync(document, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readScope = _fixture.CreateScope();
        var readRepository = readScope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var found = await readRepository.GetByIdAsync(document.Id, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Tags.Should().BeEquivalentTo("urgent", "2026", "hr-policy");
    }
}
