using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// A start and end time within a day, used for standard schedule hours and
/// non-flexible shift timing. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// End is after start. A window that genuinely crosses midnight is not expressed by
/// relaxing that rule; it is expressed by the owning <see cref="WorkShift"/>
/// declaring itself overnight and carrying a <see cref="WorkDateAnchorRule"/>, which
/// is what tells a consumer which single work date the hours belong to. Allowing an
/// end-before-start window here instead would push that determination back onto
/// every consumer, which is exactly what the anchor rule exists to prevent.
/// </summary>
public sealed class TimeWindow : ValueObject
{
    public TimeOnly Start { get; }

    public TimeOnly End { get; }

    private TimeWindow(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<TimeWindow> Create(TimeOnly start, TimeOnly end) =>
        end <= start
            ? Result.Failure<TimeWindow>(TimekeepingErrors.TimeWindowEndNotAfterStart)
            : Result.Success(new TimeWindow(start, end));

    public TimeSpan Duration => End - Start;

    public bool Contains(TimeOnly time) => time >= Start && time <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:HH\\:mm}-{End:HH\\:mm}";
}
