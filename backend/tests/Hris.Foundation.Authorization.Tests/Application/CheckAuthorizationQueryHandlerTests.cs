using FluentAssertions;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Application;

/// <summary>
/// <see cref="AuthorizationEvaluator"/> is a concrete, sealed Domain Service with no
/// interface of its own (deliberately -- authorization-framework.md's Centralized
/// Evaluation section names one canonical decision point, not a swappable strategy),
/// so it is constructed for real here, over NSubstitute repository fakes, rather than
/// substituted itself. <see cref="AuthorizationEvaluatorTests"/> already covers every
/// branch of the evaluator's own decision logic; these tests confirm the handler's
/// own job -- translating the request into the evaluator's own inputs and the
/// decision into its own DTO -- not re-deriving every evaluator branch again.
/// </summary>
public sealed class CheckAuthorizationQueryHandlerTests
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository = Substitute.For<IRoleAssignmentRepository>();
    private readonly IRolePermissionGrantRepository _grantRepository = Substitute.For<IRolePermissionGrantRepository>();
    private readonly CheckAuthorizationQueryHandler _handler;

    public CheckAuthorizationQueryHandlerTests()
    {
        var evaluator = new AuthorizationEvaluator(_roleAssignmentRepository, _grantRepository);
        _handler = new CheckAuthorizationQueryHandler(evaluator, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_ReturnsAnAllowedDto_WhenTheEvaluatorAllows()
    {
        var principalId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();
        var assignment = TestData.RoleAssignment(
            Role.HRManager,
            TestData.Scope(OrganizationalScopeLevel.Tenant, scopeId),
            principalId: new Hris.Foundation.Identity.Domain.UserAccountId(principalId));
        var grant = TestData.Grant(Role.HRManager, TestData.Permission("Employee", PermissionAction.Read));

        _roleAssignmentRepository.GetByPrincipalAsync(Arg.Any<Hris.Foundation.Identity.Domain.UserAccountId>(), Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([grant]);

        var query = new CheckAuthorizationQuery(principalId, "Employee", PermissionAction.Read, OrganizationalScopeLevel.Tenant, scopeId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsADeniedDto_WhenTheEvaluatorDenies()
    {
        _roleAssignmentRepository.GetByPrincipalAsync(Arg.Any<Hris.Foundation.Identity.Domain.UserAccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var query = new CheckAuthorizationQuery(Guid.NewGuid(), "Employee", PermissionAction.Read, OrganizationalScopeLevel.Tenant, Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue("a denial is a successful evaluation with a negative outcome, not an application-layer failure");
        result.Value.IsAllowed.Should().BeFalse();
        result.Value.DenialReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenResourceTypeIsEmpty()
    {
        var query = new CheckAuthorizationQuery(Guid.NewGuid(), string.Empty, PermissionAction.Read, OrganizationalScopeLevel.Tenant, Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ResourceTypeRequired);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenScopeIdIsEmpty()
    {
        var query = new CheckAuthorizationQuery(Guid.NewGuid(), "Employee", PermissionAction.Read, OrganizationalScopeLevel.Tenant, Guid.Empty);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthorizationErrors.ScopeIdRequired);
    }
}
