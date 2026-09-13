namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>
/// Full detail of one adjustment, including the approval chain that acted on it
/// (dto-design.md).
/// </summary>
public sealed record AttendanceAdjustmentDto(
    Guid Id,
    Guid TenantId,
    Guid AttendanceRecordId,
    DateOnly WorkDate,
    string Field,
    string OriginalValue,
    string RequestedValue,
    string Category,
    string Reason,
    IReadOnlyList<string> SupportingDocuments,
    Guid SubmittedBy,
    DateTimeOffset SubmittedOn,
    string Status,
    string? ReviewNotes,
    Guid? ReviewerId,
    Guid? ApproverId,
    ApprovalDecisionDto? Decision,
    DateTimeOffset? ApprovedOn);

/// <summary>
/// One approval-chain step's outcome, carried on the adjustment that recorded it. Mirrors
/// the domain <c>ApprovalDecision</c> shape but is a serializable DTO only.
/// </summary>
public sealed record ApprovalDecisionDto(
    Guid ApproverId,
    string Outcome,
    DateTimeOffset DecidedOnUtc,
    string? Comments,
    Guid? DelegateId,
    Guid? OriginalApproverId);
