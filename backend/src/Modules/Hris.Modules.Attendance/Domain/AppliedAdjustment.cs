namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// A read-only projection of an <see cref="AttendanceAdjustment"/> outcome already
/// applied to an <see cref="AttendanceRecord"/>, retained on the record so the record
/// carries its own adjustment history without depending on the
/// <see cref="AttendanceAdjustment"/> aggregate (which owns the authoritative request).
/// Source: docs/04-modules/attendance/domain/aggregates.md (AttendanceRecord owns an
/// "applied-adjustment history").
/// </summary>
public sealed record AppliedAdjustment(
    AttendanceAdjustmentId AdjustmentId,
    string Field,
    string RequestedValue,
    DateTimeOffset AppliedOnUtc);
