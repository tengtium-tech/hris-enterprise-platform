using Xunit;

namespace Hris.CriticalRequirements.Tests;

/// <summary>
/// CTR-WFL-001 through CTR-WFL-006, docs/09-testing/critical-test-requirements.md §8.
/// Meaningful once docs/04-modules/workflow/ (Phase 3, Sprint 2) exists.
/// </summary>
public class WorkflowOrchestrationTests
{
    [Fact(Skip = "Not yet implemented. CTR-WFL-001 — Workflow Actions Invoke Module Commands.")]
    public void CTR_WFL_001_WorkflowActionsInvokeModuleCommands()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-WFL-002 — Self-Approval Is Structurally Prevented; Top-of-Hierarchy Exemption Is Audited.")]
    public void CTR_WFL_002_SelfApprovalIsStructurallyPreventedTopOfHierarchyExemptionIsAudited()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-WFL-003 — Workflow Actions Are Idempotent.")]
    public void CTR_WFL_003_WorkflowActionsAreIdempotent()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-WFL-004 — Workflow State Survives Restart.")]
    public void CTR_WFL_004_WorkflowStateSurvivesRestart()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-WFL-005 — Running Instances Continue on Their Original Definition Version.")]
    public void CTR_WFL_005_RunningInstancesContinueOnTheirOriginalDefinitionVersion()
    {
    }

    [Fact(Skip = "Not yet implemented. CTR-WFL-006 — Exhausted Retries Reach the Dead Letter Queue.")]
    public void CTR_WFL_006_ExhaustedRetriesReachTheDeadLetterQueue()
    {
    }
}
