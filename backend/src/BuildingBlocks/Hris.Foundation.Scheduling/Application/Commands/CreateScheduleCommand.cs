using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Commands;

/// <summary>
/// Registers a new schedule, in <see cref="ScheduleStatus.Draft"/>. Carries raw
/// primitives, not Domain Value Objects, across the MediatR boundary -- this handler is
/// the one place a malformed expression or time zone becomes a
/// <see cref="SchedulingErrors"/> failure.
/// </summary>
public sealed record CreateScheduleCommand(
    Guid TenantId,
    ScheduleType ScheduleType,
    string Expression,
    string TimeZone,
    string TaskType,
    string? TaskReferenceId,
    HolidayBehavior HolidayBehavior,
    string? CalendarReference) : ICommand<Result<Guid>>;

internal sealed class CreateScheduleCommandHandler : IRequestHandler<CreateScheduleCommand, Result<Guid>>
{
    private readonly IScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateScheduleCommandHandler(IScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var expressionResult = ScheduleExpression.Create(request.Expression);
        if (expressionResult.IsFailure)
        {
            return Result.Failure<Guid>(expressionResult.Error);
        }

        var timeZoneResult = ScheduleTimeZone.Create(request.TimeZone);
        if (timeZoneResult.IsFailure)
        {
            return Result.Failure<Guid>(timeZoneResult.Error);
        }

        var createResult = Schedule.Create(
            request.TenantId,
            request.ScheduleType,
            expressionResult.Value,
            timeZoneResult.Value,
            request.TaskType,
            request.TaskReferenceId,
            request.HolidayBehavior,
            request.CalendarReference,
            _timeProvider.GetUtcNow());
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(createResult.Value.Id.Value);
    }
}
