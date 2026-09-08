namespace Hris.Modules.Position.Application.Dtos;

public sealed record JobGradeDto(
    Guid JobGradeId,
    Guid TenantId,
    string Code,
    string Name,
    string? Description,
    int? OrganizationalLevel,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record JobGradeSummaryDto(Guid JobGradeId, string Code, string Name, string Status);
