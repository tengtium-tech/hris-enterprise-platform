using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Aggregate Root for one source record's own presence in the search index -- a
/// separate, population-scale Aggregate Root from <see cref="SearchIndexDefinition"/>
/// for the identical reason <c>IssuedNumber</c> is kept independent of
/// <c>NumberSeries</c>: search-framework.md's own Non-Functional Requirements state
/// "support millions of indexed records," and every one of those millions must never be
/// loaded as part of its own entity type's configuration aggregate.
///
/// <see cref="TenantId"/> is a plain <see cref="Guid"/>, not a reference to
/// <c>Hris.Foundation.Tenant.Domain.TenantId</c> -- Tenant Framework is not listed in
/// search-framework.md's own Upstream Dependencies, and this field describes the
/// calling context a caller supplies explicitly (the same "explicit, caller-supplied
/// value rather than resolved through a not-yet-wired framework" choice
/// <c>RegisterNumberSeriesCommand</c>'s own format/reset-policy already makes), not a
/// relationship to the Tenant Aggregate itself. Every query method on
/// <see cref="IIndexedDocumentRepository"/> takes a tenant id as a mandatory parameter
/// for exactly the reason the AI Implementation Guidance names first: "apply tenant
/// filtering to every search, including index queries. A search index is a common
/// isolation gap (<c>CTR-ISO-001</c>)." Making the filter a required parameter on every
/// query method, rather than an optional one a caller could omit, is deliberate
/// structure over discipline -- see <c>Hris.Infrastructure.IntegrationTests</c>'s own
/// <c>IndexedDocumentTenantIsolationTests</c> for the real-database proof this holds
/// under genuine cross-tenant input, not just under a fake repository that happens to
/// filter correctly.
///
/// <see cref="SecurityScopeToken"/> is the structural hook the AI Implementation
/// Guidance's authorization-trimming requirement needs, populated by whatever caller
/// indexes a record but not yet evaluated against a real permission grant -- Authorization
/// Framework's own concrete RBAC/ABAC evaluation is deferred the same way every other
/// Sprint 4 framework defers it, since no aggregate this Sprint's own build has a
/// concrete authorization integration point to check against without inventing one; see
/// <c>DependencyInjection.cs</c>'s own remarks.
/// </summary>
public sealed class IndexedDocument : AggregateRoot<IndexedDocumentId>
{
    public SearchIndexDefinitionId SearchIndexDefinitionId { get; }

    public Guid TenantId { get; }

    public SearchEntityType SourceEntityType { get; }

    public string SourceEntityId { get; }

    public string SearchableContent { get; private set; }

    public string? SecurityScopeToken { get; private set; }

    public IndexedDocumentStatus Status { get; private set; }

    public DateTimeOffset IndexedAtUtc { get; }

    public DateTimeOffset LastUpdatedAtUtc { get; private set; }

