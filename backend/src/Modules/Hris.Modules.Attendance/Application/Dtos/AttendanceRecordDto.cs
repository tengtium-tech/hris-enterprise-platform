namespace Hris.Modules.Attendance.Application.Dtos;

/// <summary>Read shape for <c>AttendanceRecord</c>, carrying its calculated fields and immutable event set.</summary>
public sealed record AttendanceRecordDto(
    Guid Id,
    Guid TenantId,
    Guid EmployeeId,
    DateOnly WorkDate,
    string Status,
    string ApprovalStatus,
    string PayrollStatus,
    Guid? WorkShiftId,
    Guid? HolidayCalendarId,
    CalculatedFieldsDto? Calculated,
    IReadOnlyList<string> Exceptions,
    int PendingAdjustmentCount,
    IReadOnlyList<TimeEventDto> TimeEvents);

/// <summary>Derived working-time totals produced by the calculation engine.</summary>
public sealed record CalculatedFieldsDto(
    double WorkingHours,
    double PayableHours,
    double OvertimeHours,
    double LateMinutes,
    double UndertimeMinutes,
    double HolidayHours,
    double NightDifferentialHours);

/// <summary>Read shape for an immutable <c>TimeEvent</c>.</summary>
public sealed record TimeEventDto(
    Guid Id,
    string EventType,
    DateTimeOffset TimestampUtc,
    string Source,
    Guid? AttendanceDeviceId,
    string? RawValue,
    string? OriginatingTimeZone);
