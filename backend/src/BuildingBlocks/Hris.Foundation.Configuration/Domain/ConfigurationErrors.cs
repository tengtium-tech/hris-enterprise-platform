using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per
/// docs/02-architecture/04-domain-driven-design/error-pattern.md's "Error Catalog"
/// section: "Each bounded context owns its own error catalog." Every failure the
/// Configuration Framework's Domain layer can produce is declared here once, so the
/// same violation always carries the same stable <see cref="Error.Code"/> (`CTR-API-003`).
/// </summary>
public static class ConfigurationErrors
{
    public static readonly Error KeyRequired = new(
        "Configuration.KeyRequired",
        "A configuration key is required.",
        ErrorCategory.Validation);

    public static readonly Error KeyTooLong = new(
        "Configuration.KeyTooLong",
        "A configuration key cannot exceed 200 characters.",
        ErrorCategory.Validation);

    public static readonly Error KeyInvalidFormat = new(
        "Configuration.KeyInvalidFormat",
        "A configuration key must be one or more dot-separated segments, each starting with a letter.",
        ErrorCategory.Validation);

    public static readonly Error ScopeIdRequiredForNonGlobalLevel = new(
        "Configuration.ScopeIdRequiredForNonGlobalLevel",
        "A scope id is required for every scope level except Global.",
        ErrorCategory.Validation);

    public static readonly Error ScopeIdNotAllowedForGlobalLevel = new(
        "Configuration.ScopeIdNotAllowedForGlobalLevel",
        "A scope id is not allowed at Global scope.",
        ErrorCategory.Validation);

    public static readonly Error ValueRequired = new(
        "Configuration.ValueRequired",
        "A configuration version requires a value.",
        ErrorCategory.Validation);

    public static readonly Error ValueDoesNotMatchDataType = new(
        "Configuration.ValueDoesNotMatchDataType",
        "The configuration value does not conform to the configuration's declared data type.",
        ErrorCategory.Validation);

    public static readonly Error ChangeSummaryRequired = new(
        "Configuration.ChangeSummaryRequired",
        "A change summary is required to publish a configuration version.",
        ErrorCategory.Validation);

    public static readonly Error ExpirationBeforeEffectiveDate = new(
        "Configuration.ExpirationBeforeEffectiveDate",
        "A configuration version's expiration date cannot be before its effective date.",
        ErrorCategory.Validation);

    public static readonly Error EffectiveDateBeforePreviousVersion = new(
        "Configuration.EffectiveDateBeforePreviousVersion",
        "A new configuration version's effective date cannot be before the most recently published version's effective date.",
        ErrorCategory.Conflict);

    public static readonly Error DraftAlreadyExists = new(
        "Configuration.DraftAlreadyExists",
        "This configuration already has an unresolved draft version. Resolve it before creating another.",
        ErrorCategory.Conflict);

    public static readonly Error VersionNotFound = new(
        "Configuration.VersionNotFound",
        "The requested configuration version does not exist.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidLifecycleTransition = new(
        "Configuration.InvalidLifecycleTransition",
        "The configuration version cannot move to the requested lifecycle state from its current state.",
        ErrorCategory.Conflict);

    public static readonly Error CannotActivateBeforeEffectiveDate = new(
        "Configuration.CannotActivateBeforeEffectiveDate",
        "A configuration version cannot be activated before its own effective date.",
        ErrorCategory.Conflict);
}