    private IndexedDocument(
        IndexedDocumentId id,
        SearchIndexDefinitionId searchIndexDefinitionId,
        Guid tenantId,
        SearchEntityType sourceEntityType,
        string sourceEntityId,
        string searchableContent,
        string? securityScopeToken,
        DateTimeOffset nowUtc)
        : base(id)
    {
        SearchIndexDefinitionId = searchIndexDefinitionId;
        TenantId = tenantId;
        SourceEntityType = sourceEntityType;
        SourceEntityId = sourceEntityId;
        SearchableContent = searchableContent;
        SecurityScopeToken = securityScopeToken;
        Status = IndexedDocumentStatus.Indexed;
        IndexedAtUtc = nowUtc;
        LastUpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// EF Core materialization only -- never called by application code, which always
    /// goes through the constructor above via <see cref="Index"/>. EF Core's own
    /// constructor-binding convention cannot bind the constructor above's own
    /// <c>nowUtc</c> parameter: it sets two differently-named properties
    /// (<see cref="IndexedAtUtc"/> and <see cref="LastUpdatedAtUtc"/>), and binding
    /// works by matching a parameter's own name to a property's, not by matching what
    /// the parameter happens to be assigned to -- a different failure shape than the
    /// owned-type/Complex-Type binding limitation <c>NumberSeries</c>' own second
    /// constructor works around, found empirically here the identical way, by a real
    /// model build failing with "No suitable constructor was found." Every parameter
    /// below shares its name with the property it binds to, so EF Core selects this
    /// constructor for materialization instead.
    /// </summary>
    private IndexedDocument(
        IndexedDocumentId id,
        SearchIndexDefinitionId searchIndexDefinitionId,
        Guid tenantId,
        SearchEntityType sourceEntityType,
        string sourceEntityId,
        string searchableContent,
        string? securityScopeToken,
        IndexedDocumentStatus status,
        DateTimeOffset indexedAtUtc,
        DateTimeOffset lastUpdatedAtUtc)
        : base(id)
    {
        SearchIndexDefinitionId = searchIndexDefinitionId;
        TenantId = tenantId;
        SourceEntityType = sourceEntityType;
        SourceEntityId = sourceEntityId;
        SearchableContent = searchableContent;
        SecurityScopeToken = securityScopeToken;
        Status = status;
        IndexedAtUtc = indexedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
    }

    /// <summary>
    /// Places a new source record into the index -- "Incremental Indexing"/"Event-Driven
    /// Index Updates." <paramref name="tenantId"/> is guarded, not Result-validated: by
    /// the time this factory runs, the caller (an Application-layer command handler) is
    /// responsible for having already resolved a real tenant context, the same
    /// technical-precondition-not-business-rule category <c>guard-clauses.md</c> puts
    /// null/empty/out-of-range checks in -- see this type's own remarks for why tenant
    /// scoping is treated this strictly in this specific framework.
    /// </summary>
    public static Result<IndexedDocument> Index(
        SearchIndexDefinitionId searchIndexDefinitionId,
        SearchEntityType sourceEntityType,
        string? sourceEntityId,
        Guid tenantId,
        string? searchableContent,
        string? securityScopeToken,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(sourceEntityType, nameof(sourceEntityType));
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(sourceEntityId))
        {
            return Result.Failure<IndexedDocument>(SearchErrors.SourceEntityIdRequired);
        }

        if (string.IsNullOrWhiteSpace(searchableContent))
        {
            return Result.Failure<IndexedDocument>(SearchErrors.SearchableContentRequired);
        }

        var document = new IndexedDocument(
            new IndexedDocumentId(Guid.NewGuid()),
            searchIndexDefinitionId,
            tenantId,
            sourceEntityType,
            sourceEntityId.Trim(),
            searchableContent.Trim(),
            securityScopeToken?.Trim(),
            nowUtc);

        document.AddDomainEvent(new SearchIndexCreated(Guid.NewGuid(), nowUtc, document.Id, searchIndexDefinitionId, tenantId));
        return Result.Success(document);
    }

    /// <summary>
    /// Re-indexes this same document after its source record changed --
    /// "Event-Driven Index Updates." <see cref="SearchIndexDefinitionId"/>,
    /// <see cref="TenantId"/>, <see cref="SourceEntityType"/>, and
    /// <see cref="SourceEntityId"/> never change here: a genuinely different source
    /// record is a different <see cref="IndexedDocument"/>, found and updated by
    /// <see cref="IIndexedDocumentRepository.FindBySourceAsync"/>, never repointed onto
    /// an existing row.
    /// </summary>
    public Result UpdateContent(string? searchableContent, string? securityScopeToken, DateTimeOffset nowUtc)
    {
        if (Status == IndexedDocumentStatus.Removed)
        {
            return Result.Failure(SearchErrors.InvalidIndexedDocumentTransition);
        }

        if (string.IsNullOrWhiteSpace(searchableContent))
        {
            return Result.Failure(SearchErrors.SearchableContentRequired);
        }

        SearchableContent = searchableContent.Trim();
        SecurityScopeToken = securityScopeToken?.Trim();
        LastUpdatedAtUtc = nowUtc;

        AddDomainEvent(new SearchIndexUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Soft-removes this document from the index -- the source record was deleted or is
    /// no longer searchable. Raises no event: search-framework.md's own Domain Events
    /// list names no removal event, the same asymmetry <c>IssuedNumber.Archive</c>'s own
    /// remarks note for itself. <see cref="IIndexedDocumentRepository.SearchAsync"/>
    /// excludes <see cref="IndexedDocumentStatus.Removed"/> rows by default, per
    /// <c>CTR-DAT-004</c>.
    /// </summary>
    public Result Remove(DateTimeOffset nowUtc)
    {
        if (Status == IndexedDocumentStatus.Removed)
        {
            return Result.Failure(SearchErrors.InvalidIndexedDocumentTransition);
        }

        Status = IndexedDocumentStatus.Removed;
        LastUpdatedAtUtc = nowUtc;
        return Result.Success();
    }
}
