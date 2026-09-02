using FluentAssertions;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

public sealed class GetRoleAssignmentsForPrincipalQueryHandlerTests
{
    private readonly IRoleAssignmentRepository _repository = Substitute.For<IRoleAssignmentRepository>();
    private readonly GetRoleAssignmentsForPrincipalQueryHandler _handler;

    public GetRoleAssignmentsForPrincipalQueryHandlerTests()
    {
        _handler = new GetRoleAssignmentsForPrincipalQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsMappedAssignments_IncludingRevokedAndOpenEndedOnes_ForTheRequestedPrincipal()
    {
        var principalId = Guid.NewGuid();
        var principal = new UserAccountId(principalId);

        var active = TestData.RoleAssignment(Role.HRManager, principalId: principal, expirationDate: null);
        var revoked = TestData.RoleAssignment(Role.Employee, principalId: principal, expirationDate: TestData.Today.AddDays(30));
        revoked.Revoke(TestData.NowUtc);

        _repository.GetByPrincipalAsync(principal, Arg.Any<CancellationToken>())
            .Returns([active, revoked]);

        var result = await _handler.Handle(new GetRoleAssignmentsForPrincipalQuery(principalId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2, "an administrative view shows revoked/expired assignments too, not only effective ones");

        var activeDto = result.Value.Single(dto => dto.Role == nameof(Role.HRManager));
        activeDto.ExpirationDate.Should().BeNull();
        activeDto.IsRevoked.Should().BeFalse();

        var revokedDto = result.Value.Single(dto => dto.Role == nameof(Role.Employee));
        revokedDto.ExpirationDate.Should().Be(TestData.Today.AddDays(30));
        revokedDto.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyList_WhenThePrincipalHoldsNoAssignments()
    {
        _repository.GetByPrincipalAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetRoleAssignmentsForPrincipalQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
