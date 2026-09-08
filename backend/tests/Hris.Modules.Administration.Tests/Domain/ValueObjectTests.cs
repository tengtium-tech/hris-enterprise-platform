using FluentAssertions;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Modules.Administration.Tests.Domain;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData(ScopeLevel.Tenant, false)]
    [InlineData(ScopeLevel.ReportingLine, false)]
    [InlineData(ScopeLevel.Self, false)]
    [InlineData(ScopeLevel.LegalEntity, true)]
    [InlineData(ScopeLevel.BusinessUnit, true)]
    [InlineData(ScopeLevel.Department, true)]
    [InlineData(ScopeLevel.Team, true)]
    public void OrganizationalScope_RequiresTarget_OnlyForOrganizationalUnitLevels(ScopeLevel level, bool requiresTarget)
    {
        var withTarget = OrganizationalScope.Create(level, Guid.NewGuid());
        var withoutTarget = OrganizationalScope.Create(level, null);

        withTarget.IsSuccess.Should().Be(requiresTarget);
        withoutTarget.IsSuccess.Should().Be(!requiresTarget);
    }

    [Fact]
    public void OrganizationalScope_WithoutRequiredTarget_Fails()
    {
        var result = OrganizationalScope.Create(ScopeLevel.Department, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.ScopeTargetRequired);
    }

    [Fact]
    public void OrganizationalScope_WithProhibitedTarget_Fails()
    {
        var result = OrganizationalScope.Create(ScopeLevel.Tenant, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.ScopeTargetProhibited);
    }

    [Fact]
    public void OrganizationalScope_IsBroaderThanOrEqualTo_ComparesLevelOrdinal()
    {
        var tenant = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var department = OrganizationalScope.Create(ScopeLevel.Department, Guid.NewGuid()).Value;

        tenant.IsBroaderThanOrEqualTo(department).Should().BeTrue();
        department.IsBroaderThanOrEqualTo(tenant).Should().BeFalse();
    }

    [Fact]
    public void RoleReference_ForCanonical_Succeeds()
    {
        var role = RoleReference.ForCanonical(CanonicalRole.HRManager);

        role.Kind.Should().Be(RoleKind.Canonical);
        role.DisplayName.Should().Be("HRManager");
    }

    [Fact]
    public void RoleReference_ForTenantRole_WhenPublished_Succeeds()
    {
        var result = RoleReference.ForTenantRole(Guid.NewGuid(), "SeniorHROfficer", true);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("SeniorHROfficer");
    }

    [Fact]
    public void RoleReference_ForTenantRole_WhenNotPublished_Fails()
    {
        var result = RoleReference.ForTenantRole(Guid.NewGuid(), "SeniorHROfficer", false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole);
    }

    [Fact]
    public void GrantReason_WithValue_Succeeds()
    {
        var result = GrantReason.Create("Covering department restructuring");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void GrantReason_WhenEmpty_Fails()
    {
        var result = GrantReason.Create(string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.GrantReasonRequired);
    }

    [Fact]
    public void GrantReason_WhenTooLong_Fails()
    {
        var result = GrantReason.Create(new string('A', 501));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.GrantReasonRequired);
    }

    [Fact]
    public void PermissionReference_WithValue_Succeeds()
    {
        var result = PermissionReference.Create("employee.view");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PermissionReference_WhenEmpty_Fails()
    {
        var result = PermissionReference.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.PermissionReferenceRequired);
    }

    [Fact]
    public void PermissionReference_WhenTooLong_Fails()
    {
        var result = PermissionReference.Create(new string('A', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.PermissionReferenceRequired);
    }

    [Fact]
    public void RoleReference_ForTenantRole_WithoutName_Fails()
    {
        var result = RoleReference.ForTenantRole(Guid.NewGuid(), null, true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole);
    }

    [Fact]
    public void RoleAssignment_IsEffectiveOn_FutureEffectiveFrom_ReturnsFalse()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(Guid.NewGuid());
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, scope, today.AddDays(7), null, Guid.NewGuid(), "Future promotion", null, true, TestUserAccount.NowUtc);

        account.RoleAssignments[0].IsEffectiveOn(today).Should().BeFalse();
        account.RoleAssignments[0].IsEffectiveOn(today.AddDays(7)).Should().BeTrue();
    }

    [Fact]
    public void RoleAssignment_IsEffectiveOn_PastEffectiveTo_ReturnsFalse()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(Guid.NewGuid());
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        account.GrantRole(role, scope, today, today.AddDays(7), Guid.NewGuid(), "Temporary", null, true, TestUserAccount.NowUtc);

        account.RoleAssignments[0].IsEffectiveOn(today.AddDays(8)).Should().BeFalse();
        account.RoleAssignments[0].IsEffectiveOn(today.AddDays(3)).Should().BeTrue();
    }

    [Fact]
    public void DateRange_WithValidBounds_Succeeds()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 14);

        var result = DateRange.Create(start, end);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contains(new DateOnly(2026, 1, 7)).Should().BeTrue();
        result.Value.Contains(new DateOnly(2026, 2, 1)).Should().BeFalse();
    }

    [Fact]
    public void DateRange_WithEndBeforeStart_Fails()
    {
        var result = DateRange.Create(new DateOnly(2026, 1, 14), new DateOnly(2026, 1, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SharedKernelErrors.DateRangeEndBeforeStart);
    }
}
