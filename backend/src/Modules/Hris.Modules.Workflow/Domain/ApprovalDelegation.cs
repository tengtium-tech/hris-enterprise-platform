using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Aggregate Root representing a bounded transfer of a user's approval authority to
/// another user. Source: docs/04-modules/workflow/domain/approval-delegation.md.
///
/// Deliberately not administration's <c>AdministrativeDelegation</c>, and
/// deliberately not merged with it (WR-044). That one transfers role-granting and
/// account-management authority; this one transfers the authority to approve
/// business requests. Merging them would mean covering someone's approvals also
/// conferred their ability to grant roles and manage accounts, a privilege
/// escalation dressed as convenience. Where a person genuinely needs both kinds of
/// coverage, two delegations are created, each reviewed and revocable on its own
/// terms.
///
/// Its own aggregate root for the same reason administration's is: it involves two
/// users and belongs to neither. Placing it inside either would make the other's
/// picture incomplete and make revocation modify an aggregate the revoker does not
/// own.
///
/// One documented asymmetry with administration's delegation is intentional. That
/// module's AR-044 ends a delegation automatically when the delegator's own role
/// assignment is revoked, because it is tied to a specific revocable grant. This one
/// has no equivalent, because <see cref="Scope"/> names business processes rather
/// than a role, so there is no single "the delegator's authority was revoked" event
/// to react to. Where a delegator's standing to approve a process genuinely changes
/// mid-delegation, resolution simply stops routing that process to them and
/// consequently stops offering it to the delegate; the delegation does not need to
/// be told, because it has nothing left to act on.
/// </summary>
public sealed class ApprovalDelegation : AggregateRoot<ApprovalDelegationId>
{
    public Guid TenantId { get; }

    public Guid DelegatorUserAccountId { get; }

    public Guid DelegateUserAccountId { get; }

    /// <summary>
    /// The business processes this delegation covers. Persisted as a single
    /// JSON-serialized column: these are identifiers with no lifecycle of their own,
    /// the pattern <c>DelegatedAuthorityItem</c> established.
    ///
    /// It is deliberately not tied to a role or a definition. It covers whichever
    /// approvals resolution finds the delegator for, across role, reporting-line,
    /// and organizational-scope resolution alike, because the person going on leave
    /// does not know in advance which specific requests will arrive.
    /// </summary>
    public IReadOnlyList<Guid> Scope { get; private set; } = [];

    /// <summary>
    /// Where true, the delegation covers every process resolution would route to the
    /// delegator, and <see cref="Scope"/> is empty by design rather than by omission.
    /// </summary>
    public bool CoversAllProcesses { get; private set; }

    /// <summary>
    /// Required at both ends (WR-040). An unbounded delegation is a standing grant of
    /// approval authority and must be made through administration's role assignment
    /// instead: coverage that outlives the absence it was arranged for is the same
    /// forgotten-elevation problem delegation exists to prevent.
    /// </summary>
    public DateRange Period { get; private set; } = null!;

    public string Reason { get; private set; } = null!;

    public Guid? ApprovalReference { get; private set; }

    public ApprovalDelegationStatus Status { get; private set; }

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    public Guid? RevokedBy { get; private set; }

    public DateTimeOffset? RevokedOn { get; private set; }

