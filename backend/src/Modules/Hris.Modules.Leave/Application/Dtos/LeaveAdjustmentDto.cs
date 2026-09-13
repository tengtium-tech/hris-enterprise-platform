namespace Hris.Modules.Leave.Application.Dtos;

/// <summary>Full detail of one adjustment, including the approval chain that acted on it (dto-design.md).</summary>
public sealed record LeaveAdjustmentDto(
    Guid Id,
    Guid TenantId,
    Guid LeaveBalanceId,
    decimal OriginalValueSnapshot,
    decimal RequestedAmount,
    string Reason,
    IReadOnlyList<string> SupportingDocuments,
    string Status,
    Guid SubmittedBy,
    DateTimeOffset SubmittedOn,
    Guid? ReviewerId,
    string? ReviewNotes,
    Guid? ApproverId,
    string? RejectionReason,
    DateTimeOffset? AppliedAt);
