namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/shared-kernel.md's own
/// "What Belongs in the Shared Kernel" list (Value Objects: "DateRange..."). Built
/// here, in SharedKernel, on first genuine need from the Administration module's own
/// <c>AdministrativeDelegation.Period</c> ("REQUIRED -- both ends", per
/// docs/04-modules/administration/domain/delegated-administration.md) -- not spun up
/// speculatively, the identical "built on first genuine need" precedent
/// <see cref="EmailAddress"/>'s own remarks already state for itself.
///
/// Deliberately a closed date interval with both ends required: a caller needing an
/// open-ended period (no end date) does not construct a <see cref="DateRange"/> with
/// a null <see cref="End"/> -- it models the absence explicitly as a nullable
/// <see cref="DateRange"/> reference, or as two separate nullable dates, at the call
/// site. This keeps the type's own invariant ("End is never before Start") checkable
/// unconditionally rather than only when both ends happen to be present.
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateOnly Start { get; }

    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        return end < start
            ? Result.Failure<DateRange>(SharedKernelErrors.DateRangeEndBeforeStart)
            : Result.Success(new DateRange(start, end));
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
