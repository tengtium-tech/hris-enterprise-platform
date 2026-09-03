using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class SearchErrors
{
    public static readonly Error EntityTypeRequired = new(
        "Search.EntityTypeRequired",
        "A searchable entity type is required.",
        ErrorCategory.Validation);

    public static readonly Error EntityTypeInvalid = new(
        "Search.EntityTypeInvalid",
        "An entity type must be 1-100 letters, digits, or underscores, starting with a letter.",
        ErrorCategory.Validation);

    public static readonly Error SearchIndexDefinitionNotFound = new(
        "Search.SearchIndexDefinitionNotFound",
        "No search index definition exists for the given entity type.",
        ErrorCategory.NotFound);

    public static readonly Error SearchIndexDefinitionAlreadyRegistered = new(
        "Search.SearchIndexDefinitionAlreadyRegistered",
        "A search index definition is already registered for this entity type.",
        ErrorCategory.Conflict);

    public static readonly Error NoFieldsProvided = new(
        "Search.NoFieldsProvided",
        "At least one field must be defined for a search index.",
        ErrorCategory.Validation);

    public static readonly Error NoSearchableFieldProvided = new(
        "Search.NoSearchableFieldProvided",
        "At least one field must be marked searchable.",
        ErrorCategory.Validation);

    public static readonly Error FieldNameRequired = new(
        "Search.FieldNameRequired",
        "A field name is required.",
        ErrorCategory.Validation);

    public static readonly Error DuplicateFieldName = new(
        "Search.DuplicateFieldName",
        "A field name is defined more than once for this search index.",
        ErrorCategory.Validation);

    public static readonly Error FieldWeightOutOfRange = new(
        "Search.FieldWeightOutOfRange",
        "A field's search weight must be between 1 and 10.",
        ErrorCategory.Validation);

    public static readonly Error SourceEntityIdRequired = new(
        "Search.SourceEntityIdRequired",
        "A source entity id is required.",
        ErrorCategory.Validation);

    public static readonly Error SearchableContentRequired = new(
        "Search.SearchableContentRequired",
        "Searchable content is required.",
        ErrorCategory.Validation);

    public static readonly Error IndexedDocumentNotFound = new(
        "Search.IndexedDocumentNotFound",
        "No indexed document exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidIndexedDocumentTransition = new(
        "Search.InvalidIndexedDocumentTransition",
        "This transition is not valid from the indexed document's current status.",
        ErrorCategory.Domain);

    public static readonly Error QueryTextRequired = new(
        "Search.QueryTextRequired",
        "Query text is required.",
        ErrorCategory.Validation);

    public static readonly Error InvalidSearchExecutionTransition = new(
        "Search.InvalidSearchExecutionTransition",
        "This transition is not valid from the search execution's current status.",
        ErrorCategory.Domain);

    public static readonly Error FailureReasonRequired = new(
        "Search.FailureReasonRequired",
        "A reason is required to fail a search execution.",
        ErrorCategory.Validation);

    public static readonly Error SearchExecutionNotFound = new(
        "Search.SearchExecutionNotFound",
        "No search execution exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error SavedSearchNameRequired = new(
        "Search.SavedSearchNameRequired",
        "A name is required to save a search.",
        ErrorCategory.Validation);

    public static readonly Error SavedSearchNameTooLong = new(
        "Search.SavedSearchNameTooLong",
        "A saved search name cannot exceed 200 characters.",
        ErrorCategory.Validation);

    public static readonly Error SavedSearchNotFound = new(
        "Search.SavedSearchNotFound",
        "No saved search exists for the given identifier.",
        ErrorCategory.NotFound);
}
