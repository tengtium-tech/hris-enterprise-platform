namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// One working period within a split shift. Source:
/// docs/04-modules/timekeeping/domain/value-objects.md.
///
/// A plain record with no identity of its own — entities.md states that split
/// periods and break rules "are value objects held in collections, not entities;
/// they have no independent identity outside the shift version that defines them."
/// That shape is why <see cref="WorkShift.SplitPeriods"/> persists as a single
/// JSON-serialized column rather than an owned collection, the pattern the
/// Administration module established for values without identity.
///
/// The gap between consecutive periods is implicitly the break; no gap description
/// is carried, per that same document.
/// </summary>
/// <param name="Sequence">Order within the shift.</param>
/// <param name="Start">Period start time.</param>
/// <param name="End">Period end time, after <paramref name="Start"/>.</param>
public sealed record ShiftPeriod(int Sequence, TimeOnly Start, TimeOnly End)
{
    public bool OverlapsWith(ShiftPeriod other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && other.Start < End;
    }
}
