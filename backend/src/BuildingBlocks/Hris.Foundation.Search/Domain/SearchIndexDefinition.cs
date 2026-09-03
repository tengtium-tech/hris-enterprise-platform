using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Aggregate Root holding one searchable entity type's own index configuration --
/// which fields are searchable/sortable/filterable, their relative weight, and the
/// permission key gating them. Source: search-framework.md, Search Index ("Each
/// searchable entity should define... Indexes should be independently managed").
///
/// Fifth framework built in Sprint 4, and the first whose own AI Implementation
/// Guidance names a Critical Test Requirement explicitly (<c>CTR-ISO-001</c>, "a search
/// index is a common isolation gap") rather than leaving tenant-scoping for a later
/// Sprint the way Tenant/Extension/File Storage/Numbering's own remarks each state for
/// their own deferred Authorization Framework wiring -- see <see cref="IndexedDocument"/>'s
/// own remarks for how that guidance is honored here without inventing an ambient
/// tenant-context mechanism no framework in this codebase has built yet.
/// </summary>
public sealed class SearchIndexDefinition : AggregateRoot<SearchIndexDefinitionId>
{
    public SearchEntityType EntityType { get; }

    public IReadOnlyList<SearchFieldDefinition> Fields { get; private set; }

    public string? SecurityScopeKey { get; private set; }

    public DateTimeOffset RegisteredAtUtc { get; }

    public DateTimeOffset? LastRebuiltAtUtc { get; private set; }

    private SearchIndexDefinition(
        SearchIndexDefinitionId id,
        SearchEntityType entityType,
        IReadOnlyList<SearchFieldDefinition> fields,
        string? securityScopeKey,
        DateTimeOffset registeredAtUtc)
        : base(id)
    {
        EntityType = entityType;
        Fields = fields;
        SecurityScopeKey = securityScopeKey;
        RegisteredAtUtc = registeredAtUtc;
    }

    /// <summary>
    /// Registers a new search index definition. Entity-type uniqueness is checked by
    /// the caller before this factory runs (<see cref="ISearchIndexDefinitionRepository.ExistsByEntityTypeAsync"/>),
    /// not here -- the same split every other framework's own uniqueness-checked
    /// factory in this codebase establishes. Raises no event: search-framework.md's own
    /// Domain Events list names no "definition registered" event, the same asymmetry
    /// <c>NumberSeries.Register</c>'s own remarks note for itself.
    /// </summary>
    public static Result<SearchIndexDefinition> Register(
        SearchEntityType entityType, IReadOnlyList<SearchFieldDefinition> fields, string? securityScopeKey, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(entityType, nameof(entityType));

        var fieldsResult = ValidateFields(fields);
        if (fieldsResult.IsFailure)
        {
            return Result.Failure<SearchIndexDefinition>(fieldsResult.Error);
        }

        return Result.Success(new SearchIndexDefinition(
            new SearchIndexDefinitionId(Guid.NewGuid()), entityType, fields, securityScopeKey?.Trim(), nowUtc));
    }

    public Result UpdateFields(IReadOnlyList<SearchFieldDefinition> fields, string? securityScopeKey)
    {
        var fieldsResult = ValidateFields(fields);
        if (fieldsResult.IsFailure)
        {
            return fieldsResult;
        }

        Fields = fields;
        SecurityScopeKey = securityScopeKey?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Records that a full index rebuild for this entity type has completed --
    /// <paramref name="documentCount"/> is supplied by the caller (the background
    /// process that actually iterated source records and called
    /// <see cref="IndexedDocument.Index"/>/<see cref="IndexedDocument.UpdateContent"/>
    /// for each one), never computed here: this aggregate has no query access to
    /// <see cref="IndexedDocument"/> rows of its own, the same cross-aggregate-data-stays-
    /// with-the-caller split <c>IssuedNumber.Validate</c>'s own remarks state for
    /// <see cref="NumberSeries"/>' prefix/format.
    /// </summary>
    public Result CompleteRebuild(int documentCount, DateTimeOffset nowUtc)
    {
        if (documentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(documentCount), documentCount, "Document count cannot be negative.");
        }

        LastRebuiltAtUtc = nowUtc;
        AddDomainEvent(new SearchIndexRebuilt(Guid.NewGuid(), nowUtc, Id, documentCount));
        return Result.Success();
    }

    private static Result ValidateFields(IReadOnlyList<SearchFieldDefinition>? fields)
    {
        if (fields is null || fields.Count == 0)
        {
            return Result.Failure(SearchErrors.NoFieldsProvided);
        }

        if (!fields.Any(field => field.IsSearchable))
        {
            return Result.Failure(SearchErrors.NoSearchableFieldProvided);
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.FieldName))
            {
                return Result.Failure(SearchErrors.FieldNameRequired);
            }

            if (field.Weight is < 1 or > 10)
            {
                return Result.Failure(SearchErrors.FieldWeightOutOfRange);
            }

            if (!seenNames.Add(field.FieldName))
            {
                return Result.Failure(SearchErrors.DuplicateFieldName);
            }
        }

        return Result.Success();
    }
}
