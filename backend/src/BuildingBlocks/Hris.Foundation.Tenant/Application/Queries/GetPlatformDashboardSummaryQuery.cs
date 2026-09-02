using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Tenant.Application.Dtos;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Queries;

/// <summary>
/// Backs the Platform Operator Dashboard screen, per tenant-framework.md's own
/// <c>GetPlatformDashboardSummaryQuery</c> row (added 2026-08-23, ADR-0009's own
/// Amendment): "platform-wide aggregate counts only... no tenant identifier or
/// per-tenant breakdown returned." Reads only <see cref="ITenantRepository"/> (this
/// framework's own registry) and <see cref="IUserAccountRepository"/> (Identity
/// Framework, one of this framework's own five stated Upstream Dependencies) -- never
/// `administration`'s own richer UserAccount aggregate, which does not exist in code
/// yet. That distinction does not narrow this specific field: the query only ever
/// needs a raw count, and Identity Framework's own UserAccount row exists 1:1 with
/// whatever `administration` would eventually wrap it in, so the count is correct
/// regardless of which of the two aggregates would answer a richer, per-account
/// question this query never asks.
/// </summary>
public sealed record GetPlatformDashboardSummaryQuery : IQuery<Result<PlatformDashboardSummaryDto>>;

internal sealed class GetPlatformDashboardSummaryQueryHandler
    : IRequestHandler<GetPlatformDashboardSummaryQuery, Result<PlatformDashboardSummaryDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserAccountRepository _userAccountRepository;

    public GetPlatformDashboardSummaryQueryHandler(
        ITenantRepository tenantRepository,
        IUserAccountRepository userAccountRepository)
    {
        _tenantRepository = Guard.AgainstNull(tenantRepository, nameof(tenantRepository));
        _userAccountRepository = Guard.AgainstNull(userAccountRepository, nameof(userAccountRepository));
    }

    public async Task<Result<PlatformDashboardSummaryDto>> Handle(
        GetPlatformDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var byLifecycleState = await _tenantRepository.CountByLifecycleStateAsync(cancellationToken).ConfigureAwait(false);
        var bySubscriptionPlan = await _tenantRepository.CountBySubscriptionPlanAsync(cancellationToken).ConfigureAwait(false);
        var totalUserAccountCount = await _userAccountRepository.CountAllAsync(cancellationToken).ConfigureAwait(false);

        var dto = new PlatformDashboardSummaryDto(
            byLifecycleState.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            bySubscriptionPlan.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            totalUserAccountCount);

        return Result.Success(dto);
    }
}
