namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Source: docs/04-modules/leave/domain/leave-encashments.md's own lifecycle diagram.
/// <see cref="Processed"/> and <see cref="Paid"/> are reached only once <c>payroll</c>
/// exists and reports back (LV-073) — <see cref="Approved"/> is the terminal state this
/// module itself can reach, mirroring <c>Hris.Modules.Attendance.Domain.OvertimeRequest</c>'s
/// identical caveat. <see cref="Draft"/> and <see cref="Submitted"/> are kept for the same
/// schema-fidelity reason <see cref="LeaveRequestStatus"/> keeps them — this sprint's only
/// creation path produces a request already <see cref="PendingApproval"/> directly.
/// </summary>
public enum LeaveEncashmentStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    Cancelled,
    Processed,
    Paid,
}
