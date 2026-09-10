using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Aggregate Root representing the recurring pattern of working days, standard
/// hours, breaks, and rest days, together with the coarse organizational
/// assignments that adopt it. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md and work-schedules.md.
///
/// Assignments live inside this aggregate rather than standing alone because they
/// are coarse, infrequent, and reviewed together with the schedule itself. There is
/// no <c>ScheduleAssignmentRepository</c> (CTR-ARC-004). See
/// <see cref="ShiftAssignment"/> for the deliberately opposite decision and why the
/// two differ.
///
/// Versioning follows TK-001: a superseded version is never edited. Revising
/// produces a new aggregate instance sharing this one's lineage, and the version it
/// replaces is retained byte-identical, because attendance evaluations and payroll
/// computations already read it.
/// </summary>
public sealed class WorkSchedule : AggregateRoot<WorkScheduleId>
{
    private readonly List<ScheduleAssignment> _scheduleAssignments = [];

    public Guid TenantId { get; }

    /// <summary>Shared across every version of this schedule.</summary>
    public Guid LineageId { get; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public WorkingDayPattern WorkingDayPattern { get; internal set; } = null!;

    /// <summary>
    /// Absent for shift-driven schedules with no fixed hours of their own, per
    /// entities.md — such a schedule states which days are worked and leaves the
    /// hours to whichever shift the employee resolves to.
    /// </summary>
    public TimeWindow? StandardHours { get; internal set; }

    public IReadOnlyList<BreakRule> BreakPeriods { get; internal set; } = [];

    public int Version { get; }

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public WorkScheduleStatus Status { get; private set; }

    public IReadOnlyList<ScheduleAssignment> ScheduleAssignments => _scheduleAssignments.AsReadOnly();

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    private WorkSchedule(
        WorkScheduleId id, Guid tenantId, Guid lineageId, int version, DateOnly effectiveFrom, Guid createdBy,
        DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        LineageId = lineageId;
        Version = version;
        EffectiveFrom = effectiveFrom;
        Status = WorkScheduleStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    public static Result<WorkSchedule> Create(
        WorkScheduleId id, Guid tenantId, string? name, string? description, IReadOnlyList<DayOfWeek>? workingDays,
        TimeWindow? standardHours, IReadOnlyList<BreakRule>? breakPeriods, DateOnly effectiveFrom, Guid createdBy,
        DateTimeOffset createdOn) =>
        Build(id, tenantId, id.Value, 1, name, description, workingDays, standardHours, breakPeriods, effectiveFrom,
            createdBy, createdOn);

    /// <summary>
    /// Shared construction for a first version and a superseding one. The lineage and
    /// version are parameters rather than derived, which is what lets
    /// <see cref="Supersede"/> produce a new instance carrying this one's lineage
    /// without reaching around the type's own encapsulation to set it afterward.
    /// </summary>
    private static Result<WorkSchedule> Build(
        WorkScheduleId id, Guid tenantId, Guid lineageId, int version, string? name, string? description,
        IReadOnlyList<DayOfWeek>? workingDays, TimeWindow? standardHours, IReadOnlyList<BreakRule>? breakPeriods,
        DateOnly effectiveFrom, Guid createdBy, DateTimeOffset createdOn)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkSchedule>(TimekeepingErrors.WorkScheduleNameRequired);
        }

        var patternResult = WorkingDayPattern.Create(workingDays);
        if (patternResult.IsFailure)
        {
            return Result.Failure<WorkSchedule>(patternResult.Error);
        }

        var breaks = breakPeriods?.ToList() ?? [];
        if (breaks.Any(rule => rule.Mandatory && rule.Duration <= TimeSpan.Zero))
        {
            return Result.Failure<WorkSchedule>(TimekeepingErrors.MandatoryBreakRequiresPositiveDuration);
        }

        var schedule = new WorkSchedule(id, tenantId, lineageId, version, effectiveFrom, createdBy, createdOn)
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            WorkingDayPattern = patternResult.Value,
            StandardHours = standardHours,
            BreakPeriods = breaks,
        };

