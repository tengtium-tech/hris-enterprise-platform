using Hris.Foundation.Search.Domain;

namespace Hris.Foundation.Search.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetSearchIndexDefinitionQuery</c> returns, per
/// dto-design.md's own convention.
/// </summary>
public sealed record SearchIndexDefinitionDto(
    Guid SearchIndexDefinitionId,
    string EntityType,
    IReadOnlyList<SearchFieldDefinition> Fields,
    string? SecurityScopeKey,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset? LastRebuiltAtUtc);
