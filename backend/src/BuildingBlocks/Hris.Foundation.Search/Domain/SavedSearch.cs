using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Aggregate Root for one user's own saved search -- search-framework.md's own Search
/// Suggestions section ("Saved Searches... Suggestions should respect user
/// permissions"). Ownership (<see cref="TenantId"/>/<see cref="OwnerUserId"/>) is
/// exactly how that permission respect is structurally guaranteed here:
/// <see cref="ISavedSearchRepository.ListByOwnerAsync"/> takes both as mandatory
/// parameters, so a caller can never list another user's own saved searches by
/// omitting a filter.
/// </summary>
public sealed class SavedSearch : AggregateRoot<SavedSearchId>
{
    private const int _maxNameLength = 200;

    public Guid TenantId { get; }

    public Guid OwnerUserId { get; }

    public string Name { get; private set; }

    public string QueryText { get; private set; }

    public string? DomainFilter { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? LastSuggestedAtUtc { get; private set; }

    public int SuggestedCount { get; private set; }

    private SavedSearch(
        SavedSearchId id, Guid tenantId, Guid ownerUserId, string name, string queryText, string? domainFilter, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        OwnerUserId = ownerUserId;
        Name = name;
        QueryText = queryText;
        DomainFilter = domainFilter;
        CreatedAtUtc = nowUtc;
        SuggestedCount = 0;
    }

    /// <summary>
    /// EF Core materialization only -- never called by application code, which always
    /// goes through the constructor above via <see cref="Save"/>. The constructor
    /// above takes <c>nowUtc</c>, which does not share a name with the property it
    /// sets (<see cref="CreatedAtUtc"/>), so EF Core's own constructor-binding
    /// convention cannot bind it -- the identical failure shape <see cref="IndexedDocument"/>'s
    /// own second constructor works around, found the same way (a real model build
    /// failing with "No suitable constructor was found").
    /// </summary>
    private SavedSearch(
        SavedSearchId id,
        Guid tenantId,
        Guid ownerUserId,
        string name,
        string queryText,
        string? domainFilter,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? lastSuggestedAtUtc,
        int suggestedCount)
        : base(id)
    {
        TenantId = tenantId;
        OwnerUserId = ownerUserId;
        Name = name;
        QueryText = queryText;
        DomainFilter = domainFilter;
        CreatedAtUtc = createdAtUtc;
        LastSuggestedAtUtc = lastSuggestedAtUtc;
        SuggestedCount = suggestedCount;
    }

    /// <summary>
    /// Saves a new search. Raises no event: search-framework.md's own Domain Events
    /// list names no "saved search created" event, only <see cref="SearchSuggestionGenerated"/>,
    /// raised by <see cref="RecordSuggested"/> instead.
    /// </summary>
    public static Result<SavedSearch> Save(
        Guid tenantId, Guid ownerUserId, string? name, string? queryText, string? domainFilter, DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstDefault(ownerUserId, nameof(ownerUserId));

        var nameResult = ValidateName(name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<SavedSearch>(nameResult.Error);
        }

        if (string.IsNullOrWhiteSpace(queryText))
        {
            return Result.Failure<SavedSearch>(SearchErrors.QueryTextRequired);
        }

        return Result.Success(new SavedSearch(
            new SavedSearchId(Guid.NewGuid()), tenantId, ownerUserId, nameResult.Value, queryText.Trim(), domainFilter?.Trim(), nowUtc));
    }

    public Result Rename(string? newName)
    {
        var nameResult = ValidateName(newName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        Name = nameResult.Value;
        return Result.Success();
    }

    /// <summary>
    /// Records that this saved search was surfaced as a suggestion --
    /// search-framework.md's own "Auto Complete, Search Suggestions... Saved Searches"
    /// bullets. Called once per saved search returned by <c>GetSearchSuggestionsQuery</c>,
    /// a deliberately small, bounded-cardinality caller (a suggestion list is always a
    /// short top-N, never population-scale like <see cref="IndexedDocument"/>), so
    /// raising one event per returned item here is not the same class of concern
    /// <see cref="IndexedDocument"/>'s own remarks raise about per-record events at
    /// population scale.
    /// </summary>
    public Result RecordSuggested(DateTimeOffset nowUtc)
    {
        SuggestedCount++;
        LastSuggestedAtUtc = nowUtc;

        AddDomainEvent(new SearchSuggestionGenerated(Guid.NewGuid(), nowUtc, Id, OwnerUserId));
        return Result.Success();
    }

    private static Result<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<string>(SearchErrors.SavedSearchNameRequired);
        }

        var trimmed = name.Trim();

        return trimmed.Length > _maxNameLength
            ? Result.Failure<string>(SearchErrors.SavedSearchNameTooLong)
            : Result.Success(trimmed);
    }
}
