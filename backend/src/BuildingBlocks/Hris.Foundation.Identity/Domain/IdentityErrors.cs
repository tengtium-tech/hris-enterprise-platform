using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class IdentityErrors
{
    public static readonly Error UsernameRequired = new(
        "Identity.UsernameRequired",
        "A username is required.",
        ErrorCategory.Validation);

    public static readonly Error UsernameInvalidLength = new(
        "Identity.UsernameInvalidLength",
        "A username must be between 3 and 100 characters.",
        ErrorCategory.Validation);

    public static readonly Error AuthenticationProviderRequired = new(
        "Identity.AuthenticationProviderRequired",
        "An authentication provider key is required.",
        ErrorCategory.Validation);

    public static readonly Error PasswordHashRequired = new(
        "Identity.PasswordHashRequired",
        "A password hash is required.",
        ErrorCategory.Validation);

    public static readonly Error TenantIdRequired = new(
        "Identity.TenantIdRequired",
        "A user account must belong to a tenant.",
        ErrorCategory.Validation);

    public static readonly Error AccountNotActive = new(
        "Identity.AccountNotActive",
        "This operation requires the account to be Active.",
        ErrorCategory.Conflict);

    public static readonly Error InvalidLifecycleTransition = new(
        "Identity.InvalidLifecycleTransition",
        "The account cannot move to the requested status from its current status.",
        ErrorCategory.Conflict);

    public static readonly Error SessionNotFound = new(
        "Identity.SessionNotFound",
        "The requested session does not exist for this account.",
        ErrorCategory.NotFound);

    public static readonly Error SessionTenantMismatch = new(
        "Identity.SessionTenantMismatch",
        "A session must belong to the same tenant as its account.",
        ErrorCategory.Validation);

    public static readonly Error MfaFactorNotFound = new(
        "Identity.MfaFactorNotFound",
        "The requested multi-factor authentication factor does not exist for this account.",
        ErrorCategory.NotFound);

    public static readonly Error CredentialRequired = new(
        "Identity.CredentialRequired",
        "A password hash is required before an account can authenticate.",
        ErrorCategory.Conflict);

    public static readonly Error TooManyActiveSessions = new(
        "Identity.TooManyActiveSessions",
        "This account already holds the maximum number of concurrent active sessions.",
        ErrorCategory.Conflict);
}
