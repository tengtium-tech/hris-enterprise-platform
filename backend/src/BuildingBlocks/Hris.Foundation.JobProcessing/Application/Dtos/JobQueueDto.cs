namespace Hris.Foundation.JobProcessing.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetJobQueueQuery</c> returns, per dto-design.md's own
/// convention.
/// </summary>
public sealed record JobQueueDto(
    Guid JobQueueId,
    string Name,
    int MaxConcurrency,
    int DefaultMaxRetries,
    long DefaultRetryDelaySeconds,
    DateTimeOffset CreatedAtUtc);
