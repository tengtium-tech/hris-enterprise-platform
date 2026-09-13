using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Binds a <see cref="LeavePolicy"/> to an organizational scope for an effective period.
/// A child entity of that policy, never its own root. Source:
/// docs/04-modules/leave/domain/entities.md (PolicyAssignment).
///
/// Two assignments of different policy versions to the same scope target may not overlap
/// (LV-012); the parent's Assign/Unassign methods enforce that against the existing set. A
/// superseded assignment is end-dated and retained, never deleted — mirrors
/// <c>Hris.Modules.Attendance</c>'s own <c>PolicyAssignment</c>.
/// </summary>
public sealed class PolicyAssignment : Entity<PolicyAssignmentId>
{
    public LeavePolicyScopeLevel ScopeLevel { get; }

    /// <summary>
    /// Identifier of the target at <see cref="ScopeLevel"/>. A plain string because the
    /// target is a company, legal entity, business unit, department, position, employee
    /// group, employment type, or individual employee depending on the level, each owned
    /// by a different module and referenced by identifier only.
    /// </summary>
    public string ScopeTargetId { get; }

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public Guid AssignedBy { get; }

    public DateTimeOffset AssignedOn { get; }

    internal PolicyAssignment(
        PolicyAssignmentId id, LeavePolicyScopeLevel scopeLevel, string scopeTargetId, DateOnly effectiveFrom,
        DateOnly? effectiveTo, Guid assignedBy, DateTimeOffset assignedOn)
        : base(id)
    {
        ScopeLevel = scopeLevel;
        ScopeTargetId = Guard.AgainstNullOrWhiteSpace(scopeTargetId, nameof(scopeTargetId));
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        AssignedBy = assignedBy;
        AssignedOn = assignedOn;
    }

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || date <= EffectiveTo.Value);

    /// <summary>True where this assignment's period overlaps <paramref name="from"/> to <paramref name="to"/>.</summary>
    public bool OverlapsPeriod(DateOnly from, DateOnly? to) =>
        (to is null || EffectiveFrom <= to.Value) && (EffectiveTo is null || from <= EffectiveTo.Value);

    /// <summary>End-dates rather than deletes, per the retention rule.</summary>
    internal void EndOn(DateOnly effectiveTo) => EffectiveTo = effectiveTo;
}
