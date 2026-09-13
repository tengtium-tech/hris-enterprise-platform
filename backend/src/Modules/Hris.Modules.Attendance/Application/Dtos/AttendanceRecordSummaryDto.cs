namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>
/// Reduced attendance-record shape for roster and list views, deliberately omitting the
/// full captured time-event collection (dto-design.md). Calculated totals are denormalized
/// in so a list view never re-derives them from raw events.
/// </summary>
public sealed record AttendanceRecordSummaryDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    string Status,
    string ApprovalStatus,
    string PayrollStatus,
    Guid? WorkShiftId,
    Guid? HolidayCalendarId,
    double? WorkingHours,
    double? PayableHours,
    double? OvertimeHours,
    int PendingAdjustmentCount);
