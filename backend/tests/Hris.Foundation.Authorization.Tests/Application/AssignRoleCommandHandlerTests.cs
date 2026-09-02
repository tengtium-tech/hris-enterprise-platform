using FluentAssertions;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

public sealed class AssignRoleCommandHandlerTests
{
    private readonly IRoleAssignmentRepository _repository = Substitute.For<IRoleAssignmentRepository>();
    private readonly AssignRoleCommandHandler _handler;

    public AssignRoleCommandHandlerTests()
    {
        _handler = new AssignRoleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheAssignment_WhenInputIsValid()
    {
        var command = new AssignRoleCommand(
            Guid.NewGuid(), Role.HRManager, OrganizationalScopeLevel.Tenant, Guid.NewGuid(),
            RoleAssignmentType.Direct, TestData.Today, null, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<RoleAssignment>(a => a.Role == Role.HRManager && a.Id.Value == result.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFailure_AndDoesNotPersist_WhenScopeIdIsEmpty()
    {
        var command = new AssignRoleCommand(
            Guid.NewGuid(), Role.HRManager, OrganizationalScopeLevel.Tenant, Guid.Empty,
            RoleAssignmentType.Direct, TestData.Today, null, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ScopeIdRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<RoleAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsFailure_AndDoesNotPersist_WhenExpirationIsBeforeEffectiveDate()
    {
        var command = new AssignRoleCommand(
            Guid.NewGuid(), Role.HRManager, OrganizationalScopeLevel.Tenant, Guid.NewGuid(),
            RoleAssignmentType.Direct, TestData.Today, TestData.Today.AddDays(-1), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ExpirationBeforeEffectiveDate);
        await _repository.DidNotReceive().AddAsync(Arg.Any<RoleAssignment>(), Arg.Any<CancellationToken>());
    }
}
