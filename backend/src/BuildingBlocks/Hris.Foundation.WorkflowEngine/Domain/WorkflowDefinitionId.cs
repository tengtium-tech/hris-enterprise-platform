using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// Identity of the <see cref="WorkflowDefinition"/> Aggregate Root -- one workflow
/// template (Leave Approval, Payroll Approval, and so on), per workflow-engine.md's own
/// Core Concepts ("A Workflow Definition describes the template used to execute a
/// business process").
/// </summary>
public readonly record struct WorkflowDefinitionId(Guid Value) : IStronglyTypedId;
