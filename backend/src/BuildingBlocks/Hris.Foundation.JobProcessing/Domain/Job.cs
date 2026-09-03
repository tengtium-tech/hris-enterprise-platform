using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Aggregate Root for one submitted unit of background work -- a separate,
/// population-scale Aggregate Root from <see cref="JobQueue"/> for the identical
/// reason <c>IssuedNumber</c> is kept independent of <c>NumberSeries</c>:
/// job-processing.md's own Non-Functional Requirements state "support millions of
/// background jobs across multiple tenants." Seventh framework built in Sprint 4.
///
/// <see cref="TenantId"/> is a plain, caller-supplied <see cref="Guid"/>, the third
/// Sprint 4 framework in a row to build tenant context concretely rather than
/// deferring it -- job-processing.md's own AI Implementation Guidance names
/// <c>CTR-ISO-004</c> explicitly and gives the sharpest reason yet: "a job has no
/// request to inherit context from, which is where isolation is most often lost."
/// </summary>
public sealed class Job : AggregateRoot<JobId>
{
    public Guid TenantId { get; }

    public string JobType { get; }

    public JobQueueId JobQueueId { get; }

    public JobPriority Priority { get; }

    public string? PayloadReference { get; }

    public Guid? SubmittedByUserId { get; }

    public JobStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public int MaxRetries { get; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private Job(
        JobId id,
        Guid tenantId,
        string jobType,
        JobQueueId jobQueueId,
        JobPriority priority,
        string? payloadReference,
        Guid? submittedByUserId,
        JobStatus status,
        int retryCount,
        int maxRetries,
        string? failureReason,
        DateTimeOffset submittedAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        JobType = jobType;
        JobQueueId = jobQueueId;
        Priority = priority;
        PayloadReference = payloadReference;
        SubmittedByUserId = submittedByUserId;
        Status = status;
        RetryCount = retryCount;
        MaxRetries = maxRetries;
        FailureReason = failureReason;
        SubmittedAtUtc = submittedAtUtc;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>
    /// Submits a new job, in <see cref="JobStatus.Submitted"/>. Every constructor
    /// parameter above shares its name with the property it sets -- the
    /// constructor-binding pitfall Search Framework's own <c>IndexedDocument</c>/
    /// <c>SearchExecution</c>/<c>SavedSearch</c> each needed a second constructor for,
    /// avoided here by construction, the same discipline Scheduling Framework's own
    /// <c>Schedule</c> already applies. <paramref name="tenantId"/> is guarded, not
    /// Result-validated, the identical technical-precondition choice every other
    /// population-scale aggregate in this codebase makes for the same reason
    /// (<c>CTR-ISO-004</c>).
    /// </summary>
    public static Result<Job> Submit(
        Guid tenantId,
        string? jobType,
        JobQueueId jobQueueId,
        JobPriority priority,
        string? payloadReference,
        Guid? submittedByUserId,
        int maxRetries,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(jobType))
        {
            return Result.Failure<Job>(JobProcessingErrors.JobTypeRequired);
        }

        var job = new Job(
            new JobId(Guid.NewGuid()),
            tenantId,
            jobType.Trim(),
            jobQueueId,
            priority,
            string.IsNullOrWhiteSpace(payloadReference) ? null : payloadReference.Trim(),
            submittedByUserId,
            JobStatus.Submitted,
            retryCount: 0,
            maxRetries,
            failureReason: null,
            submittedAtUtc: nowUtc,
            startedAtUtc: null,
            completedAtUtc: null);

        job.AddDomainEvent(new JobSubmitted(Guid.NewGuid(), nowUtc, job.Id, tenantId, job.JobType, jobQueueId));
        return Result.Success(job);
    }

    public Result Enqueue(DateTimeOffset nowUtc)
    {
        if (Status != JobStatus.Submitted)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        Status = JobStatus.Queued;
        AddDomainEvent(new JobQueued(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// A worker/slot has been assigned to this job -- job-processing.md's own Job
    /// Lifecycle diagram places this between <see cref="JobStatus.Queued"/> and
    /// <see cref="JobStatus.Running"/>. Raises no event: the document's own Domain
    /// Events list names no "JobScheduled" event, the same asymmetry
    /// <see cref="JobProcessingEvents"/>'s own remarks note for itself.
    /// </summary>
    public Result MarkScheduled()
    {
        if (Status != JobStatus.Queued)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        Status = JobStatus.Scheduled;
        return Result.Success();
    }

    /// <summary>
    /// Accepts <see cref="JobStatus.Queued"/> as well as <see cref="JobStatus.Scheduled"/>
    /// as a valid starting state -- a job whose own queue processes work immediately,
    /// without a distinct scheduling step, never calls <see cref="MarkScheduled"/> at
    /// all, and should not be forced through it purely to satisfy this guard.
    /// </summary>
    public Result Start(DateTimeOffset nowUtc)
    {
        if (Status is not (JobStatus.Queued or JobStatus.Scheduled))
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        Status = JobStatus.Running;
        StartedAtUtc = nowUtc;
        AddDomainEvent(new JobStarted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Complete(DateTimeOffset nowUtc)
    {
        if (Status != JobStatus.Running)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        Status = JobStatus.Completed;
        CompletedAtUtc = nowUtc;
        AddDomainEvent(new JobCompleted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Fail(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != JobStatus.Running)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(JobProcessingErrors.FailureReasonRequired);
        }

        Status = JobStatus.Failed;
        FailureReason = reason.Trim();
        AddDomainEvent(new JobFailed(Guid.NewGuid(), nowUtc, Id, FailureReason));
        return Result.Success();
    }

    /// <summary>
    /// Returns this job to <see cref="JobStatus.Queued"/> for another attempt --
    /// job-processing.md's own AI Implementation Guidance: "retry with backoff." Fails
    /// once <see cref="RetryCount"/> would reach <see cref="MaxRetries"/> -- the caller
    /// must route to <see cref="MoveToDeadLetterQueue"/> instead
    /// (<c>CTR-WFL-006</c>: "must move to the dead letter queue... never silently
    /// abandoned"), never loop here indefinitely.
    /// </summary>
    public Result Retry(DateTimeOffset nowUtc)
    {
        if (Status != JobStatus.Failed)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        if (RetryCount >= MaxRetries)
        {
            return Result.Failure(JobProcessingErrors.RetryLimitExceeded);
        }

        RetryCount++;
        Status = JobStatus.Queued;
        AddDomainEvent(new JobRetried(Guid.NewGuid(), nowUtc, Id, RetryCount));
        return Result.Success();
    }

    /// <summary>
    /// Moves an exhausted job to the Dead Letter Queue -- job-processing.md's own Dead
    /// Letter Queue section ("Jobs that repeatedly fail should be moved to a Dead
    /// Letter Queue"). Valid from <see cref="JobStatus.Failed"/> regardless of
    /// <see cref="RetryCount"/> against <see cref="MaxRetries"/>: an administrator may
    /// also route a job here directly, not only after <see cref="Retry"/> itself
    /// refuses.
    /// </summary>
    public Result MoveToDeadLetterQueue(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != JobStatus.Failed)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(JobProcessingErrors.FailureReasonRequired);
        }

        Status = JobStatus.DeadLetter;
        FailureReason = reason.Trim();
        AddDomainEvent(new JobMovedToDlq(Guid.NewGuid(), nowUtc, Id, FailureReason));
        return Result.Success();
    }

    /// <summary>
    /// Cancels this job -- job-processing.md's own Dead Letter Queue section names
    /// "Permanent Cancellation" as a real outcome. Accepts any non-terminal state, the
    /// identical broader-than-the-diagram guard <c>Schedule.Retire</c>'s own remarks
    /// justify for itself: an administrator must always be able to cancel a job
    /// regardless of how far it progressed.
    /// </summary>
    public Result Cancel(DateTimeOffset nowUtc)
    {
        if (Status is JobStatus.Completed or JobStatus.DeadLetter or JobStatus.Cancelled)
        {
            return Result.Failure(JobProcessingErrors.InvalidJobLifecycleTransition);
        }

        Status = JobStatus.Cancelled;
        AddDomainEvent(new JobCancelled(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}
