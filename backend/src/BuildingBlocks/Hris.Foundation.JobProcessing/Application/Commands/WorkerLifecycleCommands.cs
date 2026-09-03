using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.JobProcessing.Application.Commands;

/// <summary>
/// The two <see cref="Worker"/> operations -- start, stop -- grouped into one file the
/// same way every other Sprint 3/4 framework's own bundled lifecycle commands are (see
/// <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record StartWorkerCommand(string InstanceId) : ICommand<Result<Guid>>;

internal sealed class StartWorkerCommandHandler : IRequestHandler<StartWorkerCommand, Result<Guid>>
{
    private readonly IWorkerRepository _repository;
    private readonly TimeProvider _timeProvider;

    public StartWorkerCommandHandler(IWorkerRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(StartWorkerCommand request, CancellationToken cancellationToken)
    {
        var startResult = Worker.Start(request.InstanceId, _timeProvider.GetUtcNow());
        if (startResult.IsFailure)
        {
            return Result.Failure<Guid>(startResult.Error);
        }

        await _repository.AddAsync(startResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(startResult.Value.Id.Value);
    }
}

public sealed record StopWorkerCommand(Guid WorkerId) : ICommand<Result>;

internal sealed class StopWorkerCommandHandler : IRequestHandler<StopWorkerCommand, Result>
{
    private readonly IWorkerRepository _repository;
    private readonly TimeProvider _timeProvider;

    public StopWorkerCommandHandler(IWorkerRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(StopWorkerCommand request, CancellationToken cancellationToken)
    {
        var worker = await _repository.GetByIdAsync(new WorkerId(request.WorkerId), cancellationToken).ConfigureAwait(false);
        return worker is null ? Result.Failure(JobProcessingErrors.WorkerNotFound) : worker.Stop(_timeProvider.GetUtcNow());
    }
}
