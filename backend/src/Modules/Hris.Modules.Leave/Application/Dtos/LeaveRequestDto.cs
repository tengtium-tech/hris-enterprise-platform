namespace Hris.Modules.Leave.Application.Dtos;

/// <summary>
/// Read shape for <c>LeaveRequest</c> (dto-design.md). Never carries VAWC/Special Leave
/// Benefit supporting-document content inline (LV-083, LV-084) — that requires the
/// separate, narrowly-scoped, distinctly-audited elevated-documentation query.
/// </summary>
public sealed record LeaveRequestDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool HalfDayAtStart,
    bool HalfDayAtEnd,
    decimal RequestedDays,
    string Status,
    string? PayTreatment,
    decimal? PaidDays,
    Guid SubmittedBy,
    DateTimeOffset SubmittedOn,
    Guid? ApproverId,
    string? RejectionReason,
    string? CancellationReason);
