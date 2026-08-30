using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// Aggregate Root of the Identity Framework: one credential-bearing account and
/// every <see cref="Session"/> and <see cref="MfaFactor"/> it owns. Source:
/// docs/03-foundation/identity-framework.md.
///
/// Built third in Sprint 3's bootstrap order, after Configuration and Logging
/// Frameworks and before Event and Authorization Frameworks -- its own stated
/// Upstream Dependencies (Configuration, Event, Logging) -- per IMPLEMENTATION-PLAN.md's
/// "minimal version of each exists before any of them is feature-complete" resolution.
/// This aggregate establishes identity only, per this document's own repeated
/// instruction: "Never make authorization decisions here; delegate to the
/// Authorization Framework (ADR-0002)." It has no `Role`/`Permission` concept at all.
///
/// <see cref="LinkedIdentityId"/> is a raw, optional <see cref="Guid"/> rather than a
/// strongly typed <c>EmployeeId</c>: the Employee module does not exist until Phase 2
/// Sprint 4, and Identity Framework has no dependency on it (`CTR-ARC-002`). Revisit
/// once it does.
/// </summary>
public sealed class UserAccount : AggregateRoot<UserAccountId>
{
    private readonly List<Session> _sessions = [];
    private readonly List<MfaFactor> _mfaFactors = [];

    public Guid TenantId { get; }

    public Username Username { get; }

    public EmailAddress EmailAddress { get; private set; }

    public string DisplayName { get; private set; }

    public IdentityType IdentityType { get; }

    public Guid? LinkedIdentityId { get; }

    public UserAccountStatus Status { get; private set; }

    public AuthenticationProvider AuthenticationProvider { get; }

    public PasswordHash? PasswordHash { get; private set; }

