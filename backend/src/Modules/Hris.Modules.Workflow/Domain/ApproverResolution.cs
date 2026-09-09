namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// The runtime half of CTR-WFL-002. Source:
/// docs/04-modules/workflow/domain/approval-routing.md's "The Requester Exclusion"
/// and "The Top-of-Hierarchy Exemption" sections, and escalation-and-sla.md's
/// statement that every escalation hop applies the identical rule.
///
/// A pure function over a caller-supplied candidate set. Producing that set requires
/// administration's role assignments, employment's reporting line, and
/// organization's placement data, none of which this module takes a compile-time
/// reference on; what the module owns, and what is genuinely testable here, is what
/// must happen to the set once it exists. That division is the same one every prior
/// module in this codebase used for a cross-module fact.
///
/// The exclusion is enforced in two places for two different failures. The static
/// half is structural and unconditional: <see cref="ApproverResolutionRule"/> has no
/// field capable of naming an individual, so no rule that always resolves to the
/// requester is constructible, and publication additionally rejects a definition a
/// caller reports as capable of routing to the requester. The dynamic half is here,
/// and catches what no static check can see: a rule that is fine for almost every
/// requester and happens, for this one, to include them.
/// </summary>
public static class ApproverResolution
{
    /// <summary>
    /// Applies the requester exclusion and, where that empties the candidate set,
    /// the top-of-hierarchy exemption.
    ///
    /// Delegation is applied before the final exclusion, not after, per that
    /// document's own guidance to "apply the requester exclusion after delegation
    /// substitutes a delegate, not only to the original resolved approver" -- a
    /// delegate who is themself the requester is excluded exactly as the delegator
    /// would have been, so delegation is not a route around self-approval
    /// prevention.
    /// </summary>
    /// <param name="requesterUserAccountId">Whose request is under approval.</param>
    /// <param name="candidateApprovers">
    /// Everyone the step's <see cref="ApproverResolutionRule"/> found, supplied by
    /// the caller from the owning modules' public contracts.
    /// </param>
    /// <param name="activeDelegations">
    /// Delegations to consider. Only those covering <paramref name="businessProcessId"/>
    /// on <paramref name="asOfDate"/>, whose delegator is in the candidate set, have
    /// any effect.
    /// </param>
    /// <param name="businessProcessId">The process being approved.</param>
    /// <param name="asOfDate">The date the delegation period is tested against.</param>
    /// <param name="stepIsNonDelegable">
    /// Where true, no delegate is offered the step regardless of an active, in-scope
    /// delegation (WR-012). Some approvals are personal by design, and delegation
    /// exists to cover routine approval load rather than relax a deliberately
    /// personal check.
    /// </param>
    public static ApproverResolutionOutcome Resolve(
        Guid requesterUserAccountId,
        IReadOnlyList<Guid> candidateApprovers,
        IReadOnlyList<ApprovalDelegation> activeDelegations,
        Guid businessProcessId,
        DateOnly asOfDate,
        bool stepIsNonDelegable)
    {
        ArgumentNullException.ThrowIfNull(candidateApprovers);
        ArgumentNullException.ThrowIfNull(activeDelegations);

        var eligible = candidateApprovers
            .Where(candidate => candidate != requesterUserAccountId)
            .Distinct()
            .ToList();

        if (!stepIsNonDelegable)
        {
            var delegates = activeDelegations
                .Where(delegation => delegation.CoversProcessOn(businessProcessId, asOfDate))
                .Where(delegation => eligible.Contains(delegation.DelegatorUserAccountId))
                .Select(delegation => delegation.DelegateUserAccountId);

            foreach (var candidate in delegates)
            {
                if (!eligible.Contains(candidate))
                {
                    eligible.Add(candidate);
                }
            }
        }

        eligible.RemoveAll(candidate => candidate == requesterUserAccountId);

        return eligible.Count == 0
            ? ApproverResolutionOutcome.Exempt
            : ApproverResolutionOutcome.Resolved(eligible);
    }
}
