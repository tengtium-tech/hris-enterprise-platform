namespace Hris.Foundation.Identity.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.UserAccount"/>, returned by
/// <c>GetMyAccountQuery</c> -- exactly the attribute list identity-framework.md's own
/// Client-Facing Commands and Queries table names for it: "username, email address,
/// display name, status, authentication provider, MFA status, last login." Primitive-
/// only, for the same reason <c>ConfigurationSettingDto</c> is (see that type's own
/// remarks): a query's output should not force a future Presentation-layer caller to
/// understand this framework's internal Value Object shapes. Never carries
/// <see cref="Domain.PasswordHash"/> or any other credential material, per
/// identity-framework.md's Security Considerations ("Authentication data should never
/// be exposed to business modules").
/// </summary>
public sealed record UserAccountDto(
    Guid Id,
    string Username,
    string EmailAddress,
    string DisplayName,
    string Status,
    string AuthenticationProvider,
    bool MfaEnabled,
    DateTimeOffset? LastLoginAtUtc);

/// <summary>
/// The read-side shape of a <see cref="Domain.Session"/>, returned by
/// <c>GetMyActiveSessionsQuery</c> -- exactly the attribute list that query's own
/// table entry names: "device/client, approximate location, last-active timestamp."
/// </summary>
public sealed record SessionDto(
    Guid Id,
    string DeviceLabel,
    string? ApproximateLocation,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset LastActiveAtUtc);

/// <summary>
/// The outcome of <c>AuthenticateCommand</c>. Deliberately not itself wrapped in a
/// failed <see cref="Hris.SharedKernel.Result{TValue}"/> for a wrong-password or
/// account-not-found outcome -- see <c>AuthenticateCommandHandler</c>'s own remarks for
/// why a rejected login is still a *successful* command execution carrying
/// <see cref="IsAuthenticated"/> <c>false</c>, not a <see cref="Hris.SharedKernel.Result"/>
/// failure.
/// </summary>
public sealed record AuthenticationResultDto(
    bool IsAuthenticated,
    Guid? UserAccountId,
    Guid? SessionId,
    DateTimeOffset? SessionExpiresAtUtc);
