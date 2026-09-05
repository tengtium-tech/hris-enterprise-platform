namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Trigger Types section: "a trigger determines when a
/// workflow instance is created." <see cref="SystemEvent"/> is "the most common trigger
/// type" per that section's own wording, listed first here to match.
/// </summary>
public enum WorkflowTriggerType
{
    SystemEvent = 0,
    Scheduled = 1,
    Manual = 2,
    Api = 3,
}
