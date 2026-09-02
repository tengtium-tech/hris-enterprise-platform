using FluentAssertions;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

public sealed class RevokeRoleAssignmentCommandHandlerTests
{
    private readonly IRoleAssignmentRepository _repository = Substitute.For<IRoleAssignmentRepository>();
    private readonly RevokeRoleAssignmentCommandHandler _handler;

    public RevokeRoleAssignmentCommandHandlerTests()
    {
        _handler = new RevokeRoleAssignmentCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_RevokesTheAssignment_WhenItExists()
    {
        var assignment = TestData.RoleAssignment();
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(new RevokeRoleAssignmentCommand(assignment.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.RevokedAtUtc.Should().Be(TestData.NowUtc);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenTheAssignmentDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<RoleAssignmentId>(), Arg.Any<CancellationToken>())
            .Returns((RoleAssignment?)null);

        var result = await _handler.Handle(new RevokeRoleAssignmentCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.RoleAssignmentNotFound);
    }
}
