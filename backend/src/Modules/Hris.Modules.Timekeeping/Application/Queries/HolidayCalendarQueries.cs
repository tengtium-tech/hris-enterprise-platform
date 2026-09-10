using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Application.Dtos;
using Hris.Modules.Timekeeping.Application.Mapping;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Queries;

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
