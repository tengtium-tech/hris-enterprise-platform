namespace Hris.Modules.Position.Application.Dtos;

public sealed record JobFamilyDto(
    Guid JobFamilyId, Guid TenantId, string Code, string Name, string? Description, string Status, DateTimeOffset CreatedAtUtc);

public sealed record JobFamilySummaryDto(Guid JobFamilyId, string Code, string Name, string Status);
