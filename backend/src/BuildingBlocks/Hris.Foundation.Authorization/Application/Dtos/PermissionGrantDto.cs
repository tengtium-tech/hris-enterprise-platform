namespace Hris.Foundation.Authorization.Application.Dtos;

/// <summary>The read-side shape of a <see cref="Domain.RolePermissionGrant"/>.</summary>
public sealed record PermissionGrantDto(
    Guid Id,
    string Role,
    string ResourceType,
    string Action,
    bool IsActive);
