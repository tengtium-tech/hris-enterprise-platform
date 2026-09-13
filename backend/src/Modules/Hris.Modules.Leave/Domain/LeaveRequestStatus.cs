namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Source: docs/04-modules/leave/domain/leave-requests.md's own lifecycle diagram and
/// infrastructure/persistence.md's Status column. <see cref="Draft"/> and
/// <see cref="Submitted"/> name pre-decision states the lifecycle diagram draws with no
/// labeled transition between them and <see cref="PendingApproval"/> — this sprint's only
/// creation path (<c>SubmitLeaveRequestCommand</c>) produces a request already in
/// <see cref="PendingApproval"/> directly, the same way an employee's own in-progress,
/// not-yet-submitted form is a client-side concept this aggregate never persists. Both
/// values are kept in the enum because the schema and lifecycle diagram both name them,
/// not because either is reachable through the public API this sprint.
/// </summary>
public enum LeaveRequestStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    Cancelled,
}
