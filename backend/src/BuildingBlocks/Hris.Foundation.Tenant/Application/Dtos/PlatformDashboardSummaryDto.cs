namespace Hris.Foundation.Tenant.Application.Dtos;

/// <summary>
/// Backs <c>GetPlatformDashboardSummaryQuery</c>, per tenant-framework.md's own Returns
/// column: "tenant count by Tenant Lifecycle state, tenant count by Subscription Plan
/// edition, and total UserAccount count across every tenant... no tenant identifier or
/// per-tenant breakdown." Enum keys are serialized as their string names (not raw
/// ints), matching every other enum-keyed read shape this platform exposes across a
/// query boundary.
/// </summary>
public sealed record PlatformDashboardSummaryDto(
    IReadOnlyDictionary<string, int> TenantCountByLifecycleState,
    IReadOnlyDictionary<string, int> TenantCountBySubscriptionPlan,
    int TotalUserAccountCount);
