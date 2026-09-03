using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// Aggregate Root for one actual trigger/fire occurrence of a <see cref="Schedule"/> --
/// a separate, population-scale Aggregate Root from <see cref="Schedule"/> for the
/// identical reason <c>IssuedNumber</c> is kept independent of <c>NumberSeries</c>:
/// scheduling-framework.md's own Non-Functional Requirements state "hundreds of
/// thousands of active schedules," each of which accumulates its own execution history
/// over time. Backs scheduling-framework.md's own Schedule History section
/// ("Schedule Identifier, Execution Time, Trigger Time, Job Identifier, Result,
/// Duration, Failure Reason, Retry Information").
/// </summary>
public sealed class ScheduleExecution : AggregateRoot<ScheduleExecutionId>
{
    public ScheduleId ScheduleId { get; }

    public Guid TenantId { get; }

    public ScheduleExecutionStatus Status { get; private set; }

    public string? JobIdentifier { get; }

    public int RetryCount { get; }

    public long? DurationMs { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset TriggeredAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private ScheduleExecution(
        ScheduleExecutionId id,
        ScheduleId scheduleId,
        Guid tenantId,
        string? jobIdentifier,
        int retryCount,
        DateTimeOffset triggeredAtUtc)
        : base(id)
    {
        ScheduleId = scheduleId;
        TenantId = tenantId;
        Status = ScheduleExecutionStatus.Triggered;
        JobIdentifier = jobIdentifier;
        RetryCount = retryCount;
        TriggeredAtUtc = triggeredAtUtc;
    }

    /// <summary>
    /// Records that <paramref name="scheduleId"/> fired -- <paramref name="jobIdentifier"/>
    /// is a plain, nullable string reference to whatever Job Processing Framework's own
    /// job this trigger handed work to, generic by design since that framework is not
    /// yet built (the same "generic reference, not a strongly-typed FK to an unbuilt
    /// framework" choice <c>IssuedNumber.AssignedToType</c>'s own remarks make).
    /// <paramref name="tenantId"/> is guarded, not Result-validated, the identical
    /// technical-precondition choice <c>IndexedDocument.Index</c>'s own remarks explain
    /// for the same reason (<c>CTR-ISO-004</c> here, rather than <c>CTR-ISO-001</c>).
    /// </summary>
    public static Result<ScheduleExecution> Trigger(
        ScheduleId scheduleId, Guid tenantId, string? jobIdentifier, int retryCount, DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (retryCount < 0)
        {
            return Result.Failure<ScheduleExecution>(SchedulingErrors.RetryCountNegative);
        }

        var execution = new ScheduleExecution(
            new ScheduleExecutionId(Guid.NewGuid()), scheduleId, tenantId, jobIdentifier, retryCount, nowUtc);

        execution.AddDomainEvent(new ScheduleTriggered(Guid.NewGuid(), nowUtc, execution.Id, scheduleId, tenantId));
        return Result.Success(execution);
    }

    public Result Complete(long durationMs, DateTimeOffset nowUtc)
    {
        if (Status != ScheduleExecutionStatus.Triggered)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleExecutionTransition);
        }

        Status = ScheduleExecutionStatus.Completed;
        DurationMs = durationMs;
        CompletedAtUtc = nowUtc;

        AddDomainEvent(new ScheduleCompleted(Guid.NewGuid(), nowUtc, Id, durationMs));
        return Result.Success();
    }

    public Result Fail(string? reason, long durationMs, DateTimeOffset nowUtc)
    {
        if (Status != ScheduleExecutionStatus.Triggered)
        {
            return Result.Failure(SchedulingErrors.InvalidScheduleExecutionTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(SchedulingErrors.FailureReasonRequired);
        }

        Status = ScheduleExecutionStatus.Failed;
        DurationMs = durationMs;
        FailureReason = reason.Trim();
        CompletedAtUtc = nowUtc;

        AddDomainEvent(new ScheduleFailed(Guid.NewGuid(), nowUtc, Id, FailureReason));
        return Result.Success();
    }
}
