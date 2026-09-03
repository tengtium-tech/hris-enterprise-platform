using Hris.Foundation.Search.Domain;

namespace Hris.Foundation.Search.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid OtherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static SearchEntityType NewEntityType(string? value = null) => SearchEntityType.Create(value ?? "EMPLOYEE").Value;

    public static IReadOnlyList<SearchFieldDefinition> NewFields() =>
    [
        new SearchFieldDefinition("FullName", IsSearchable: true, IsSortable: true, IsFilterable: false, Weight: 10),
        new SearchFieldDefinition("Department", IsSearchable: true, IsSortable: false, IsFilterable: true, Weight: 5),
    ];

    public static SearchIndexDefinition RegisteredDefinition(
        SearchEntityType? entityType = null, IReadOnlyList<SearchFieldDefinition>? fields = null, string? securityScopeKey = null) =>
        SearchIndexDefinition.Register(entityType ?? NewEntityType(), fields ?? NewFields(), securityScopeKey, NowUtc).Value;

    public static IndexedDocument IndexedDoc(
        SearchIndexDefinitionId? definitionId = null,
        SearchEntityType? sourceEntityType = null,
        string sourceEntityId = "employee-0001",
        Guid? tenantId = null,
        string searchableContent = "Juan Dela Cruz Software Engineer",
        string? securityScopeToken = null,
        DateTimeOffset? nowUtc = null) =>
        IndexedDocument.Index(
            definitionId ?? new SearchIndexDefinitionId(Guid.NewGuid()),
            sourceEntityType ?? NewEntityType(),
            sourceEntityId,
            tenantId ?? TenantId,
            searchableContent,
            securityScopeToken,
            nowUtc ?? NowUtc).Value;

    public static SearchExecution RequestedExecution(
        Guid? tenantId = null, Guid? requestedByUserId = null, string queryText = "Juan", string? domainFilter = null, DateTimeOffset? nowUtc = null) =>
        SearchExecution.Request(tenantId ?? TenantId, requestedByUserId ?? UserId, queryText, domainFilter, nowUtc ?? NowUtc).Value;

    public static SavedSearch SavedSearchFor(
        Guid? tenantId = null,
        Guid? ownerUserId = null,
        string name = "My employees",
        string queryText = "Engineer",
        string? domainFilter = null,
        DateTimeOffset? nowUtc = null) =>
        SavedSearch.Save(tenantId ?? TenantId, ownerUserId ?? UserId, name, queryText, domainFilter, nowUtc ?? NowUtc).Value;
}
