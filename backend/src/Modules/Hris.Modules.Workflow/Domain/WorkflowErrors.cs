using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section, citing the business-rule IDs from
/// docs/04-modules/workflow/domain/business-rules.md in each entry's own remarks
/// where one exists.
///
/// Publication failures are catalogued individually rather than collapsed into one
/// generic entry, per commands.md's own statement that "a generic 'publication
/// failed' loses the information the audit trail, and the next author trying to fix
/// the definition, both need."
///
/// Several documented rules have no error entry here deliberately, matching every
/// prior module's own documented-gap precedent. WR-001's "the referenced command
/// exists in the named module's public contract" requires a live registry of every
/// module's command surface, which no module in this codebase publishes yet; it is
/// expressed as a caller-supplied boolean the Application layer computes, this
/// platform's own standing "no compile-time cross-module reference" rule. WR-031's
/// bound on an approval authority limit against what the granting role actually
/// holds requires live Authorization Framework data this module does not own. Both
/// are tracked in STATUS.md rather than silently invented checks.
/// </summary>
public static class WorkflowErrors
{
    // Definition authoring.
    public static readonly Error DefinitionNameRequired = new(
        "Workflow.DefinitionNameRequired",
        "A workflow definition requires a name.",
        ErrorCategory.Validation);

    public static readonly Error DefinitionNameNotUniqueForBusinessProcess = new(
        "Workflow.DefinitionNameNotUniqueForBusinessProcess",
        "A definition with this name already exists for this business process in this tenant. (WR-006)",
        ErrorCategory.Conflict);

    public static readonly Error DefinitionNotFound = new(
        "Workflow.DefinitionNotFound",
        "The workflow definition was not found.",
        ErrorCategory.NotFound);

    public static readonly Error DefinitionNotDraft = new(
        "Workflow.DefinitionNotDraft",
        "Only a draft definition may be edited. A published definition is immutable; author a new version instead. (WR-004)",
        ErrorCategory.Domain);

    public static readonly Error DefinitionNotPublished = new(
        "Workflow.DefinitionNotPublished",
        "The operation requires a published definition.",
        ErrorCategory.Domain);

    public static readonly Error DefinitionAlreadyPublished = new(
        "Workflow.DefinitionAlreadyPublished",
        "This definition version is already published. Publication is a deliberate act and a duplicate attempt is rejected rather than silently accepted.",
        ErrorCategory.Conflict);

    // Publication: whole-graph validation (WR-002 through WR-006, WR-011).
    public static readonly Error DefinitionHasNoSteps = new(
        "Workflow.DefinitionHasNoSteps",
        "A definition with no steps cannot be published. (WR-005)",
        ErrorCategory.Domain);

    public static readonly Error StepGraphContainsCycle = new(
        "Workflow.StepGraphContainsCycle",
        "The step graph contains a cycle; an instance reaching it would run forever. (WR-002)",
        ErrorCategory.Domain);

    public static readonly Error StepGraphContainsUnreachableStep = new(
        "Workflow.StepGraphContainsUnreachableStep",
        "The step graph contains a step no path reaches. (WR-002)",
        ErrorCategory.Domain);

    public static readonly Error StepGraphDoesNotTerminate = new(
        "Workflow.StepGraphDoesNotTerminate",
        "A step has neither a successor nor a terminal marker, so the graph does not terminate. (WR-002)",
        ErrorCategory.Domain);

    public static readonly Error StepSuccessorNotFound = new(
        "Workflow.StepSuccessorNotFound",
        "A step names a successor that is not part of this definition. (WR-002)",
        ErrorCategory.Domain);

    public static readonly Error ConditionalStepRequiresDefaultSuccessor = new(
        "Workflow.ConditionalStepRequiresDefaultSuccessor",
        "Every conditional step requires a default successor; an unmatched condition must not strand an instance. (WR-003)",
        ErrorCategory.Domain);

    public static readonly Error ReferencedCommandDoesNotExist = new(
        "Workflow.ReferencedCommandDoesNotExist",
        "An automated step references a command that does not exist in the named module's public contract. (WR-001, CTR-WFL-001)",
        ErrorCategory.Domain);

