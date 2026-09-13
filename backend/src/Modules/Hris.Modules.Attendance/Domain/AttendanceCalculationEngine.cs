using System.Globalization;
using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// Stateless domain service implementing BUS-070's fifteen-step deterministic pipeline,
/// invoked within <see cref="AttendanceRecord"/>'s own transaction by the
/// <c>RunCalculationCommand</c> handler. Source:
/// docs/04-modules/attendance/domain/attendance-records.md.
///
/// It resolves nothing itself. The resolved shift, holiday, leave, effective policy, and
/// approved overtime are handed in as a <see cref="CalculationInputs"/> value object, so
/// the same event set and the same inputs always reproduce the same
/// <see cref="CalculationResult"/> (AT-003) — re-running never drifts an unrelated value.
/// It must not be a repository-backed aggregate; it reads only the inputs it is given.
///
/// Shift start/end arrive as local <see cref="TimeOnly"/> values; the pipeline compares
/// them to each event's wall-clock time, which is the business intent (a 09:00 shift is
/// 09:00 wherever the employee's site is). Time-zone conversion of the captured instants
/// is the caller's responsibility before the events reach the engine.
/// </summary>
public static class AttendanceCalculationEngine
{
    public static CalculationResult Calculate(
        IReadOnlyList<TimeEvent> timeEvents, DateOnly workDate, CalculationInputs inputs, DateTimeOffset calculatedAtUtc)
    {
        Guard.AgainstNull(inputs, nameof(inputs));

        // Steps 1-4 (validate employee/employment, resolve schedule/shift, resolve holiday,
        // resolve leave) are performed by the caller; their results are the inputs below.
        // Generating an identifier or reading the clock here would break AT-003's
        // reproducibility, so calculatedAtUtc is accepted only to be carried onto the
        // resulting event by the caller, never used in the arithmetic.
        _ = calculatedAtUtc;

        var exceptions = new List<string>();
        var policy = inputs.Policy;

        // Step 5: the effective policy version is already selected by the caller (AT-002).
        // Step 6: validate the time event set.
        var ordered = timeEvents.OrderBy(e => e.TimestampUtc).ToList();

        if (ordered.Count == 0)
        {
            exceptions.Add("No time events captured; working time cannot be derived.");
        }

        if (inputs.ShiftUnresolved)
        {
            exceptions.Add("Shift could not be resolved for the employee on the work date.");
        }

        var firstIn = ordered.FirstOrDefault(e => e.EventType == TimeEventType.ClockIn)?.TimestampUtc;
        var lastOut = ordered.LastOrDefault(e => e.EventType == TimeEventType.ClockOut)?.TimestampUtc;

        var workingHours = 0d;
        var unpaidBreaks = 0d;
        var lateMinutes = 0d;
        var undertimeMinutes = 0d;
        var overtimeHours = 0d;
        var holidayHours = 0d;
        var nightDifferentialHours = 0d;

        if (firstIn.HasValue && lastOut.HasValue && lastOut.Value > firstIn.Value)
        {
            workingHours = (lastOut.Value - firstIn.Value).TotalHours;

            // Step 7: evaluate break rules by pairing Break-Start with Break-End.
            unpaidBreaks = EvaluateBreaks(ordered, policy.BreakPolicy, exceptions);

            // Step 8: working hours already derived above.
            // Step 9: apply the uniform rounding rule.
            workingHours = ApplyRounding(workingHours, policy.RoundingRule);

            // Step 10: tardiness against the resolved shift start and grace period.
            if (inputs.ShiftStart.HasValue)
            {
                var graceStart = inputs.ShiftStart.Value.ToTimeSpan()
                    .Add(TimeSpan.FromMinutes(policy.GracePeriod.Minutes));
                var actualStart = firstIn.Value.TimeOfDay;
                if (actualStart > graceStart)
                {
                    lateMinutes = (actualStart - graceStart).TotalMinutes;
                }
            }

            // Step 11: undertime against the resolved shift end.
            if (inputs.ShiftEnd.HasValue)
            {
                var actualEnd = lastOut.Value.TimeOfDay;
                if (actualEnd < inputs.ShiftEnd.Value.ToTimeSpan())
                {
                    undertimeMinutes = (inputs.ShiftEnd.Value.ToTimeSpan() - actualEnd).TotalMinutes;
                }
            }

            // Steps 12 & 13: overtime and absence detection.
            if (inputs.ShiftEnd.HasValue && lastOut.Value.TimeOfDay > inputs.ShiftEnd.Value.ToTimeSpan())
            {
                overtimeHours = (lastOut.Value.TimeOfDay - inputs.ShiftEnd.Value.ToTimeSpan()).TotalHours;
                overtimeHours = ApplyRounding(overtimeHours, policy.RoundingRule);

                if (!policy.OvertimePolicy.Eligible)
                {
                    exceptions.Add("Overtime recorded but the effective policy is not overtime-eligible.");
                    overtimeHours = 0;
                }
                else if (policy.OvertimePolicy.RequiresPriorAuthorization
                    && !inputs.ApprovedOvertime.Any(o => o.WorkDate == workDate))
                {
                    exceptions.Add("Overtime exceeds approved authorization; flagged per AT-042.");
                }
            }

            // Step 13 (continued): absence is derived, never recorded (AT-060).
            if (inputs.ShiftStart.HasValue && !firstIn.HasValue)
            {
                exceptions.Add("No clock-in on a scheduled work date; absence derived.");
            }

            // Holiday hours attribution (AT-060 family): a holiday's worked hours are
            // reported separately so payroll can price the premium.
            if (inputs.IsHoliday)
            {
                holidayHours = workingHours;
                if (!policy.HolidayRestDay.HolidayPremiumApplies)
                {
                    exceptions.Add("Worked on a holiday but the policy does not apply a holiday premium.");
                }
            }

            // Night differential: hours the work span overlaps 22:00-06:00.
            nightDifferentialHours = NightOverlapHours(firstIn.Value, lastOut.Value);
        }

        // Steps 14 (exceptions) and 15 (produce result).
        var calculated = new CalculatedFields(
            WorkingHours: Round(workingHours),
            PayableHours: Round(workingHours - unpaidBreaks - (undertimeMinutes / 60d) + ApprovedOvertimeHours(inputs, workDate)),
            OvertimeHours: Round(overtimeHours),
            LateMinutes: Round(lateMinutes),
            UndertimeMinutes: Round(undertimeMinutes),
            HolidayHours: Round(holidayHours),
            NightDifferentialHours: Round(nightDifferentialHours));

        return new CalculationResult(calculated, exceptions, "RunCalculationCommand");
    }

