namespace Hris.Modules.Position.Application.Dtos;

public sealed record JobClassificationDto(
    Guid JobClassificationId,
    Guid TenantId,
    string Code,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record JobClassificationSummaryDto(Guid JobClassificationId, string Code, string Name, string Status);
