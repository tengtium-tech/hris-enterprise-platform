using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Identity of the <see cref="WorkflowInstance"/> Aggregate Root -- one running
/// execution of a <see cref="WorkflowDefinition"/>, per workflow-engine.md's own Core
/// Concepts ("A Workflow Instance represents a running execution of a workflow
/// definition. Each request creates its own workflow instance.").
/// </summary>
public readonly record struct WorkflowInstanceId(Guid Value) : IStronglyTypedId;
