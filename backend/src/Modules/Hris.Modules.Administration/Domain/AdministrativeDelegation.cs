using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Aggregate Root representing a time-bounded transfer of a user's authority to
/// another user, without granting that authority permanently. Source:
/// docs/04-modules/administration/domain/aggregates.md and
/// delegated-administration.md.
///
/// Deliberately not implemented as "grant the role, revoke it later" --
/// delegated-administration.md's own "Delegation Is Not a Grant" section: ending
/// is automatic at a stored end date, the audit record distinguishes acting under
/// delegation from acting under one's own authority, and delegated authority
/// cannot be delegated onward (AR-042). A delegation involves two accounts and
/// belongs to neither, which is why it is its own Aggregate Root rather than
/// nested inside either <see cref="UserAccount"/>.
/// </summary>
public sealed class AdministrativeDelegation : AggregateRoot<DelegationId>
{
    public Guid TenantId { get; }

    public Guid DelegatorUserAccountId { get; }

    public Guid DelegateUserAccountId { get; }

    /// <summary>
    /// Mapped as a single JSON-serialized column, not an owned collection --
    /// <see cref="DelegatedAuthorityItem"/> has no identity of its own (it is a
    /// value, not an entity), and this Sprint keeps every owned-type mapping to
    /// the one proven nesting depth (root Aggregate -&gt; owned collection -&gt; one
    /// nested owned value, as <c>CompensationRecord.Amount</c> already
    /// established) rather than adding a second, less-proven EF Core shape
    /// (an owned collection with no explicit key) with no live Postgres in this
    /// sandbox to verify it against beyond the Model-build smoke test.
    /// </summary>
    public IReadOnlyList<DelegatedAuthorityItem> DelegatedAuthority { get; private set; } = [];

    public DateRange Period { get; private set; } = null!;

    public string Reason { get; private set; } = null!;

    public Guid? ApprovalReference { get; private set; }

    public DelegationStatus Status { get; private set; }

    public DateTimeOffset CreatedOn { get; }

    private AdministrativeDelegation(
        DelegationId id, Guid tenantId, Guid delegatorUserAccountId, Guid delegateUserAccountId, DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        DelegatorUserAccountId = delegatorUserAccountId;
        DelegateUserAccountId = delegateUserAccountId;
        Status = DelegationStatus.Scheduled;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// <paramref name="authorityExceedsDelegatorHoldings"/> (AR-041) and
    /// <paramref name="violatesDelegateSeparationOfDuties"/> (AR-045) are computed
    /// by the calling Application-layer command handler: the former by checking
    /// <paramref name="delegatedAuthority"/> against the delegator's own loaded
    /// <see cref="UserAccount"/>; the latter by combining the delegate's own
    /// effective canonical roles with <paramref name="delegatedAuthority"/> and
    /// calling <see cref="UserAccount.ViolatesSeparationOfDuties"/> -- both
    /// cross-aggregate-instance facts this Aggregate cannot compute about itself.
    /// Always created <see cref="DelegationStatus.Scheduled"/>, per
    /// delegated-administration.md's own "Scheduled Confers Nothing": a separate
    /// <see cref="Activate"/> call, not this factory, is what the Authorization
    /// Framework acts on.
    /// </summary>
    public static Result<AdministrativeDelegation> Create(
        DelegationId id, Guid tenantId, Guid delegatorUserAccountId, Guid delegateUserAccountId,
        IReadOnlyList<DelegatedAuthorityItem> delegatedAuthority, DateOnly periodStart, DateOnly periodEnd, string? reason,
        Guid? approvalReference, bool authorityExceedsDelegatorHoldings, bool violatesDelegateSeparationOfDuties,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(delegatedAuthority);

        if (delegatorUserAccountId == delegateUserAccountId)
        {
            return Result.Failure<AdministrativeDelegation>(AdministrationErrors.DelegatorEqualsDelegate);
        }

        if (delegatedAuthority.Count == 0)
        {
            return Result.Failure<AdministrativeDelegation>(AdministrationErrors.DelegatedAuthorityEmpty);
        }

        if (authorityExceedsDelegatorHoldings)
        {
            return Result.Failure<AdministrativeDelegation>(AdministrationErrors.DelegatedAuthorityExceedsDelegatorHoldings);
        }

        if (violatesDelegateSeparationOfDuties)
        {
            return Result.Failure<AdministrativeDelegation>(AdministrationErrors.DelegationSeparationOfDutiesViolation);
        }

        var periodResult = DateRange.Create(periodStart, periodEnd);
        if (periodResult.IsFailure)
        {
            return Result.Failure<AdministrativeDelegation>(periodResult.Error);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<AdministrativeDelegation>(AdministrationErrors.GrantReasonRequired);
        }

        var delegation = new AdministrativeDelegation(id, tenantId, delegatorUserAccountId, delegateUserAccountId, nowUtc)
        {
            DelegatedAuthority = delegatedAuthority.ToList(),
            Period = periodResult.Value,
            Reason = reason.Trim(),
            ApprovalReference = approvalReference,
        };

        delegation.AddDomainEvent(new AdministrativeDelegationCreated(
            Guid.NewGuid(), nowUtc, id, delegatorUserAccountId, delegateUserAccountId, reason.Trim(), approvalReference));

        return Result.Success(delegation);
    }

    /// <summary>
    /// AR-041 is re-validated at activation, not only at creation, since the
    /// delegator's own authority may have changed in the interim.
    /// <paramref name="authorityExceedsDelegatorHoldings"/> and
    /// <paramref name="violatesDelegateSeparationOfDuties"/> are recomputed by the
    /// caller against current state, the same shape <see cref="Create"/> uses.
    /// </summary>
    public Result Activate(bool authorityExceedsDelegatorHoldings, bool violatesDelegateSeparationOfDuties, DateTimeOffset nowUtc)
    {
        if (Status != DelegationStatus.Scheduled)
        {
            return Result.Failure(AdministrationErrors.DelegationNotScheduled);
        }

        if (authorityExceedsDelegatorHoldings)
        {
            return Result.Failure(AdministrationErrors.DelegatedAuthorityExceedsDelegatorHoldings);
        }

        if (violatesDelegateSeparationOfDuties)
        {
            return Result.Failure(AdministrationErrors.DelegationSeparationOfDutiesViolation);
        }

        Status = DelegationStatus.Active;
        AddDomainEvent(new AdministrativeDelegationActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>Automatic, no actor (AR-043).</summary>
    public Result Expire(DateTimeOffset nowUtc)
    {
        if (Status != DelegationStatus.Active)
        {
            return Result.Failure(AdministrationErrors.DelegationNotActive);
        }

        Status = DelegationStatus.Expired;
        AddDomainEvent(new AdministrativeDelegationExpired(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>Idempotent: revoking an already-terminal delegation succeeds without change.</summary>
    public Result Revoke(Guid revokedBy, string? reason, DateTimeOffset nowUtc)
    {
        if (Status is DelegationStatus.Expired or DelegationStatus.Revoked)
        {
            return Result.Success();
        }

        Status = DelegationStatus.Revoked;
        AddDomainEvent(new AdministrativeDelegationRevoked(Guid.NewGuid(), nowUtc, Id, revokedBy, reason ?? string.Empty));
        return Result.Success();
    }
}