        return Result.Success(schedule);
    }

    public Result Publish(Guid publishedBy, DateTimeOffset nowUtc)
    {
        if (Status == WorkScheduleStatus.Superseded)
        {
            return Result.Failure(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (Status != WorkScheduleStatus.Draft)
        {
            return Result.Failure(TimekeepingErrors.VersionNotActive);
        }

        Status = WorkScheduleStatus.Active;
        AddDomainEvent(new WorkSchedulePublished(
            Guid.NewGuid(), nowUtc, Id, TenantId, Version, EffectiveFrom, publishedBy));

        return Result.Success();
    }

    /// <summary>
    /// Produces the next version as a new aggregate instance sharing this lineage,
    /// and end-dates this one the day before the new version takes effect. This
    /// instance's own content is never altered (TK-001), so a historical query still
    /// returns exactly what it returned before.
    ///
    /// The new version's effective date must fall after this one's, or the two would
    /// both apply on the same date and TK-002's resolution would be ambiguous.
    /// </summary>
    public Result<WorkSchedule> Supersede(
        WorkScheduleId newId, IReadOnlyList<DayOfWeek>? workingDays, TimeWindow? standardHours,
        IReadOnlyList<BreakRule>? breakPeriods, DateOnly newEffectiveFrom, Guid createdBy, DateTimeOffset nowUtc)
    {
        if (Status != WorkScheduleStatus.Active)
        {
            return Result.Failure<WorkSchedule>(TimekeepingErrors.VersionNotActive);
        }

        if (newEffectiveFrom <= EffectiveFrom)
        {
            return Result.Failure<WorkSchedule>(TimekeepingErrors.EffectiveFromNotAfterCurrentVersion);
        }

        var nextResult = Build(
            newId, TenantId, LineageId, Version + 1, Name, Description, workingDays, standardHours, breakPeriods,
            newEffectiveFrom, createdBy, nowUtc);
        if (nextResult.IsFailure)
        {
            return nextResult;
        }

        var next = nextResult.Value;

        Status = WorkScheduleStatus.Superseded;
        EffectiveTo = newEffectiveFrom.AddDays(-1);

        next.AddDomainEvent(new WorkScheduleSuperseded(
            Guid.NewGuid(), nowUtc, next.Id, TenantId, Version, next.Version, newEffectiveFrom));

        return Result.Success(next);
    }

    /// <summary>
    /// TK-011: two assignments of this schedule to the same target may not have
    /// overlapping effective periods. The check is inside the aggregate because the
    /// complete assignment set is inside the aggregate — this is the one thing the
    /// nesting decision buys, and it would be unavailable if assignments stood alone.
    /// </summary>
    public Result<ScheduleAssignmentId> AssignTo(
        ScheduleAssignmentId assignmentId, OrganizationalAssignmentLevel targetLevel, string? targetId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, Guid assignedBy, DateTimeOffset nowUtc)
    {
        if (Status == WorkScheduleStatus.Superseded)
        {
            return Result.Failure<ScheduleAssignmentId>(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return Result.Failure<ScheduleAssignmentId>(TimekeepingErrors.AssignmentTargetRequired);
        }

        if (effectiveTo is not null && effectiveTo.Value < effectiveFrom)
        {
            return Result.Failure<ScheduleAssignmentId>(SharedKernelErrors.DateRangeEndBeforeStart);
        }

        var trimmedTarget = targetId.Trim();

        var overlaps = _scheduleAssignments.Any(existing =>
            existing.TargetLevel == targetLevel
            && string.Equals(existing.TargetId, trimmedTarget, StringComparison.Ordinal)
            && existing.OverlapsPeriod(effectiveFrom, effectiveTo));

        if (overlaps)
        {
            return Result.Failure<ScheduleAssignmentId>(TimekeepingErrors.ScheduleAssignmentOverlapsExisting);
        }

        var assignment = new ScheduleAssignment(
            assignmentId, targetLevel, trimmedTarget, effectiveFrom, effectiveTo, assignedBy, nowUtc);
        _scheduleAssignments.Add(assignment);

        AddDomainEvent(new WorkScheduleAssignedToOrganizationalUnit(
            Guid.NewGuid(), nowUtc, Id, TenantId, assignmentId, targetLevel, trimmedTarget, effectiveFrom, effectiveTo,
            assignedBy));

        return Result.Success(assignmentId);
    }

    /// <summary>
    /// End-dates rather than deletes (TK-012). What schedule applied to an
    /// organizational unit during a past period stays answerable afterward, which is
    /// the whole reason the record is kept.
    /// </summary>
    public Result Unassign(ScheduleAssignmentId assignmentId, DateOnly effectiveTo, Guid unassignedBy, DateTimeOffset nowUtc)
    {
        if (Status == WorkScheduleStatus.Superseded)
        {
            return Result.Failure(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        var assignment = _scheduleAssignments.Find(candidate => candidate.Id == assignmentId);
        if (assignment is null)
        {
            return Result.Failure(TimekeepingErrors.ScheduleAssignmentNotFound);
        }

        assignment.EndOn(effectiveTo);

        AddDomainEvent(new WorkScheduleUnassignedFromOrganizationalUnit(
            Guid.NewGuid(), nowUtc, Id, TenantId, assignmentId, assignment.TargetLevel, assignment.TargetId, effectiveTo,
            unassignedBy));

        return Result.Success();
    }

    public Result Retire(Guid retiredBy, DateOnly effectiveFrom, DateTimeOffset nowUtc)
    {
        if (Status == WorkScheduleStatus.Retired)
        {
            return Result.Success();
        }

        if (Status != WorkScheduleStatus.Active)
        {
            return Result.Failure(TimekeepingErrors.VersionNotActive);
        }

        Status = WorkScheduleStatus.Retired;
        AddDomainEvent(new WorkScheduleRetired(Guid.NewGuid(), nowUtc, Id, TenantId, effectiveFrom, retiredBy));
        return Result.Success();
    }

    /// <summary>TK-002: this version applies only on dates within its own effective period.</summary>
    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || date <= EffectiveTo.Value);

    /// <summary>
    /// The assignment governing <paramref name="targetId"/> at
    /// <paramref name="targetLevel"/> on <paramref name="date"/>, or null. TK-011
    /// guarantees at most one, so this returns a single value rather than a set.
    /// </summary>
    public ScheduleAssignment? AssignmentOn(OrganizationalAssignmentLevel targetLevel, string targetId, DateOnly date) =>
        _scheduleAssignments.Find(assignment =>
            assignment.TargetLevel == targetLevel
            && string.Equals(assignment.TargetId, targetId, StringComparison.Ordinal)
            && assignment.IsEffectiveOn(date));
}
