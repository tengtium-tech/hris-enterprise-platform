namespace Hris.Foundation.Search.Domain;

/// <summary>
/// One field's own participation in a <see cref="SearchIndexDefinition"/>, per
/// search-framework.md's own Search Index section ("Search Fields, Indexed Fields,
/// Sortable Fields, Filterable Fields, Search Weight"). A plain positional record, not
/// a <see cref="ValueObject"/>-derived type with its own <c>Create</c> factory --
/// unlike <see cref="SearchEntityType"/>, this shape needs no normalization and its
/// only real invariants (name required, weight in range, no duplicate names within one
/// definition) are collection-level checks that belong to
/// <see cref="SearchIndexDefinition"/> itself, the same "plain string, validated by the
/// owning aggregate" choice <c>IssuedNumber.AssignedToType</c> already makes for a
/// simpler field. Persisted as a single JSON column
/// (<see cref="Infrastructure.Persistence.SearchIndexDefinitionConfiguration"/>), the
/// same choice <c>RuleDefinitionConfiguration</c>'s own <c>Parameters</c> dictionary
/// already makes for a collection with no independent per-row query need of its own.
/// </summary>
public sealed record SearchFieldDefinition(
    string FieldName,
    bool IsSearchable,
    bool IsSortable,
    bool IsFilterable,
    int Weight);
