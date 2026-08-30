using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// The outcome of <see cref="RuleEvaluator.EvaluateAsync"/>. <see cref="RuleExecuted"/>
/// is carried on both <see cref="Matched"/> and <see cref="NotMatched"/> -- an
/// evaluation that completed and found no match is a routine, expected outcome, the
/// same way <c>AuthorizationDecision.Deny</c> still carries
/// <c>AuthorizationEvaluated</c> rather than treating denial as a failure.
/// <see cref="RuleEvaluationFailed"/> is reserved for a genuine evaluation error --
/// this document's own catalog names it separately from a plain non-match, and
/// conflating "the conditions were false" with "evaluation could not complete" would
/// make a missing fact field indistinguishable from an ordinary no-match result.
///
/// A stateless Domain Service's decision carrying its own event rather than raising
/// one into an Aggregate's collection -- see <c>AuthorizationDecision</c>'s own
/// remarks for the full reasoning, which applies identically here.
/// </summary>
public sealed class RuleEvaluationResult
{
    public bool IsMatched { get; }

    public IReadOnlyList<RuleActionDirective> Actions { get; }

    public string? FailureReason { get; }

    public IDomainEvent Event { get; }

    private RuleEvaluationResult(bool isMatched, IReadOnlyList<RuleActionDirective> actions, string? failureReason, IDomainEvent domainEvent)
    {
        IsMatched = isMatched;
        Actions = actions;
        FailureReason = failureReason;
        Event = domainEvent;
    }

    public static RuleEvaluationResult Matched(
        RuleDefinitionId ruleId, RuleVersionId versionId, IReadOnlyList<RuleActionDirective> actions, UserAccountId? initiatedBy, DateTimeOffset nowUtc) =>
        new(true, actions, null, new RuleExecuted(Guid.NewGuid(), nowUtc, ruleId, versionId, initiatedBy));

    public static RuleEvaluationResult NotMatched(
        RuleDefinitionId ruleId, RuleVersionId versionId, UserAccountId? initiatedBy, DateTimeOffset nowUtc) =>
        new(false, [], null, new RuleExecuted(Guid.NewGuid(), nowUtc, ruleId, versionId, initiatedBy));

    public static RuleEvaluationResult EvaluationFailed(
        RuleDefinitionId ruleId, RuleVersionId versionId, string reason, DateTimeOffset nowUtc) =>
        new(false, [], reason, new RuleEvaluationFailed(Guid.NewGuid(), nowUtc, ruleId, versionId, reason));
}
