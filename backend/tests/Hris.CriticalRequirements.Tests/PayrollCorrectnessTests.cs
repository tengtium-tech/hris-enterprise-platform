using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-PAY-001 through CTR-PAY-009, docs/09-testing/critical-test-requirements.md §11.
/// Meaningful once docs/04-modules/payroll/ (Phase 4, Sprint 1) exists.
/// NEXT-STEPS.md's own recorded finding on CTR-PAY-001 applies here directly: it
/// "cannot be enforced by tooling" alone -- test values must derive from the
/// versioned statutory reference-data fixtures (docs/03-foundation/statutory-reference-data.md),
/// never from pasting the implementation's own output, or the assertion proves
/// nothing.
/// </summary>
public class PayrollCorrectnessTests
{
    [Fact(Skip = "Not yet implemented. CTR-PAY-001 — Statutory Computation Matches Published Tables.")]
    public void CTR_PAY_001_StatutoryComputationMatchesPublishedTables()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-002 — Bracket Boundaries Are Correct.")]
    public void CTR_PAY_002_BracketBoundariesAreCorrect()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-003 — Rounding Is Applied Consistently and at the Correct Stage.")]
    public void CTR_PAY_003_RoundingIsAppliedConsistentlyAndAtTheCorrectStage()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-004 — Retroactive Computation Uses Historical Values.")]
    public void CTR_PAY_004_RetroactiveComputationUsesHistoricalValues()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-005 — Payroll Processing Is Idempotent.")]
    public void CTR_PAY_005_PayrollProcessingIsIdempotent()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-006 — Locked Payroll Cannot Be Modified.")]
    public void CTR_PAY_006_LockedPayrollCannotBeModified()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-007 — Payroll Data Has No Loss Window.")]
    public void CTR_PAY_007_PayrollDataHasNoLossWindow()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-008 — Government Report Totals Reconcile to Source Records.")]
    public void CTR_PAY_008_GovernmentReportTotalsReconcileToSourceRecords()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-PAY-009 — Statutory Table Selection and Provenance Are Enforced.")]
    public void CTR_PAY_009_StatutoryTableSelectionAndProvenanceAreEnforced()
    {
    }
}
