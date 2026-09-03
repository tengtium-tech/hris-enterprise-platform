using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Commands;

/// <summary>
/// Records that a schedule fired -- called by whatever trigger evaluator (outside this
/// Sprint's own scope, see <c>DependencyInjection.cs</c>) actually decided this
/// schedule's own <see cref="ScheduleExpression"/> is due, per
/// <see cref="ScheduleExecution.Trigger"/>'s own remarks. Loads <see cref="Schedule"/>
/// first only to confirm it exists and to derive its own tenant id, never to evaluate
/// whether it is actually due -- that computation is explicitly out of this
/// framework's own Scope ("Background Job Execution").
/// </summary>
public sealed record TriggerScheduleExecutionCommand(Guid ScheduleId, string? JobIdentifier, int RetryCount) : ICommand<Result<Guid>>;

internal sealed class TriggerScheduleExecutionCommandHandler : IRequestHandler<TriggerScheduleExecutionCommand, Result<Guid>>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IScheduleExecutionRepository _executionRepository;
    private readonly TimeProvider _timeProvider;

    public TriggerScheduleExecutionCommandHandler(
        IScheduleRepository scheduleRepository, IScheduleExecutionRepository executionRepository, TimeProvider timeProvider)
    {
        _scheduleRepository = Guard.AgainstNull(scheduleRepository, nameof(scheduleRepository));
        _executionRepository = Guard.AgainstNull(executionRepository, nameof(executionRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(TriggerScheduleExecutionCommand request, CancellationToken cancellationToken)
    {
        var scheduleId = new ScheduleId(request.ScheduleId);

        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId, cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Result.Failure<Guid>(SchedulingErrors.ScheduleNotFound);
        }

        var triggerResult = ScheduleExecution.Trigger(
            scheduleId, schedule.TenantId, request.JobIdentifier, request.RetryCount, _timeProvider.GetUtcNow());
        if (triggerResult.IsFailure)
        {
            return Result.Failure<Guid>(triggerResult.Error);
        }

        await _executionRepository.AddAsync(triggerResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(triggerResult.Value.Id.Value);
    }
}
