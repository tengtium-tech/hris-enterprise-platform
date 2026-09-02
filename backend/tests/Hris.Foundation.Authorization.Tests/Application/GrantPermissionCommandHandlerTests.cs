using FluentAssertions;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

public sealed class GrantPermissionCommandHandlerTests
{
    private readonly IRolePermissionGrantRepository _repository = Substitute.For<IRolePermissionGrantRepository>();
    private readonly GrantPermissionCommandHandler _handler;

    public GrantPermissionCommandHandlerTests()
    {
        _handler = new GrantPermissionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheGrant_WhenInputIsValid()
    {
        var command = new GrantPermissionCommand(Role.HRManager, "Employee", PermissionAction.Update);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<RolePermissionGrant>(g => g.Role == Role.HRManager && g.Id.Value == result.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFailure_AndDoesNotPersist_WhenResourceTypeIsEmpty()
    {
        var command = new GrantPermissionCommand(Role.HRManager, string.Empty, PermissionAction.Update);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ResourceTypeRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<RolePermissionGrant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFailure_AndDoesNotPersist_WhenAuditorRoleRequestsAMutationPermission()
    {
        var command = new GrantPermissionCommand(Role.Auditor, "Employee", PermissionAction.Delete);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.AuditorCannotHoldMutationPermission);
        await _repository.DidNotReceive().AddAsync(Arg.Any<RolePermissionGrant>(), Arg.Any<CancellationToken>());
    }
}
