using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class AuthorizationErrors
{
    public static readonly Error ResourceTypeRequired = new(
        "Authorization.ResourceTypeRequired",
        "A permission's resource type is required.",
        ErrorCategory.Validation);

    public static readonly Error ScopeIdRequired = new(
        "Authorization.ScopeIdRequired",
        "An organizational scope requires a scope id.",
        ErrorCategory.Validation);

    public static readonly Error ExpirationBeforeEffectiveDate = new(
        "Authorization.ExpirationBeforeEffectiveDate",
        "A role assignment's expiration date cannot be before its effective date.",
        ErrorCategory.Validation);

    public static readonly Error AuditorCannotHoldMutationPermission = new(
        "Authorization.AuditorCannotHoldMutationPermission",
        "The Auditor role must not hold a mutation permission (Create, Update, Delete, Approve, Reject, Import, or Configure) on any resource (CTR-AUT-003).",
        ErrorCategory.Validation);
}
