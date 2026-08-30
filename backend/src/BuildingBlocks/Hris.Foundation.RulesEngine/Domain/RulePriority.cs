namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// The four levels rules-engine.md's Rule Priorities section names: "Critical, High,
/// Normal, Low... determines evaluation order when multiple rules apply." Ordinal
/// order matches severity, most-urgent first, for direct use as a sort key.
/// </summary>
public enum RulePriority
{
    Critical = 0,
    High,
    Normal,
    Low,
}
