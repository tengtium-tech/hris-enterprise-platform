namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// How a <see cref="RuleCondition"/> compares a fact's value against its own
/// expected value. Not literally named in rules-engine.md (which gives only example
/// conditions like "Years of Service," not comparison mechanics) but required to
/// make "Conditions may be combined using logical operators" concretely evaluable --
/// a condition is meaningless without stating how comparison happens.
/// </summary>
public enum ComparisonOperator
{
    Equals = 0,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    In,
}