    private ApprovalDelegation(
        ApprovalDelegationId id, Guid tenantId, Guid delegatorUserAccountId, Guid delegateUserAccountId, Guid createdBy,
        DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        DelegatorUserAccountId = delegatorUserAccountId;
        DelegateUserAccountId = delegateUserAccountId;
        Status = ApprovalDelegationStatus.Scheduled;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// <paramref name="createdBy"/> and <paramref name="delegatorUserAccountId"/> are
    /// recorded separately, as commands.md requires, because they differ when an
    /// administrator creates coverage on behalf of an absent manager.
    ///
    /// <paramref name="scopeExceedsDelegatorStanding"/> (WR-041) and
    /// <paramref name="authorityIsItselfDelegated"/> (WR-042) are computed by the
    /// calling handler against the delegator's current approval standing, facts this
    /// aggregate cannot see about itself.
    ///
    /// Always created <see cref="ApprovalDelegationStatus.Scheduled"/>: a separate
    /// <see cref="Activate"/> call, not this factory, is what routing acts on.
    /// </summary>
    public static Result<ApprovalDelegation> Create(
        ApprovalDelegationId id, Guid tenantId, Guid delegatorUserAccountId, Guid delegateUserAccountId,
        IReadOnlyList<Guid>? scope, bool coversAllProcesses, DateOnly periodStart, DateOnly periodEnd, string? reason,
        Guid? approvalReference, bool scopeExceedsDelegatorStanding, bool authorityIsItselfDelegated, Guid createdBy,
        DateTimeOffset nowUtc)
    {
        if (delegatorUserAccountId == delegateUserAccountId)
        {
            return Result.Failure<ApprovalDelegation>(WorkflowErrors.DelegatorEqualsDelegate);
        }

        var resolvedScope = scope?.Distinct().ToList() ?? [];

        if (!coversAllProcesses && resolvedScope.Count == 0)
        {
            return Result.Failure<ApprovalDelegation>(WorkflowErrors.DelegationScopeEmpty);
        }

        if (scopeExceedsDelegatorStanding)
        {
            return Result.Failure<ApprovalDelegation>(WorkflowErrors.DelegationScopeExceedsDelegatorStanding);
        }

        if (authorityIsItselfDelegated)
        {
            return Result.Failure<ApprovalDelegation>(WorkflowErrors.DelegationOfDelegatedAuthority);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<ApprovalDelegation>(WorkflowErrors.DelegationReasonRequired);
        }

        var periodResult = DateRange.Create(periodStart, periodEnd);
        if (periodResult.IsFailure)
        {
            return Result.Failure<ApprovalDelegation>(periodResult.Error);
        }

        var delegation = new ApprovalDelegation(id, tenantId, delegatorUserAccountId, delegateUserAccountId, createdBy, nowUtc)
        {
            Scope = coversAllProcesses ? [] : resolvedScope,
            CoversAllProcesses = coversAllProcesses,
            Period = periodResult.Value,
            Reason = reason.Trim(),
            ApprovalReference = approvalReference,
        };

        delegation.AddDomainEvent(new ApprovalDelegationCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, delegatorUserAccountId, delegateUserAccountId,
            delegation.Scope, periodStart, periodEnd, reason.Trim()));

        return Result.Success(delegation);
    }

    /// <summary>
    /// WR-041 is re-validated here, not only at creation: weeks may pass between
    /// scheduling and the period beginning, and the delegator's own standing may
    /// have changed in the interim.
    /// </summary>
    public Result Activate(bool scopeExceedsDelegatorStanding, DateTimeOffset nowUtc)
    {
        if (Status != ApprovalDelegationStatus.Scheduled)
        {
            return Result.Failure(WorkflowErrors.DelegationNotScheduled);
        }

        if (scopeExceedsDelegatorStanding)
        {
            return Result.Failure(WorkflowErrors.DelegationScopeExceedsDelegatorStanding);
        }

        Status = ApprovalDelegationStatus.Active;
        AddDomainEvent(new ApprovalDelegationActivated(Guid.NewGuid(), nowUtc, Id, TenantId));
        return Result.Success();
    }

    /// <summary>Automatic at period end, no actor (WR-043).</summary>
    public Result Expire(DateTimeOffset nowUtc)
    {
        if (Status != ApprovalDelegationStatus.Active)
        {
            return Result.Failure(WorkflowErrors.DelegationNotActive);
        }

        Status = ApprovalDelegationStatus.Expired;
        AddDomainEvent(new ApprovalDelegationExpired(Guid.NewGuid(), nowUtc, Id, TenantId));
        return Result.Success();
    }

    /// <summary>
    /// Idempotent by convergence, per commands.md: revoking an already-terminal
    /// delegation succeeds without change, because failing the retry would leave
    /// callers uncertain whether it took effect. A revoked delegation is retained,
    /// never deleted, so what was approved under it remains attributable.
    /// </summary>
    public Result Revoke(Guid revokedBy, string? reason, DateTimeOffset nowUtc)
    {
        if (Status is ApprovalDelegationStatus.Expired or ApprovalDelegationStatus.Revoked)
        {
            return Result.Success();
        }

        Status = ApprovalDelegationStatus.Revoked;
        RevokedBy = revokedBy;
        RevokedOn = nowUtc;

        AddDomainEvent(new ApprovalDelegationRevoked(
            Guid.NewGuid(), nowUtc, Id, TenantId, revokedBy, reason?.Trim() ?? string.Empty));

        return Result.Success();
    }

    /// <summary>
    /// Whether this delegation would offer <paramref name="businessProcessId"/> to
    /// the delegate on <paramref name="asOfDate"/>. Requires an active status as well
    /// as a covering period: a scheduled delegation confers nothing, and an expired
    /// or revoked one confers nothing either.
    /// </summary>
    public bool CoversProcessOn(Guid businessProcessId, DateOnly asOfDate) =>
        Status == ApprovalDelegationStatus.Active
        && Period.Contains(asOfDate)
        && (CoversAllProcesses || Scope.Contains(businessProcessId));
}
