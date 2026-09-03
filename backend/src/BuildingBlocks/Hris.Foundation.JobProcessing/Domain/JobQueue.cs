using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Foundation.JobProcessing.Domain;

/// <summary>
/// Aggregate Root holding one queue's own processing policy -- job-processing.md's own
/// Core Concepts ("Queues organize jobs awaiting execution... may have independent
/// processing policies") and Concurrency section ("Concurrency settings should be
/// configurable per queue"). Actually enforcing <see cref="MaxConcurrency"/> at dequeue
/// time (limiting how many <see cref="Job"/> rows run at once for this queue) is a
/// real worker/queue-execution concern outside this Sprint's own Scope, see
/// <c>DependencyInjection.cs</c>'s own remarks -- this aggregate records the policy, it
/// does not enforce it.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "\"Queue\" is job-processing.md's own ubiquitous-language name for " +
        "this Aggregate Root's own Core Concept (\"Queues organize jobs awaiting " +
        "execution\") -- the identical \"rename would break the source document's own " +
        "vocabulary to satisfy a naming lint\" reasoning Tenant Framework's own Tenant " +
        "class states for CA1724. This type does not implement any queue-like collection " +
        "interface (no Enqueue/Dequeue pair operating on its own elements) that CA1711's " +
        "own rule is actually protecting against confusing.")]
public sealed class JobQueue : AggregateRoot<JobQueueId>
{
    public JobQueueName Name { get; }

    public int MaxConcurrency { get; private set; }

    public int DefaultMaxRetries { get; private set; }

    public long DefaultRetryDelaySeconds { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    private JobQueue(
        JobQueueId id, JobQueueName name, int maxConcurrency, int defaultMaxRetries, long defaultRetryDelaySeconds, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        MaxConcurrency = maxConcurrency;
        DefaultMaxRetries = defaultMaxRetries;
        DefaultRetryDelaySeconds = defaultRetryDelaySeconds;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Registers a new queue. Name uniqueness is checked by the caller before this
    /// factory runs (<see cref="IJobQueueRepository.ExistsByNameAsync"/>), not here --
    /// the same split every other uniqueness-checked factory in this codebase
    /// establishes. Raises no event: job-processing.md's own Domain Events list names
    /// no "queue registered" event, the same asymmetry <see cref="JobProcessingEvents"/>'s
    /// own remarks note for itself.
    /// </summary>
    public static Result<JobQueue> Register(
        JobQueueName name, int maxConcurrency, int defaultMaxRetries, long defaultRetryDelaySeconds, DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(name, nameof(name));

        var policyResult = ValidatePolicy(maxConcurrency, defaultMaxRetries, defaultRetryDelaySeconds);
        if (policyResult.IsFailure)
        {
            return Result.Failure<JobQueue>(policyResult.Error);
        }

        return Result.Success(new JobQueue(new JobQueueId(Guid.NewGuid()), name, maxConcurrency, defaultMaxRetries, defaultRetryDelaySeconds, nowUtc));
    }

    public Result UpdatePolicy(int maxConcurrency, int defaultMaxRetries, long defaultRetryDelaySeconds)
    {
        var policyResult = ValidatePolicy(maxConcurrency, defaultMaxRetries, defaultRetryDelaySeconds);
        if (policyResult.IsFailure)
        {
            return policyResult;
        }

        MaxConcurrency = maxConcurrency;
        DefaultMaxRetries = defaultMaxRetries;
        DefaultRetryDelaySeconds = defaultRetryDelaySeconds;
        return Result.Success();
    }

    private static Result ValidatePolicy(int maxConcurrency, int defaultMaxRetries, long defaultRetryDelaySeconds)
    {
        if (maxConcurrency < 1)
        {
            return Result.Failure(JobProcessingErrors.MaxConcurrencyOutOfRange);
        }

        if (defaultMaxRetries < 0)
        {
            return Result.Failure(JobProcessingErrors.DefaultMaxRetriesNegative);
        }

        return defaultRetryDelaySeconds < 0
            ? Result.Failure(JobProcessingErrors.DefaultRetryDelayNegative)
            : Result.Success();
    }
}
