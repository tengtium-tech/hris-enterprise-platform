using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// Aggregate Root holding one schedule's own configuration and its own journey through
/// scheduling-framework.md's own Schedule Lifecycle ("Draft -&gt; Validated -&gt; Approved
/// -&gt; Active -&gt; Paused -&gt; Resumed -&gt; Retired"). Sixth framework built in Sprint 4.
///
/// <see cref="TenantId"/> is a plain <see cref="Guid"/>, caller-supplied, the same
/// "explicit parameter rather than an ambient tenant-context service" choice
/// <c>IndexedDocument</c>'s own remarks explain -- built concretely here, not deferred,
/// because scheduling-framework.md's own AI Implementation Guidance names
/// <c>CTR-ISO-004</c> explicitly ("establish explicit tenant context in every scheduled
/// execution"), the identical materially-stronger-than-usual instruction
/// search-framework.md's own remarks describe for <c>CTR-ISO-001</c>.
///
/// <see cref="Active"/> and <see cref="Resumed"/> (<see cref="ScheduleStatus"/>) are
/// both "currently triggering" states; <see cref="Pause"/> accepts either as its own
/// starting state, and <see cref="Retire"/> accepts any non-terminal state -- broader
/// than the document's own strictly linear diagram, since an administrator must always
/// be able to retire a schedule regardless of how far it progressed, the identical
/// multi-state guard <c>IssuedNumber.Release</c>'s own remarks justify for itself.
/// </summary>
public sealed class Schedule : AggregateRoot<ScheduleId>
{
    public Guid TenantId { get; }

    public ScheduleType ScheduleType { get; }

    public ScheduleExpression Expression { get; private set; }

    public ScheduleTimeZone TimeZone { get; private set; }

    public string TaskType { get; private set; }

    public string? TaskReferenceId { get; private set; }

    public HolidayBehavior HolidayBehavior { get; private set; }

    public string? CalendarReference { get; private set; }

    public ScheduleStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? LastTransitionAtUtc { get; private set; }