    public static readonly Error DefinitionCanRouteToRequester = new(
        "Workflow.DefinitionCanRouteToRequester",
        "A definition capable of routing an approval to the requester is rejected at publication, under every tenant configuration. (WR-011, CTR-WFL-002)",
        ErrorCategory.Domain);

    public static readonly Error TriggerConditionOverlapsPublishedDefinition = new(
        "Workflow.TriggerConditionOverlapsPublishedDefinition",
        "Another published definition for this business process matches the same requests, leaving which process governs ambiguous.",
        ErrorCategory.Conflict);

    // Step construction (entities.md validation rules).
    public static readonly Error StepNameRequired = new(
        "Workflow.StepNameRequired",
        "A workflow step requires a name.",
        ErrorCategory.Validation);

    public static readonly Error ApprovalStepRequiresResolutionRule = new(
        "Workflow.ApprovalStepRequiresResolutionRule",
        "An approver resolution rule is present if and only if the step is an approval step.",
        ErrorCategory.Validation);

    public static readonly Error AutomatedStepRequiresCommandReference = new(
        "Workflow.AutomatedStepRequiresCommandReference",
        "A command reference is present if and only if the step is an automated step.",
        ErrorCategory.Validation);

    public static readonly Error ConditionalStepRequiresCondition = new(
        "Workflow.ConditionalStepRequiresCondition",
        "A condition is present if and only if the step is a conditional step.",
        ErrorCategory.Validation);

    public static readonly Error TerminalStepCannotHaveSuccessors = new(
        "Workflow.TerminalStepCannotHaveSuccessors",
        "A terminal step ends its path and has no successors.",
        ErrorCategory.Validation);

    public static readonly Error JoinModeRequiresMultiplePredecessors = new(
        "Workflow.JoinModeRequiresMultiplePredecessors",
        "A join mode is present only where the step converges more than one predecessor; there is otherwise nothing to join.",
        ErrorCategory.Validation);

    public static readonly Error StepNotFound = new(
        "Workflow.StepNotFound",
        "The step is not part of this definition.",
        ErrorCategory.NotFound);

    // Approver resolution (WR-010, WR-011, CTR-WFL-002).
    public static readonly Error RoleResolutionRequiresRoleName = new(
        "Workflow.RoleResolutionRequiresRoleName",
        "A role resolution rule requires a role name; the other kinds hold no stored target. (WR-010)",
        ErrorCategory.Validation);

    public static readonly Error ResolutionRuleTargetProhibited = new(
        "Workflow.ResolutionRuleTargetProhibited",
        "A reporting-line rule resolves against the requester's manager chain at runtime and holds no stored target.",
        ErrorCategory.Validation);

    public static readonly Error ScopeResolutionRequiresScope = new(
        "Workflow.ScopeResolutionRequiresScope",
        "An organizational-scope resolution rule requires the scope it resolves against.",
        ErrorCategory.Validation);

    public static readonly Error ResolutionRuleStaticallyNamesRequester = new(
        "Workflow.ResolutionRuleStaticallyNamesRequester",
        "No approver resolution rule may statically name or imply the requester. (WR-011, CTR-WFL-002)",
        ErrorCategory.Domain);

    // Escalation and SLA (WR-020 through WR-023).
    public static readonly Error EscalationTriggerMustBePositive = new(
        "Workflow.EscalationTriggerMustBePositive",
        "An escalation trigger duration must be positive.",
        ErrorCategory.Validation);

    public static readonly Error EscalationTriggerExceedsPlatformMaximum = new(
        "Workflow.EscalationTriggerExceedsPlatformMaximum",
        "An escalation trigger duration must not exceed the platform maximum.",
        ErrorCategory.Validation);

    public static readonly Error EscalationDepthMustBePositive = new(
        "Workflow.EscalationDepthMustBePositive",
        "An escalation chain must permit at least one hop.",
        ErrorCategory.Validation);

    public static readonly Error EscalationDepthUnbounded = new(
        "Workflow.EscalationDepthUnbounded",
        "An unbounded escalation chain is rejected at construction. (WR-021)",
        ErrorCategory.Validation);

