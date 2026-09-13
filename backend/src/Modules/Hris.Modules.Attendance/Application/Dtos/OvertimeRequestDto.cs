namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>
/// Read shape for <c>OvertimeRequest</c>, carrying its category, planned window, and
/// approval chain (dto-design.md).
/// </summary>
public sealed record OvertimeRequestDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    TimeOnly? PlannedStart,
    TimeOnly? PlannedEnd,
    double EstimatedHours,
    string Category,
    string Justification,
    string Status,
    Guid? ApproverId,
    DateTimeOffset? DecidedOn,
    string? RejectionReason);
