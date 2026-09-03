using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Commands;

public sealed record UpdateScheduleCommand(
    Guid ScheduleId,
    string Expression,
    string TimeZone,
    string TaskType,
    string? TaskReferenceId,
    HolidayBehavior HolidayBehavior,
    string? CalendarReference) : ICommand<Result>;

internal sealed class UpdateScheduleCommandHandler : IRequestHandler<UpdateScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateScheduleCommandHandler(IScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        if (schedule is null)
        {
            return Result.Failure(SchedulingErrors.ScheduleNotFound);
        }

        var expressionResult = ScheduleExpression.Create(request.Expression);
        if (expressionResult.IsFailure)
        {
            return Result.Failure(expressionResult.Error);
        }

        var timeZoneResult = ScheduleTimeZone.Create(request.TimeZone);
        if (timeZoneResult.IsFailure)
        {
            return Result.Failure(timeZoneResult.Error);
        }

        return schedule.Update(
            expressionResult.Value,
            timeZoneResult.Value,
            request.TaskType,
            request.TaskReferenceId,
            request.HolidayBehavior,
            request.CalendarReference,
            _timeProvider.GetUtcNow());
    }
}
