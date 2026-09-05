namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Workflow Designer "Node Types" table, minus the Trigger
/// node -- a trigger is <see cref="WorkflowDefinition"/>'s own
/// <see cref="WorkflowTriggerType"/>/<c>TriggerExpression</c>, not a step within the
/// sequence <see cref="WorkflowStepDefinition"/> models.
/// </summary>
public enum WorkflowStepType
{
    Condition = 0,
    Decision = 1,
    Approval = 2,
    Action = 3,
    Notification = 4,
    Delay = 5,
    Loop = 6,
    End = 7,
}
