using Hris.Foundation.Tenant.Application.Dtos;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code --
/// the identical choice every other Sprint 3/4 framework's own mapper already
/// establishes.
///
/// Every file in this framework outside the Domain namespace itself refers to the
/// Aggregate Root through the <see cref="TenantAggregate"/> alias, never a bare
/// <c>using Hris.Foundation.Tenant.Domain;</c> plus unqualified <c>Tenant</c>: this
/// project's own root namespace (<c>Hris.Foundation.Tenant</c>) is a direct child
/// namespace of <c>Hris.Foundation</c> literally named <c>Tenant</c>, which C#'s own
/// enclosing-namespace simple-name lookup finds before an outer-scope <c>using</c>
/// gets a chance to -- the identical ancestor-namespace-shadowing hazard
/// <c>Hris.Foundation.Authorization.Tests.TestData</c> hit for real this session
/// (fixed there the same way, via an explicit alias), except unavoidable here on
/// every single reference rather than one file, since no other Sprint 3/4 framework's
/// own Aggregate Root name collides with its own project's last namespace segment.
/// </summary>
internal static class TenantMapper
{
    public static TenantDto ToDto(TenantAggregate tenant) => new(
        tenant.Id.Value,
        tenant.TenantCode.Value,
        tenant.Organization,
        tenant.SubscriptionPlan.ToString(),
        tenant.LifecycleState.ToString());
}
