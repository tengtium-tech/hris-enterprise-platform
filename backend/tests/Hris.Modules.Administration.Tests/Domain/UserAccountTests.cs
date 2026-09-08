using FluentAssertions;
using Hris.Modules.Administration.Domain;
using Xunit;

namespace Hris.Modules.Administration.Tests.Domain;

public sealed class UserAccountTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_EmployeeLinked_WithEmployeeId_Succeeds()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.EmployeeLinked, Guid.NewGuid(), null, null,
            Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserAccountStatus.Pending);
    }

    [Fact]
    public void Create_EmployeeLinked_WithoutEmployeeId_Fails()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.EmployeeLinked, null, null, null, Guid.NewGuid(),
            TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.EmployeeIdRequiredForEmployeeLinkedAccount);
    }

    [Fact]
    public void Create_External_WithoutExpiryDate_Fails()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.External, null, null, null, Guid.NewGuid(),
            TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.ExpiryDateRequiredForExternalAccount);
    }

    [Fact]
    public void Create_External_WithExpiryDate_Succeeds()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.External, null, new DateOnly(2027, 1, 1), null,
            Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Service_WithoutOwner_Fails()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.Service, null, null, null, Guid.NewGuid(),
            TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.OwnerRequiredForServiceAccount);
    }

    [Fact]
    public void Create_Service_WithOwner_Succeeds()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.Service, null, null, Guid.NewGuid(), Guid.NewGuid(),
            TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_External_WithEmployeeId_Fails()
    {
        var result = UserAccount.Create(
            new UserAccountId(Guid.NewGuid()), _tenantId, AccountType.External, Guid.NewGuid(), new DateOnly(2027, 1, 1), null,
            Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.EmployeeIdProhibitedForNonEmployeeLinkedAccount);
    }

    [Fact]
    public void Activate_FromPending_Succeeds()
    {
        var account = TestUserAccount.CreateEmployeeLinked(_tenantId);

        var result = account.Activate(TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Active);
    }

    [Fact]
    public void Activate_WhenNotPending_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var result = account.Activate(TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.AccountNotPending);
    }

    [Fact]
    public void Suspend_FromActive_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var result = account.Suspend("Investigation", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Suspended);
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_IsIdempotent()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        account.Suspend("Investigation", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        var result = account.Suspend("Investigation again", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Suspended);
    }

    [Fact]
    public void Suspend_WhenPending_Fails()
    {
        var account = TestUserAccount.CreateEmployeeLinked(_tenantId);

        var result = account.Suspend("Reason", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.AccountNotActive);
    }

    [Fact]
    public void Suspend_WhenLastTenantAdministrator_Fails()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);

        var result = account.Suspend("Leaving", Guid.NewGuid(), true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.CannotRemoveLastTenantAdministrator);
    }

    [Fact]
    public void Reinstate_FromSuspended_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        account.Suspend(null, Guid.NewGuid(), false, TestUserAccount.NowUtc);

        var result = account.Reinstate(TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Active);
    }

    [Fact]
    public void Reinstate_WhenNotSuspended_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var result = account.Reinstate(TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.AccountNotSuspended);
    }

    [Fact]
    public void Deprovision_RevokesAllActiveAssignments()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Department, Guid.NewGuid()).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Ops staffing", null, true, TestUserAccount.NowUtc);

        var result = account.Deprovision("Termination", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Deprovisioned);
        account.RoleAssignments[0].RevokedOn.Should().NotBeNull();
    }

    [Fact]
    public void Deprovision_WhenAlreadyDeprovisioned_IsIdempotent()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        account.Deprovision("Termination", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        var result = account.Deprovision("Termination again", Guid.NewGuid(), false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Deprovision_WhenLastTenantAdministrator_Fails()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);

        var result = account.Deprovision("Leaving", Guid.NewGuid(), true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.CannotRemoveLastTenantAdministrator);
    }

    [Fact]
    public void Deprovision_WhenLastAdminFlagSetButDoesNotHoldTheRole_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var result = account.Deprovision("Leaving", Guid.NewGuid(), true, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void GrantRole_WithValidData_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Department, Guid.NewGuid()).Value;

        var result = account.GrantRole(
            role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Ops staffing", null, true,
            TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.RoleAssignments.Should().ContainSingle();
    }

    [Fact]
    public void GrantRole_SelfGrant_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;

        var result = account.GrantRole(
            role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, account.Id.Value, "Self-service", null,
            true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.SelfGrantProhibited);
    }

    [Fact]
    public void GrantRole_WithoutSufficientAuthority_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.SystemAdministrator);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;

        var result = account.GrantRole(
            role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Escalation", null,
            false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.InsufficientAuthorityToGrant);
    }

    [Fact]
    public void GrantRole_WithoutReason_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;

        var result = account.GrantRole(
            role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), null, null, true,
            TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.GrantReasonRequired);
    }

    [Fact]
    public void GrantRole_DuplicateActiveAssignment_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, scope, today, null, Guid.NewGuid(), "First", null, true, TestUserAccount.NowUtc);

        var result = account.GrantRole(role, scope, today, null, Guid.NewGuid(), "Second", null, true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DuplicateActiveAssignment);
    }

    [Fact]
    public void GrantRole_ProhibitedCombination_AuditorWithMutationRole_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        var tenantScope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.Auditor), tenantScope, today, null, Guid.NewGuid(), "Audit", null, true,
            TestUserAccount.NowUtc);

        var result = account.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HROfficer), tenantScope, today, null, Guid.NewGuid(), "Ops", null, true,
            TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.ProhibitedRoleCombination);
    }

    [Fact]
    public void GrantRole_ProhibitedCombination_SystemAdministratorWithHRManager_Fails()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        var tenantScope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;

        var result = account.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HRManager), tenantScope, today, null, Guid.NewGuid(), "Escalation", null, true,
            TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.ProhibitedRoleCombination);
    }

    [Fact]
    public void GrantRole_AuditorWithExecutive_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        var tenantScope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.Auditor), tenantScope, today, null, Guid.NewGuid(), "Audit", null, true,
            TestUserAccount.NowUtc);

        var result = account.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.Executive), tenantScope, today, null, Guid.NewGuid(), "Reporting", null, true,
            TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RevokeRole_WithExistingAssignment_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Ops", null, true, TestUserAccount.NowUtc);
        var assignmentId = account.RoleAssignments[0].Id.Value;

        var result = account.RevokeRole(assignmentId, Guid.NewGuid(), "No longer needed", false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.RoleAssignments[0].RevokedOn.Should().NotBeNull();
    }

    [Fact]
    public void RevokeRole_NotFound_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var result = account.RevokeRole(Guid.NewGuid(), Guid.NewGuid(), "Reason", false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleAssignmentNotFound);
    }

    [Fact]
    public void RevokeRole_AlreadyRevoked_IsIdempotent()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Ops", null, true, TestUserAccount.NowUtc);
        var assignmentId = account.RoleAssignments[0].Id.Value;
        account.RevokeRole(assignmentId, Guid.NewGuid(), "First revoke", false, TestUserAccount.NowUtc);

        var result = account.RevokeRole(assignmentId, Guid.NewGuid(), "Second revoke", false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RevokeRole_LastTenantAdministrator_Fails()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        var assignmentId = account.RoleAssignments[0].Id.Value;

        var result = account.RevokeRole(assignmentId, Guid.NewGuid(), "Leaving", true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.CannotRemoveLastTenantAdministrator);
    }

    [Fact]
    public void RevokeRole_LastAdminFlagSetButAssignmentIsNotSystemAdministratorAtTenant_Succeeds()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        account.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HROfficer), OrganizationalScope.Create(ScopeLevel.Tenant, null).Value,
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Additional duties", null, true,
            TestUserAccount.NowUtc);
        var hrOfficerAssignmentId = account.RoleAssignments.Single(a => a.Role.CanonicalRole == CanonicalRole.HROfficer).Id.Value;

        var result = account.RevokeRole(hrOfficerAssignmentId, Guid.NewGuid(), "No longer needed", true, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ExpireAssignment_WhenAlreadyExpired_IsIdempotent()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Temp", null, true, TestUserAccount.NowUtc);
        var assignmentId = account.RoleAssignments[0].Id.Value;
        account.ExpireAssignment(assignmentId, TestUserAccount.NowUtc);

        var result = account.ExpireAssignment(assignmentId, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void HoldsEffectiveSystemAdministratorAtTenantScope_WhenHeld_ReturnsTrue()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);

        account.HoldsEffectiveSystemAdministratorAtTenantScope(DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime)).Should().BeTrue();
    }

    [Fact]
    public void HoldsEffectiveSystemAdministratorAtTenantScope_WhenNotHeld_ReturnsFalse()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        account.HoldsEffectiveSystemAdministratorAtTenantScope(DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime)).Should().BeFalse();
    }

    [Fact]
    public void HoldsEffectiveSystemAdministratorAtTenantScope_WhenRevoked_ReturnsFalse()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        var assignmentId = account.RoleAssignments[0].Id.Value;
        account.RevokeRole(assignmentId, Guid.NewGuid(), "Stepping down", false, TestUserAccount.NowUtc);

        account.HoldsEffectiveSystemAdministratorAtTenantScope(DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime)).Should().BeFalse();
    }

    [Fact]
    public void HoldsRoleAtOrBroaderThan_WhenAssignmentRevoked_ReturnsFalse()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, scope, today, null, Guid.NewGuid(), "Ops", null, true, TestUserAccount.NowUtc);
        account.RevokeRole(account.RoleAssignments[0].Id.Value, Guid.NewGuid(), "Reassigned", false, TestUserAccount.NowUtc);

        account.HoldsRoleAtOrBroaderThan(role, scope, today).Should().BeFalse();
    }

    [Fact]
    public void ExpireAssignment_WithExistingAssignment_Succeeds()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var effectiveFrom = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, scope, effectiveFrom, effectiveFrom, Guid.NewGuid(), "Temp", null, true, TestUserAccount.NowUtc);
        var assignmentId = account.RoleAssignments[0].Id.Value;

        var result = account.ExpireAssignment(assignmentId, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        account.RoleAssignments[0].IsExpired.Should().BeTrue();
    }

    [Fact]
    public void ExpireAssignment_NotFound_Fails()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var result = account.ExpireAssignment(Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleAssignmentNotFound);
    }

    [Theory]
    [InlineData(CanonicalRole.Auditor, CanonicalRole.HROfficer, true)]
    [InlineData(CanonicalRole.Auditor, CanonicalRole.Executive, false)]
    [InlineData(CanonicalRole.SystemAdministrator, CanonicalRole.HRManager, true)]
    [InlineData(CanonicalRole.PayrollOfficer, CanonicalRole.HRManager, true)]
    [InlineData(CanonicalRole.HROfficer, CanonicalRole.PeopleManager, false)]
    public void ViolatesSeparationOfDuties_ChecksProhibitedCombinations(CanonicalRole first, CanonicalRole second, bool expected)
    {
        var result = UserAccount.ViolatesSeparationOfDuties([first, second]);

        result.Should().Be(expected);
    }

    [Fact]
    public void HoldsRoleAtOrBroaderThan_WithBroaderHeldScope_ReturnsTrue()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var tenantScope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, tenantScope, today, null, Guid.NewGuid(), "Tenant-wide", null, true, TestUserAccount.NowUtc);

        var narrowerScope = OrganizationalScope.Create(ScopeLevel.Department, Guid.NewGuid()).Value;

        account.HoldsRoleAtOrBroaderThan(role, narrowerScope, today).Should().BeTrue();
    }

    [Fact]
    public void HoldsRoleAtOrBroaderThan_WithNarrowerHeldScope_ReturnsFalse()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var departmentScope = OrganizationalScope.Create(ScopeLevel.Department, Guid.NewGuid()).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, departmentScope, today, null, Guid.NewGuid(), "Dept only", null, true, TestUserAccount.NowUtc);

        var tenantScope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;

        account.HoldsRoleAtOrBroaderThan(role, tenantScope, today).Should().BeFalse();
    }
}
