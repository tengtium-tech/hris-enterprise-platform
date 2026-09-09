namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Source: docs/04-modules/workflow/domain/value-objects.md's "Other Value Objects"
/// table and entities.md's own attribute table. The type drives which of a step's
/// three optional configurations is required and which are prohibited, enforced as
/// a construction invariant on <see cref="WorkflowStep"/> rather than left as
/// nullable fields to validate later (entities.md's own AI Implementation Guidance).
/// </summary>
public enum StepType
{
    /// <summary>Requires a person's decision, resolved by <see cref="ApproverResolutionRule"/>.</summary>
    Approval = 0,

    /// <summary>Invokes a module's public command by <see cref="CommandReference"/> (WR-001).</summary>
    Automated = 1,

    /// <summary>Branches on a <see cref="StepCondition"/>; requires a default successor (WR-003).</summary>
    Conditional = 2,

    /// <summary>Ends a path through the graph; has no successors.</summary>
    Terminal = 3,
}
