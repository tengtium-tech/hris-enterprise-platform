using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Identity of the <see cref="WorkflowDefinition"/> Aggregate Root.
/// </summary>
public readonly record struct WorkflowDefinitionId(Guid Value) : IStronglyTypedId;
