using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// Evaluates a published rule's Active version against a caller-supplied
/// <see cref="RuleEvaluationContext"/>, per rules-engine.md's Rule Evaluation
/// section. A Domain Service (domain-services.md) rather than a method on
/// <see cref="RuleDefinition"/> itself: it coordinates a repository lookup with a
/// pure evaluation step, which is orchestration slightly beyond what an Aggregate's
/// own methods should do.
///
/// Deliberately thin: this document's Rule Evaluation section names "Sequential
/// Evaluation, Parallel Evaluation... Nested Rules, Rule Chaining" as capabilities
/// the framework "should support," but evaluating *one* rule against *one* context is
/// the primitive every one of those strategies is built from, and none of them is
/// concretely specified beyond being named -- chaining multiple rules together, or
/// evaluating a batch in parallel, is Application-layer orchestration over this
/// primitive, not something this method invents a policy for on its own.
/// </summary>
public sealed class RuleEvaluator
{
    private readonly IRuleDefinitionRepository _repository;

    public RuleEvaluator(IRuleDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<RuleEvaluationResult>> EvaluateAsync(
        RuleDefinitionId ruleId,
        RuleEvaluationContext context,
        UserAccountId? initiatedBy,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        Guard.AgainstNull(context, nameof(context));

        var definition = await _repository.GetByIdAsync(ruleId, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Failure<RuleEvaluationResult>(RuleErrors.VersionNotFound);
        }

        var activeVersionResult = definition.GetActiveVersion();
        if (activeVersionResult.IsFailure)
        {
            return Result.Failure<RuleEvaluationResult>(activeVersionResult.Error);
        }

        var activeVersion = activeVersionResult.Value;
        var matchResult = activeVersion.IsSatisfiedBy(context);

        if (matchResult.IsFailure)
        {
            return Result.Success(RuleEvaluationResult.EvaluationFailed(
                ruleId, activeVersion.Id, matchResult.Error.Description, nowUtc));
        }

        var outcome = matchResult.Value
            ? RuleEvaluationResult.Matched(ruleId, activeVersion.Id, activeVersion.Actions, initiatedBy, nowUtc)
            : RuleEvaluationResult.NotMatched(ruleId, activeVersion.Id, initiatedBy, nowUtc);

        return Result.Success(outcome);
    }
}
