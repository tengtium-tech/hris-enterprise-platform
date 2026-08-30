using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-API-001 through CTR-API-003, docs/09-testing/critical-test-requirements.md §12.
/// Meaningful once Hris.Api exposes at least one published endpoint contract to
/// hold stable against, per docs/03-foundation/api-standards.md and ADR-0006.
/// </summary>
public class ApiContractStabilityTests
{
    [Fact(Skip = "Not yet implemented. CTR-API-001 — Published Contracts Do Not Regress.")]
    public void CTR_API_001_PublishedContractsDoNotRegress()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-API-002 — API and Web Enforce Identical Rules.")]
    public void CTR_API_002_ApiAndWebEnforceIdenticalRules()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-API-003 — Errors Are Machine-Readable and Stable.")]
    public void CTR_API_003_ErrorsAreMachineReadableAndStable()
    {
    }
}
