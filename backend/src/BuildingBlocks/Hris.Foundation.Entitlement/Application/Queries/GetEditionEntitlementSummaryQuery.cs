using Hris.Application.Abstractions;
using Hris.Foundation.Entitlement.Application.Dtos;
using Hris.Foundation.Entitlement.Application.Mapping;
using Hris.Foundation.Entitlement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Entitlement.Application.Queries;

/// <summary>
/// Every Process Pack's own entitlement standing for one edition -- the "what does a
/// tenant of this edition hold" summary tenant-configuration.md's own
/// <c>GetTenantQuery</c> reference names, computed here from edition defaults alone
/// since this framework holds no per-tenant override state (see this framework's own
/// Scope). A future caller combining this with Administration module's own
/// <c>ProcessPackActivation</c> overrides (Phase 2) folds the two together at that
/// caller's own layer, not here.
/// </summary>
public sealed record GetEditionEntitlementSummaryQuery(TenantEditionCode Edition)
    : IQuery<Result<IReadOnlyCollection<PackEntitlementDto>>>;

internal sealed class GetEditionEntitlementSummaryQueryHandler
    : IRequestHandler<GetEditionEntitlementSummaryQuery, Result<IReadOnlyCollection<PackEntitlementDto>>>
{
    public Task<Result<IReadOnlyCollection<PackEntitlementDto>>> Handle(
        GetEditionEntitlementSummaryQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PackEntitlementDto> summary = ProcessPackCatalog.AllPacks
            .Select(pack => pack.ToPackEntitlementDto(request.Edition))
            .ToList();

        return Task.FromResult(Result.Success(summary));
    }
}
