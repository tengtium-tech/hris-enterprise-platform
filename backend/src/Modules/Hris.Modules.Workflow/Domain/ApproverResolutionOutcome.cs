namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// The result of resolving an approval step's approver. Source:
/// docs/04-modules/workflow/domain/approval-routing.md.
///
/// The two outcomes are deliberately distinguishable rather than collapsed into
/// "zero approvers means approved". <see cref="IsExempt"/> is what makes
/// CTR-WFL-002's exemption visible: a reviewer asking "who approved this" for an
/// exempted step gets an answer saying no one did, and why not, rather than a
/// record indistinguishable from a normal approval.
/// </summary>
/// <param name="Approvers">
/// Everyone entitled to act on the step, as a shared queue. Empty when
/// <see cref="IsExempt"/> is true, and never containing the requester.
/// </param>
/// <param name="IsExempt">
/// Whether the top-of-hierarchy exemption applies: resolution found nobody eligible
/// once the requester was excluded. Not an error and not a stuck instance.
/// </param>
public sealed record ApproverResolutionOutcome(IReadOnlyList<Guid> Approvers, bool IsExempt)
{
    public static ApproverResolutionOutcome Resolved(IReadOnlyList<Guid> approvers) => new(approvers, false);

    public static ApproverResolutionOutcome Exempt { get; } = new([], true);
}
