using FluentAssertions;
using Hris.Modules.Administration.Application.Commands;
using Hris.Modules.Administration.Application.Validators;
using Hris.Modules.Administration.Domain;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

public sealed class AdministrationCommandValidatorsTests
{
    [Fact]
    public void ProvisionUserAccountCommandValidator_Valid_Passes()
    {
        var result = new ProvisionUserAccountCommandValidator().Validate(
            new ProvisionUserAccountCommand(Guid.NewGuid(), AccountType.EmployeeLinked, Guid.NewGuid(), null, null, Guid.NewGuid(), null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProvisionUserAccountCommandValidator_WithoutTenantId_Fails()
    {
        var result = new ProvisionUserAccountCommandValidator().Validate(
            new ProvisionUserAccountCommand(Guid.Empty, AccountType.EmployeeLinked, Guid.NewGuid(), null, null, Guid.NewGuid(), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateUserAccountCommandValidator_Valid_Passes()
    {
        var result = new ActivateUserAccountCommandValidator().Validate(new ActivateUserAccountCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SuspendUserAccountCommandValidator_WithoutSuspendedBy_Fails()
    {
        var result = new SuspendUserAccountCommandValidator().Validate(
            new SuspendUserAccountCommand(Guid.NewGuid(), Guid.NewGuid(), "Reason", Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReinstateUserAccountCommandValidator_Valid_Passes()
    {
        var result = new ReinstateUserAccountCommandValidator().Validate(new ReinstateUserAccountCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeprovisionUserAccountCommandValidator_Valid_Passes()
    {
        var result = new DeprovisionUserAccountCommandValidator().Validate(
            new DeprovisionUserAccountCommand(Guid.NewGuid(), Guid.NewGuid(), "Reason", Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GrantRoleCommandValidator_WithoutReason_Fails()
    {
        var result = new GrantRoleCommandValidator().Validate(
            new GrantRoleCommand(
                Guid.NewGuid(), Guid.NewGuid(), RoleKind.Canonical, CanonicalRole.HROfficer, null, ScopeLevel.Tenant, null,
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RevokeRoleCommandValidator_Valid_Passes()
    {
        var result = new RevokeRoleCommandValidator().Validate(
            new RevokeRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Reason"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExpireRoleAssignmentCommandValidator_Valid_Passes()
    {
        var result = new ExpireRoleAssignmentCommandValidator().Validate(
            new ExpireRoleAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DefineTenantRoleCommandValidator_WithoutName_Fails()
    {
        var result = new DefineTenantRoleCommandValidator().Validate(
            new DefineTenantRoleCommand(Guid.NewGuid(), null, null, Guid.NewGuid(), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddPermissionToTenantRoleCommandValidator_WithoutPermission_Fails()
    {
        var result = new AddPermissionToTenantRoleCommandValidator().Validate(
            new AddPermissionToTenantRoleCommand(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemovePermissionFromTenantRoleCommandValidator_Valid_Passes()
    {
        var result = new RemovePermissionFromTenantRoleCommandValidator().Validate(
            new RemovePermissionFromTenantRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PublishTenantRoleCommandValidator_Valid_Passes()
    {
        var result = new PublishTenantRoleCommandValidator().Validate(
            new PublishTenantRoleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangeTenantRolePermissionsCommandValidator_WithoutReason_Fails()
    {
        var result = new ChangeTenantRolePermissionsCommandValidator().Validate(
            new ChangeTenantRolePermissionsCommand(Guid.NewGuid(), Guid.NewGuid(), [], [], Guid.NewGuid(), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeprecateTenantRoleCommandValidator_Valid_Passes()
    {
        var result = new DeprecateTenantRoleCommandValidator().Validate(new DeprecateTenantRoleCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteTenantRoleCommandValidator_Valid_Passes()
    {
        var result = new DeleteTenantRoleCommandValidator().Validate(new DeleteTenantRoleCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateDelegationCommandValidator_WithEmptyAuthority_Fails()
    {
        var result = new CreateDelegationCommandValidator().Validate(
            new CreateDelegationCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [], DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime),
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), "Reason", null, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateDelegationCommandValidator_Valid_Passes()
    {
        var result = new ActivateDelegationCommandValidator().Validate(new ActivateDelegationCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExpireDelegationCommandValidator_Valid_Passes()
    {
        var result = new ExpireDelegationCommandValidator().Validate(new ExpireDelegationCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RevokeDelegationCommandValidator_WithoutRevokedBy_Fails()
    {
        var result = new RevokeDelegationCommandValidator().Validate(
            new RevokeDelegationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Reason"));

        result.IsValid.Should().BeFalse();
    }
}
