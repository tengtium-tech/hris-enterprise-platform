using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Identity of the <see cref="SearchIndexDefinition"/> Aggregate Root -- one per
/// searchable entity type (for example "Employee"), per search-framework.md's own
/// Search Index section: "Each searchable entity should define... Indexes should be
/// independently managed."
/// </summary>
public readonly record struct SearchIndexDefinitionId(Guid Value) : IStronglyTypedId;
