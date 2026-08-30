using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// Identity of a <see cref="RuleVersion"/> child Entity. rules-engine.md's own Rule
/// Version section: "Historical evaluations should reference the rule version that
/// was executed" -- this id is what an evaluation record references.
/// </summary>
public readonly record struct RuleVersionId(Guid Value) : IStronglyTypedId;
