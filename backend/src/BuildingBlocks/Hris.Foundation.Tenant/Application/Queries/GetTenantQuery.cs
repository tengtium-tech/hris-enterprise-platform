using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Application.Dtos;
using Hris.Foundation.Tenant.Application.Mapping;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Queries;

/// <summary>
/// One tenant's own registry record, per tenant-framework.md's own
/// <c>GetTenantQuery</c> row. That row's own Returns column also names "Process Pack
/// entitlement summary" -- deliberately not returned here, see
/// <see cref="Dtos.TenantDto"/>'s own remarks for why.
/// </summary>
public sealed record GetTenantQuery(Guid TenantId) : IQuery<Result<TenantDto>>;

internal sealed class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    private readonly ITenantRepository _repository;

    public GetTenantQueryHandler(ITenantRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<TenantDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);

        return tenant is null
            ? Result.Failure<TenantDto>(TenantErrors.TenantNotFound)
            : Result.Success(TenantMapper.ToDto(tenant));
    }
}
