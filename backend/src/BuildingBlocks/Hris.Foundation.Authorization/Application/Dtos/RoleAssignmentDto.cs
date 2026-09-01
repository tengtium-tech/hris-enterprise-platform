namespace Hris.Foundation.Authorization.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.RoleAssignment"/>, per the identical
/// primitive-only reasoning <c>UserAccountDto</c>/<c>ConfigurationSettingDto</c>
/// already state for their own query-side DTOs.
/// </summary>
public sealed record RoleAssignmentDto(
    Guid Id,
    Guid PrincipalId,
    string Role,
    string ScopeLevel,
    Guid ScopeId,
    string AssignmentType,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate,
    bool IsRevoked);
