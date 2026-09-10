using Hris.Modules.Timekeeping.Application.Dtos;
using Hris.Modules.Timekeeping.Domain;

namespace Hris.Modules.Timekeeping.Application.Mapping;

/// <summary>
/// Explicit hand-written projection from aggregate to DTO, per mapping.md's
/// convention across every prior module: no reflection-based mapper, so what leaves
/// the module is visible in source and a newly added domain property never escapes
/// into a response by default.
/// </summary>
internal static class TimekeepingMapper
{
    public static WorkScheduleDto ToDto(WorkSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return new WorkScheduleDto(
            schedule.Id.Value,
            schedule.TenantId,
            schedule.LineageId,
            schedule.Name,
            schedule.Description,
            schedule.WorkingDayPattern.WorkingDays.Select(day => day.ToString()).ToList(),
            schedule.WorkingDayPattern.RestDays.Select(day => day.ToString()).ToList(),
            schedule.StandardHours?.Start,
            schedule.StandardHours?.End,
            schedule.BreakPeriods.Select(ToDto).ToList(),
            schedule.Version,
            schedule.EffectiveFrom,
            schedule.EffectiveTo,
            schedule.Status.ToString(),
            schedule.ScheduleAssignments.Select(ToDto).ToList());
    }

    public static ScheduleAssignmentDto ToDto(ScheduleAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new ScheduleAssignmentDto(
            assignment.Id.Value,
            assignment.TargetLevel.ToString(),
            assignment.TargetId,
            assignment.EffectiveFrom,
            assignment.EffectiveTo,
            assignment.AssignedBy,
            assignment.AssignedOn);
    }

    public static BreakRuleDto ToDto(BreakRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new BreakRuleDto(
            rule.Kind.ToString(), rule.Duration, rule.Window?.Start, rule.Window?.End, rule.Paid, rule.Mandatory);
    }

    public static WorkShiftDto ToDto(WorkShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);

        return new WorkShiftDto(
            shift.Id.Value,
            shift.TenantId,
            shift.LineageId,
            shift.Code.Value,
            shift.Name,
            ToDto(shift.Timing),
            shift.IsOvernight,
            shift.AnchorRule is null
                ? null
                : new WorkDateAnchorRuleDto(shift.AnchorRule.AnchorPoint.ToString(), shift.AnchorRule.Description),
            shift.SplitPeriods.Select(period => new ShiftPeriodDto(period.Sequence, period.Start, period.End)).ToList(),
            shift.BreakRules.Select(ToDto).ToList(),
            shift.OvertimeEligible,
            new PremiumEligibilityDto(
                shift.PremiumEligibility.NightDifferentialEligible,
                shift.PremiumEligibility.HazardEligible,
                shift.PremiumEligibility.HolidayPremiumEligible),
            shift.Version,
            shift.EffectiveFrom,
            shift.EffectiveTo,
            shift.Status.ToString());
    }

    public static ShiftTimingDto ToDto(ShiftTiming timing)
    {
        ArgumentNullException.ThrowIfNull(timing);

        return new ShiftTimingDto(
            timing.Kind.ToString(),
            timing.FixedWindow?.Start,
            timing.FixedWindow?.End,
            timing.EarliestStart,
            timing.LatestStart,
            timing.CoreHours?.Start,
            timing.CoreHours?.End,
            timing.RequiredHours);
    }

    public static ShiftAssignmentDto ToDto(ShiftAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new ShiftAssignmentDto(
            assignment.Id.Value,
            assignment.TenantId,
            assignment.TargetType.ToString(),
            assignment.TargetId,
            assignment.TargetLevel.ToString(),
            assignment.WorkShiftId.Value,
            assignment.EffectiveFrom,
            assignment.EffectiveTo,
            assignment.Status.ToString(),
            assignment.RotationCycleReference?.Value,
            assignment.SwapLinkedAssignmentId?.Value,
            assignment.PendingSwap is null ? null : ToDto(assignment.PendingSwap),
            assignment.AssignedBy,
            assignment.AssignedOn);
    }

    public static PendingShiftSwapDto ToDto(PendingShiftSwap swap)
    {
        ArgumentNullException.ThrowIfNull(swap);

        return new PendingShiftSwapDto(
            swap.CounterpartAssignmentId.Value,
            swap.ProposedEffectiveFrom,
            swap.InitiatedBy,
            swap.PrimaryEmployeeId,
            swap.CounterpartEmployeeId,
            swap.PrimaryConsented,
            swap.CounterpartConsented,
            swap.OverriddenBy,
            swap.OverrideReason,
            swap.IsReadyToActivate);
    }

    public static HolidayCalendarDto ToDto(HolidayCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);

        return new HolidayCalendarDto(
            calendar.Id.Value,
            calendar.TenantId,
            calendar.LineageId,
            calendar.Name,
            calendar.Level.ToString(),
            calendar.ScopeTargetId,
            calendar.CountryCode,
            calendar.ParentCalendarId?.Value,
            calendar.Holidays.OrderBy(holiday => holiday.Date).Select(ToDto).ToList(),
            calendar.Version,
            calendar.EffectiveFrom,
            calendar.EffectiveTo,
            calendar.Status.ToString());
    }

    public static HolidayDto ToDto(Holiday holiday)
    {
        ArgumentNullException.ThrowIfNull(holiday);

        return new HolidayDto(
            holiday.Id.Value, holiday.Date, holiday.Name, holiday.Type.ToString(), holiday.WorkRule.ToString());
    }

    public static ShiftResolutionDto ToDto(ShiftResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        return new ShiftResolutionDto(
            resolution.Assignment is null ? null : ToDto(resolution.Assignment),
            resolution.Assignment?.WorkShiftId.Value,
            resolution.IsUnresolved,
            resolution.IsAmbiguous);
    }

    public static HolidayResolutionDto ToDto(HolidayResolution? resolution) =>
        resolution is null
            ? new HolidayResolutionDto(null, null, null, false)
            : new HolidayResolutionDto(
                ToDto(resolution.Holiday), resolution.SourceCalendarId.Value, resolution.SourceLevel.ToString(), true);
}
