namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// One approval-chain step's outcome, used by <see cref="AttendanceRecord"/>,
/// <see cref="AttendanceAdjustment"/>, and <see cref="OvertimeRequest"/> alike, per
/// docs/04-modules/attendance/domain/value-objects.md. Where the decision was made
/// under delegated authority, both the delegate and the original approver's identity
/// are preserved (AT-033), never collapsed into a single field that loses one.
/// </summary>
public sealed record ApprovalDecision(
    Guid ApproverId,
    ApprovalDecisionOutcome Outcome,
    DateTimeOffset DecidedOnUtc,
    string? Comments,
    Guid? DelegateId,
    Guid? OriginalApproverId);
