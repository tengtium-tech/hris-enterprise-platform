namespace Hris.Foundation.WorkflowEngine.Application.Dtos;

public sealed record WorkflowDefinitionVersionDto(
    Guid WorkflowDefinitionVersionId,
    int VersionNumber,
    IReadOnlyList<WorkflowStepDto> Steps,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
