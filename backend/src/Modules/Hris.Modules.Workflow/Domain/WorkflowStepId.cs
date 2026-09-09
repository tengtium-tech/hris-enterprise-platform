using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Identity of a <see cref="WorkflowStep"/>, unique within its
/// <see cref="WorkflowDefinition"/>. Steps are reached only through their root; no
/// repository exists for this identifier (CTR-ARC-004).
/// </summary>
public readonly record struct WorkflowStepId(Guid Value) : IStronglyTypedId;
