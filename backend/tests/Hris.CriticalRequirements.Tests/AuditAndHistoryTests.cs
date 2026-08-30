using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-AUD-001 through CTR-AUD-005, docs/09-testing/critical-test-requirements.md §9.
/// Meaningful once Hris.Foundation.Audit (Sprint 3) has a real implementation.
/// </summary>
public class AuditAndHistoryTests
{
    [Fact(Skip = "Not yet implemented. CTR-AUD-001 — Audit Records Are Immutable.")]
    public void CTR_AUD_001_AuditRecordsAreImmutable()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUD-002 — Every Business Change Produces an Audit Record.")]
    public void CTR_AUD_002_EveryBusinessChangeProducesAnAuditRecord()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUD-003 — History Is Written Transactionally With the Change.")]
    public void CTR_AUD_003_HistoryIsWrittenTransactionallyWithTheChange()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUD-004 — Point-in-Time Queries Return Historically Accurate Values.")]
    public void CTR_AUD_004_PointInTimeQueriesReturnHistoricallyAccurateValues()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-AUD-005 — History Survives Pack Deactivation.")]
    public void CTR_AUD_005_HistorySurvivesPackDeactivation()
    {
    }
}
