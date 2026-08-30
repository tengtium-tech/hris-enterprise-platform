using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// One check against a named fact, per rules-engine.md's Rule Condition section. Both
/// <see cref="FieldName"/> ("Employee Type," "Years of Service," "Payroll Group" in
/// this document's own examples) and <see cref="ComparisonValue"/> are open strings
/// rather than closed types: Rules Engine has no compile-time knowledge of any
/// business module's schema (those modules do not exist until Phase 2 onward), and
/// this document's own Extension Points section names "Custom Condition Providers"
/// as the intended mechanism for exactly this kind of business-specific vocabulary.
/// <see cref="RuleEvaluator"/> resolves <see cref="FieldName"/> against whatever the
/// caller's own <see cref="RuleEvaluationContext"/> supplies for it.
/// </summary>
public sealed class RuleCondition : ValueObject
{
    public string FieldName { get; }

    public ComparisonOperator Operator { get; }

    public string ComparisonValue { get; }

    private RuleCondition(string fieldName, ComparisonOperator @operator, string comparisonValue)
    {
        FieldName = fieldName;
        Operator = @operator;
        ComparisonValue = comparisonValue;
    }

    public static Result<RuleCondition> Create(string? fieldName, ComparisonOperator @operator, string? comparisonValue)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return Result.Failure<RuleCondition>(RuleErrors.FieldNameRequired);
        }

        if (string.IsNullOrWhiteSpace(comparisonValue))
        {
            return Result.Failure<RuleCondition>(RuleErrors.ComparisonValueRequired);
        }

        return Result.Success(new RuleCondition(fieldName.Trim(), @operator, comparisonValue.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldName;
        yield return Operator;
        yield return ComparisonValue;
    }

    public override string ToString() => $"{FieldName} {Operator} {ComparisonValue}";
}
