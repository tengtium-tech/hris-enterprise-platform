namespace Hris.Foundation.WorkflowEngine.Application.Dtos;

public sealed record WorkflowInstanceDto(
    Guid WorkflowInstanceId,
    Guid TenantId,
    Guid WorkflowDefinitionId,
    int WorkflowDefinitionVersionNumber,
    string? TriggeringReference,
    Guid InitiatedByUserId,
    string Status,
    int CurrentStepOrder,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? FailureReason);
