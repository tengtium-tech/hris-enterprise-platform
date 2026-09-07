using Hris.Application.Abstractions;
using Hris.Modules.Organization.Application.Dtos;
using Hris.Modules.Organization.Application.Mapping;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Queries;

public sealed record GetLegalEntityQuery(Guid LegalEntityId, Guid TenantId) : IQuery<Result<LegalEntityDto>>;

internal sealed class GetLegalEntityQueryHandler : IRequestHandler<GetLegalEntityQuery, Result<LegalEntityDto>>
{
    private readonly ILegalEntityRepository _repository;

    public GetLegalEntityQueryHandler(ILegalEntityRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<LegalEntityDto>> Handle(GetLegalEntityQuery request, CancellationToken cancellationToken)
    {
        var legalEntityResult = await OrganizationLookup.LoadLegalEntityForTenantAsync(
            _repository, request.LegalEntityId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return legalEntityResult.IsFailure
            ? Result.Failure<LegalEntityDto>(legalEntityResult.Error)
            : Result.Success(OrganizationMapper.ToDto(legalEntityResult.Value));
    }
}

public sealed record ListLegalEntitiesQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<LegalEntitySummaryDto>>>;

internal sealed class ListLegalEntitiesQueryHandler
    : IRequestHandler<ListLegalEntitiesQuery, Result<IReadOnlyList<LegalEntitySummaryDto>>>
{
    private readonly ILegalEntityRepository _repository;

    public ListLegalEntitiesQueryHandler(ILegalEntityRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<LegalEntitySummaryDto>>> Handle(
        ListLegalEntitiesQuery request, CancellationToken cancellationToken)
    {
        var legalEntities = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<LegalEntitySummaryDto> dtos = legalEntities.Select(OrganizationMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
