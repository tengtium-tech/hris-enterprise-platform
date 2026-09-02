using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2's own Value Objects section:
/// "Equality is by value, not reference." These six records are Domain Events, not
/// Value Objects, but the same expectation applies to any immutable data-carrying
/// type this framework hands to a caller -- each is confirmed here to actually
/// behave as a proper record (value equality, a real <c>ToString</c>), not merely
/// constructed and never inspected.
/// </summary>
public sealed class AuthorizationEventsTests
{
    [Fact]
    public void RoleAssigned_HasValueEquality_AndAUsefulToString()
    {
        var original = new RoleAssigned(
            Guid.NewGuid(), TestData.NowUtc, new RoleAssignmentId(Guid.NewGuid()), TestData.NewPrincipalId(),
            Role.HRManager, TestData.Scope());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(RoleAssigned));
    }

    [Fact]
    public void RoleRevoked_HasValueEquality_AndAUsefulToString()
    {
        var original = new RoleRevoked(
            Guid.NewGuid(), TestData.NowUtc, new RoleAssignmentId(Guid.NewGuid()), TestData.NewPrincipalId(), Role.HRManager);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(RoleRevoked));
    }

    [Fact]
    public void PermissionGranted_HasValueEquality_AndAUsefulToString()
    {
        var original = new PermissionGranted(
            Guid.NewGuid(), TestData.NowUtc, new RolePermissionGrantId(Guid.NewGuid()), Role.HRManager, TestData.Permission());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(PermissionGranted));
    }

    [Fact]
    public void PermissionRevoked_HasValueEquality_AndAUsefulToString()
    {
        var original = new PermissionRevoked(
            Guid.NewGuid(), TestData.NowUtc, new RolePermissionGrantId(Guid.NewGuid()), Role.HRManager, TestData.Permission());
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(PermissionRevoked));
    }

    [Fact]
    public void AuthorizationEvaluated_HasValueEquality_AndAUsefulToString()
    {
        var original = new AuthorizationEvaluated(
            Guid.NewGuid(), TestData.NowUtc, TestData.NewPrincipalId(), TestData.Permission(), IsAllowed: true);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(AuthorizationEvaluated));
    }

    [Fact]
    public void AuthorizationDenied_HasValueEquality_AndAUsefulToString()
    {
        var original = new AuthorizationDenied(
            Guid.NewGuid(), TestData.NowUtc, TestData.NewPrincipalId(), TestData.Permission(), "denied for testing");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(AuthorizationDenied));
    }
}
