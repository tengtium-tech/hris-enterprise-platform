using Hris.Modules.Timekeeping.Domain;

namespace Hris.Modules.Timekeeping.Tests;

/// <summary>
/// Shared fixtures, each building the minimum valid shape a test needs so a failure
/// points at the rule under test rather than at incidental setup.
/// </summary>
internal static class TestTimekeeping
{
    public static readonly DateTimeOffset NowUtc = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(NowUtc.UtcDateTime);

    public static readonly DayOfWeek[] MondayToFriday =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
    ];

    public static TimeWindow Window(int startHour, int endHour) =>
        TimeWindow.Create(new TimeOnly(startHour, 0), new TimeOnly(endHour, 0)).Value;

    public static ShiftTiming FixedTiming(int startHour = 9, int endHour = 18) =>
        ShiftTiming.Fixed(Window(startHour, endHour)).Value;

    public static WorkSchedule Schedule(Guid tenantId, DateOnly? effectiveFrom = null) =>
        WorkSchedule.Create(
            new WorkScheduleId(Guid.NewGuid()), tenantId, "Standard Office", "Mon-Fri", MondayToFriday,
            Window(9, 18), null, effectiveFrom ?? Today, Guid.NewGuid(), NowUtc).Value;

    public static WorkSchedule ActiveSchedule(Guid tenantId, DateOnly? effectiveFrom = null)
    {
        var schedule = Schedule(tenantId, effectiveFrom);
        schedule.Publish(Guid.NewGuid(), NowUtc);
        return schedule;
    }

    public static WorkShift Shift(
        Guid tenantId, string code = "DAY", bool isOvernight = false, bool overtimeEligible = true,
        DateOnly? effectiveFrom = null) =>
        WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), tenantId, code, "Day Shift", FixedTiming(), isOvernight,
            isOvernight ? WorkDateAnchorRule.AnchoredToShiftStart : null, null, null, overtimeEligible,
            PremiumEligibilityFlags.None, effectiveFrom ?? Today, false, Guid.NewGuid(), NowUtc).Value;

    public static WorkShift ActiveShift(Guid tenantId, string code = "DAY", DateOnly? effectiveFrom = null)
    {
        var shift = Shift(tenantId, code, effectiveFrom: effectiveFrom);
        shift.Publish(Guid.NewGuid(), NowUtc);
        return shift;
    }

    public static ShiftAssignment Assignment(
        Guid tenantId,
        string targetId,
        OrganizationalAssignmentLevel level = OrganizationalAssignmentLevel.IndividualEmployee,
        DateOnly? from = null,
        DateOnly? to = null,
        WorkShiftId? shiftId = null,
        bool overlaps = false) =>
        ShiftAssignment.Create(
            new ShiftAssignmentId(Guid.NewGuid()), tenantId,
            level == OrganizationalAssignmentLevel.IndividualEmployee
                ? AssignmentTargetType.Employee
                : AssignmentTargetType.OrganizationalUnit,
            targetId, level, shiftId ?? new WorkShiftId(Guid.NewGuid()), from ?? Today, to, false, null, overlaps,
            Guid.NewGuid(), NowUtc).Value;

    public static HolidayCalendar CountryCalendar(DateOnly? effectiveFrom = null) =>
        HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), null, "Philippines National", HolidayCalendarLevel.Country, "PH",
            "PH", null, effectiveFrom ?? Today, Guid.NewGuid(), NowUtc).Value;

    public static HolidayCalendar CompanyCalendar(Guid tenantId, HolidayCalendarId parentId, DateOnly? effectiveFrom = null) =>
        HolidayCalendar.Create(
            new HolidayCalendarId(Guid.NewGuid()), tenantId, "Acme Company", HolidayCalendarLevel.Company, "acme", "PH",
            parentId, effectiveFrom ?? Today, Guid.NewGuid(), NowUtc).Value;

    /// <summary>A published country calendar carrying one statutory regular holiday.</summary>
    public static HolidayCalendar PublishedCountryCalendar(DateOnly holidayDate, DateOnly? effectiveFrom = null)
    {
        var calendar = CountryCalendar(effectiveFrom);
        calendar.AddHoliday(
            new HolidayId(Guid.NewGuid()), holidayDate, "Independence Day", HolidayType.RegularHoliday,
            HolidayWorkRule.NoWorkExpected, true, Guid.NewGuid(), NowUtc);
        calendar.Publish(Guid.NewGuid(), NowUtc);
        return calendar;
    }
}
