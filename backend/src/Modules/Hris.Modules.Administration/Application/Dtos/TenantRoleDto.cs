namespace Hris.Modules.Administration.Application.Dtos;

public sealed record TenantRoleDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    string Status,
    Guid CreatedBy,
    DateTimeOffset CreatedOn,
    Guid? PublishedBy,
    DateTimeOffset? PublishedOn,
    IReadOnlyList<PermissionGrantDto> PermissionGrants);

public sealed record TenantRoleSummaryDto(Guid Id, string Name, string Status);

public sealed record PermissionGrantDto(Guid Id, string Permission, Guid AddedBy, DateTimeOffset AddedOn);
