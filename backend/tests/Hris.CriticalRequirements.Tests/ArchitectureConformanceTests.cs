using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-ARC-001 through CTR-ARC-004, docs/09-testing/critical-test-requirements.md §4.
/// These are architecture tests (NetArchTest or ArchUnitNET, per coding-standards.md's
/// Static Analysis table) more than they are unit tests -- each should assert a
/// structural property of the compiled assemblies, not behavior of a single method.
/// Skipped until the module/project structure they inspect exists in enough depth
/// to make the assertion meaningful.
/// </summary>
public class ArchitectureConformanceTests
{
    [Fact(Skip = "Not yet implemented. CTR-ARC-001 — Domain Layer Has No Outward Dependencies.")]
    public void CTR_ARC_001_DomainLayerHasNoOutwardDependencies()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ARC-002 — No Cross-Module Internal References.")]
    public void CTR_ARC_002_NoCrossModuleInternalReferences()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ARC-003 — No Shared Business Tables.")]
    public void CTR_ARC_003_NoSharedBusinessTables()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-ARC-004 — Repositories Exist Only for Aggregate Roots.")]
    public void CTR_ARC_004_RepositoriesExistOnlyForAggregateRoots()
    {
    }
}
