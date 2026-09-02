using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

public sealed class PermissionKeyTests
{
    [Fact]
    public void Create_Succeeds_WithAValidResourceType()
    {
        var result = PermissionKey.Create("Employee", PermissionAction.Read);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("Employee");
        result.Value.Action.Should().Be(PermissionAction.Read);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenResourceTypeIsNullOrWhitespace(string? resourceType)
    {
        var result = PermissionKey.Create(resourceType, PermissionAction.Read);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ResourceTypeRequired);
    }

    [Fact]
    public void Create_TrimsResourceType()
    {
        var result = PermissionKey.Create("  Employee  ", PermissionAction.Read);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceType.Should().Be("Employee");
    }

    [Fact]
    public void Equality_IsByValue_ForSameResourceTypeAndAction()
    {
        var first = PermissionKey.Create("Employee", PermissionAction.Read).Value;
        var second = PermissionKey.Create("Employee", PermissionAction.Read).Value;

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_Differs_WhenActionDiffers()
    {
        var first = PermissionKey.Create("Employee", PermissionAction.Read).Value;
        var second = PermissionKey.Create("Employee", PermissionAction.Update).Value;

        first.Should().NotBe(second);
    }
}
