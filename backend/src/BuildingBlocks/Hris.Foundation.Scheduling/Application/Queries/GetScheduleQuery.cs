using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Application.Dtos;
using Hris.Foundation.Scheduling.Application.Mapping;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Queries;

public sealed record GetScheduleQuery(Guid ScheduleId) : IQuery<Result<ScheduleDto>>;

internal sealed class GetScheduleQueryHandler : IRequestHandler<GetScheduleQuery, Result<ScheduleDto>>
{
    private readonly IScheduleRepository _repository;

    public GetScheduleQueryHandler(IScheduleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ScheduleDto>> Handle(GetScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);

        return schedule is null
            ? Result.Failure<ScheduleDto>(SchedulingErrors.ScheduleNotFound)
            : Result.Success(SchedulingMapper.ToDto(schedule));
    }
}
