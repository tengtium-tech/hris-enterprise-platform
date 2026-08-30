using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// What a rule instructs when its conditions match, per rules-engine.md's Rule
/// Action section ("Approve, Reject, Calculate, Generate Warning, Escalate, Notify,
/// Apply Deduction, Apply Benefit"). <see cref="ActionKey"/> is an open string for
/// the same reason <see cref="RuleCondition.FieldName"/> is: this document's own
/// example list is illustrative, and "Custom Actions" is a named Extension Point.
/// Rules Engine records the directive; it never performs the action itself --
/// "Never orchestrate processes from the Rules Engine... belongs to the Workflow
/// Engine" (this document's own Implementation Guidance) applies with equal force
/// to executing an action's real-world effect, which is always the caller's own
/// module, not this framework.
/// </summary>
public sealed class RuleActionDirective : ValueObject
{
    public string ActionKey { get; }

    public IReadOnlyDictionary<string, string> Parameters { get; }

    private RuleActionDirective(string actionKey, IReadOnlyDictionary<string, string> parameters)
    {
        ActionKey = actionKey;
        Parameters = parameters;
    }

    public static Result<RuleActionDirective> Create(string? actionKey, IReadOnlyDictionary<string, string>? parameters = null)
    {
        return string.IsNullOrWhiteSpace(actionKey)
            ? Result.Failure<RuleActionDirective>(RuleErrors.ActionKeyRequired)
            : Result.Success(new RuleActionDirective(actionKey.Trim(), parameters ?? new Dictionary<string, string>()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ActionKey;
        foreach (var pair in Parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            yield return pair.Key;
            yield return pair.Value;
        }
    }

    public override string ToString() => ActionKey;
}
