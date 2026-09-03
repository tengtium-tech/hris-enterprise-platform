using Hris.Foundation.JobProcessing.Application.Dtos;
using Hris.Foundation.JobProcessing.Domain;

namespace Hris.Foundation.JobProcessing.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4 framework's own mapper already establishes.
/// </summary>
internal static class JobProcessingMapper
{
    public static JobDto ToDto(Job job) => new(
        job.Id.Value,
        job.TenantId,
        job.JobType,
        job.JobQueueId.Value,
        job.Priority.ToString(),
        job.PayloadReference,
        job.SubmittedByUserId,
        job.Status.ToString(),
        job.RetryCount,
        job.MaxRetries,
        job.FailureReason,
        job.SubmittedAtUtc,
        job.StartedAtUtc,
        job.CompletedAtUtc);

    public static JobQueueDto ToDto(JobQueue jobQueue) => new(
        jobQueue.Id.Value,
        jobQueue.Name.Value,
        jobQueue.MaxConcurrency,
        jobQueue.DefaultMaxRetries,
        jobQueue.DefaultRetryDelaySeconds,
        jobQueue.CreatedAtUtc);
}
