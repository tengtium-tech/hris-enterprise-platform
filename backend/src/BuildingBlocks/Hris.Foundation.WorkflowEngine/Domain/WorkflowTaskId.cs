using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Identity of the <see cref="WorkflowTask"/> Aggregate Root -- one unit of work
/// assigned to a participant for action, per workflow-engine.md's own Core Concepts
/// ("A Workflow Task is assigned to a user or role for action"). Kept as its own
/// Aggregate Root rather than a child Entity of <see cref="WorkflowInstance"/>, the same
/// reason <c>IssuedNumberId</c>'s own remarks give for <c>IssuedNumber</c>: tasks
/// accumulate without bound across every instance's own steps, and this document's own
/// Permissions table queries tasks independently of their owning instance ("Act on
/// assigned approval," "View team workflow instances").
/// </summary>
public readonly record struct WorkflowTaskId(Guid Value) : IStronglyTypedId;
