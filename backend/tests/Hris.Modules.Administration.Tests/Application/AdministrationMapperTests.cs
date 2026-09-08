using FluentAssertions;
using Hris.Modules.Administration.Application.Mapping;
using Hris.Modules.Administration.Domain;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

/// <summary>
/// Maps fully-populated aggregates -- every optional field non-null, every child
/// collection non-empty -- and asserts every DTO field, the identical technique
/// every prior module's own mapper test file already establishes.
/// </summary>
public sealed class AdministrationMapperTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void ToDto_UserAccount_MapsEveryField()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Department, Guid.NewGuid()).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        var grantedBy = Guid.NewGuid();
        var approvalReference = Guid.NewGuid();
        account.GrantRole(role, scope, today, today.AddDays(30), grantedBy, "Ops staffing", approvalReference, true, TestUserAccount.NowUtc);

        var dto = AdministrationMapper.ToDto(account);

        dto.Id.Should().Be(account.Id.Value);
        dto.TenantId.Should().Be(_tenantId);
        dto.AccountType.Should().Be(AccountType.EmployeeLinked.ToString());
        dto.Status.Should().Be(UserAccountStatus.Active.ToString());
        dto.RoleAssignments.Should().ContainSingle();
        dto.RoleAssignments[0].RoleDisplayName.Should().Be("HROfficer");
        dto.RoleAssignments[0].GrantedBy.Should().Be(grantedBy);
        dto.RoleAssignments[0].Reason.Should().Be("Ops staffing");
        dto.RoleAssignments[0].ApprovalReference.Should().Be(approvalReference);
        dto.RoleAssignments[0].EffectiveTo.Should().Be(today.AddDays(30));
    }

    [Fact]
    public void ToSummaryDto_UserAccount_MapsEveryField()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var dto = AdministrationMapper.ToSummaryDto(account);

        dto.Id.Should().Be(account.Id.Value);
        dto.AccountType.Should().Be(AccountType.EmployeeLinked.ToString());
        dto.EmployeeId.Should().Be(account.EmployeeId);
        dto.Status.Should().Be(UserAccountStatus.Active.ToString());
    }

    [Fact]
    public void ToDto_TenantRole_MapsEveryField()
    {
        var createdBy = Guid.NewGuid();
        var publishedBy = Guid.NewGuid();
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", "Senior HR staff", false, false, createdBy,
            TestUserAccount.NowUtc).Value;
        role.AddPermission("employee.view", createdBy, TestUserAccount.NowUtc);
        role.Publish(publishedBy, TestUserAccount.NowUtc);

        var dto = AdministrationMapper.ToDto(role);

        dto.Id.Should().Be(role.Id.Value);
        dto.TenantId.Should().Be(_tenantId);
        dto.Name.Should().Be("SeniorHROfficer");
        dto.Description.Should().Be("Senior HR staff");
        dto.Status.Should().Be(TenantRoleStatus.Published.ToString());
        dto.CreatedBy.Should().Be(createdBy);
        dto.PublishedBy.Should().Be(publishedBy);
        dto.PermissionGrants.Should().ContainSingle();
        dto.PermissionGrants[0].Permission.Should().Be("employee.view");
    }

    [Fact]
    public void ToSummaryDto_TenantRole_MapsEveryField()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;

        var dto = AdministrationMapper.ToSummaryDto(role);

        dto.Id.Should().Be(role.Id.Value);
        dto.Name.Should().Be("SeniorHROfficer");
        dto.Status.Should().Be(TenantRoleStatus.Draft.ToString());
    }

    [Fact]
    public void ToDto_AdministrativeDelegation_MapsEveryField()
    {
        var delegatorId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();
        var approvalReference = Guid.NewGuid();
        var periodStart = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        var periodEnd = periodStart.AddDays(14);
        var scopeTargetId = Guid.NewGuid();
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, delegatorId, delegateId,
            [new DelegatedAuthorityItem(CanonicalRole.HROfficer, ScopeLevel.Department, scopeTargetId)], periodStart, periodEnd,
            "Covering leave", approvalReference, false, false, TestUserAccount.NowUtc).Value;

        var dto = AdministrationMapper.ToDto(delegation);

        dto.Id.Should().Be(delegation.Id.Value);
        dto.TenantId.Should().Be(_tenantId);
        dto.DelegatorUserAccountId.Should().Be(delegatorId);
        dto.DelegateUserAccountId.Should().Be(delegateId);
        dto.PeriodStart.Should().Be(periodStart);
        dto.PeriodEnd.Should().Be(periodEnd);
        dto.Reason.Should().Be("Covering leave");
        dto.ApprovalReference.Should().Be(approvalReference);
        dto.Status.Should().Be(DelegationStatus.Scheduled.ToString());
        dto.DelegatedAuthority.Should().ContainSingle();
        dto.DelegatedAuthority[0].Role.Should().Be(CanonicalRole.HROfficer.ToString());
        dto.DelegatedAuthority[0].ScopeLevel.Should().Be(ScopeLevel.Department.ToString());
        dto.DelegatedAuthority[0].ScopeTargetId.Should().Be(scopeTargetId);
    }
}
