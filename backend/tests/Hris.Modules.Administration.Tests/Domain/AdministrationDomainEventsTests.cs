using FluentAssertions;
using Hris.Modules.Administration.Domain;
using Xunit;

namespace Hris.Modules.Administration.Tests.Domain;

/// <summary>
/// Constructs every Domain Event record directly and asserts every property, the
/// identical technique every prior module's own domain-events test file already
/// establishes to close the coverage gap `record` types with unread
/// auto-generated getters leave behind.
/// </summary>
public sealed class AdministrationDomainEventsTests
{
    private static readonly Guid _eventId = Guid.NewGuid();
    private static readonly DateTimeOffset _now = TestUserAccount.NowUtc;
    private static readonly UserAccountId _userAccountId = new(Guid.NewGuid());
    private static readonly TenantRoleId _tenantRoleId = new(Guid.NewGuid());
    private static readonly DelegationId _delegationId = new(Guid.NewGuid());
    private static readonly RoleAssignmentId _roleAssignmentId = new(Guid.NewGuid());

    [Fact]
    public void UserAccountProvisioned_CarriesEveryProperty()
    {
        var tenantId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var provisionedBy = Guid.NewGuid();
        var evt = new UserAccountProvisioned(_eventId, _now, _userAccountId, tenantId, AccountType.EmployeeLinked, employeeId, provisionedBy);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.UserAccountId.Should().Be(_userAccountId);
        evt.TenantId.Should().Be(tenantId);
        evt.AccountType.Should().Be(AccountType.EmployeeLinked);
        evt.EmployeeId.Should().Be(employeeId);
        evt.ProvisionedBy.Should().Be(provisionedBy);
    }

    [Fact]
    public void UserAccountActivated_CarriesEveryProperty()
    {
        var evt = new UserAccountActivated(_eventId, _now, _userAccountId);

        evt.EventId.Should().Be(_eventId);
        evt.OccurredOnUtc.Should().Be(_now);
        evt.UserAccountId.Should().Be(_userAccountId);
    }

    [Fact]
    public void UserAccountSuspended_CarriesEveryProperty()
    {
        var suspendedBy = Guid.NewGuid();
        var evt = new UserAccountSuspended(_eventId, _now, _userAccountId, suspendedBy, "Investigation");

        evt.SuspendedBy.Should().Be(suspendedBy);
        evt.Reason.Should().Be("Investigation");
    }

    [Fact]
    public void UserAccountReinstated_CarriesEveryProperty()
    {
        var evt = new UserAccountReinstated(_eventId, _now, _userAccountId);

        evt.UserAccountId.Should().Be(_userAccountId);
    }

    [Fact]
    public void UserAccountDeprovisioned_CarriesEveryProperty()
    {
        var deprovisionedBy = Guid.NewGuid();
        var revokedIds = new List<Guid> { Guid.NewGuid() };
        var evt = new UserAccountDeprovisioned(_eventId, _now, _userAccountId, deprovisionedBy, "Termination", revokedIds);

        evt.DeprovisionedBy.Should().Be(deprovisionedBy);
        evt.Reason.Should().Be("Termination");
        evt.RevokedAssignmentIds.Should().BeEquivalentTo(revokedIds);
    }

    [Fact]
    public void RoleGranted_CarriesEveryProperty()
    {
        var grantedBy = Guid.NewGuid();
        var approvalReference = Guid.NewGuid();
        var effectiveFrom = DateOnly.FromDateTime(_now.UtcDateTime);
        var evt = new RoleGranted(
            _eventId, _now, _userAccountId, _roleAssignmentId, "HRManager", "Tenant", effectiveFrom, null, grantedBy, "Reason",
            approvalReference);

        evt.RoleAssignmentId.Should().Be(_roleAssignmentId);
        evt.Role.Should().Be("HRManager");
        evt.Scope.Should().Be("Tenant");
        evt.EffectiveFrom.Should().Be(effectiveFrom);
        evt.EffectiveTo.Should().BeNull();
        evt.GrantedBy.Should().Be(grantedBy);
        evt.Reason.Should().Be("Reason");
        evt.ApprovalReference.Should().Be(approvalReference);
    }

