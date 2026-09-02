using System.Reflection;
using FluentValidation;
using Hris.Foundation.Tenant.Domain;
using Hris.Foundation.Tenant.Infrastructure.Persistence;
using Hris.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Tenant;

/// <summary>
/// Tenant Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the identical shape every Sprint 3 kernel
/// framework's own registration establishes, now the first of Sprint 4's own eight.
///
/// This Sprint's own build implements six of tenant-framework.md's eight
/// Platform-Operator-Facing Commands and Queries' commands in full
/// (RegisterTenant/Suspend/Reactivate/Archive/Delete/UpdateOrganizationName), one
/// with a narrowed scope (ChangeTenantSubscriptionPlan -- the field change only, not
/// pack activation/removal), one command this document names with no command surface
/// of its own (CompleteTenantProvisioning, the mechanical trigger for the
/// document's own "Automated provisioning completes" transition), and three of the
/// five queries (ListTenants, GetTenant, GetPlatformDashboardSummary).
///
/// <c>ListTenantUsersQuery</c> and <c>GetTenantConfigurationQuery</c> -- the two
/// queries ADR-0009's own 2026-08-23 Amendment added -- are deliberately NOT built
/// here. Both read `administration`'s own aggregates
/// (docs/04-modules/administration/domain/aggregates.md's User Account and Tenant
/// Configuration Aggregates), and `administration` is a Phase 2 business module that
/// does not exist in `backend/` code yet -- this Sprint (Sprint 4) builds Foundation
/// frameworks only, per IMPLEMENTATION-PLAN.md and CLAUDE.md's own "Coding Phase"
/// scope gate. Building either query now would mean either querying a repository
/// interface with no real implementation to inject, or silently substituting a
/// narrower read against a different, already-built aggregate (Identity Framework's
/// own UserAccount lacks Account Type and Employee linkage; there is no
/// TenantConfiguration equivalent to substitute at all) -- both worse than the
/// explicit gap stated here. Add both once `administration` exists and can supply a
/// real <c>IUserAccountRepository</c>/<c>ITenantConfigurationRepository</c> of its
/// own to inject.
///
/// Of this framework's own five Upstream Dependencies (Identity, Authorization,
/// Configuration, Audit, Localization), only Identity is concretely wired --
/// <c>GetPlatformDashboardSummaryQuery</c> reads its <c>IUserAccountRepository</c>
/// directly for a platform-wide count, and <c>ActivateTenantCommand</c>/
/// <c>TenantActivated</c> reference its <c>UserAccountId</c> type. The other four are
/// not yet called through MediatR or referenced concretely: this Sprint's own build
/// has no tenant-scoped write to gate through Authorization Framework (every
/// Platform-Operator-Facing command is explicitly outside any tenant's own
/// authorization boundary, per ADR-0009 -- see <c>RegisterTenantCommand</c>'s own
/// remarks for why that is a routing/authentication concern, not a
/// per-request Authorization Framework check), no configuration value to resolve
/// through Configuration Framework, no write here carries a real tenant id an Audit
/// Framework record could be attributed to without inventing one (the same reasoning
/// Localization Framework's own remarks already state for its own deferred Audit
/// wiring), and no locale/currency/time-zone value this framework itself resolves
/// through Localization Framework (those are `RegisterTenantCommand`'s own
/// registration-time inputs, consumed by TenantConfiguration once it exists, never by
/// Tenant Framework itself). Each is a real, stated Upstream Dependency this
/// framework may call through MediatR once a concrete integration point needs it, the
/// same "wire it in when a real caller exists" precedent every other Sprint 3/4
/// framework's own DependencyInjection.cs already documents for at least one of its
/// own nominally-unused dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var thisAssembly = Assembly.GetExecutingAssembly();

        PersistenceAssemblyRegistry.Register(thisAssembly);

        services.AddMediatR(config => config.RegisterServicesFromAssembly(thisAssembly));
        services.AddValidatorsFromAssembly(thisAssembly);

        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}
