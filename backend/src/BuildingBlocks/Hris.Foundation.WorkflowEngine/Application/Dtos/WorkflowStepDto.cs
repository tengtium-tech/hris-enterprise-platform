namespace Hris.Foundation.WorkflowEngine.Application.Dtos;

/// <summary>
/// The read-side shape one <c>Hris.Foundation.WorkflowEngine.Domain.WorkflowStepDefinition</c>
/// maps to, per dto-design.md's own convention.
/// </summary>
public sealed record WorkflowStepDto(
    string StepName,
    string StepType,
    int Order,
    string? ParticipantType,
    string? ParticipantRoleName,
    string? ActionName,
    string? NotificationTemplateKey);
