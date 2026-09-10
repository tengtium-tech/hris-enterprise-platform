using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// States which single calendar work date an overnight shift's hours belong to.
/// Source: docs/04-modules/timekeeping/domain/value-objects.md and aggregates.md's
/// own "Why Overnight Anchoring Is a WorkShift Invariant" section.
///
/// The platform default is <see cref="WorkDateAnchorPoint.ShiftStart"/> — a 22:00 to
/// 06:00 shift belongs to the date it starts on. Stating that as an explicit value
/// object rather than as fixed platform behaviour is deliberate: it makes the rule
/// visible at the point <c>attendance</c> needs to apply it, and it lets a query
/// answer "what work date do these hours belong to" without re-deriving the rule
/// from raw timing each time.
///
/// The failure this prevents is subtle and expensive. If the rule lived in
/// <c>attendance</c> instead, two independent readers — a nightly attendance run and
/// a later payroll reconciliation — could each derive a different anchor date for the
/// same ambiguous timestamp, producing two internally consistent and mutually
/// contradictory answers about the same worked hours.
/// </summary>
public sealed class WorkDateAnchorRule : ValueObject
{
    public WorkDateAnchorPoint AnchorPoint { get; }

    /// <summary>Human-readable statement of the rule, carried for display and audit.</summary>
    public string Description { get; }

    private WorkDateAnchorRule(WorkDateAnchorPoint anchorPoint, string description)
    {
        AnchorPoint = anchorPoint;
        Description = description;
    }

    public static WorkDateAnchorRule Create(WorkDateAnchorPoint anchorPoint, string? description) =>
        new(anchorPoint, string.IsNullOrWhiteSpace(description) ? DefaultDescription(anchorPoint) : description.Trim());

    /// <summary>The platform default, stated rather than assumed.</summary>
    public static WorkDateAnchorRule AnchoredToShiftStart => Create(WorkDateAnchorPoint.ShiftStart, null);

    /// <summary>
    /// Resolves the work date a shift instance's hours belong to, given the calendar
    /// date the shift started on. This is the whole point of the type: one place
    /// computes it, every consumer reads the same answer.
    /// </summary>
    public DateOnly ResolveWorkDate(DateOnly startDate) =>
        AnchorPoint == WorkDateAnchorPoint.ShiftStart ? startDate : startDate.AddDays(1);

    private static string DefaultDescription(WorkDateAnchorPoint anchorPoint) =>
        anchorPoint == WorkDateAnchorPoint.ShiftStart
            ? "Hours belong to the calendar date the shift starts on."
            : "Hours belong to the calendar date the shift ends on.";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AnchorPoint;
        yield return Description;
    }
}
