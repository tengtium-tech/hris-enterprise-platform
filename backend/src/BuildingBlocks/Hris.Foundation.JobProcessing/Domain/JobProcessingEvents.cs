using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// job-processing.md's own Domain Events section names exactly ten events -- every one
/// implemented here, split eight/two across <see cref="Job"/> and <see cref="Worker"/>.
/// No "JobScheduled"/"JobQueueRegistered"/"JobQueueUpdated" events exist in that list --
/// <see cref="Job.MarkScheduled"/>, <see cref="JobQueue.Register"/>, and
/// <see cref="JobQueue.UpdatePolicy"/> raise nothing, the same asymmetry
/// scheduling-framework.md's own Validate/Approve precedent already establishes.
/// </summary>
public sealed record JobSubmitted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId,
    Guid TenantId,
    string JobType,
    JobQueueId JobQueueId) : IDomainEvent;

public sealed record JobQueued(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId) : IDomainEvent;

public sealed record JobStarted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId) : IDomainEvent;

public sealed record JobCompleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId) : IDomainEvent;

public sealed record JobFailed(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId,
    string Reason) : IDomainEvent;

public sealed record JobRetried(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId,
    int RetryCount) : IDomainEvent;

public sealed record JobCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId) : IDomainEvent;

public sealed record JobMovedToDlq(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    JobId JobId,
    string Reason) : IDomainEvent;

public sealed record WorkerStarted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkerId WorkerId,
    string InstanceId) : IDomainEvent;

public sealed record WorkerStopped(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkerId WorkerId) : IDomainEvent;
