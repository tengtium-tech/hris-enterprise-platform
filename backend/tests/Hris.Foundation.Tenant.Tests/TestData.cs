using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Tenant.Domain;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same
/// document's own 2.1 ("must not touch... a clock").
///
/// Every method here returns or accepts <see cref="TenantAggregate"/>, the alias for
/// <see cref="Domain.Tenant"/> -- see <c>TenantMapper</c>'s own remarks (production
/// project) for why a bare <c>Tenant</c> reference is unsafe in any namespace under
/// this project's own root, and this test project's own root
/// (<c>Hris.Foundation.Tenant.Tests</c>) is one such namespace.
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static TenantCode NewTenantCode(string? value = null) =>
        TenantCode.Create(value ?? $"tenant-{Guid.NewGuid():N}"[..20]).Value;

    public static PlatformOperatorId NewPlatformOperatorId() => new(Guid.NewGuid());

    public static UserAccountId NewUserAccountId() => new(Guid.NewGuid());

    public static TenantConfigurationId NewTenantConfigurationId() => new(Guid.NewGuid());

    public static TenantAggregate RegisteredTenant(
        TenantCode? tenantCode = null,
        string organization = "ACME Manufacturing",
        SubscriptionPlan subscriptionPlan = SubscriptionPlan.Growth,
        PlatformOperatorId? registeredBy = null,
        DateTimeOffset? nowUtc = null) =>
        TenantAggregate.Register(
            tenantCode ?? NewTenantCode(),
            organization,
            subscriptionPlan,
            registeredBy ?? NewPlatformOperatorId(),
            nowUtc ?? NowUtc).Value;

    /// <summary>A tenant already in <see cref="TenantLifecycleState.Configured"/>.</summary>
    public static TenantAggregate ConfiguredTenant(DateTimeOffset? nowUtc = null)
    {
        var tenant = RegisteredTenant(nowUtc: nowUtc);
        tenant.CompleteProvisioning(NewTenantConfigurationId(), nowUtc ?? NowUtc);
        return tenant;
    }

    /// <summary>A tenant already <see cref="TenantLifecycleState.Active"/>.</summary>
    public static TenantAggregate ActiveTenant(DateTimeOffset? nowUtc = null)
    {
        var tenant = ConfiguredTenant(nowUtc);
        tenant.Activate(NewUserAccountId(), nowUtc ?? NowUtc);
        return tenant;
    }

    /// <summary>A tenant already <see cref="TenantLifecycleState.Suspended"/>.</summary>
    public static TenantAggregate SuspendedTenant(DateTimeOffset? nowUtc = null)
    {
        var tenant = ActiveTenant(nowUtc);
        tenant.Suspend("Non-payment", NewPlatformOperatorId(), nowUtc ?? NowUtc);
        return tenant;
    }

    /// <summary>A tenant already <see cref="TenantLifecycleState.Archived"/>.</summary>
    public static TenantAggregate ArchivedTenant(DateTimeOffset? nowUtc = null)
    {
        var tenant = ActiveTenant(nowUtc);
        tenant.Archive("Churned", NewPlatformOperatorId(), nowUtc ?? NowUtc);
        return tenant;
    }

    /// <summary>A tenant already <see cref="TenantLifecycleState.Deleted"/>.</summary>
    public static TenantAggregate DeletedTenant(DateTimeOffset? nowUtc = null)
    {
        var tenant = ArchivedTenant(nowUtc);
        tenant.Delete("Retention window elapsed", "RA 10173 erasure request", NewPlatformOperatorId(), nowUtc ?? NowUtc);
        return tenant;
    }
}
