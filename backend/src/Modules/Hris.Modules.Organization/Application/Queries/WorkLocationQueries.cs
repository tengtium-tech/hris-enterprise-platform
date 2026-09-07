using Hris.Application.Abstractions;
using Hris.Modules.Organization.Application.Dtos;
using Hris.Modules.Organization.Application.Mapping;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Queries;

public sealed record GetWorkLocationQuery(Guid WorkLocationId, Guid TenantId) : IQuery<Result<WorkLocationDto>>;

internal sealed class GetWorkLocationQueryHandler : IRequestHandler<GetWorkLocationQuery, Result<WorkLocationDto>>
{
    private readonly IWorkLocationRepository _repository;

    public GetWorkLocationQueryHandler(IWorkLocationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<WorkLocationDto>> Handle(GetWorkLocationQuery request, CancellationToken cancellationToken)
    {
        var workLocationResult = await OrganizationLookup.LoadWorkLocationForTenantAsync(
            _repository, request.WorkLocationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return workLocationResult.IsFailure
            ? Result.Failure<WorkLocationDto>(workLocationResult.Error)
            : Result.Success(OrganizationMapper.ToDto(workLocationResult.Value));
    }
}

public sealed record ListWorkLocationsQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<WorkLocationSummaryDto>>>;

internal sealed class ListWorkLocationsQueryHandler
    : IRequestHandler<ListWorkLocationsQuery, Result<IReadOnlyList<WorkLocationSummaryDto>>>
{
    private readonly IWorkLocationRepository _repository;

    public ListWorkLocationsQueryHandler(IWorkLocationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<WorkLocationSummaryDto>>> Handle(
        ListWorkLocationsQuery request, CancellationToken cancellationToken)
    {
        var workLocations = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WorkLocationSummaryDto> dtos = workLocations.Select(OrganizationMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
