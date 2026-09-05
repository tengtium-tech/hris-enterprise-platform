using Hris.Foundation.WorkflowEngine.Application.Dtos;
using Hris.Foundation.WorkflowEngine.Domain;

namespace Hris.Foundation.WorkflowEngine.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4/5 framework's own mapper already
/// establishes.
/// </summary>
internal static class WorkflowEngineMapper
{
    public static WorkflowStepDto ToDto(WorkflowStepDefinition step) => new(
        step.StepName,
        step.StepType.ToString(),
        step.Order,
        step.ParticipantType?.ToString(),
        step.ParticipantRoleName,
        step.ActionName,
        step.NotificationTemplateKey);

    public static WorkflowDefinitionVersionDto ToDto(WorkflowDefinitionVersion version) => new(
        version.Id.Value,
        version.VersionNumber,
        version.Steps.Select(ToDto).ToList(),
        version.Status.ToString(),
        version.CreatedAtUtc,
        version.PublishedAtUtc);

    public static WorkflowDefinitionDto ToDto(WorkflowDefinition definition) => new(
        definition.Id.Value,
        definition.TenantId,
        definition.Name,
        definition.TriggerType.ToString(),
        definition.TriggerExpression,
        definition.CreatedAtUtc,
        definition.Versions.Select(ToDto).ToList());

    public static WorkflowInstanceDto ToDto(WorkflowInstance instance) => new(
        instance.Id.Value,
        instance.TenantId,
        instance.WorkflowDefinitionId.Value,
        instance.WorkflowDefinitionVersionNumber,
        instance.TriggeringReference,
        instance.InitiatedByUserId,
        instance.Status.ToString(),
        instance.CurrentStepOrder,
        instance.StartedAtUtc,
        instance.CompletedAtUtc,
        instance.FailureReason);

    public static WorkflowTaskDto ToDto(WorkflowTask task) => new(
        task.Id.Value,
        task.TenantId,
        task.WorkflowInstanceId.Value,
        task.StepName,
        task.StepOrder,
        task.ParticipantType.ToString(),
        task.ParticipantRoleName,
        task.AssignedToUserId,
        task.Status.ToString(),
        task.Comments,
        task.DelegatedToUserId,
        task.EscalationLevel,
        task.CreatedAtUtc,
        task.CompletedAtUtc);
}
