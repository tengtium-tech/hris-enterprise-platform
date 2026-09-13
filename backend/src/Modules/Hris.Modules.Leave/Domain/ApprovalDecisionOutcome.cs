namespace Hris.Modules.Leave.Domain;

/// <summary>The decision recorded by an <see cref="ApprovalDecision"/>.</summary>
public enum ApprovalDecisionOutcome
{
    Approved,
    Rejected,
    ReturnedForCorrection,
}
