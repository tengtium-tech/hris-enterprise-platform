using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Commands;

/// <summary>
/// Updates a tenant's own Organization name, per tenant-framework.md's own
/// <c>UpdateTenantOrganizationNameCommand</c> row -- callable in any lifecycle state
/// except Deleted. Never carries a Tenant Code field: Tenant Code is immutable once
/// set (Tenant Aggregate, Invariants).
/// </summary>
public sealed record UpdateTenantOrganizationNameCommand(Guid TenantId, string NewOrganization, Guid UpdatedBy) : ICommand<Result>;

internal sealed class UpdateTenantOrganizationNameCommandHandler : IRequestHandler<UpdateTenantOrganizationNameCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateTenantOrganizationNameCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateTenantOrganizationNameCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.UpdateOrganizationName(request.NewOrganization, new PlatformOperatorId(request.UpdatedBy), _timeProvider.GetUtcNow());
    }
}
