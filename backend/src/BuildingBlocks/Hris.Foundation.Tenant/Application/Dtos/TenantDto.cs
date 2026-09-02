namespace Hris.Foundation.Tenant.Application.Dtos;

/// <summary>
/// The read-side shape <c>ListTenantsQuery</c> and <c>GetTenantQuery</c> return, per
/// dto-design.md's convention. Registry fields only -- Tenant Code, Organization,
/// Subscription Plan, current Lifecycle state -- per tenant-framework.md's own
/// <c>ListTenantsQuery</c>/<c>GetTenantQuery</c> Returns column.
///
/// <c>GetTenantQuery</c>'s own Returns column also names "Process Pack entitlement
/// summary" -- deliberately not on this DTO: that summary is `administration`'s own
/// TenantConfiguration/ProcessPackActivation data (does not exist in code yet, see
/// <see cref="Domain.TenantConfigurationId"/>'s own remarks), not something
/// <see cref="Domain.Tenant"/> itself holds. A future pass that wires up
/// TenantConfiguration adds that summary as an additional field or a second query,
/// not by inventing Process Pack data here.
/// </summary>
public sealed record TenantDto(
    Guid TenantId,
    string TenantCode,
    string Organization,
    string SubscriptionPlan,
    string LifecycleState);