    public int FailedAuthenticationAttemptCount { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public IReadOnlyList<Session> Sessions => _sessions.AsReadOnly();

    public IReadOnlyList<MfaFactor> MfaFactors => _mfaFactors.AsReadOnly();

    private UserAccount(
        UserAccountId id,
        Guid tenantId,
        Username username,
        EmailAddress emailAddress,
        string displayName,
        IdentityType identityType,
        Guid? linkedIdentityId,
        AuthenticationProvider authenticationProvider)
        : base(id)
    {
        TenantId = tenantId;
        Username = username;
        EmailAddress = emailAddress;
        DisplayName = displayName;
        IdentityType = identityType;
        LinkedIdentityId = linkedIdentityId;
        AuthenticationProvider = authenticationProvider;
        Status = UserAccountStatus.Invited;
    }

    public static Result<UserAccount> Create(
        Guid tenantId,
        Username username,
        EmailAddress emailAddress,
        string? displayName,
        IdentityType identityType,
        AuthenticationProvider authenticationProvider,
        DateTimeOffset nowUtc,
        Guid? linkedIdentityId = null)
    {
        Guard.AgainstNull(username, nameof(username));
        Guard.AgainstNull(emailAddress, nameof(emailAddress));
        Guard.AgainstNull(authenticationProvider, nameof(authenticationProvider));

        if (tenantId == Guid.Empty)
        {
            return Result.Failure<UserAccount>(IdentityErrors.TenantIdRequired);
        }

        var account = new UserAccount(
            new UserAccountId(Guid.NewGuid()),
            tenantId,
            username,
            emailAddress,
            string.IsNullOrWhiteSpace(displayName) ? username.Value : displayName.Trim(),
            identityType,
            linkedIdentityId,
            authenticationProvider);

        account.AddDomainEvent(new UserCreated(Guid.NewGuid(), nowUtc, account.Id, tenantId, identityType));
        return Result.Success(account);
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Invited)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Active;
        AddDomainEvent(new UserActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Lock(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Active)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Locked;
        AddDomainEvent(new UserLocked(Guid.NewGuid(), nowUtc, Id, FailedAuthenticationAttemptCount));
        return Result.Success();
    }

    public Result Unlock(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Locked)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Active;
        FailedAuthenticationAttemptCount = 0;
        AddDomainEvent(new UserUnlocked(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Suspend(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Active)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Suspended;
        RevokeAllActiveSessions(nowUtc);
        AddDomainEvent(new UserSuspended(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Reinstate(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Suspended)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Active;
        AddDomainEvent(new UserReinstated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Deprovisions the account. Revokes every active session as part of the same
    /// operation, per `CTR-AUT-008` ("Deactivation Terminates Active Sessions...
    /// Suspending or deprovisioning a user account must terminate that account's
    /// active sessions, not merely prevent future authentication") -- an
    /// implementation that only flipped <see cref="Status"/> and left existing
    /// sessions valid would satisfy the state-machine shape of this requirement
    /// while failing the actual security property it exists for.
    /// </summary>
    public Result Disable(DateTimeOffset nowUtc)
    {
        if (Status is UserAccountStatus.Disabled or UserAccountStatus.Archived)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Disabled;
        RevokeAllActiveSessions(nowUtc);
        AddDomainEvent(new UserDisabled(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Archive(DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Disabled)
        {
            return Result.Failure(IdentityErrors.InvalidLifecycleTransition);
        }

        Status = UserAccountStatus.Archived;
        AddDomainEvent(new UserArchived(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Records a hash an Infrastructure-layer <c>IPasswordHasher</c> already computed
    /// -- this method never sees, and this aggregate never stores, plaintext
    /// (identity-framework.md, Security Considerations).
    /// </summary>
    public Result ChangePassword(PasswordHash newPasswordHash, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(newPasswordHash, nameof(newPasswordHash));

        if (Status is not (UserAccountStatus.Invited or UserAccountStatus.Active))
        {
            return Result.Failure(IdentityErrors.AccountNotActive);
        }

        PasswordHash = newPasswordHash;
        AddDomainEvent(new PasswordChanged(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Applies the business consequence of an authentication attempt an
    /// Infrastructure-layer credential verifier already evaluated -- this aggregate
    /// never compares a plaintext password against <see cref="PasswordHash"/>
    /// itself, since computing and comparing that digest is a cryptography concern
    /// (`CTR-ARC-001`). Whether an account exists at all is resolved by the
    /// repository lookup before this method is ever reached, per this document's own
    /// "Never confirm whether an account exists in a failed authentication response";
    /// this method has no "account not found" outcome to leak in the first place.
    /// </summary>
    public Result<Session> RecordSuccessfulAuthenticationAndCreateSession(
        Guid tenantId,
        string deviceLabel,
        string? approximateLocation,
        DateTimeOffset nowUtc,
        TimeSpan sessionLifetime,
        int maxConcurrentSessions)
    {
        var sessionResult = CreateSession(tenantId, deviceLabel, approximateLocation, nowUtc, sessionLifetime, maxConcurrentSessions);
        if (sessionResult.IsFailure)
        {
            return sessionResult;
        }

        FailedAuthenticationAttemptCount = 0;
        LastLoginAtUtc = nowUtc;
        AddDomainEvent(new UserAuthenticated(Guid.NewGuid(), nowUtc, Id, sessionResult.Value.Id));
        return sessionResult;
    }

    public Result RecordFailedAuthentication(int maxFailedAttempts, DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Active)
        {
            return Result.Failure(IdentityErrors.AccountNotActive);
        }

        FailedAuthenticationAttemptCount++;

        return FailedAuthenticationAttemptCount >= maxFailedAttempts
            ? Lock(nowUtc)
            : Result.Success();
    }

    private Result<Session> CreateSession(
        Guid tenantId,
        string deviceLabel,
        string? approximateLocation,
        DateTimeOffset nowUtc,
        TimeSpan sessionLifetime,
        int maxConcurrentSessions)
    {
        if (Status != UserAccountStatus.Active)
        {
            return Result.Failure<Session>(IdentityErrors.AccountNotActive);
        }

        if (tenantId != TenantId)
        {
            return Result.Failure<Session>(IdentityErrors.SessionTenantMismatch);
        }

        if (_sessions.Count(s => s.IsActive(nowUtc)) >= maxConcurrentSessions)
        {
            return Result.Failure<Session>(IdentityErrors.TooManyActiveSessions);
        }

        var session = new Session(
            new SessionId(Guid.NewGuid()),
            tenantId,
            deviceLabel,
            approximateLocation,
            nowUtc,
            nowUtc + sessionLifetime);

        _sessions.Add(session);
        return Result.Success(session);
    }

    /// <summary>
    /// Idempotent: revoking an already-revoked session succeeds without raising a
    /// second <see cref="UserLoggedOut"/>, per this document's own "RevokeMySessionCommand
    /// Never Revokes the Calling Session Silently Mid-Request" -- "a client that
    /// retries a revoke against an already-revoked session receives the same
    /// successful outcome, not an error."
    /// </summary>
    public Result RevokeSession(SessionId sessionId, DateTimeOffset nowUtc)
    {
        var session = _sessions.FirstOrDefault(s => s.Id.Equals(sessionId));
        if (session is null)
        {
            return Result.Failure(IdentityErrors.SessionNotFound);
        }

        var alreadyRevoked = session.RevokedAtUtc is not null;
        var result = session.Revoke(nowUtc);

        if (result.IsSuccess && !alreadyRevoked)
        {
            AddDomainEvent(new UserLoggedOut(Guid.NewGuid(), nowUtc, Id, sessionId));
        }

        return result;
    }

    private void RevokeAllActiveSessions(DateTimeOffset nowUtc)
    {
        foreach (var session in _sessions.Where(s => s.IsActive(nowUtc)))
        {
            session.Revoke(nowUtc);
        }
    }

    public Result<MfaFactor> EnrollMfaFactor(MfaFactorType factorType, string secretReference, DateTimeOffset nowUtc)
    {
        if (Status != UserAccountStatus.Active)
        {
            return Result.Failure<MfaFactor>(IdentityErrors.AccountNotActive);
        }

        var factor = new MfaFactor(new MfaFactorId(Guid.NewGuid()), factorType, secretReference, nowUtc);
        _mfaFactors.Add(factor);

        AddDomainEvent(new MfaEnabled(Guid.NewGuid(), nowUtc, Id, factor.Id, factorType));
        return Result.Success(factor);
    }

    public Result RemoveMfaFactor(MfaFactorId factorId, DateTimeOffset nowUtc)
    {
        var factor = _mfaFactors.FirstOrDefault(f => f.Id.Equals(factorId));
        if (factor is null)
        {
            return Result.Failure(IdentityErrors.MfaFactorNotFound);
        }

        var result = factor.Remove(nowUtc);
        if (result.IsSuccess)
        {
            AddDomainEvent(new MfaDisabled(Guid.NewGuid(), nowUtc, Id, factorId));
        }

        return result;
    }

    public void RecordExternalSynchronization(string sourceSystem, DateTimeOffset nowUtc)
    {
        AddDomainEvent(new IdentitySynchronized(Guid.NewGuid(), nowUtc, Id, sourceSystem));
    }

    public void UpdateProfile(EmailAddress emailAddress, string displayName)
    {
        Guard.AgainstNull(emailAddress, nameof(emailAddress));
        EmailAddress = emailAddress;
        DisplayName = Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName));
    }
}
