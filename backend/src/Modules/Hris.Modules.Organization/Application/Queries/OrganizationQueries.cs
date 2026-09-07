using Hris.Application.Abstractions;
using Hris.Modules.Organization.Application.Dtos;
using Hris.Modules.Organization.Application.Mapping;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Queries;

/// <summary>
/// Reads one organization back by its own identifier, with its full nested
/// hierarchy, tenant-checked via <see cref="OrganizationLookup"/> per CTR-ISO-004.
/// </summary>
public sealed record GetOrganizationQuery(Guid OrganizationId, Guid TenantId) : IQuery<Result<OrganizationDto>>;

internal sealed class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, Result<OrganizationDto>>
{
    private readonly IOrganizationRepository _repository;

    public GetOrganizationQueryHandler(IOrganizationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<OrganizationDto>> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure<OrganizationDto>(organizationResult.Error)
            : Result.Success(OrganizationMapper.ToDto(organizationResult.Value));
    }
}

/// <summary>
/// Lists every organization for one tenant, not paged: per roadmap.md's own
/// enterprise-scale expectations, a tenant's organization count (legal entities and
/// their own top-level org charts) is small compared to a business-record collection
/// such as Document Management's own documents, the identical reasoning
/// <c>ListConnectorsQuery</c> already states for itself.
/// </summary>
public sealed record ListOrganizationsQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<OrganizationSummaryDto>>>;

internal sealed class ListOrganizationsQueryHandler
    : IRequestHandler<ListOrganizationsQuery, Result<IReadOnlyList<OrganizationSummaryDto>>>
{
    private readonly IOrganizationRepository _repository;

    public ListOrganizationsQueryHandler(IOrganizationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<OrganizationSummaryDto>>> Handle(
        ListOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var organizations = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<OrganizationSummaryDto> dtos = organizations.Select(OrganizationMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
