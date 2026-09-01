namespace Hris.Foundation.RulesEngine.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.RuleEvaluationResult"/>, returned by
/// <c>EvaluateRuleQuery</c>. Never carries the Domain-layer
/// <see cref="Hris.SharedKernel.IDomainEvent"/> the result also carries -- that event
/// exists for a future, selectively-invoked auditing caller to publish, the same
/// reasoning <c>AuthorizationDecisionDto</c>'s own remarks give for the identical
/// omission.
/// </summary>
public sealed record RuleEvaluationResultDto(
    bool IsMatched,
    IReadOnlyList<RuleActionDirectiveDto> Actions,
    string? FailureReason);
