using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// How a shift's daily hours are determined. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// The two kinds are mutually exclusive in their fields, enforced at construction
/// rather than left as nullable properties a consumer must interpret: a Fixed shift
/// carries a window and no flexible fields; a Flexible shift carries all four
/// flexible fields and no window. A half-populated timing reaching <c>attendance</c>
/// produces silently wrong payable hours, which is the severity class this module's
/// value objects exist to prevent.
///
/// <see cref="FixedWindow"/> and <see cref="CoreHours"/> are init-assigned with
/// internal setters rather than constructor parameters, because both are owned
/// navigations once mapped and EF Core rejects a constructor parameter bound to an
/// owned navigation. That issue has now recurred across six aggregates in this
/// codebase; the object-initializer shape is the established fix.
/// </summary>
public sealed class ShiftTiming : ValueObject
{
    public ShiftTimingKind Kind { get; }

    public TimeWindow? FixedWindow { get; internal set; }

    public TimeOnly? EarliestStart { get; }

    public TimeOnly? LatestStart { get; }

    public TimeWindow? CoreHours { get; internal set; }

    public TimeSpan? RequiredHours { get; }

    private ShiftTiming(ShiftTimingKind kind, TimeOnly? earliestStart, TimeOnly? latestStart, TimeSpan? requiredHours)
    {
        Kind = kind;
        EarliestStart = earliestStart;
        LatestStart = latestStart;
        RequiredHours = requiredHours;
    }

    public static Result<ShiftTiming> Fixed(TimeWindow? window) =>
        window is null
            ? Result.Failure<ShiftTiming>(TimekeepingErrors.FixedTimingRequiresWindow)
            : Result.Success(new ShiftTiming(ShiftTimingKind.Fixed, null, null, null) { FixedWindow = window });

    public static Result<ShiftTiming> Flexible(
        TimeOnly? earliestStart, TimeOnly? latestStart, TimeWindow? coreHours, TimeSpan? requiredHours)
    {
        if (earliestStart is null || latestStart is null || coreHours is null || requiredHours is null)
        {
            return Result.Failure<ShiftTiming>(TimekeepingErrors.FlexibleTimingRequiresAllFlexibleFields);
        }

        if (requiredHours.Value <= TimeSpan.Zero)
        {
            return Result.Failure<ShiftTiming>(TimekeepingErrors.RequiredHoursMustBePositive);
        }

        if (earliestStart.Value >= latestStart.Value)
        {
            return Result.Failure<ShiftTiming>(TimekeepingErrors.FlexibleTimingEarliestNotBeforeLatest);
        }

        // Core hours must be reachable by someone starting at the earliest permitted
        // time and by someone starting at the latest: the window they are guaranteed
        // to overlap runs from the earliest start to the latest start plus the
        // required hours. Core hours outside it cannot be met by every valid start.
        var latestPossibleEnd = latestStart.Value.Add(requiredHours.Value);
        if (coreHours.Start < earliestStart.Value || coreHours.End > latestPossibleEnd)
        {
            return Result.Failure<ShiftTiming>(TimekeepingErrors.FlexibleTimingCoreHoursOutsideRange);
        }

        var timing = new ShiftTiming(ShiftTimingKind.Flexible, earliestStart, latestStart, requiredHours)
        {
            CoreHours = coreHours,
        };

        return Result.Success(timing);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return FixedWindow;
        yield return EarliestStart;
        yield return LatestStart;
        yield return CoreHours;
        yield return RequiredHours;
    }
}
