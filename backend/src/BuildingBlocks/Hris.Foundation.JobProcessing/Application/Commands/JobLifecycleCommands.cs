using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Commands;

/// <summary>
/// The eight remaining <see cref="Job"/>-level lifecycle transitions -- enqueue, mark
/// scheduled, start, complete, fail, retry, move to the dead letter queue, cancel --
/// grouped into one file the same way every other Sprint 3/4 framework's own bundled
/// lifecycle commands are (see <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record EnqueueJobCommand(Guid JobId) : ICommand<Result>;

internal sealed class EnqueueJobCommandHandler : IRequestHandler<EnqueueJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EnqueueJobCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(EnqueueJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.Enqueue(_timeProvider.GetUtcNow());
    }
}

public sealed record MarkJobScheduledCommand(Guid JobId) : ICommand<Result>;

internal sealed class MarkJobScheduledCommandHandler : IRequestHandler<MarkJobScheduledCommand, Result>
{
    private readonly IJobRepository _repository;

    public MarkJobScheduledCommandHandler(IJobRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(MarkJobScheduledCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.MarkScheduled();
    }
}

public sealed record StartJobCommand(Guid JobId) : ICommand<Result>;

internal sealed class StartJobCommandHandler : IRequestHandler<StartJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public StartJobCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(StartJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.Start(_timeProvider.GetUtcNow());
    }
}

public sealed record CompleteJobCommand(Guid JobId) : ICommand<Result>;

internal sealed class CompleteJobCommandHandler : IRequestHandler<CompleteJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteJobCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CompleteJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.Complete(_timeProvider.GetUtcNow());
    }
}

public sealed record FailJobCommand(Guid JobId, string Reason) : ICommand<Result>;

internal sealed class FailJobCommandHandler : IRequestHandler<FailJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FailJobCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FailJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.Fail(request.Reason, _timeProvider.GetUtcNow());
    }
}

public sealed record RetryJobCommand(Guid JobId) : ICommand<Result>;

internal sealed class RetryJobCommandHandler : IRequestHandler<RetryJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetryJobCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetryJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.Retry(_timeProvider.GetUtcNow());
    }
}

public sealed record MoveJobToDeadLetterQueueCommand(Guid JobId, string Reason) : ICommand<Result>;

internal sealed class MoveJobToDeadLetterQueueCommandHandler : IRequestHandler<MoveJobToDeadLetterQueueCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MoveJobToDeadLetterQueueCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MoveJobToDeadLetterQueueCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null
            ? Result.Failure(JobProcessingErrors.JobNotFound)
            : job.MoveToDeadLetterQueue(request.Reason, _timeProvider.GetUtcNow());
    }
}

public sealed record CancelJobCommand(Guid JobId) : ICommand<Result>;

internal sealed class CancelJobCommandHandler : IRequestHandler<CancelJobCommand, Result>
{
    private readonly IJobRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelJobCommandHandler(IJobRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(new JobId(request.JobId), cancellationToken).ConfigureAwait(false);
        return job is null ? Result.Failure(JobProcessingErrors.JobNotFound) : job.Cancel(_timeProvider.GetUtcNow());
    }
}
