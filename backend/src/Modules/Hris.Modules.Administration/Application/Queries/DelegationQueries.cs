using Hris.Application.Abstractions;
using Hris.Modules.Administration.Application.Dtos;
using Hris.Modules.Administration.Application.Mapping;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Queries;

public sealed record GetDelegationQuery(Guid DelegationId, Guid TenantId) : IQuery<Result<AdministrativeDelegationDto>>;

internal sealed class GetDelegationQueryHandler : IRequestHandler<GetDelegationQuery, Result<AdministrativeDelegationDto>>
{
    private readonly IAdministrativeDelegationRepository _repository;

    public GetDelegationQueryHandler(IAdministrativeDelegationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<AdministrativeDelegationDto>> Handle(GetDelegationQuery request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadDelegationForTenantAsync(
            _repository, request.DelegationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<AdministrativeDelegationDto>(result.Error)
            : Result.Success(AdministrationMapper.ToDto(result.Value));
    }
}

public sealed record ListDelegationsQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<AdministrativeDelegationDto>>>;

internal sealed class ListDelegationsQueryHandler : IRequestHandler<ListDelegationsQuery, Result<IReadOnlyList<AdministrativeDelegationDto>>>
{
    private readonly IAdministrativeDelegationRepository _repository;

    public ListDelegationsQueryHandler(IAdministrativeDelegationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<AdministrativeDelegationDto>>> Handle(ListDelegationsQuery request, CancellationToken cancellationToken)
    {
        var delegations = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AdministrativeDelegationDto> dtos = delegations.Select(AdministrationMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
