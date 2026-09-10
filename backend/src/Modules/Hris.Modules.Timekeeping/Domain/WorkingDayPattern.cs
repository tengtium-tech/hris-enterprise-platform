using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Which days of the week a schedule expects work. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// Working days and rest days partition the week completely: no day is both, and no
/// day is neither. That is stronger than "at least one working day" and is the
/// property that makes a downstream consumer's question ("is this weekday expected
/// to be worked?") total rather than three-valued.
///
/// A four-day week and a rotating rest day are both expressed through this one
/// shape, with rotation handled at the <see cref="ShiftAssignment"/> level rather
/// than by inventing a second pattern type.
/// </summary>
public sealed class WorkingDayPattern : ValueObject
{
    public IReadOnlyList<DayOfWeek> WorkingDays { get; }

    /// <summary>
    /// Computed from <see cref="WorkingDays"/> rather than stored, which is what makes
    /// the partition invariant unbreakable rather than merely checked at construction:
    /// there is no second field that could disagree with the first, in memory or in
    /// the database. It is also why persistence stores only the working set — a row
    /// carrying both could contradict itself, and this type would have no way to tell.
    /// </summary>
    public IReadOnlyList<DayOfWeek> RestDays =>
        Enum.GetValues<DayOfWeek>().Except(WorkingDays).OrderBy(day => (int)day).ToList();

    private WorkingDayPattern(IReadOnlyList<DayOfWeek> workingDays)
    {
        WorkingDays = workingDays;
    }

    /// <summary>
    /// Only the working set is supplied; rest days follow from it. A caller cannot
    /// pass a pair that fails the partition rule, because there is no pair to pass.
    /// </summary>
    public static Result<WorkingDayPattern> Create(IReadOnlyList<DayOfWeek>? workingDays)
    {
        if (workingDays is null || workingDays.Count == 0)
        {
            return Result.Failure<WorkingDayPattern>(TimekeepingErrors.WorkingDayPatternRequiresAWorkingDay);
        }

        var working = workingDays.Distinct().OrderBy(day => (int)day).ToList();

        return working.Count > 7
            ? Result.Failure<WorkingDayPattern>(TimekeepingErrors.WorkingDayPatternMustPartitionTheWeek)
            : Result.Success(new WorkingDayPattern(working));
    }

    public bool IsWorkingDay(DayOfWeek day) => WorkingDays.Contains(day);

    public bool IsWorkingDay(DateOnly date) => IsWorkingDay(date.DayOfWeek);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var day in WorkingDays)
        {
            yield return day;
        }
    }
}
