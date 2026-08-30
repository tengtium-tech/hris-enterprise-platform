using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// The "fact" a caller supplies for one evaluation -- rules-engine.md's own Rule
/// Sources section ("Employee Data, Organization Data, Payroll Data...") named as
/// what conditions may reference, none of which Rules Engine itself has schema
/// knowledge of. The caller (a future module's Application layer) is responsible for
/// resolving whatever tenant-scoped data a condition's <see cref="RuleCondition.FieldName"/>
/// needs and placing it here as a string *before* calling
/// <see cref="RuleEvaluator.EvaluateAsync"/> -- this type never reaches out to fetch
/// anything itself, which is what keeps evaluation tenant-safe (`CTR-ISO-001`): there
/// is no code path here that could read another tenant's data, because there is no
/// data access here at all.
/// </summary>
public sealed class RuleEvaluationContext
{
    private readonly IReadOnlyDictionary<string, string> _facts;

    private RuleEvaluationContext(IReadOnlyDictionary<string, string> facts)
    {
        _facts = facts;
    }

    public static RuleEvaluationContext Create(IReadOnlyDictionary<string, string> facts)
    {
        Guard.AgainstNull(facts, nameof(facts));
        return new RuleEvaluationContext(new Dictionary<string, string>(facts, StringComparer.OrdinalIgnoreCase));
    }

    internal Result<string> TryGetValue(string fieldName)
    {
        return _facts.TryGetValue(fieldName, out var value)
            ? Result.Success(value)
            : Result.Failure<string>(RuleErrors.FactFieldMissing);
    }
}