    [Fact]
    public void RoleRevoked_CarriesEveryProperty()
    {
        var revokedBy = Guid.NewGuid();
        var evt = new RoleRevoked(_eventId, _now, _userAccountId, _roleAssignmentId, "HRManager", "Tenant", revokedBy, "No longer needed");

        evt.RevokedBy.Should().Be(revokedBy);
        evt.Reason.Should().Be("No longer needed");
    }

    [Fact]
    public void RoleAssignmentExpired_CarriesEveryProperty()
    {
        var evt = new RoleAssignmentExpired(_eventId, _now, _userAccountId, _roleAssignmentId);

        evt.UserAccountId.Should().Be(_userAccountId);
        evt.RoleAssignmentId.Should().Be(_roleAssignmentId);
    }

    [Fact]
    public void TenantRoleDefined_CarriesEveryProperty()
    {
        var createdBy = Guid.NewGuid();
        var evt = new TenantRoleDefined(_eventId, _now, _tenantRoleId, createdBy);

        evt.TenantRoleId.Should().Be(_tenantRoleId);
        evt.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void TenantRolePublished_CarriesEveryProperty()
    {
        var publishedBy = Guid.NewGuid();
        var permissions = new List<string> { "employee.view" };
        var evt = new TenantRolePublished(_eventId, _now, _tenantRoleId, "SeniorHROfficer", permissions, publishedBy);

        evt.Name.Should().Be("SeniorHROfficer");
        evt.PermissionSet.Should().BeEquivalentTo(permissions);
        evt.PublishedBy.Should().Be(publishedBy);
    }

    [Fact]
    public void TenantRoleDeprecated_CarriesEveryProperty()
    {
        var evt = new TenantRoleDeprecated(_eventId, _now, _tenantRoleId);

        evt.TenantRoleId.Should().Be(_tenantRoleId);
    }

    [Fact]
    public void TenantRolePermissionsChanged_CarriesEveryProperty()
    {
        var changedBy = Guid.NewGuid();
        var added = new List<string> { "employee.update" };
        var removed = new List<string> { "employee.delete" };
        var evt = new TenantRolePermissionsChanged(_eventId, _now, _tenantRoleId, added, removed, changedBy, "Expanded scope");

        evt.AddedPermissions.Should().BeEquivalentTo(added);
        evt.RemovedPermissions.Should().BeEquivalentTo(removed);
        evt.ChangedBy.Should().Be(changedBy);
        evt.Reason.Should().Be("Expanded scope");
    }

    [Fact]
    public void AdministrativeDelegationCreated_CarriesEveryProperty()
    {
        var delegatorId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();
        var approvalReference = Guid.NewGuid();
        var evt = new AdministrativeDelegationCreated(_eventId, _now, _delegationId, delegatorId, delegateId, "Covering leave", approvalReference);

        evt.DelegationId.Should().Be(_delegationId);
        evt.DelegatorUserAccountId.Should().Be(delegatorId);
        evt.DelegateUserAccountId.Should().Be(delegateId);
        evt.Reason.Should().Be("Covering leave");
        evt.ApprovalReference.Should().Be(approvalReference);
    }

    [Fact]
    public void AdministrativeDelegationActivated_CarriesEveryProperty()
    {
        var evt = new AdministrativeDelegationActivated(_eventId, _now, _delegationId);

        evt.DelegationId.Should().Be(_delegationId);
    }

    [Fact]
    public void AdministrativeDelegationExpired_CarriesEveryProperty()
    {
        var evt = new AdministrativeDelegationExpired(_eventId, _now, _delegationId);

        evt.DelegationId.Should().Be(_delegationId);
    }

    [Fact]
    public void AdministrativeDelegationRevoked_CarriesEveryProperty()
    {
        var revokedBy = Guid.NewGuid();
        var evt = new AdministrativeDelegationRevoked(_eventId, _now, _delegationId, revokedBy, "No longer needed");

        evt.RevokedBy.Should().Be(revokedBy);
        evt.Reason.Should().Be("No longer needed");
    }
}
