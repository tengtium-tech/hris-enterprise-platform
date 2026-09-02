using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

public sealed class RoleAssignmentTests
{
    [Fact]
    public void Create_Succeeds_WithValidInput()
    {
        var principalId = TestData.NewPrincipalId();
        var scope = TestData.Scope();
        var grantedBy = TestData.NewPrincipalId();

        var result = RoleAssignment.Create(
            principalId, Role.HRManager, scope, RoleAssignmentType.Direct,
            TestData.Today, null, grantedBy, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.PrincipalId.Should().Be(principalId);
        result.Value.Role.Should().Be(Role.HRManager);
        result.Value.Scope.Should().Be(scope);
        result.Value.AssignmentType.Should().Be(RoleAssignmentType.Direct);
        result.Value.EffectiveDate.Should().Be(TestData.Today);
        result.Value.ExpirationDate.Should().BeNull();
        result.Value.GrantedByPrincipalId.Should().Be(grantedBy);
        result.Value.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_Fails_WhenExpirationBeforeEffectiveDate()
    {
        var result = RoleAssignment.Create(
            TestData.NewPrincipalId(), Role.HRManager, TestData.Scope(), RoleAssignmentType.Direct,
            effectiveDate: TestData.Today,
            expirationDate: TestData.Today.AddDays(-1),
            TestData.NewPrincipalId(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ExpirationBeforeEffectiveDate);
    }

    [Fact]
    public void Create_RaisesRoleAssignedEvent_WithCorrectData()
    {
        var principalId = TestData.NewPrincipalId();
        var scope = TestData.Scope();

        var assignment = TestData.RoleAssignment(Role.PayrollOfficer, scope, principalId: principalId);

        assignment.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RoleAssigned>()
            .Which.Should().BeEquivalentTo(new
            {
                RoleAssignmentId = assignment.Id,
                PrincipalId = principalId,
                Role = Role.PayrollOfficer,
                Scope = scope,
            });
    }

    [Fact]
    public void IsEffective_ReturnsTrue_OnTheEffectiveDateItself()
    {
        var assignment = TestData.RoleAssignment(effectiveDate: TestData.Today);

        assignment.IsEffective(TestData.Today).Should().BeTrue();
    }

    [Fact]
    public void IsEffective_ReturnsFalse_TheDayBeforeTheEffectiveDate()
    {
        var assignment = TestData.RoleAssignment(effectiveDate: TestData.Today);

        assignment.IsEffective(TestData.Today.AddDays(-1)).Should().BeFalse();
    }

    [Fact]
    public void IsEffective_ReturnsTrue_TheDayBeforeExpiration()
    {
        var assignment = TestData.RoleAssignment(
            effectiveDate: TestData.Today,
            expirationDate: TestData.Today.AddDays(10));

        assignment.IsEffective(TestData.Today.AddDays(9)).Should().BeTrue();
    }

    [Fact]
    public void IsEffective_ReturnsFalse_OnTheExpirationDateItself()
    {
        var assignment = TestData.RoleAssignment(
            effectiveDate: TestData.Today,
            expirationDate: TestData.Today.AddDays(10));

        assignment.IsEffective(TestData.Today.AddDays(10)).Should().BeFalse();
    }

    [Fact]
    public void IsEffective_ReturnsTrue_WithNoExpirationDate_FarInTheFuture()
    {
        var assignment = TestData.RoleAssignment(effectiveDate: TestData.Today, expirationDate: null);

        assignment.IsEffective(TestData.Today.AddYears(10)).Should().BeTrue();
    }

    [Fact]
    public void IsEffective_ReturnsFalse_WhenRevoked_EvenWithinTheEffectiveWindow()
    {
        var assignment = TestData.RoleAssignment(effectiveDate: TestData.Today, expirationDate: null);
        assignment.Revoke(TestData.NowUtc);

        assignment.IsEffective(TestData.Today).Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtUtc_AndRaisesRoleRevokedEvent()
    {
        var assignment = TestData.RoleAssignment();
        var revokedAt = TestData.NowUtc.AddHours(1);

        var result = assignment.Revoke(revokedAt);

        result.IsSuccess.Should().BeTrue();
        assignment.RevokedAtUtc.Should().Be(revokedAt);
        assignment.DomainEvents.Should().ContainSingle(e => e is RoleRevoked);
    }

    [Fact]
    public void Revoke_IsIdempotent_AndDoesNotRaiseASecondEvent_WhenAlreadyRevoked()
    {
        var assignment = TestData.RoleAssignment();
        var firstRevokedAt = TestData.NowUtc.AddHours(1);
        var secondRevokedAt = TestData.NowUtc.AddHours(2);

        assignment.Revoke(firstRevokedAt);
        var secondResult = assignment.Revoke(secondRevokedAt);

        secondResult.IsSuccess.Should().BeTrue();
        assignment.RevokedAtUtc.Should().Be(firstRevokedAt, "a retried revoke must not overwrite the original revocation time");
        assignment.DomainEvents.Should().ContainSingle(e => e is RoleRevoked, "a retried revoke must not raise a second event");
    }
}
