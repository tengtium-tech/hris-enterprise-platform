using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Binds a <see cref="WorkSchedule"/> to an organizational unit for an effective
/// period. A child entity of that schedule, never its own root. Source:
/// docs/04-modules/timekeeping/domain/entities.md.
///
/// This is the deliberate opposite of <see cref="ShiftAssignment"/>, and
/// aggregates.md says so outright rather than leaving the asymmetry to look like an
/// oversight: a schedule assignment is coarse, infrequent, and reviewed together
/// with the schedule it adopts — a tenant moves "the Manila office" between
/// schedules a handful of times a year — so there is no independent swap mechanism
/// and no separation-of-duties-style requirement to read the complete assignment set
/// atomically. There is consequently no <c>ScheduleAssignmentRepository</c>.
///
/// A superseded assignment is end-dated and retained, never deleted (TK-012), so
/// "which schedule applied to this department last March" stays answerable.
/// </summary>
public sealed class ScheduleAssignment : Entity<ScheduleAssignmentId>
{
    public OrganizationalAssignmentLevel TargetLevel { get; }

    /// <summary>
    /// Identifier of the target at <see cref="TargetLevel"/>. A plain string because
    /// the target is an organizational unit, a position, an employment type, or an
    /// employee group depending on the level, each owned by a different module and
    /// referenced by identifier only.
    /// </summary>
    public string TargetId { get; }

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public Guid AssignedBy { get; }

    public DateTimeOffset AssignedOn { get; }

    internal ScheduleAssignment(
        ScheduleAssignmentId id, OrganizationalAssignmentLevel targetLevel, string targetId, DateOnly effectiveFrom,
        DateOnly? effectiveTo, Guid assignedBy, DateTimeOffset assignedOn)
        : base(id)
    {
        TargetLevel = targetLevel;
        TargetId = targetId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        AssignedBy = assignedBy;
        AssignedOn = assignedOn;
    }

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || date <= EffectiveTo.Value);

    /// <summary>
    /// True where this assignment's period overlaps <paramref name="from"/> to
    /// <paramref name="to"/>. An open-ended period (null <paramref name="to"/>)
    /// extends indefinitely, which is what makes TK-011's overlap check total.
    /// </summary>
    public bool OverlapsPeriod(DateOnly from, DateOnly? to) =>
        (to is null || EffectiveFrom <= to.Value) && (EffectiveTo is null || from <= EffectiveTo.Value);

    /// <summary>End-dates rather than deletes, per TK-012.</summary>
    internal void EndOn(DateOnly effectiveTo) => EffectiveTo = effectiveTo;
}
