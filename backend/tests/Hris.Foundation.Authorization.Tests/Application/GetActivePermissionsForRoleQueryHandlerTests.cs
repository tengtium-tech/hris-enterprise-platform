using FluentAssertions;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

public sealed class GetActivePermissionsForRoleQueryHandlerTests
{
    private readonly IRolePermissionGrantRepository _repository = Substitute.For<IRolePermissionGrantRepository>();
    private readonly GetActivePermissionsForRoleQueryHandler _handler;

    public GetActivePermissionsForRoleQueryHandlerTests()
    {
        _handler = new GetActivePermissionsForRoleQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsMappedGrants_ForTheRequestedRole()
    {
        var permission = TestData.Permission("Employee", PermissionAction.Update);
        var grant = TestData.Grant(Role.HRManager, permission);

        _repository.GetActiveGrantsForRolesAsync(
                Arg.Is<IReadOnlyCollection<Role>>(roles => roles.Single() == Role.HRManager), Arg.Any<CancellationToken>())
            .Returns([grant]);

        var result = await _handler.Handle(new GetActivePermissionsForRoleQuery(Role.HRManager), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var dto = result.Value.Single();
        dto.Role.Should().Be(nameof(Role.HRManager));
        dto.ResourceType.Should().Be("Employee");
        dto.Action.Should().Be(nameof(PermissionAction.Update));
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyList_WhenTheRoleHoldsNoActiveGrants()
    {
        _repository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetActivePermissionsForRoleQuery(Role.Employee), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
