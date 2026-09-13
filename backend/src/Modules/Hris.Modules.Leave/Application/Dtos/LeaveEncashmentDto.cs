namespace Hris.Modules.Leave.Application.Dtos;

/// <summary>Full detail of one encashment request (dto-design.md).</summary>
public sealed record LeaveEncashmentDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveBalanceId,
    decimal RequestedAmount,
    string Status,
    Guid SubmittedBy,
    DateTimeOffset SubmittedOn,
    Guid? ApproverId,
    string? RejectionReason,
    string? CancellationReason);
