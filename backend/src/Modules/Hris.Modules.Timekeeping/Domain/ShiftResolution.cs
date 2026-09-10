namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// The outcome of resolving which shift an employee owes on a given work date.
/// Source: docs/04-modules/timekeeping/domain/business-rules.md TK-030.
///
/// Three outcomes, deliberately distinguishable rather than collapsed into a
/// nullable result. "No assignment applies" and "two assignments contradict each
/// other" are different facts with different remedies, and an implementation
/// returning null for both leaves a caller unable to tell a gap in configuration
/// from a conflict in it.
/// </summary>
/// <param name="Assignment">The single governing assignment, where one was found.</param>
/// <param name="IsUnresolved">True where no assignment applies to this employee on this date.</param>
/// <param name="IsAmbiguous">
/// True where two or more assignments at the same precedence level apply. Resolution
/// refuses to pick, because picking would make the answer depend on data-access order
/// rather than on a business rule.
/// </param>
public sealed record ShiftResolution(ShiftAssignment? Assignment, bool IsUnresolved, bool IsAmbiguous)
{
    public static ShiftResolution Resolved(ShiftAssignment assignment) => new(assignment, false, false);

    public static ShiftResolution Unresolved { get; } = new(null, true, false);

    public static ShiftResolution Ambiguous { get; } = new(null, false, true);
}
