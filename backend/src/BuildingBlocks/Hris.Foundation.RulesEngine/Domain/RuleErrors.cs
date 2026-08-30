using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class RuleErrors
{
    public static readonly Error KeyRequired = new(
        "Rule.KeyRequired",
        "A rule key is required.",
        ErrorCategory.Validation);

    public static readonly Error KeyTooLong = new(
        "Rule.KeyTooLong",
        "A rule key cannot exceed 200 characters.",
        ErrorCategory.Validation);

    public static readonly Error KeyInvalidFormat = new(
        "Rule.KeyInvalidFormat",
        "A rule key must be one or more dot-separated segments, each starting with a letter.",
        ErrorCategory.Validation);

    public static readonly Error CategoryRequired = new(
        "Rule.CategoryRequired",
        "A rule's category is required.",
        ErrorCategory.Validation);

    public static readonly Error FieldNameRequired = new(
        "Rule.FieldNameRequired",
        "A rule condition's field name is required.",
        ErrorCategory.Validation);

    public static readonly Error ComparisonValueRequired = new(
        "Rule.ComparisonValueRequired",
        "A rule condition's comparison value is required.",
        ErrorCategory.Validation);

    public static readonly Error ActionKeyRequired = new(
        "Rule.ActionKeyRequired",
        "A rule action's key is required.",
        ErrorCategory.Validation);

    public static readonly Error AtLeastOneConditionRequired = new(
        "Rule.AtLeastOneConditionRequired",
        "A rule version requires at least one condition.",
        ErrorCategory.Validation);

    public static readonly Error AtLeastOneActionRequired = new(
        "Rule.AtLeastOneActionRequired",
        "A rule version requires at least one action.",
        ErrorCategory.Validation);

    public static readonly Error DraftAlreadyExists = new(
        "Rule.DraftAlreadyExists",
        "This rule already has an unresolved draft version. Resolve it before creating another.",
        ErrorCategory.Conflict);

    public static readonly Error VersionNotFound = new(
        "Rule.VersionNotFound",
        "The requested rule version does not exist.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidLifecycleTransition = new(
        "Rule.InvalidLifecycleTransition",
        "The rule version cannot move to the requested lifecycle state from its current state.",
        ErrorCategory.Conflict);

    public static readonly Error NoActiveVersion = new(
        "Rule.NoActiveVersion",
        "This rule has no Active version to evaluate.",
        ErrorCategory.Conflict);

    public static readonly Error FactFieldMissing = new(
        "Rule.FactFieldMissing",
        "The evaluation context does not carry a value for a field this rule's conditions reference.",
        ErrorCategory.Validation);
}
