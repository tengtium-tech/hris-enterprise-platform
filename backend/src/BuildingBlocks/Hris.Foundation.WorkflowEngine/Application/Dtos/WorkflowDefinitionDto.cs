namespace Hris.Foundation.WorkflowEngine.Application.Dtos;

public sealed record WorkflowDefinitionDto(
    Guid WorkflowDefinitionId,
    Guid TenantId,
    string Name,
    string TriggerType,
    string? TriggerExpression,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<WorkflowDefinitionVersionDto> Versions);
