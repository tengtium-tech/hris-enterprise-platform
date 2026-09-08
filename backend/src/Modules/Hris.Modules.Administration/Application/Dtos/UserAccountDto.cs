namespace Hris.Modules.Administration.Application.Dtos;

public sealed record UserAccountDto(
    Guid Id,
    Guid TenantId,
    string AccountType,
    Guid? EmployeeId,
    string Status,
    DateOnly? ExpiryDate,
    Guid? ServiceAccountOwnerId,
    Guid ProvisionedBy,
    DateTimeOffset ProvisionedOn,
    IReadOnlyList<RoleAssignmentDto> RoleAssignments);

public sealed record UserAccountSummaryDto(Guid Id, string AccountType, Guid? EmployeeId, string Status);

public sealed record RoleAssignmentDto(
    Guid Id,
    string RoleDisplayName,
    string Scope,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid GrantedBy,
    DateTimeOffset GrantedOn,
    string Reason,
    Guid? ApprovalReference,
    Guid? RevokedBy,
    DateTimeOffset? RevokedOn,
    bool IsExpired);
