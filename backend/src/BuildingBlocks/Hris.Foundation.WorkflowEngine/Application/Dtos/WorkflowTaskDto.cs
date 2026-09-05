namespace Hris.Foundation.WorkflowEngine.Application.Dtos;

public sealed record WorkflowTaskDto(
    Guid WorkflowTaskId,
    Guid TenantId,
    Guid WorkflowInstanceId,
    string StepName,
    int StepOrder,
    string ParticipantType,
    string? ParticipantRoleName,
    Guid? AssignedToUserId,
    string Status,
    string? Comments,
    Guid? DelegatedToUserId,
    int EscalationLevel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
