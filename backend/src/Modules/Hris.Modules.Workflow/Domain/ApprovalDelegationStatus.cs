namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Source: docs/04-modules/workflow/domain/approval-delegation.md's own lifecycle
/// diagram. Named with the <c>Approval</c> prefix rather than reusing
/// administration's own <c>DelegationStatus</c> name, because the two delegations
/// are deliberately separate arrangements (WR-044) in separate modules.
///
/// <see cref="Scheduled"/> confers nothing: a delegation created two weeks ahead of
/// its period must not let the delegate act during those two weeks, which is why
/// routing acts on activation rather than creation.
/// </summary>
public enum ApprovalDelegationStatus
{
    /// <summary>Created, period not yet begun. Confers no authority.</summary>
    Scheduled = 0,

    /// <summary>Period begun. Routing offers in-scope steps to the delegate.</summary>
    Active = 1,

    /// <summary>Period ended automatically, with no actor recorded (WR-043).</summary>
    Expired = 2,

    /// <summary>Ended early by the delegator or an administrator, with actor and reason.</summary>
    Revoked = 3,
}
