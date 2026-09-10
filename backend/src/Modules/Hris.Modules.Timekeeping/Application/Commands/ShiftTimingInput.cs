using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Application.Commands;

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
