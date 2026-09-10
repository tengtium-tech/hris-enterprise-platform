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
