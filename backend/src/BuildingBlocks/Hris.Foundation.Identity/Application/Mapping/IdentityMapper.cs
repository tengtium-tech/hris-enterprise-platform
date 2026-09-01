using Hris.Foundation.Identity.Application.Dtos;
using Hris.Foundation.Identity.Domain;

namespace Hris.Foundation.Identity.Application.Mapping;

/// <summary>
/// Maps <see cref="UserAccount"/>/<see cref="Session"/> to their query-side DTOs, by
/// hand rather than through a registered Mapster profile -- the identical deviation
/// <c>ConfigurationMapper</c> states and justifies for the same reason: every field
/// here either unwraps a Value Object (<see cref="Username"/>, <see cref="EmailAddress"/>,
/// <see cref="AuthenticationProvider"/>) or converts an enum to its DTO-side string.
/// </summary>
internal static class IdentityMapper
{
    public static UserAccountDto ToDto(this UserAccount account) => new(
        account.Id.Value,
        account.Username.Value,
        account.EmailAddress.Value,
        account.DisplayName,
        account.Status.ToString(),
        account.AuthenticationProvider.Key,
        account.MfaFactors.Any(factor => factor.IsActive),
        account.LastLoginAtUtc);

    public static SessionDto ToDto(this Session session) => new(
        session.Id.Value,
        session.DeviceLabel,
        session.ApproximateLocation,
        session.CreatedAtUtc,
        session.ExpiresAtUtc,
        session.LastActiveAtUtc);
}
