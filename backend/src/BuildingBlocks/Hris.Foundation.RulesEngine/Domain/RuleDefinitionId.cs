using Hris.SharedKernel;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>Identity of the <see cref="RuleDefinition"/> Aggregate Root.</summary>
public readonly record struct RuleDefinitionId(Guid Value) : IStronglyTypedId;
