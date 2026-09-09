namespace Hris.Modules.Workflow.Application.Dtos;

/// <summary>
/// Read shape of a workflow definition. Enumerations are projected as strings, per
/// dto-design.md's own convention across every prior module, so a consumer is never
/// coupled to an ordinal.
/// </summary>
public sealed record WorkflowDefinitionDto(
    Guid Id,
    Guid TenantId,
    Guid BusinessProcessId,
    Guid LineageId,
    string Name,
    string? Description,
    string? TriggerCondition,
    int Version,
    string Status,
    IReadOnlyList<WorkflowStepDto> Steps,
    Guid CreatedBy,
    DateTimeOffset CreatedOn,
    Guid? PublishedBy,
    DateTimeOffset? PublishedOn,
    Guid? DeprecatedBy,
    DateTimeOffset? DeprecatedOn,
    string? DeprecationReason);

public sealed record WorkflowStepDto(
    Guid Id,
    int Order,
    string StepType,
    string Name,
    ApproverResolutionRuleDto? ApproverResolutionRule,
    CommandReferenceDto? CommandReference,
    StepConditionDto? Condition,
    IReadOnlyList<Guid> Successors,
    Guid? DefaultSuccessor,
    string? JoinMode,
    bool NonDelegable,
    EscalationPolicyDto? EscalationOverride,
    TimeSpan? SlaOverride);

public sealed record ApproverResolutionRuleDto(string Kind, string? RoleName, Guid? ScopeId);

public sealed record CommandReferenceDto(string ModuleName, string CommandId);

public sealed record StepConditionDto(string Expression, IReadOnlyList<ConditionBranchDto> Branches);

public sealed record ConditionBranchDto(string Predicate, Guid TargetStepId);

public sealed record EscalationPolicyDto(TimeSpan TriggerAfter, ApproverResolutionRuleDto EscalateTo, int MaxDepth);
