using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// identity-framework.md's own Domain Events section, plus three additions --
/// <see cref="UserSuspended"/>, <see cref="UserReinstated"/>, <see cref="UserArchived"/>
/// -- not in that "Typical events include" list. The document's own Identity
/// Lifecycle section names Suspended and Archived as real, reachable states; leaving
/// their transitions with no corresponding event would be a silent gap in exactly the
/// audit trail this framework's own Security Considerations section calls for, and
/// "Typical" is stated as illustrative, not exhaustive, unlike the other frameworks
/// built so far where every reachable state already had a listed event.
/// </summary>
public sealed record UserCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId UserAccountId,
    Guid TenantId,
    IdentityType IdentityType) : IDomainEvent;

public sealed record UserActivated(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserLocked(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, int FailedAttempts) : IDomainEvent;

public sealed record UserUnlocked(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserSuspended(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserReinstated(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserDisabled(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserArchived(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record UserAuthenticated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId UserAccountId,
    SessionId SessionId) : IDomainEvent;

public sealed record UserLoggedOut(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    UserAccountId UserAccountId,
    SessionId SessionId) : IDomainEvent;

public sealed record PasswordChanged(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId) : IDomainEvent;

public sealed record MfaEnabled(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, MfaFactorId FactorId, MfaFactorType FactorType) : IDomainEvent;

public sealed record MfaDisabled(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, MfaFactorId FactorId) : IDomainEvent;

public sealed record IdentitySynchronized(Guid EventId, DateTimeOffset OccurredOnUtc, UserAccountId UserAccountId, string SourceSystem) : IDomainEvent;
