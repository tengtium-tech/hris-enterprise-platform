using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The configurable rulebook the calculation engine evaluates every record against,
/// authored as effective-dated versions. Source: docs/04-modules/attendance/domain/
/// aggregates.md (AttendancePolicy).
///
/// A version is created as <see cref="AttendancePolicyStatus.Draft"/>, published to
/// <see cref="AttendancePolicyStatus.Active"/> with an effective date, and superseded by
/// a new version rather than edited in place (AT-002). Its <see cref="PolicyAssignment"/>
/// children bind it to organizational scopes and are end-dated, never deleted. The
/// engine-facing configuration is held as a single <see cref="PolicyCalculationConfiguration"/>
/// so <c>RunCalculationCommand</c> can hand it to <see cref="AttendanceCalculationEngine"/>
/// unchanged.
/// </summary>
public sealed class AttendancePolicy : AggregateRoot<AttendancePolicyId>
{
    public Guid TenantId { get; }

    public Guid LineageId { get; }

    public string Name { get; private set; }

    public PolicyCalculationConfiguration Configuration { get; private set; }

    public AttendancePolicyStatus Status { get; private set; }

    public int Version { get; }

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    private readonly List<PolicyAssignment> _policyAssignments = new();

    public IReadOnlyList<PolicyAssignment> PolicyAssignments => _policyAssignments.AsReadOnly();

    private AttendancePolicy(
        AttendancePolicyId id, Guid tenantId, Guid lineageId, string name,
        PolicyCalculationConfiguration configuration, int version, DateOnly effectiveFrom, Guid createdBy,
        DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        LineageId = lineageId;
        Name = name;
        Configuration = configuration;
        Version = version;
        EffectiveFrom = effectiveFrom;
        Status = AttendancePolicyStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    public static Result<AttendancePolicy> Create(
        AttendancePolicyId id, Guid tenantId, string? name, PolicyCalculationConfiguration configuration,
        DateOnly effectiveFrom, Guid createdBy, DateTimeOffset createdOn)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<AttendancePolicy>(AttendanceErrors.PolicyNameRequired);
        }

        var policy = new AttendancePolicy(
            id, tenantId, id.Value, name.Trim(), configuration, 1, effectiveFrom, createdBy, createdOn);
        policy.AddDomainEvent(new AttendancePolicyDefined(
            Guid.NewGuid(), createdOn, id, tenantId, createdBy));
        return Result.Success(policy);
    }

    /// <summary>Publishes a Draft version; sets its effective date and marks it Active.</summary>
    public Result Publish(DateOnly effectiveFrom, Guid actorId, DateTimeOffset nowUtc)
    {
        if (Status != AttendancePolicyStatus.Draft)
        {
            return Result.Failure(AttendanceErrors.PolicyVersionNotDraft);
        }

        Status = AttendancePolicyStatus.Active;
        AddDomainEvent(new AttendancePolicyPublished(
            Guid.NewGuid(), nowUtc, Id, TenantId, effectiveFrom, actorId));
        return Result.Success();
    }

    /// <summary>
    /// Produces the next version as a Draft, leaving this one byte-identical (AT-002). Its
    /// own <see cref="EffectiveTo"/> is set so a work date resolves to exactly one version.
    /// </summary>
    public Result<AttendancePolicy> Revise(
        AttendancePolicyId newId, PolicyCalculationConfiguration configuration, DateOnly newEffectiveFrom,
        Guid changedBy, DateTimeOffset nowUtc)
    {
        if (Status != AttendancePolicyStatus.Active)
        {
            return Result.Failure<AttendancePolicy>(AttendanceErrors.PolicyVersionNotActive);
        }

        if (newEffectiveFrom <= EffectiveFrom)
        {
            return Result.Failure<AttendancePolicy>(AttendanceErrors.PolicyRevisionEffectiveDateMustAdvance);
        }

        var next = new AttendancePolicy(
            newId, TenantId, LineageId, Name, configuration, Version + 1, newEffectiveFrom, changedBy, nowUtc);
        next.Status = AttendancePolicyStatus.Draft;

        EffectiveTo = newEffectiveFrom.AddDays(-1);
        AddDomainEvent(new AttendancePolicyRevised(
            Guid.NewGuid(), nowUtc, Id, TenantId, newId, newEffectiveFrom, changedBy));
        return Result.Success(next);
    }

    /// <summary>
    /// Binds this policy to a scope. Refuses an overlapping effective period for the same
    /// scope target within this policy (AT-011); cross-policy overlap is enforced by the
    /// assigning handler against the other policy versions' assignments.
    /// </summary>
    public Result Assign(
        PolicyAssignmentId assignmentId, PolicyScopeLevel scopeLevel, string scopeTargetId, DateOnly effectiveFrom,
        DateOnly? effectiveTo, Guid assignedBy, DateTimeOffset assignedOn)
    {
        if (Status != AttendancePolicyStatus.Active && Status != AttendancePolicyStatus.Draft)
        {
            return Result.Failure(AttendanceErrors.PolicyVersionNotDraft);
        }

        foreach (var existing in _policyAssignments)
        {
            if (existing.ScopeTargetId == scopeTargetId
                && existing.ScopeLevel == scopeLevel
                && existing.OverlapsPeriod(effectiveFrom, effectiveTo))
            {
                return Result.Failure(AttendanceErrors.PolicyAssignmentOverlap);
            }
        }

        var assignment = new PolicyAssignment(
            assignmentId, scopeLevel, scopeTargetId, effectiveFrom, effectiveTo, assignedBy, assignedOn);
        _policyAssignments.Add(assignment);
        AddDomainEvent(new AttendancePolicyAssigned(
            Guid.NewGuid(), assignedOn, Id, TenantId, assignmentId, scopeTargetId, effectiveFrom, assignedBy));
        return Result.Success();
    }

    public Result Unassign(PolicyAssignmentId assignmentId, DateOnly effectiveTo, Guid actorId, DateTimeOffset nowUtc)
    {
        var assignment = _policyAssignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return Result.Failure(AttendanceErrors.PolicyAssignmentNotFound);
        }

        assignment.EndOn(effectiveTo);
        AddDomainEvent(new AttendancePolicyUnassigned(
            Guid.NewGuid(), nowUtc, Id, TenantId, assignmentId, effectiveTo, actorId));
        return Result.Success();
    }

    public Result Retire(Guid actorId, string reason, DateTimeOffset nowUtc)
    {
        if (Status == AttendancePolicyStatus.Retired)
        {
            return Result.Success();
        }

        Status = AttendancePolicyStatus.Retired;
        AddDomainEvent(new AttendancePolicyRetired(Guid.NewGuid(), nowUtc, Id, TenantId, actorId, reason));
        return Result.Success();
    }

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || date <= EffectiveTo.Value);
}
