using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// The test a conditional step evaluates to choose a branch. Source:
/// docs/04-modules/workflow/domain/value-objects.md.
///
/// Branches are evaluated in order and the first matching predicate's target is
/// taken. They are deliberately separate from the step's own mandatory
/// <c>DefaultSuccessor</c> (WR-003), which is taken when no predicate matches: the
/// default is a property of the step, required whether or not this condition
/// happens to cover every case, because a condition matching nothing does not error
/// and does not log. It simply stops, holding a business request indefinitely.
/// </summary>
public sealed class StepCondition : ValueObject
{
    public string Expression { get; }

    public IReadOnlyList<ConditionBranch> Branches { get; }

    private StepCondition(string expression, IReadOnlyList<ConditionBranch> branches)
    {
        Expression = expression;
        Branches = branches;
    }

    public static Result<StepCondition> Create(string? expression, IReadOnlyList<ConditionBranch>? branches)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Result.Failure<StepCondition>(WorkflowErrors.StepConditionExpressionRequired);
        }

        if (branches is null || branches.Count == 0)
        {
            return Result.Failure<StepCondition>(WorkflowErrors.StepConditionRequiresBranches);
        }

        if (branches.Any(branch => string.IsNullOrWhiteSpace(branch.Predicate)))
        {
            return Result.Failure<StepCondition>(WorkflowErrors.StepConditionBranchPredicateRequired);
        }

        return Result.Success(new StepCondition(expression.Trim(), branches.ToList()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Expression;
        foreach (var branch in Branches)
        {
            yield return branch;
        }
    }
}
