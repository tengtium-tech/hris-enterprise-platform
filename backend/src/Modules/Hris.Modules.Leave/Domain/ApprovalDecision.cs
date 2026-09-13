namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Captures one approval-chain step's outcome, used by <c>LeaveRequest</c>,
/// <c>LeaveAdjustment</c>, and <c>LeaveEncashment</c> alike. Mirrors the shape
/// <c>Hris.Modules.Attendance.Domain.ApprovalDecision</c> defines for Attendance, but is
/// this module's own type rather than a shared reference to Attendance's — this module
/// carries no compile-time dependency on any sibling module (CTR-ARC-002). Delegated-
/// authority decisions preserve both the delegate and original approver's identity, never
/// collapsed into one field (LV-043). Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record ApprovalDecision(
    Guid ApproverId,
    ApprovalDecisionOutcome Outcome,
    DateTimeOffset DecidedOnUtc,
    string? Comments,
    Guid? DelegateId,
    Guid? OriginalApproverId);
