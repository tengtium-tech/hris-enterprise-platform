using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// scheduling-framework.md's own Domain Events section names exactly nine events --
/// every one implemented here, split six/three across <see cref="Schedule"/> and
/// <see cref="ScheduleExecution"/>. Unlike numbering-framework.md's own asymmetric list,
/// this document names a creation event (<see cref="ScheduleCreated"/>) and no
/// "ScheduleValidated"/"ScheduleApproved" events -- <see cref="Schedule.Validate"/> and
/// <see cref="Schedule.Approve"/> raise nothing, matching that same asymmetry.
/// </summary>
public sealed record ScheduleCreated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleId ScheduleId,
    Guid TenantId,
    ScheduleType ScheduleType) : IDomainEvent;

public sealed record ScheduleUpdated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleId ScheduleId) : IDomainEvent;

public sealed record ScheduleActivated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleId ScheduleId) : IDomainEvent;

public sealed record SchedulePaused(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleId ScheduleId) : IDomainEvent;

public sealed record ScheduleResumed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleId ScheduleId) : IDomainEvent;

public sealed record ScheduleRetired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleId ScheduleId) : IDomainEvent;

public sealed record ScheduleTriggered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleExecutionId ScheduleExecutionId,
    ScheduleId ScheduleId,
    Guid TenantId) : IDomainEvent;

public sealed record ScheduleCompleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleExecutionId ScheduleExecutionId,
    long DurationMs) : IDomainEvent;

public sealed record ScheduleFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ScheduleExecutionId ScheduleExecutionId,
    string Reason) : IDomainEvent;
