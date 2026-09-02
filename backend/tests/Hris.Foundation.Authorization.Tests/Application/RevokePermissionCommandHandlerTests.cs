using FluentAssertions;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

public sealed class RevokePermissionCommandHandlerTests
{
    private readonly IRolePermissionGrantRepository _repository = Substitute.For<IRolePermissionGrantRepository>();
    private readonly RevokePermissionCommandHandler _handler;

    public RevokePermissionCommandHandlerTests()
    {
        _handler = new RevokePermissionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_RevokesTheGrant_WhenItExists()
    {
        var grant = TestData.Grant();
        _repository.GetByIdAsync(grant.Id, Arg.Any<CancellationToken>()).Returns(grant);

        var result = await _handler.Handle(new RevokePermissionCommand(grant.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        grant.RevokedAtUtc.Should().Be(TestData.NowUtc);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenTheGrantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<RolePermissionGrantId>(), Arg.Any<CancellationToken>())
            .Returns((RolePermissionGrant?)null);

        var result = await _handler.Handle(new RevokePermissionCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.RolePermissionGrantNotFound);
    }
}
