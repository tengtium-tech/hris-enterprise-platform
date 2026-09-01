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

    /// <summary>
    /// Added alongside this framework's Application layer, for
    /// <c>IUserAccountRepository.GetByIdAsync</c> returning <c>null</c> against an
    /// id/tenant pair a command or query was given -- e.g. the acting user's own
    /// account somehow no longer resolving. Distinct from <see cref="AuthenticationFailed"/>
    /// below: this covers an already-authenticated caller acting on their own account,
    /// where there is no enumeration risk in saying so plainly (contrast
    /// identity-framework.md's "Never confirm whether an account exists in a failed
    /// authentication response," which governs the pre-authentication path only).
    /// </summary>
    public static readonly Error AccountNotFound = new(
        "Identity.AccountNotFound",
        "The requested user account does not exist.",
        ErrorCategory.NotFound);

    public static readonly Error UsernameAlreadyExists = new(
        "Identity.UsernameAlreadyExists",
        "An account with this username already exists for this tenant.",
        ErrorCategory.Conflict);

    /// <summary>
    /// The one, deliberately generic outcome <c>AuthenticateCommandHandler</c> returns
    /// for every pre-authentication rejection reason -- account not found, wrong
    /// password, account not Active -- per identity-framework.md's own "Never confirm
    /// whether an account exists in a failed authentication response; account
    /// enumeration is a disclosure." A category of <see cref="ErrorCategory.Domain"/>,
    /// not <see cref="ErrorCategory.Authorization"/>: error-pattern.md's Authorization
    /// Errors are permission/RBAC failures ("Insufficient permissions," "Approval
    /// authority missing"), a concern this framework must never take on
    /// (identity-framework.md: "Never make authorization decisions here; delegate to
    /// the Authorization Framework"). Credential verification failing is a fact about
    /// identity, not about permission.
    /// </summary>
    public static readonly Error AuthenticationFailed = new(
        "Identity.AuthenticationFailed",
        "The username or password is incorrect.",
        ErrorCategory.Domain);

    /// <summary>
    /// Distinct from <see cref="AuthenticationFailed"/>: this rejects
    /// <c>ChangeOwnPasswordCommand</c>'s own current-password check, issued by a
    /// caller who is already authenticated and acting on their own resolved account --
    /// no enumeration risk remains at that point, so the more specific message is safe
    /// to return (the same distinction <see cref="AccountNotFound"/>'s own remarks draw).
    /// </summary>
    public static readonly Error CurrentPasswordIncorrect = new(
        "Identity.CurrentPasswordIncorrect",
        "The current password provided is incorrect.",
        ErrorCategory.Domain);
}
