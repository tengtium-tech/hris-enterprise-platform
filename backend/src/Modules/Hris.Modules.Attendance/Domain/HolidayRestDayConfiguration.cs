namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// The holiday and rest-day premium ruleset, part of <see cref="AttendancePolicy"/>.
/// Source: docs/04-modules/attendance/domain/aggregates.md (AttendancePolicy owns
/// "holiday and rest-day policy configuration").
/// </summary>
public readonly record struct HolidayRestDayConfiguration(
    bool HolidayPremiumApplies,
    bool RestDayPremiumApplies);
