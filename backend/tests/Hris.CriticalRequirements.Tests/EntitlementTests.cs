using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-ENT-001 through CTR-ENT-008, docs/09-testing/critical-test-requirements.md §7.
/// Meaningful once docs/03-foundation/tenant-framework.md's Process Pack entitlement
/// model and at least one gated module exist.
/// </summary>
public class EntitlementTests
{
    [Fact(Skip = "Not yet implemented. CTR-ENT-001 — Entitlement Evaluated Before Authorization.")]
    public void CTR_ENT_001_EntitlementEvaluatedBeforeAuthorization()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-002 — Entitlement Enforced Server-Side on All Entry Points.")]
    public void CTR_ENT_002_EntitlementEnforcedServerSideOnAllEntryPoints()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-003 — Queries Tolerate Data From Inactive Packs.")]
    public void CTR_ENT_003_QueriesTolerateDataFromInactivePacks()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-004 — Deactivation Retains Data.")]
    public void CTR_ENT_004_DeactivationRetainsData()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-005 — In-Flight Workflows Are Resolved on Deactivation.")]
    public void CTR_ENT_005_InFlightWorkflowsAreResolvedOnDeactivation()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-006 — Custom Workflows Are Suspended, Not Deleted.")]
    public void CTR_ENT_006_CustomWorkflowsAreSuspendedNotDeleted()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-007 — Entitlement Denial Is Distinguishable From Authorization Denial.")]
    public void CTR_ENT_007_EntitlementDenialIsDistinguishableFromAuthorizationDenial()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ENT-008 — Core Capabilities Are Never Gated.")]
    public void CTR_ENT_008_CoreCapabilitiesAreNeverGated()
    {
    }
}
