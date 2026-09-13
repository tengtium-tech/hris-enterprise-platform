namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Seven states, mirroring <c>Hris.Modules.Attendance.Domain.AdjustmentStatus</c> exactly
/// (leave-adjustments.md states this directly). <see cref="Applied"/> is reached only after
/// <c>LeaveBalance</c> confirms the ledger entry was incorporated — never set optimistically
/// at approval (LV-052).
/// </summary>
public enum LeaveAdjustmentStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Cancelled,
    Applied,
}
