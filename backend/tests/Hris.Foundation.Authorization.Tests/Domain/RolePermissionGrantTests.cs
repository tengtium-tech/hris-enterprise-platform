using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

public sealed class RolePermissionGrantTests
{
    [Fact]
    public void Create_Succeeds_WithValidInput()
    {
        var permission = TestData.Permission();

        var result = RolePermissionGrant.Create(Role.HRManager, permission, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(Role.HRManager);
        result.Value.Permission.Should().Be(permission);
        result.Value.GrantedAtUtc.Should().Be(TestData.NowUtc);
        result.Value.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesPermissionGrantedEvent_WithCorrectData()
    {
        var permission = TestData.Permission();

        var grant = TestData.Grant(Role.PayrollOfficer, permission);

        grant.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PermissionGranted>()
            .Which.Should().BeEquivalentTo(new
            {
                GrantId = grant.Id,
                Role = Role.PayrollOfficer,
                Permission = permission,
            });
    }

    [Theory]
    [InlineData(PermissionAction.Create)]
    [InlineData(PermissionAction.Update)]
    [InlineData(PermissionAction.Delete)]
    [InlineData(PermissionAction.Approve)]
    [InlineData(PermissionAction.Reject)]
    [InlineData(PermissionAction.Import)]
    [InlineData(PermissionAction.Configure)]
    public void Create_Fails_WhenAuditorRoleHoldsAMutationPermission(PermissionAction mutatingAction)
    {
        var permission = TestData.Permission(action: mutatingAction);

        var result = RolePermissionGrant.Create(Role.Auditor, permission, TestData.NowUtc);

        result.IsFailure.Should().BeTrue("CTR-AUT-003 prohibits the Auditor role from holding any mutation permission");
        result.Error.Should().Be(AuthorizationErrors.AuditorCannotHoldMutationPermission);
    }

    [Theory]
    [InlineData(PermissionAction.Read)]
    [InlineData(PermissionAction.Export)]
    [InlineData(PermissionAction.Execute)]
    public void Create_Succeeds_WhenAuditorRoleHoldsANonMutationPermission(PermissionAction nonMutatingAction)
    {
        var permission = TestData.Permission(action: nonMutatingAction);

        var result = RolePermissionGrant.Create(Role.Auditor, permission, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue("CTR-AUT-003 only prohibits mutation permissions, not read-only ones");
    }

    [Fact]
    public void Create_Succeeds_WhenANonAuditorRoleHoldsAMutationPermission()
    {
        var permission = TestData.Permission(action: PermissionAction.Delete);

        var result = RolePermissionGrant.Create(Role.HRManager, permission, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue("the mutation-permission restriction is specific to the Auditor role");
    }

    [Fact]
    public void IsActive_ReturnsTrue_WhenNotRevoked()
    {
        var grant = TestData.Grant();

        grant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenRevoked()
    {
        var grant = TestData.Grant();
        grant.Revoke(TestData.NowUtc);

        grant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtUtc_AndRaisesPermissionRevokedEvent()
    {
        var grant = TestData.Grant();
        var revokedAt = TestData.NowUtc.AddHours(1);

        var result = grant.Revoke(revokedAt);

        result.IsSuccess.Should().BeTrue();
        grant.RevokedAtUtc.Should().Be(revokedAt);
        grant.DomainEvents.Should().ContainSingle(e => e is PermissionRevoked);
    }

    [Fact]
    public void Revoke_IsIdempotent_AndDoesNotRaiseASecondEvent_WhenAlreadyRevoked()
    {
        var grant = TestData.Grant();
        var firstRevokedAt = TestData.NowUtc.AddHours(1);
        var secondRevokedAt = TestData.NowUtc.AddHours(2);

        grant.Revoke(firstRevokedAt);
        var secondResult = grant.Revoke(secondRevokedAt);

        secondResult.IsSuccess.Should().BeTrue();
        grant.RevokedAtUtc.Should().Be(firstRevokedAt, "a retried revoke must not overwrite the original revocation time");
        grant.DomainEvents.Should().ContainSingle(e => e is PermissionRevoked, "a retried revoke must not raise a second event");
    }
}
