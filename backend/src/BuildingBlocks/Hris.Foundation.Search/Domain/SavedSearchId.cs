using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Identity of the <see cref="SavedSearch"/> Aggregate Root, per search-framework.md's
/// own Search Suggestions section ("Saved Searches", "Recent Searches" -- "Suggestions
/// should respect user permissions").
/// </summary>
public readonly record struct SavedSearchId(Guid Value) : IStronglyTypedId;
