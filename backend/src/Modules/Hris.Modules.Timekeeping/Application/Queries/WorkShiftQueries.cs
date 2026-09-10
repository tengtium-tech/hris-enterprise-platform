using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Application.Dtos;
using Hris.Modules.Timekeeping.Application.Mapping;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Queries;

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
