using FluentAssertions;
using Hris.Foundation.Authorization.Application.Dtos;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

/// <summary>
/// Confirms these query-side DTOs behave as proper records (value equality, a real
/// <c>ToString</c>) -- the same expectation
/// <see cref="Domain.AuthorizationEventsTests"/> confirms for this framework's own
/// Domain Events.
/// </summary>
public sealed class AuthorizationDtoTests
{
    [Fact]
    public void PermissionGrantDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new PermissionGrantDto(Guid.NewGuid(), "HRManager", "Employee", "Read", true);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(PermissionGrantDto));
    }

    [Fact]
    public void RoleAssignmentDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new RoleAssignmentDto(
            Guid.NewGuid(), Guid.NewGuid(), "HRManager", "Tenant", Guid.NewGuid(), "Direct",
            TestData.Today, TestData.Today.AddDays(30), false);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(RoleAssignmentDto));
    }
}
