using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>
/// The command-boundary shape of a break, converted to the domain's own
/// <see cref="BreakRule"/> by <see cref="ToBreakRule"/>. Flat and primitive-typed,
/// because a command is a request object crossing the application boundary and
/// accepting a constructed domain value there would let a caller bypass the
/// validation the domain type performs.
/// </summary>
public sealed record BreakRuleInput(
    BreakKind Kind, TimeSpan Duration, TimeOnly? WindowStart, TimeOnly? WindowEnd, bool Paid, bool Mandatory)
{
    public BreakRule ToBreakRule() => new(
        Kind,
        Duration,
        WindowStart is not null && WindowEnd is not null ? new TimeWindowSnapshot(WindowStart.Value, WindowEnd.Value) : null,
        Paid,
        Mandatory);
}

/// <summary>The command-boundary shape of one split-shift period.</summary>
public sealed record ShiftPeriodInput(int Sequence, TimeOnly Start, TimeOnly End)
{
    public ShiftPeriod ToShiftPeriod() => new(Sequence, Start, End);
}

/// <summary>
/// The command-boundary shape of a shift's timing. Converts to the domain's own
/// <see cref="ShiftTiming"/>, surfacing its validation failures unchanged rather
/// than pre-checking them, so the domain type stays the single place a timing's
/// shape is decided.
/// </summary>
public sealed record ShiftTimingInput(
    ShiftTimingKind Kind,
    TimeOnly? FixedStart,
    TimeOnly? FixedEnd,
    TimeOnly? EarliestStart,
    TimeOnly? LatestStart,
    TimeOnly? CoreStart,
    TimeOnly? CoreEnd,
    TimeSpan? RequiredHours)
{
    public Result<ShiftTiming> ToTiming()
    {
        if (Kind == ShiftTimingKind.Fixed)
        {
            if (FixedStart is null || FixedEnd is null)
            {
                return Result.Failure<ShiftTiming>(TimekeepingErrors.FixedTimingRequiresWindow);
            }

            var windowResult = TimeWindow.Create(FixedStart.Value, FixedEnd.Value);
            return windowResult.IsFailure
                ? Result.Failure<ShiftTiming>(windowResult.Error)
                : ShiftTiming.Fixed(windowResult.Value);
        }

        if (CoreStart is null || CoreEnd is null)
        {
            return Result.Failure<ShiftTiming>(TimekeepingErrors.FlexibleTimingRequiresAllFlexibleFields);
        }

        var coreResult = TimeWindow.Create(CoreStart.Value, CoreEnd.Value);
        return coreResult.IsFailure
            ? Result.Failure<ShiftTiming>(coreResult.Error)
            : ShiftTiming.Flexible(EarliestStart, LatestStart, coreResult.Value, RequiredHours);
    }
}
