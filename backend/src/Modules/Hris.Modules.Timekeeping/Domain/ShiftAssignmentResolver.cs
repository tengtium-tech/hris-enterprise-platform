namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// TK-030, the module's core algorithm: resolving the single <see cref="ShiftAssignment"/>
/// that governs an employee on a work date. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md's precedence list and
/// business-rules.md TK-030.
///
/// A pure function over a caller-supplied candidate set. Producing that set means
/// knowing the employee's position, department, business unit, legal entity, and
/// company — data owned by <c>organization</c> and <c>employment</c>, which this
/// module references by identifier and never joins to. What Timekeeping owns, and
/// what is genuinely testable here, is what must happen to the candidates once they
/// exist. That division is the same one every prior module used for a cross-module
/// fact.
///
/// The rule this exists to prevent is subtle: an implementation that took "the first
/// matching assignment found" when two candidates tied would produce a resolution
/// that depends on data-access order rather than on a business rule, and would
/// return different answers for the same question depending on how the rows came
/// back. That is why a same-level tie is an explicit ambiguous outcome rather than a
/// silent pick.
/// </summary>
public static class ShiftAssignmentResolver
{
    /// <summary>
    /// Applies the precedence order — most specific level wins — to the candidates
    /// that actually apply on <paramref name="workDate"/>.
    /// </summary>
    /// <param name="candidates">
    /// Every assignment that could bind this employee, at any level. The caller
    /// supplies the individual-level assignments plus the unit-level ones covering
    /// the employee's placement; this function does not discover them.
    /// </param>
    /// <param name="workDate">The date being resolved.</param>
    public static ShiftResolution Resolve(IReadOnlyList<ShiftAssignment> candidates, DateOnly workDate)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var applicable = candidates.Where(assignment => assignment.AppliesOn(workDate)).ToList();

        if (applicable.Count == 0)
        {
            return ShiftResolution.Unresolved;
        }

        // OrganizationalAssignmentLevel runs broadest to most specific, so the
        // highest ordinal is the winner. The comparison is the precedence rule; there
        // is no lookup table to drift from it.
        var mostSpecificLevel = applicable.Max(assignment => assignment.TargetLevel);
        var winners = applicable.Where(assignment => assignment.TargetLevel == mostSpecificLevel).ToList();

        return winners.Count > 1 ? ShiftResolution.Ambiguous : ShiftResolution.Resolved(winners[0]);
    }
}
