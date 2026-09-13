namespace Hris.Modules.Leave.Domain;

/// <summary>
/// The start and end date of a <see cref="LeaveRequest"/>, including half-day flags at
/// either end. <see cref="RequestedDays"/> is a caller-supplied, already-resolved value —
/// computed against <c>../../timekeeping</c>'s work schedule and holiday calendar (LV-032),
/// never by this module querying that one directly (CTR-ARC-002/003), the identical
/// pre-resolved-primitive shape Attendance's own <c>RunCalculationCommand</c> uses for the
/// same reason. Immutable once a request is submitted (LV-035): a change in dates is a new
/// request, not an edit of the original. Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public sealed record LeaveDateRange(DateOnly StartDate, DateOnly EndDate, bool HalfDayAtStart, bool HalfDayAtEnd, decimal RequestedDays)
{
    /// <summary>True where this range shares any day with <paramref name="other"/> (LV-031).</summary>
    public bool OverlapsWith(LeaveDateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartDate <= other.EndDate && other.StartDate <= EndDate;
    }
}
