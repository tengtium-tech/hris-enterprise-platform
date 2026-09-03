using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Commands;

/// <summary>
/// The two remaining <see cref="ScheduleExecution"/>-level operations -- complete,
/// fail -- grouped into one file the same way every other Sprint 3/4 framework's own
/// bundled lifecycle commands are (see <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record CompleteScheduleExecutionCommand(Guid ScheduleExecutionId, long DurationMs) : ICommand<Result>;

internal sealed class CompleteScheduleExecutionCommandHandler : IRequestHandler<CompleteScheduleExecutionCommand, Result>
{
    private readonly IScheduleExecutionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteScheduleExecutionCommandHandler(IScheduleExecutionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CompleteScheduleExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository
            .GetByIdAsync(new ScheduleExecutionId(request.ScheduleExecutionId), cancellationToken)
            .ConfigureAwait(false);

        return execution is null
            ? Result.Failure(SchedulingErrors.ScheduleExecutionNotFound)
            : execution.Complete(request.DurationMs, _timeProvider.GetUtcNow());
    }
}

public sealed record FailScheduleExecutionCommand(Guid ScheduleExecutionId, string Reason, long DurationMs) : ICommand<Result>;

internal sealed class FailScheduleExecutionCommandHandler : IRequestHandler<FailScheduleExecutionCommand, Result>
{
    private readonly IScheduleExecutionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FailScheduleExecutionCommandHandler(IScheduleExecutionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FailScheduleExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository
            .GetByIdAsync(new ScheduleExecutionId(request.ScheduleExecutionId), cancellationToken)
            .ConfigureAwait(false);

        return execution is null
            ? Result.Failure(SchedulingErrors.ScheduleExecutionNotFound)
            : execution.Fail(request.Reason, request.DurationMs, _timeProvider.GetUtcNow());
    }
}