    private Schedule(
        ScheduleId id,
        Guid tenantId,
        ScheduleType scheduleType,
        ScheduleExpression expression,
        ScheduleTimeZone timeZone,
        string taskType,
        string? taskReferenceId,
        HolidayBehavior holidayBehavior,
        string? calendarReference,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        ScheduleType = scheduleType;
        Expression = expression;
        TimeZone = timeZone;
        TaskType = taskType;
        TaskReferenceId = taskReferenceId;
        HolidayBehavior = holidayBehavior;
        CalendarReference = calendarReference;
        Status = ScheduleStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Registers a new schedule, in <see cref="ScheduleStatus.Draft"/>. Every
    /// constructor parameter above shares its name with the property it sets
    /// (<c>createdAtUtc</c> -&gt; <see cref="CreatedAtUtc"/>, not a differently-named
    /// <c>nowUtc</c>) -- the constructor-binding pitfall <see cref="Infrastructure.Persistence.ScheduleConfiguration"/>'s
    /// own remarks would otherwise need to work around, avoided here by construction
    /// rather than discovered after the fact, and confirmed by a real EF Core model
    /// build needing no second constructor for this aggregate, unlike
    /// <c>IndexedDocument</c>/<c>SearchExecution</c>/<c>SavedSearch</c>. <see cref="Status"/>
    /// and <see cref="LastTransitionAtUtc"/> are not constructor parameters at all --
    /// both have reachable private setters, so EF Core populates them the normal way it
    /// populates every other property never taken as a constructor parameter.
    /// </summary>
    public static Result<Schedule> Create(
        Guid tenantId,
        ScheduleType scheduleType,
        ScheduleExpression expression,
        ScheduleTimeZone timeZone,
        string? taskType,
        string? taskReferenceId,
        HolidayBehavior holidayBehavior,
        string? calendarReference,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstNull(expression, nameof(expression));
        Guard.AgainstNull(timeZone, nameof(timeZone));

        if (string.IsNullOrWhiteSpace(taskType))
        {
            return Result.Failure<Schedule>(SchedulingErrors.TaskTypeRequired);
        }

        var schedule = new Schedule(
            new ScheduleId(Guid.NewGuid()),
            tenantId,
            scheduleType,
            expression,
            timeZone,
            taskType.Trim(),
            string.IsNullOrWhiteSpace(taskReferenceId) ? null : taskReferenceId.Trim(),
            holidayBehavior,
            string.IsNullOrWhiteSpace(calendarReference) ? null : calendarReference.Trim(),
            nowUtc);

        schedule.AddDomainEvent(new ScheduleCreated(Guid.NewGuid(), nowUtc, schedule.Id, tenantId, scheduleType));
        return Result.Success(schedule);
    }

    /// <summary>
    /// Edits this schedule's own configuration -- valid only while still
    /// <see cref="ScheduleStatus.Draft"/>, since <see cref="Validate"/> and
    /// <see cref="Approve"/> exist specifically to lock a reviewed configuration in
    /// before <see cref="Activate"/> lets it actually trigger.
    /// </summary>
    public Result Update(
        ScheduleExpression expression,
        ScheduleTimeZone timeZone,
        string? taskType,
        string? taskReferenceId,
        HolidayBehavior holidayBehavior,
        string? calendarReference,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(expression, nameof(expression));
        Guard.AgainstNull(timeZone, nameof(timeZone));

        if (Status != ScheduleStatus.Draft)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(taskType))
        {
            return Result.Failure(SchedulingErrors.TaskTypeRequired);
        }

        Expression = expression;
        TimeZone = timeZone;
        TaskType = taskType.Trim();
        TaskReferenceId = string.IsNullOrWhiteSpace(taskReferenceId) ? null : taskReferenceId.Trim();
        HolidayBehavior = holidayBehavior;
        CalendarReference = string.IsNullOrWhiteSpace(calendarReference) ? null : calendarReference.Trim();

        AddDomainEvent(new ScheduleUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Confirms this schedule's own configuration is well-formed -- a review step, not
    /// an authorization gate (see <see cref="Approve"/> for that). Raises no event:
    /// scheduling-framework.md's own Domain Events list names no "ScheduleValidated"
    /// event.
    /// </summary>
    public Result Validate()
    {
        if (Status != ScheduleStatus.Draft)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        Status = ScheduleStatus.Validated;
        return Result.Success();
    }

    /// <summary>
    /// Records administrator sign-off -- scheduling-framework.md's own Security
    /// Considerations: "Approval for Critical Schedules." The actual RBAC check itself
    /// is deferred, the identical reasoning every other Sprint 4 framework's own
    /// remarks state for Authorization Framework's concrete wiring; this method only
    /// records that sign-off already happened. Raises no event, the same asymmetry
    /// <see cref="Validate"/>'s own remarks note for itself.
    /// </summary>
    public Result Approve()
    {
        if (Status != ScheduleStatus.Validated)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        Status = ScheduleStatus.Approved;
        return Result.Success();
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (Status != ScheduleStatus.Approved)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        Status = ScheduleStatus.Active;
        LastTransitionAtUtc = nowUtc;
        AddDomainEvent(new ScheduleActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Pause(DateTimeOffset nowUtc)
    {
        if (Status is not (ScheduleStatus.Active or ScheduleStatus.Resumed))
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        Status = ScheduleStatus.Paused;
        LastTransitionAtUtc = nowUtc;
        AddDomainEvent(new SchedulePaused(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Resume(DateTimeOffset nowUtc)
    {
        if (Status != ScheduleStatus.Paused)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        Status = ScheduleStatus.Resumed;
        LastTransitionAtUtc = nowUtc;
        AddDomainEvent(new ScheduleResumed(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Retire(DateTimeOffset nowUtc)
    {
        if (Status == ScheduleStatus.Retired)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleLifecycleTransition);
        }

        Status = ScheduleStatus.Retired;
        LastTransitionAtUtc = nowUtc;
        AddDomainEvent(new ScheduleRetired(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}
