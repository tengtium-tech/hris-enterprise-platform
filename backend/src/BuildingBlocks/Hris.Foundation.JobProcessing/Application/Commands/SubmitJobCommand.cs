using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Commands;

/// <summary>
/// Submits a new job to a named queue. Carries raw primitives, not Domain Value
/// Objects, across the MediatR boundary -- this handler is the one place a malformed
/// job type or unknown queue name becomes a <see cref="JobProcessingErrors"/> failure.
/// <paramref name="MaxRetries"/> defaults to the queue's own
/// <see cref="JobQueue.DefaultMaxRetries"/> when not overridden, matching
/// job-processing.md's own "Retry policies should be configurable per job type"
/// alongside "per queue."
/// </summary>
public sealed record SubmitJobCommand(
    Guid TenantId,
    string JobType,
    string QueueName,
    JobPriority Priority,
    string? PayloadReference,
    Guid? SubmittedByUserId,
    int? MaxRetries) : ICommand<Result<Guid>>;

internal sealed class SubmitJobCommandHandler : IRequestHandler<SubmitJobCommand, Result<Guid>>
{
    private readonly IJobQueueRepository _jobQueueRepository;
    private readonly IJobRepository _jobRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitJobCommandHandler(IJobQueueRepository jobQueueRepository, IJobRepository jobRepository, TimeProvider timeProvider)
    {
        _jobQueueRepository = Guard.AgainstNull(jobQueueRepository, nameof(jobQueueRepository));
        _jobRepository = Guard.AgainstNull(jobRepository, nameof(jobRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(SubmitJobCommand request, CancellationToken cancellationToken)
    {
        var queueNameResult = JobQueueName.Create(request.QueueName);
        if (queueNameResult.IsFailure)
        {
            return Result.Failure<Guid>(queueNameResult.Error);
        }

        var jobQueue = await _jobQueueRepository.GetByNameAsync(queueNameResult.Value, cancellationToken).ConfigureAwait(false);
        if (jobQueue is null)
        {
            return Result.Failure<Guid>(JobProcessingErrors.JobQueueNotFound);
        }

        var submitResult = Job.Submit(
            request.TenantId,
            request.JobType,
            jobQueue.Id,
            request.Priority,
            request.PayloadReference,
            request.SubmittedByUserId,
            request.MaxRetries ?? jobQueue.DefaultMaxRetries,
            _timeProvider.GetUtcNow());
        if (submitResult.IsFailure)
        {
            return Result.Failure<Guid>(submitResult.Error);
        }

        await _jobRepository.AddAsync(submitResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(submitResult.Value.Id.Value);
    }
}
