namespace Hris.Foundation.Search.Application.Dtos;

/// <summary>
/// The read-side shape <c>ListSavedSearchesQuery</c> and <c>GetSearchSuggestionsQuery</c>
/// return, per dto-design.md's own convention.
/// </summary>
public sealed record SavedSearchDto(
    Guid SavedSearchId,
    string Name,
    string QueryText,
    string? DomainFilter,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastSuggestedAtUtc,
    int SuggestedCount);
