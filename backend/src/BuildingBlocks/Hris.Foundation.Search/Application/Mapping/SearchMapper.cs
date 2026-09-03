using Hris.Foundation.Search.Application.Dtos;
using Hris.Foundation.Search.Domain;

namespace Hris.Foundation.Search.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4 framework's own mapper already establishes.
/// </summary>
internal static class SearchMapper
{
    private const int _snippetMaxLength = 200;

    public static SearchIndexDefinitionDto ToDto(SearchIndexDefinition definition) => new(
        definition.Id.Value,
        definition.EntityType.Value,
        definition.Fields,
        definition.SecurityScopeKey,
        definition.RegisteredAtUtc,
        definition.LastRebuiltAtUtc);

    public static SavedSearchDto ToDto(SavedSearch savedSearch) => new(
        savedSearch.Id.Value,
        savedSearch.Name,
        savedSearch.QueryText,
        savedSearch.DomainFilter,
        savedSearch.CreatedAtUtc,
        savedSearch.LastSuggestedAtUtc,
        savedSearch.SuggestedCount);

    /// <summary>
    /// Groups a flat, rank-ordered hit list by <see cref="IndexedDocumentSearchHit.SourceEntityType"/>
    /// -- search-framework.md's own <c>GlobalSearchQuery</c> subsection: "returns
    /// results grouped by domain." Group order follows each group's own best (first,
    /// since <paramref name="hits"/> already arrives rank-ordered) hit, so the
    /// highest-relevance domain appears first.
    /// </summary>
    public static GlobalSearchResultDto ToDto(SearchExecution execution, IReadOnlyList<IndexedDocumentSearchHit> hits)
    {
        var groups = hits
            .GroupBy(hit => hit.SourceEntityType.Value)
            .Select(group => new SearchResultGroupDto(
                group.Key,
                group.Select(ToHitDto).ToList()))
            .ToList();

        return new GlobalSearchResultDto(execution.Id.Value, execution.QueryText, hits.Count, groups);
    }

    private static SearchResultHitDto ToHitDto(IndexedDocumentSearchHit hit) => new(
        hit.IndexedDocumentId.Value,
        hit.SourceEntityId,
        hit.SearchableContent.Length > _snippetMaxLength
            ? string.Concat(hit.SearchableContent.AsSpan(0, _snippetMaxLength), "...")
            : hit.SearchableContent,
        hit.Rank);
}
