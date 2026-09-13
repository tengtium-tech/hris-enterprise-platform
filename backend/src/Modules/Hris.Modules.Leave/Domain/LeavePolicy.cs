using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// The tenant-configured, versioned, effective-dated rulebook governing accrual,
/// eligibility, entitlement, and carryover for one <see cref="LeaveType"/>. Source:
/// docs/04-modules/leave/domain/aggregates.md, leave-policies.md.
///
/// A version is created as <see cref="LeavePolicyStatus.Draft"/> with no
/// <see cref="EffectiveFrom"/> yet, published to <see cref="LeavePolicyStatus.Active"/>
/// with an effective date, and — rather than edited in place (LV-010) — superseded by a
/// new Draft version produced by <see cref="Revise"/>, which also end-dates this version
/// and transitions it to <see cref="LeavePolicyStatus.Superseded"/>. Its
/// <see cref="PolicyAssignment"/> children bind it to organizational scopes and are
/// end-dated, never deleted. LV-013's statutory floor (a policy against a statutory
/// <see cref="LeaveType"/> may not configure <see cref="EntitlementCap.MaximumAccruable"/>
/// below that type's <c>StatutoryMinimum</c>) is enforced by the publishing command
/// handler, which alone can read both aggregates — never inside this aggregate itself,
/// per Aggregate Design Rule 13's prohibition on cross-aggregate transactions.
/// </summary>
public sealed class LeavePolicy : AggregateRoot<LeavePolicyId>
{
    private readonly List<PolicyAssignment> _policyAssignments = [];

    public Guid TenantId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public Guid LineageId { get; }

    public int Version { get; }

    public LeavePolicyRuleset Ruleset { get; private set; }

    public LeavePolicyStatus Status { get; private set; }

    public DateOnly? EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    public IReadOnlyList<PolicyAssignment> PolicyAssignments => _policyAssignments.AsReadOnly();

    private LeavePolicy(
        LeavePolicyId id, Guid tenantId, LeaveTypeId leaveTypeId, Guid lineageId, int version,
        LeavePolicyRuleset ruleset, Guid createdBy, DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        LeaveTypeId = leaveTypeId;
        LineageId = lineageId;
        Version = version;
        Ruleset = ruleset;
        Status = LeavePolicyStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    public static Result<LeavePolicy> Create(
        LeavePolicyId id, Guid tenantId, LeaveTypeId leaveTypeId, LeavePolicyRuleset ruleset, Guid createdBy,
        DateTimeOffset createdOn)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        var policy = new LeavePolicy(id, tenantId, leaveTypeId, id.Value, 1, ruleset, createdBy, createdOn);
        return Result.Success(policy);
    }

    /// <summary>Publishes a Draft version: sets its effective date and marks it Active.</summary>
    public Result Publish(DateOnly effectiveFrom, Guid actorId, DateTimeOffset publishedOnUtc)
    {
        if (Status != LeavePolicyStatus.Draft)
        {
            return Result.Failure(LeaveErrors.PolicyVersionNotDraft);
        }

        EffectiveFrom = effectiveFrom;
        Status = LeavePolicyStatus.Active;
        AddDomainEvent(new LeavePolicyPublished(Guid.NewGuid(), publishedOnUtc, Id, TenantId, effectiveFrom, actorId));
        return Result.Success();
    }

