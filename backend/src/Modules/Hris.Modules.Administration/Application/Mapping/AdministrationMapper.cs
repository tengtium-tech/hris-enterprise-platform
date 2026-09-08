using Hris.Modules.Administration.Application.Dtos;
using Hris.Modules.Administration.Domain;

namespace Hris.Modules.Administration.Application.Mapping;

internal static class AdministrationMapper
{
    public static UserAccountDto ToDto(UserAccount account)
    {
        return new UserAccountDto(
            account.Id.Value,
            account.TenantId,
            account.AccountType.ToString(),
            account.EmployeeId,
            account.Status.ToString(),
            account.ExpiryDate,
            account.ServiceAccountOwnerId,
            account.ProvisionedBy,
            account.ProvisionedOn,
            account.RoleAssignments.Select(ToDto).ToList());
    }

    public static UserAccountSummaryDto ToSummaryDto(UserAccount account) =>
        new(account.Id.Value, account.AccountType.ToString(), account.EmployeeId, account.Status.ToString());

    private static RoleAssignmentDto ToDto(RoleAssignment assignment)
    {
        return new RoleAssignmentDto(
            assignment.Id.Value, assignment.Role.DisplayName, assignment.Scope.ToString(), assignment.EffectiveFrom,
            assignment.EffectiveTo, assignment.GrantedBy, assignment.GrantedOn, assignment.Reason.Value,
            assignment.ApprovalReference, assignment.RevokedBy, assignment.RevokedOn, assignment.IsExpired);
    }

    public static TenantRoleDto ToDto(TenantRole role)
    {
        return new TenantRoleDto(
            role.Id.Value, role.TenantId, role.Name, role.Description, role.Status.ToString(), role.CreatedBy, role.CreatedOn,
            role.PublishedBy, role.PublishedOn, role.PermissionGrants.Select(ToDto).ToList());
    }

    public static TenantRoleSummaryDto ToSummaryDto(TenantRole role) => new(role.Id.Value, role.Name, role.Status.ToString());

    private static PermissionGrantDto ToDto(PermissionGrant grant) =>
        new(grant.Id.Value, grant.Permission.Value, grant.AddedBy, grant.AddedOn);

    public static AdministrativeDelegationDto ToDto(AdministrativeDelegation delegation)
    {
        return new AdministrativeDelegationDto(
            delegation.Id.Value, delegation.TenantId, delegation.DelegatorUserAccountId, delegation.DelegateUserAccountId,
            delegation.DelegatedAuthority.Select(ToDto).ToList(), delegation.Period.Start, delegation.Period.End,
            delegation.Reason, delegation.ApprovalReference, delegation.Status.ToString(), delegation.CreatedOn);
    }

    private static DelegatedAuthorityItemDto ToDto(DelegatedAuthorityItem item) =>
        new(item.Role.ToString(), item.ScopeLevel.ToString(), item.ScopeTargetId);
}
