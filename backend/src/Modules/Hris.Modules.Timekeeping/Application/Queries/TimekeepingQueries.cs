using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Application.Dtos;
using Hris.Modules.Timekeeping.Application.Mapping;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Queries;

public sealed record GetWorkScheduleQuery(Guid WorkScheduleId, Guid TenantId) : IQuery<Result<WorkScheduleDto>>;

internal sealed class GetWorkScheduleQueryHandler : IRequestHandler<GetWorkScheduleQuery, Result<WorkScheduleDto>>
{
    private readonly IWorkScheduleRepository _repository;

    public GetWorkScheduleQueryHandler(IWorkScheduleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkScheduleDto>> Handle(GetWorkScheduleQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await TimekeepingLookup
            .LoadScheduleForTenantAsync(_repository, request.WorkScheduleId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<WorkScheduleDto>(result.Error)
            : Result.Success(TimekeepingMapper.ToDto(result.Value));
    }
}

public sealed record ListWorkSchedulesQuery(Guid TenantId, string? Status)
    : IQuery<Result<IReadOnlyList<WorkScheduleDto>>>;

internal sealed class ListWorkSchedulesQueryHandler
    : IRequestHandler<ListWorkSchedulesQuery, Result<IReadOnlyList<WorkScheduleDto>>>
{
    private readonly IWorkScheduleRepository _repository;

    public ListWorkSchedulesQueryHandler(IWorkScheduleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkScheduleDto>>> Handle(
        ListWorkSchedulesQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var schedules = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WorkScheduleDto> dtos = schedules
            .Where(schedule => request.Status is null
                || string.Equals(schedule.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase))
            .Select(TimekeepingMapper.ToDto)
            .ToList();

        return Result.Success(dtos);
    }
}

public sealed record GetWorkShiftQuery(Guid WorkShiftId, Guid TenantId) : IQuery<Result<WorkShiftDto>>;

internal sealed class GetWorkShiftQueryHandler : IRequestHandler<GetWorkShiftQuery, Result<WorkShiftDto>>
{
    private readonly IWorkShiftRepository _repository;

    public GetWorkShiftQueryHandler(IWorkShiftRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkShiftDto>> Handle(GetWorkShiftQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await TimekeepingLookup
            .LoadShiftForTenantAsync(_repository, request.WorkShiftId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<WorkShiftDto>(result.Error)
            : Result.Success(TimekeepingMapper.ToDto(result.Value));
    }
}

public sealed record ListWorkShiftsQuery(Guid TenantId, string? Status) : IQuery<Result<IReadOnlyList<WorkShiftDto>>>;

internal sealed class ListWorkShiftsQueryHandler
    : IRequestHandler<ListWorkShiftsQuery, Result<IReadOnlyList<WorkShiftDto>>>
{
    private readonly IWorkShiftRepository _repository;

    public ListWorkShiftsQueryHandler(IWorkShiftRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkShiftDto>>> Handle(
        ListWorkShiftsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var shifts = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WorkShiftDto> dtos = shifts
            .Where(shift => request.Status is null
                || string.Equals(shift.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase))
            .Select(TimekeepingMapper.ToDto)
            .ToList();

        return Result.Success(dtos);
    }
}

public sealed record ListShiftAssignmentsQuery(Guid TenantId, string? TargetId)
    : IQuery<Result<IReadOnlyList<ShiftAssignmentDto>>>;

internal sealed class ListShiftAssignmentsQueryHandler
    : IRequestHandler<ListShiftAssignmentsQuery, Result<IReadOnlyList<ShiftAssignmentDto>>>
{
    private readonly IShiftAssignmentRepository _repository;

    public ListShiftAssignmentsQueryHandler(IShiftAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<ShiftAssignmentDto>>> Handle(
        ListShiftAssignmentsQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var assignments = string.IsNullOrWhiteSpace(request.TargetId)
            ? await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false)
            : await _repository.ListByTargetAsync(request.TenantId, request.TargetId.Trim(), cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyList<ShiftAssignmentDto> dtos = assignments.Select(TimekeepingMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}

public sealed record GetHolidayCalendarQuery(Guid HolidayCalendarId, Guid TenantId)
    : IQuery<Result<HolidayCalendarDto>>;

internal sealed class GetHolidayCalendarQueryHandler
    : IRequestHandler<GetHolidayCalendarQuery, Result<HolidayCalendarDto>>
{
    private readonly IHolidayCalendarRepository _repository;

    public GetHolidayCalendarQueryHandler(IHolidayCalendarRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<HolidayCalendarDto>> Handle(
        GetHolidayCalendarQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await TimekeepingLookup
            .LoadCalendarForTenantAsync(_repository, request.HolidayCalendarId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<HolidayCalendarDto>(result.Error)
            : Result.Success(TimekeepingMapper.ToDto(result.Value));
    }
}
