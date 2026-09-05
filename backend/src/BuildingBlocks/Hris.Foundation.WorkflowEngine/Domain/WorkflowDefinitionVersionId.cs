using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Identity of a <see cref="WorkflowDefinitionVersion"/> child Entity, unique within the
/// context of its owning <see cref="WorkflowDefinition"/> Aggregate -- the identical
/// shape <c>ConfigurationVersionId</c>'s own remarks already establish for its sibling
/// versioned child Entity.
/// </summary>
public readonly record struct WorkflowDefinitionVersionId(Guid Value) : IStronglyTypedId;
