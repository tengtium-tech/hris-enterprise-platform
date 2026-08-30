using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-ISO-001 through CTR-ISO-004, docs/09-testing/critical-test-requirements.md §5.
/// These become meaningful once docs/03-foundation/tenant-framework.md's Tenant
/// Context resolution exists (Sprint 4, per IMPLEMENTATION-PLAN.md Phase 1) and at
/// least one tenant-scoped module exists to isolate (Phase 2 onward).
/// </summary>
public class TenantIsolationTests
{
    [Fact(Skip = "Not yet implemented. CTR-ISO-001 — No Cross-Tenant Read.")]
    public void CTR_ISO_001_NoCrossTenantRead()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ISO-002 — Identifier Enumeration Does Not Cross Tenants.")]
    public void CTR_ISO_002_IdentifierEnumerationDoesNotCrossTenants()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ISO-003 — Isolation Enforced Below the Application Layer.")]
    public void CTR_ISO_003_IsolationEnforcedBelowTheApplicationLayer()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ISO-004 — Background Jobs Carry Tenant Context.")]
    public void CTR_ISO_004_BackgroundJobsCarryTenantContext()
    {
    }
}