    /// <summary>
    /// Produces the next version as a Draft, leaving this one's own <see cref="Ruleset"/>
    /// untouched (LV-010). This version is end-dated and transitioned to
    /// <see cref="LeavePolicyStatus.Superseded"/> so an evaluation date resolves to exactly
    /// one version; the new Draft still needs its own <see cref="Publish"/> call before it
    /// is evaluable — revising does not itself activate it, the same two-step shape
    /// leave-policies.md describes for workflow-routed approval of policy changes.
    /// </summary>
    public Result<LeavePolicy> Revise(
        LeavePolicyId newId, LeavePolicyRuleset newRuleset, DateOnly newEffectiveFrom, Guid changedBy,
        DateTimeOffset revisedOnUtc)
    {
        if (Status != LeavePolicyStatus.Active)
        {
            return Result.Failure<LeavePolicy>(LeaveErrors.PolicyVersionNotActive);
        }

        if (newEffectiveFrom <= EffectiveFrom!.Value)
        {
            return Result.Failure<LeavePolicy>(LeaveErrors.PolicyRevisionEffectiveDateMustAdvance);
        }

        var next = new LeavePolicy(newId, TenantId, LeaveTypeId, LineageId, Version + 1, newRuleset, changedBy, revisedOnUtc);

        EffectiveTo = newEffectiveFrom.AddDays(-1);
        Status = LeavePolicyStatus.Superseded;
        AddDomainEvent(new LeavePolicySuperseded(Guid.NewGuid(), revisedOnUtc, Id, TenantId, next.Id, changedBy));

        return Result.Success(next);
    }

    /// <summary>
    /// Binds this policy to a scope. Refuses an overlapping effective period for the same
    /// scope target within this policy (LV-012); cross-policy overlap is enforced by the
    /// assigning handler against the other policy versions' assignments.
    /// </summary>
    public Result Assign(
        PolicyAssignmentId assignmentId, LeavePolicyScopeLevel scopeLevel, string scopeTargetId, DateOnly effectiveFrom,
        DateOnly? effectiveTo, Guid assignedBy, DateTimeOffset assignedOn)
    {
        if (Status != LeavePolicyStatus.Draft && Status != LeavePolicyStatus.Active)
        {
            return Result.Failure(LeaveErrors.LeavePolicyNotAssignable);
        }

        foreach (var existing in _policyAssignments)
        {
            if (existing.ScopeTargetId == scopeTargetId
                && existing.ScopeLevel == scopeLevel
                && existing.OverlapsPeriod(effectiveFrom, effectiveTo))
            {
                return Result.Failure(LeaveErrors.PolicyAssignmentOverlap);
            }
        }

        var assignment = new PolicyAssignment(assignmentId, scopeLevel, scopeTargetId, effectiveFrom, effectiveTo, assignedBy, assignedOn);
        _policyAssignments.Add(assignment);
        AddDomainEvent(new LeavePolicyAssigned(Guid.NewGuid(), assignedOn, Id, TenantId, assignmentId, scopeTargetId, assignedBy));
        return Result.Success();
    }

    /// <summary>
    /// End-dates rather than removes the assignment (entities.md's retention rule).
    /// Unlike <see cref="Assign"/>, no <c>LeavePolicyUnassigned</c> domain event exists in
    /// domain-events.md's own catalog to raise — the actor and timestamp an
    /// <c>UnassignLeavePolicyCommand</c> otherwise carries are for the audit record the
    /// command dispatch itself produces, not for a bespoke event this method would raise.
    /// </summary>
    public Result Unassign(PolicyAssignmentId assignmentId, DateOnly effectiveTo)
    {
        var assignment = _policyAssignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Result.Failure(LeaveErrors.PolicyAssignmentNotFound);
        }

        assignment.EndOn(effectiveTo);
        return Result.Success();
    }

    /// <summary>
    /// No <c>LeavePolicyRetired</c> domain event exists in domain-events.md's own catalog
    /// either, for the identical reason <see cref="Unassign"/>'s own remarks give.
    /// </summary>
    public Result Retire()
    {
        Status = LeavePolicyStatus.Retired;
        return Result.Success();
    }

    /// <summary>
    /// A version is effective only while Active and within its own effective window.
    /// Checking <see cref="Status"/> here (unlike the otherwise-identical
    /// <c>AttendancePolicy.IsEffectiveOn</c>, which relies solely on its caller having
    /// already filtered by status) closes a gap for any future direct domain-level caller
    /// that has not pre-filtered.
    /// </summary>
    public bool IsEffectiveOn(DateOnly date) =>
        Status == LeavePolicyStatus.Active && EffectiveFrom.HasValue && EffectiveFrom.Value <= date
        && (EffectiveTo is null || date <= EffectiveTo.Value);
}
