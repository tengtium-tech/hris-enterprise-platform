using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Commands;

/// <summary>
/// The two <see cref="JobQueue"/> operations -- register, update policy -- grouped
/// into one file the same way every other Sprint 3/4 framework's own bundled
/// lifecycle commands are (see <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record RegisterJobQueueCommand(
    string Name, int MaxConcurrency, int DefaultMaxRetries, long DefaultRetryDelaySeconds) : ICommand<Result<Guid>>;

internal sealed class RegisterJobQueueCommandHandler : IRequestHandler<RegisterJobQueueCommand, Result<Guid>>
{
    private readonly IJobQueueRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterJobQueueCommandHandler(IJobQueueRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterJobQueueCommand request, CancellationToken cancellationToken)
    {
        var nameResult = JobQueueName.Create(request.Name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<Guid>(nameResult.Error);
        }

        if (await _repository.ExistsByNameAsync(nameResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(JobProcessingErrors.JobQueueNameAlreadyRegistered);
        }

        var registerResult = JobQueue.Register(
            nameResult.Value, request.MaxConcurrency, request.DefaultMaxRetries, request.DefaultRetryDelaySeconds, _timeProvider.GetUtcNow());
        if (registerResult.IsFailure)
        {
            return Result.Failure<Guid>(registerResult.Error);
        }

        await _repository.AddAsync(registerResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registerResult.Value.Id.Value);
    }
}

public sealed record UpdateJobQueuePolicyCommand(Guid JobQueueId, int MaxConcurrency, int DefaultMaxRetries, long DefaultRetryDelaySeconds) : ICommand<Result>;

internal sealed class UpdateJobQueuePolicyCommandHandler : IRequestHandler<UpdateJobQueuePolicyCommand, Result>
{
    private readonly IJobQueueRepository _repository;

    public UpdateJobQueuePolicyCommandHandler(IJobQueueRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(UpdateJobQueuePolicyCommand request, CancellationToken cancellationToken)
    {
        var jobQueue = await _repository.GetByIdAsync(new JobQueueId(request.JobQueueId), cancellationToken).ConfigureAwait(false);

        return jobQueue is null
            ? Result.Failure(JobProcessingErrors.JobQueueNotFound)
            : jobQueue.UpdatePolicy(request.MaxConcurrency, request.DefaultMaxRetries, request.DefaultRetryDelaySeconds);
    }
}
