namespace Hris.Foundation.JobProcessing.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetJobQuery</c> and <c>ListJobHistoryQuery</c> return, per
/// dto-design.md's own convention -- job-processing.md's own Job History section.
/// </summary>
public sealed record JobDto(
    Guid JobId,
    Guid TenantId,
    string JobType,
    Guid JobQueueId,
    string Priority,
    string? PayloadReference,
    Guid? SubmittedByUserId,
    string Status,
    int RetryCount,
    int MaxRetries,
    string? FailureReason,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);
