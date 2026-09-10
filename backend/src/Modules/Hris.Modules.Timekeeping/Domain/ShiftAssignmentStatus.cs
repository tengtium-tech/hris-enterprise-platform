namespace Hris.Modules.Timekeeping.Domain;

/// <summary>Source: docs/04-modules/timekeeping/domain/value-objects.md.</summary>
public enum ShiftAssignmentStatus
{
    Scheduled = 0,
    Active = 1,

    /// <summary>Replaced through a shift swap; linked to its counterpart (TK-033).</summary>
    Swapped = 2,

    /// <summary>Reached its end date automatically, with no administrative action (TK-032).</summary>
    Expired = 3,

    Cancelled = 4,
}
