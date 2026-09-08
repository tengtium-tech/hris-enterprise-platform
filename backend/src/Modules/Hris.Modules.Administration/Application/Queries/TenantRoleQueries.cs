using Hris.Application.Abstractions;
using Hris.Modules.Administration.Application.Dtos;
using Hris.Modules.Administration.Application.Mapping;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Queries;

public sealed record GetTenantRoleQuery(Guid TenantRoleId, Guid TenantId) : IQuery<Result<TenantRoleDto>>;

internal sealed class GetTenantRoleQueryHandler : IRequestHandler<GetTenantRoleQuery, Result<TenantRoleDto>>
{
    private readonly ITenantRoleRepository _repository;

    public GetTenantRoleQueryHandler(ITenantRoleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<TenantRoleDto>> Handle(GetTenantRoleQuery request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<TenantRoleDto>(result.Error)
            : Result.Success(AdministrationMapper.ToDto(result.Value));
    }
}

public sealed record ListTenantRolesQuery(Guid TenantId, TenantRoleStatus? StatusFilter) : IQuery<Result<IReadOnlyList<TenantRoleSummaryDto>>>;

internal sealed class ListTenantRolesQueryHandler : IRequestHandler<ListTenantRolesQuery, Result<IReadOnlyList<TenantRoleSummaryDto>>>
{
    private readonly ITenantRoleRepository _repository;

    public ListTenantRolesQueryHandler(ITenantRoleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<TenantRoleSummaryDto>>> Handle(ListTenantRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IEnumerable<TenantRole> filtered = roles;
        if (request.StatusFilter.HasValue)
        {
            filtered = filtered.Where(role => role.Status == request.StatusFilter.Value);
        }

        IReadOnlyList<TenantRoleSummaryDto> dtos = filtered.Select(AdministrationMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
