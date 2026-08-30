using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// One version of a <see cref="RuleDefinition"/>'s actual policy content -- its
/// conditions and actions -- per rules-engine.md's Rule Version section: "Historical
/// evaluations should reference the rule version that was executed." A child Entity,
/// never an Aggregate Root of its own (aggregate-design-rules.md Rule 7); its
/// constructor and transition methods are <c>internal</c>, reachable only through
/// <see cref="RuleDefinition"/>.
///
/// Mirrors <c>ConfigurationVersion</c>'s own shape closely -- both frameworks
/// independently specify the identical draft/validate/publish/activate/deprecate/
/// archive lifecycle for versioned, tenant-configurable content.
/// </summary>
public sealed class RuleVersion : Entity<RuleVersionId>
{
    private readonly List<RuleCondition> _conditions;
    private readonly List<RuleActionDirective> _actions;

    public int VersionNumber { get; }

    public IReadOnlyList<RuleCondition> Conditions => _conditions.AsReadOnly();

    public LogicalOperator ConditionOperator { get; }

    public IReadOnlyList<RuleActionDirective> Actions => _actions.AsReadOnly();

    public RulePriority Priority { get; }

    public UserAccountId CreatedByUserId { get; }

    public RuleLifecycleState State { get; private set; }

    internal RuleVersion(
        RuleVersionId id,
        int versionNumber,
        IReadOnlyCollection<RuleCondition> conditions,
        LogicalOperator conditionOperator,
        IReadOnlyCollection<RuleActionDirective> actions,
        RulePriority priority,
        UserAccountId createdByUserId)
        : base(id)
    {
        VersionNumber = versionNumber;
        _conditions = [.. conditions];
        ConditionOperator = conditionOperator;
        _actions = [.. actions];
        Priority = priority;
        CreatedByUserId = createdByUserId;
        State = RuleLifecycleState.Draft;
    }

    /// <summary>
    /// Evaluates <see cref="Conditions"/> against <paramref name="context"/>,
    /// combined by <see cref="ConditionOperator"/>. Pure and deterministic -- no I/O,
    /// no ambient state -- per this document's own Implementation Guidance ("Make
    /// rule evaluation deterministic. The same inputs and the same rule version must
    /// always produce the same result").
    /// </summary>
    internal Result<bool> IsSatisfiedBy(RuleEvaluationContext context)
    {
        var evaluations = new List<bool>(_conditions.Count);

        foreach (var condition in _conditions)
        {
            var factResult = context.TryGetValue(condition.FieldName);
            if (factResult.IsFailure)
            {
                return Result.Failure<bool>(factResult.Error);
            }

            evaluations.Add(RuleConditionEvaluator.Matches(condition, factResult.Value));
        }

        var matched = ConditionOperator == LogicalOperator.All
            ? evaluations.All(e => e)
            : evaluations.Any(e => e);

        return Result.Success(matched);
    }

    internal Result MarkValidated()
    {
        if (State != RuleLifecycleState.Draft)
        {
            return Result.Failure(RuleErrors.InvalidLifecycleTransition);
        }

        if (_conditions.Count == 0)
        {
            return Result.Failure(RuleErrors.AtLeastOneConditionRequired);
        }

        if (_actions.Count == 0)
        {
            return Result.Failure(RuleErrors.AtLeastOneActionRequired);
        }

        State = RuleLifecycleState.Validated;
        return Result.Success();
    }

    internal Result Publish()
    {
        if (State != RuleLifecycleState.Validated)
        {
            return Result.Failure(RuleErrors.InvalidLifecycleTransition);
        }

        State = RuleLifecycleState.Published;
        return Result.Success();
    }

    internal Result Activate()
    {
        if (State != RuleLifecycleState.Published)
        {
            return Result.Failure(RuleErrors.InvalidLifecycleTransition);
        }

        State = RuleLifecycleState.Active;
        return Result.Success();
    }

    internal Result Deprecate()
    {
        if (State != RuleLifecycleState.Active)
        {
            return Result.Failure(RuleErrors.InvalidLifecycleTransition);
        }

        State = RuleLifecycleState.Deprecated;
        return Result.Success();
    }

    internal Result Archive()
    {
        if (State != RuleLifecycleState.Deprecated)
        {
            return Result.Failure(RuleErrors.InvalidLifecycleTransition);
        }

        State = RuleLifecycleState.Archived;
        return Result.Success();
    }
}
