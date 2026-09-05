using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class WorkflowEngineErrors
{
    public static readonly Error DefinitionNameRequired = new(
        "WorkflowEngine.DefinitionNameRequired",
        "A workflow definition name is required.",
        ErrorCategory.Validation);

    public static readonly Error TriggerExpressionRequired = new(
        "WorkflowEngine.TriggerExpressionRequired",
        "A trigger expression is required for a system-event or scheduled trigger.",
        ErrorCategory.Validation);

    public static readonly Error StepsRequired = new(
        "WorkflowEngine.StepsRequired",
        "At least one workflow step is required.",
        ErrorCategory.Validation);

    public static readonly Error DefinitionNotFound = new(
        "WorkflowEngine.DefinitionNotFound",
        "No workflow definition exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error DraftAlreadyExists = new(
        "WorkflowEngine.DraftAlreadyExists",
        "This workflow definition already has an unpublished draft version.",
        ErrorCategory.Conflict);

    public static readonly Error VersionNotFound = new(
        "WorkflowEngine.VersionNotFound",
        "No version exists for the given version number on this workflow definition.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidVersionLifecycleTransition = new(
        "WorkflowEngine.InvalidVersionLifecycleTransition",
        "This transition is not valid from the version's current status.",
        ErrorCategory.Domain);

    public static readonly Error SelfApprovalRoutingNotAllowed = new(
        "WorkflowEngine.SelfApprovalRoutingNotAllowed",
        "A workflow definition cannot route an approval step to the requester.",
        ErrorCategory.Domain);

    public static readonly Error InvalidParticipantRoleName = new(
        "WorkflowEngine.InvalidParticipantRoleName",
        "An approval step's participant role must be one of the canonical roles this framework recognizes as a workflow participant.",
        ErrorCategory.Validation);

    public static readonly Error InstanceNotFound = new(
        "WorkflowEngine.InstanceNotFound",
        "No workflow instance exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidInstanceLifecycleTransition = new(
        "WorkflowEngine.InvalidInstanceLifecycleTransition",
        "This transition is not valid from the workflow instance's current status.",
        ErrorCategory.Domain);

    public static readonly Error ReasonRequired = new(
        "WorkflowEngine.ReasonRequired",
        "A reason is required for this transition.",
        ErrorCategory.Validation);

    public static readonly Error TaskNotFound = new(
        "WorkflowEngine.TaskNotFound",
        "No workflow task exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidTaskLifecycleTransition = new(
        "WorkflowEngine.InvalidTaskLifecycleTransition",
        "This transition is not valid from the workflow task's current status.",
        ErrorCategory.Domain);

    public static readonly Error DelegateToUserRequired = new(
        "WorkflowEngine.DelegateToUserRequired",
        "A target user is required to delegate a workflow task.",
        ErrorCategory.Validation);

    public static readonly Error EscalateToUserRequired = new(
        "WorkflowEngine.EscalateToUserRequired",
        "A target user is required to escalate a workflow task.",
        ErrorCategory.Validation);
}
