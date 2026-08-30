using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// One authenticated session, per identity-framework.md's Session Management section
/// ("Secure Session Creation, Session Timeout, Idle Timeout, Concurrent Session
/// Limits, Forced Logout, Session Revocation") and its own
/// <c>GetMyActiveSessionsQuery</c> ("device/client, approximate location, last-active
/// timestamp").
///
/// A child Entity of <see cref="UserAccount"/>, never its own Aggregate Root
/// (aggregate-design-rules.md Rule 7) -- enforcing "Concurrent Session Limits"
/// requires seeing every session under one consistency boundary, which is exactly
/// what one Aggregate protects. <see cref="TenantId"/> is carried on the session
/// itself, not merely inherited from the account, so <see cref="UserAccount.CreateSession"/>
/// can guard "a session must never span tenants" (this document's own AI
/// Implementation Guidance) as a structural equality check rather than an assumption.
/// </summary>
public sealed class Session : Entity<SessionId>
{
    public Guid TenantId { get; }

    public string DeviceLabel { get; }

    public string? ApproximateLocation { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset LastActiveAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    internal Session(
        SessionId id,
        Guid tenantId,
        string deviceLabel,
        string? approximateLocation,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        DeviceLabel = deviceLabel;
        ApproximateLocation = approximateLocation;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        LastActiveAtUtc = createdAtUtc;
    }

    public bool IsActive(DateTimeOffset asOfUtc) => RevokedAtUtc is null && asOfUtc < ExpiresAtUtc;

    internal void Touch(DateTimeOffset nowUtc, DateTimeOffset newExpiresAtUtc)
    {
        LastActiveAtUtc = nowUtc;
        ExpiresAtUtc = newExpiresAtUtc;
    }

    internal Result Revoke(DateTimeOffset nowUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return Result.Success();
        }

        RevokedAtUtc = nowUtc;
        return Result.Success();
    }
}
