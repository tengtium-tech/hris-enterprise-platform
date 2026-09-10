using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// A shift swap that has been proposed but not yet activated. Source:
/// docs/04-modules/timekeeping/application/commands.md's "Why Swap Is Four Commands,
/// Not One".
///
/// That document requires propose, consent, override, and activate to be four
/// distinct actions, because they have different actors, different authorization
/// requirements, and different points at which the swap can still be abandoned.
/// Collapsing them makes it easy to apply "the swap happened" logic to a state where
/// only one party has actually agreed.
///
/// Four steps need somewhere to hold the state between them, and this module owns
/// exactly four Aggregate Roots — aggregates.md is explicit that there are no more.
/// The proposal therefore lives as an owned value on the primary
/// <see cref="ShiftAssignment"/> rather than becoming a fifth root. That placement
/// also matches how the swap actually resolves: TK-033 says a swap creates or
/// updates exactly two assignments linked to each other, so the assignment is
/// already the thing that carries swap identity.
///
/// Consent is tracked per named employee rather than as a count. A count cannot tell
/// "both parties agreed" from "one party agreed twice", which is precisely the state
/// TK-034 exists to prevent taking effect.
/// </summary>
public sealed class PendingShiftSwap : ValueObject
{
    public ShiftAssignmentId CounterpartAssignmentId { get; }

    public DateOnly ProposedEffectiveFrom { get; }

    public Guid InitiatedBy { get; }

    /// <summary>The employee owning the primary assignment.</summary>
    public string PrimaryEmployeeId { get; }

    /// <summary>The employee owning the counterpart assignment.</summary>
    public string CounterpartEmployeeId { get; }

    public bool PrimaryConsented { get; internal set; }

    public bool CounterpartConsented { get; internal set; }

    /// <summary>
    /// Set where an authorized actor substituted for one or both consents. Recorded
    /// distinctly from an ordinary mutual swap, because an auditor's question is not
    /// "did this swap happen" but "on whose agreement".
    /// </summary>
    public Guid? OverriddenBy { get; internal set; }

    public string? OverrideReason { get; internal set; }

    private PendingShiftSwap(
        ShiftAssignmentId counterpartAssignmentId, DateOnly proposedEffectiveFrom, Guid initiatedBy,
        string primaryEmployeeId, string counterpartEmployeeId)
    {
        CounterpartAssignmentId = counterpartAssignmentId;
        ProposedEffectiveFrom = proposedEffectiveFrom;
        InitiatedBy = initiatedBy;
        PrimaryEmployeeId = primaryEmployeeId;
        CounterpartEmployeeId = counterpartEmployeeId;
    }

    public static Result<PendingShiftSwap> Create(
        ShiftAssignmentId counterpartAssignmentId, DateOnly proposedEffectiveFrom, Guid initiatedBy,
        string? primaryEmployeeId, string? counterpartEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(primaryEmployeeId) || string.IsNullOrWhiteSpace(counterpartEmployeeId))
        {
            return Result.Failure<PendingShiftSwap>(TimekeepingErrors.AssignmentTargetRequired);
        }

        return string.Equals(primaryEmployeeId, counterpartEmployeeId, StringComparison.Ordinal)
            ? Result.Failure<PendingShiftSwap>(TimekeepingErrors.SwapRequiresTwoDistinctAssignments)
            : Result.Success(new PendingShiftSwap(
                counterpartAssignmentId, proposedEffectiveFrom, initiatedBy, primaryEmployeeId.Trim(),
                counterpartEmployeeId.Trim()));
    }

    /// <summary>
    /// TK-034 satisfied: both named parties agreed, or an authorized actor
    /// substituted. Nothing else counts.
    /// </summary>
    public bool IsReadyToActivate => (PrimaryConsented && CounterpartConsented) || OverriddenBy is not null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CounterpartAssignmentId;
        yield return ProposedEffectiveFrom;
        yield return InitiatedBy;
        yield return PrimaryEmployeeId;
        yield return CounterpartEmployeeId;
        yield return PrimaryConsented;
        yield return CounterpartConsented;
        yield return OverriddenBy;
    }
}
