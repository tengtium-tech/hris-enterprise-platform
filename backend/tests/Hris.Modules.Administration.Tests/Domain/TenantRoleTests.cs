using FluentAssertions;
using Hris.Modules.Administration.Domain;
using Xunit;

namespace Hris.Modules.Administration.Tests.Domain;

public sealed class TenantRoleTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static TenantRole CreateDraft() =>
        TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", "Senior HR staff", false, false, Guid.NewGuid(),
            TestUserAccount.NowUtc).Value;

    [Fact]
    public void Create_WithValidName_Succeeds()
    {
        var result = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TenantRoleStatus.Draft);
    }

    [Fact]
    public void Create_WithoutName_Fails()
    {
        var result = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, null, null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleNameRequired);
    }

    [Fact]
    public void Create_CollidingWithCanonicalRole_Fails()
    {
        var result = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "HRManager", null, true, false, Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleNameCollidesWithCanonicalRole);
    }

    [Fact]
    public void Create_AlreadyExistsInTenant_Fails()
    {
        var result = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, true, Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleNameCollidesWithCanonicalRole);
    }

    [Fact]
    public void Create_WithNameTooLong_Fails()
    {
        var result = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, new string('A', 101), null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleNameTooLong);
    }

    [Fact]
    public void AddPermission_WhileDraft_Succeeds()
    {
        var role = CreateDraft();

        var result = role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().ContainSingle();
    }

    [Fact]
    public void AddPermission_Duplicate_Fails()
    {
        var role = CreateDraft();
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DuplicatePermissionGrant);
    }

    [Fact]
    public void RemovePermission_WithExistingGrant_Succeeds()
    {
        var role = CreateDraft();
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);
        var grantId = role.PermissionGrants[0].Id.Value;

        var result = role.RemovePermission(grantId);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().BeEmpty();
    }

    [Fact]
    public void RemovePermission_NotFound_Fails()
    {
        var role = CreateDraft();

        var result = role.RemovePermission(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.PermissionGrantNotFound);
    }

    [Fact]
    public void RemovePermission_WhenPublished_Fails()
    {
        var role = CreateDraft();
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);
        var grantId = role.PermissionGrants[0].Id.Value;
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.RemovePermission(grantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotDraft);
    }

    [Fact]
    public void AddPermission_WithInvalidFormat_Fails()
    {
        var role = CreateDraft();

        var result = role.AddPermission(null, Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.PermissionReferenceRequired);
    }

    [Fact]
    public void Publish_FromDraft_Succeeds()
    {
        var role = CreateDraft();
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        role.Status.Should().Be(TenantRoleStatus.Published);
    }

    [Fact]
    public void Publish_WhenNotDraft_Fails()
    {
        var role = CreateDraft();
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotDraft);
    }

    [Fact]
    public void AddPermission_WhenPublished_Fails()
    {
        var role = CreateDraft();
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotDraft);
    }

    [Fact]
    public void ChangePermissions_WhenPublished_Succeeds()
    {
        var role = CreateDraft();
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);
        var existingGrantId = role.PermissionGrants[0].Id.Value;

        var result = role.ChangePermissions(
            ["employee.update"], [existingGrantId], Guid.NewGuid(), "Expanding responsibilities", TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().ContainSingle(g => g.Permission.Value == "employee.update");
    }

    [Fact]
    public void ChangePermissions_WhenDraft_Fails()
    {
        var role = CreateDraft();

        var result = role.ChangePermissions([], [], Guid.NewGuid(), "Reason", TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotPublished);
    }

    [Fact]
    public void ChangePermissions_WithoutReason_Fails()
    {
        var role = CreateDraft();
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.ChangePermissions([], [], Guid.NewGuid(), null, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.GrantReasonRequired);
    }

    [Fact]
    public void ChangePermissions_WithInvalidPermissionFormat_Fails()
    {
        var role = CreateDraft();
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.ChangePermissions([null], [], Guid.NewGuid(), "Reason", TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.PermissionReferenceRequired);
    }

    [Fact]
    public void ChangePermissions_SkipsAlreadyComposedPermission()
    {
        var role = CreateDraft();
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.ChangePermissions(["employee.view"], [], Guid.NewGuid(), "Reason", TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().ContainSingle();
    }

    [Fact]
    public void ChangePermissions_SkipsUnknownGrantIdOnRemoval()
    {
        var role = CreateDraft();
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.ChangePermissions([], [Guid.NewGuid()], Guid.NewGuid(), "Reason", TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Deprecate_FromPublished_Succeeds()
    {
        var role = CreateDraft();
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);

        var result = role.Deprecate(TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        role.Status.Should().Be(TenantRoleStatus.Deprecated);
    }

    [Fact]
    public void Deprecate_WhenDraft_Fails()
    {
        var role = CreateDraft();

        var result = role.Deprecate(TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotPublished);
    }

    [Fact]
    public void EnsureDeletable_WhenReferenced_Fails()
    {
        var role = CreateDraft();

        var result = role.EnsureDeletable(isReferencedByActiveAssignment: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleInUseCannotBeDeleted);
    }

    [Fact]
    public void EnsureDeletable_WhenNotReferenced_Succeeds()
    {
        var role = CreateDraft();

        var result = role.EnsureDeletable(isReferencedByActiveAssignment: false);

        result.IsSuccess.Should().BeTrue();
    }
}
