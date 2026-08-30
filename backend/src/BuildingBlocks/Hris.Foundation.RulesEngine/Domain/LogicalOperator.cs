namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// How a <see cref="RuleVersion"/>'s <see cref="RuleCondition"/>s combine, per
/// rules-engine.md's Rule Condition section: "Conditions may be combined using
/// logical operators." <c>All</c> requires every condition to hold (AND);
/// <c>Any</c> requires at least one (OR). One operator applies across a version's
/// whole condition set rather than an arbitrarily nested expression tree -- every
/// example this document gives is a flat list of attribute checks, and a fully
/// general nested boolean expression grammar is exactly the kind of speculative
/// generality "Custom Condition Providers" (this document's own Extension Points
/// section) exists to let a tenant add later, not something to build ahead of a
/// concrete need for it.
/// </summary>
public enum LogicalOperator
{
    All = 0,
    Any,
}
