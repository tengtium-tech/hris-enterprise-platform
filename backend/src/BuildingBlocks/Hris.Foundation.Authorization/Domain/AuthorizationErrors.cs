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

    /// <summary>
    /// Added alongside this framework's Application layer, for
    /// <c>IRoleAssignmentRepository.GetByIdAsync</c> returning <c>null</c> against an
    /// id a command was given -- e.g. <c>RevokeRoleAssignmentCommand</c> targeting an
    /// assignment that no longer exists.
    /// </summary>
    public static readonly Error RoleAssignmentNotFound = new(
        "Authorization.RoleAssignmentNotFound",
        "The requested role assignment does not exist.",
        ErrorCategory.NotFound);

    public static readonly Error RolePermissionGrantNotFound = new(
        "Authorization.RolePermissionGrantNotFound",
        "The requested role permission grant does not exist.",
        ErrorCategory.NotFound);
}
