namespace Hris.Foundation.Scheduling.Application.Dtos;

/// <summary>
/// The read-side shape <c>ListScheduleExecutionHistoryQuery</c> returns, per
/// dto-design.md's own convention -- scheduling-framework.md's own Schedule History
/// section.
/// </summary>
public sealed record ScheduleExecutionDto(
    Guid ScheduleExecutionId,
    Guid ScheduleId,
    string Status,
    string? JobIdentifier,
    int RetryCount,
    long? DurationMs,
    string? FailureReason,
    DateTimeOffset TriggeredAtUtc,
    DateTimeOffset? CompletedAtUtc);
