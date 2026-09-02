using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Application.Dtos;
using Hris.Foundation.Tenant.Application.Mapping;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Queries;

/// <summary>
/// Every tenant's own registry record, per tenant-framework.md's own
/// <c>ListTenantsQuery</c> row -- "Platform Operator only, never scoped to or
/// filterable by a single tenant's own business data." Ungated the same way
/// <c>GetCountryConfigurationQuery</c> is, for the same structural reason
/// <c>RegisterTenantCommand</c>'s own remarks state: Authorization Framework's own
/// <c>CheckAuthorizationQuery</c> has no platform-wide scope to evaluate this
/// Platform-Operator-only query against.
/// </summary>
public sealed record ListTenantsQuery : IQuery<Result<IReadOnlyCollection<TenantDto>>>;

internal sealed class ListTenantsQueryHandler : IRequestHandler<ListTenantsQuery, Result<IReadOnlyCollection<TenantDto>>>
{
    private readonly ITenantRepository _repository;

    public ListTenantsQueryHandler(ITenantRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyCollection<TenantDto>>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<TenantDto> dtos = tenants.Select(TenantMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