    private static double EvaluateBreaks(
        IReadOnlyList<TimeEvent> ordered, BreakPolicyConfiguration breakPolicy, List<string> exceptions)
    {
        double unpaid = 0;
        var openStarts = new Stack<TimeEvent>();

        foreach (var ev in ordered)
        {
            if (ev.EventType == TimeEventType.BreakStart)
            {
                openStarts.Push(ev);
            }
            else if (ev.EventType == TimeEventType.BreakEnd)
            {
                if (!openStarts.TryPop(out var start))
                {
                    exceptions.Add("Break end without a matching break start.");
                    continue;
                }

                var duration = (ev.TimestampUtc - start.TimestampUtc).TotalMinutes;
                if (duration < breakPolicy.MinimumDurationMinutes || duration > breakPolicy.MaximumDurationMinutes)
                {
                    exceptions.Add("Break duration outside the configured min/max; treated as invalid.");
                }

                if (!breakPolicy.BreaksPaid)
                {
                    unpaid += duration / 60d;
                }
            }
        }

        if (openStarts.Count > 0)
        {
            exceptions.Add("Break start without a matching break end.");
        }

        return unpaid;
    }

    private static double ApplyRounding(double value, RoundingRule rule)
    {
        if (rule == RoundingRule.Exact || value == 0)
        {
            return value;
        }

        var minutes = rule switch
        {
            RoundingRule.NearestMinute => 1,
            RoundingRule.NearestFiveMinutes => 5,
            RoundingRule.NearestFifteenMinutes => 15,
            RoundingRule.NearestThirtyMinutes => 30,
            _ => 1,
        };

        var totalMinutes = value * 60d;
        var rounded = Math.Round(totalMinutes / minutes, MidpointRounding.AwayFromZero) * minutes;
        return rounded / 60d;
    }

    private static double ApprovedOvertimeHours(CalculationInputs inputs, DateOnly workDate) =>
        inputs.ApprovedOvertime.Where(o => o.WorkDate == workDate).Sum(o => o.EstimatedHours);

    private static double NightOverlapHours(DateTimeOffset start, DateTimeOffset end)
    {
        var nightStart = TimeSpan.FromHours(22);
        var nightEnd = TimeSpan.FromHours(6);

        double overlap = 0;
        var cursor = start;
        while (cursor < end)
        {
            var next = cursor.AddHours(1);
            var windowStart = cursor.TimeOfDay;
            var windowEnd = next.TimeOfDay;

            var inNight = windowStart >= nightStart
                || windowStart < nightEnd
                || (windowEnd > nightStart || windowEnd <= nightEnd);
            if (inNight)
            {
                overlap += 1;
            }

            cursor = next;
        }

        return overlap;
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