    public static readonly Error SlaDurationMustBePositive = new(
        "Workflow.SlaDurationMustBePositive",
        "An SLA duration must be positive. (WR-023)",
        ErrorCategory.Validation);

    public static readonly Error SlaDurationExceedsPlatformMaximum = new(
        "Workflow.SlaDurationExceedsPlatformMaximum",
        "An SLA duration must not exceed the platform maximum; an unbounded SLA is equivalent to no SLA. (WR-023)",
        ErrorCategory.Validation);

    // Command reference (WR-001).
    public static readonly Error CommandReferenceModuleRequired = new(
        "Workflow.CommandReferenceModuleRequired",
        "A command reference requires the owning module's name.",
        ErrorCategory.Validation);

    public static readonly Error CommandReferenceCommandRequired = new(
        "Workflow.CommandReferenceCommandRequired",
        "A command reference requires the command identifier.",
        ErrorCategory.Validation);

    // Step condition.
    public static readonly Error StepConditionExpressionRequired = new(
        "Workflow.StepConditionExpressionRequired",
        "A step condition requires an expression.",
        ErrorCategory.Validation);

    public static readonly Error StepConditionRequiresBranches = new(
        "Workflow.StepConditionRequiresBranches",
        "A step condition requires at least one branch; a condition with none can only ever take the default path.",
        ErrorCategory.Validation);

    public static readonly Error StepConditionBranchPredicateRequired = new(
        "Workflow.StepConditionBranchPredicateRequired",
        "Every condition branch requires a predicate.",
        ErrorCategory.Validation);

    // Approval policy (WR-030 through WR-032).
    public static readonly Error ApprovalPolicyNotFound = new(
        "Workflow.ApprovalPolicyNotFound",
        "No approval policy exists for this tenant.",
        ErrorCategory.NotFound);

    public static readonly Error ApprovalPolicyAlreadyExistsForTenant = new(
        "Workflow.ApprovalPolicyAlreadyExistsForTenant",
        "Exactly one approval policy exists per tenant. (WR-030)",
        ErrorCategory.Conflict);

    public static readonly Error PolicyChangeReasonRequired = new(
        "Workflow.PolicyChangeReasonRequired",
        "A policy change requires a reason; removing an approval requirement is a control removal and is the change most worth having one for.",
        ErrorCategory.Validation);

    public static readonly Error AuthorityLimitExceedsRoleStanding = new(
        "Workflow.AuthorityLimitExceedsRoleStanding",
        "An approval authority limit cannot exceed what the granting role holds elsewhere in the platform. (WR-031)",
        ErrorCategory.Domain);

    // Approval delegation (WR-040 through WR-044).
    public static readonly Error DelegationNotFound = new(
        "Workflow.DelegationNotFound",
        "The approval delegation was not found.",
        ErrorCategory.NotFound);

    public static readonly Error DelegatorEqualsDelegate = new(
        "Workflow.DelegatorEqualsDelegate",
        "A delegation transfers authority between two different users.",
        ErrorCategory.Validation);

    public static readonly Error DelegationScopeEmpty = new(
        "Workflow.DelegationScopeEmpty",
        "A delegation covers named business processes, or all of them; it cannot cover none.",
        ErrorCategory.Validation);

    public static readonly Error DelegationReasonRequired = new(
        "Workflow.DelegationReasonRequired",
        "A delegation requires a reason.",
        ErrorCategory.Validation);

    public static readonly Error DelegationScopeExceedsDelegatorStanding = new(
        "Workflow.DelegationScopeExceedsDelegatorStanding",
        "A delegator delegates only approval authority they currently hold. (WR-041)",
        ErrorCategory.Domain);

    public static readonly Error DelegationOfDelegatedAuthority = new(
        "Workflow.DelegationOfDelegatedAuthority",
        "Approval authority received by delegation cannot itself be delegated. (WR-042)",
        ErrorCategory.Domain);

    public static readonly Error DelegationNotScheduled = new(
        "Workflow.DelegationNotScheduled",
        "Only a scheduled delegation may be activated.",
        ErrorCategory.Domain);

    public static readonly Error DelegationNotActive = new(
        "Workflow.DelegationNotActive",
        "Only an active delegation may expire.",
        ErrorCategory.Domain);
}
