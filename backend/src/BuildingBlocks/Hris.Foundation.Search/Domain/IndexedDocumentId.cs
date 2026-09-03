using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Identity of the <see cref="IndexedDocument"/> Aggregate Root -- one per source
/// record instance actually placed in the index. See <see cref="IndexedDocument"/>'s
/// own remarks for why this is a separate, population-scale Aggregate Root from
/// <see cref="SearchIndexDefinition"/>, not a child Entity of it.
/// </summary>
public readonly record struct IndexedDocumentId(Guid Value) : IStronglyTypedId;
