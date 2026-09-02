using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

/// <summary>
/// <see cref="AuthorizationEvaluator"/> is the framework's own centralized RBAC-plus-scope
/// decision point (`CTR-AUT-001`); every branch of its own eight-step-derived logic is
/// covered here, per docs/09-testing/unit-and-integration-testing.md 2.2's own
/// "each documented business rule maps to at least one test." Repositories are
/// NSubstitute fakes, never a real HrisDbContext -- 2.1's own "must not touch...
/// a database... or a container."
/// </summary>
public sealed class AuthorizationEvaluatorTests
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository = Substitute.For<IRoleAssignmentRepository>();
    private readonly IRolePermissionGrantRepository _grantRepository = Substitute.For<IRolePermissionGrantRepository>();
    private readonly AuthorizationEvaluator _evaluator;

    public AuthorizationEvaluatorTests()
    {
        _evaluator = new AuthorizationEvaluator(_roleAssignmentRepository, _grantRepository);
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenPrincipalHasNoRoleAssignmentsAtAll()
    {
        var principalId = TestData.NewPrincipalId();
        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, TestData.Permission(), TestData.Scope(), TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("no effective role assignment");
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenEveryAssignmentIsExpired()
    {
        var principalId = TestData.NewPrincipalId();
        var expired = TestData.RoleAssignment(
            principalId: principalId,
            effectiveDate: TestData.Today.AddDays(-30),
            expirationDate: TestData.Today.AddDays(-1));

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([expired]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, TestData.Permission(), TestData.Scope(), TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("no effective role assignment");
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenEveryAssignmentIsRevoked()
    {
        var principalId = TestData.NewPrincipalId();
        var assignment = TestData.RoleAssignment(principalId: principalId);
        assignment.Revoke(TestData.NowUtc);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([assignment]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, TestData.Permission(), TestData.Scope(), TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse("CTR-AUT-007: revocation takes effect immediately");
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenNoEffectiveRoleGrantsTheRequestedPermission()
    {
        var principalId = TestData.NewPrincipalId();
        var scope = TestData.Scope();
        var assignment = TestData.RoleAssignment(Role.Employee, scope, principalId: principalId);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, TestData.Permission(), scope, TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("No effective role grants");
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenTheMatchingGrantIsRevoked()
    {
        var principalId = TestData.NewPrincipalId();
        var scope = TestData.Scope();
        var permission = TestData.Permission();
        var assignment = TestData.RoleAssignment(Role.HRManager, scope, principalId: principalId);
        var revokedGrant = TestData.Grant(Role.HRManager, permission);
        revokedGrant.Revoke(TestData.NowUtc);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([revokedGrant]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, permission, scope, TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse("a revoked grant must not count even if it still matches the requested permission");
    }

    [Fact]
    public async Task EvaluateAsync_Denies_WhenTheRoleHoldsThePermission_ButNotAtTheRequestedScope()
    {
        var principalId = TestData.NewPrincipalId();
        var permission = TestData.Permission();
        var assignedScope = TestData.Scope(OrganizationalScopeLevel.Department, Guid.NewGuid());
        var requestedScope = TestData.Scope(OrganizationalScopeLevel.Department, Guid.NewGuid());
        var assignment = TestData.RoleAssignment(Role.HRManager, assignedScope, principalId: principalId);
        var grant = TestData.Grant(Role.HRManager, permission);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([grant]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, permission, requestedScope, TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse("CTR-AUT-006: scope is enforced, not only assigned");
        decision.DenialReason.Should().Contain("not at the requested scope");
    }

    [Fact]
    public async Task EvaluateAsync_Allows_WhenTheRoleHoldsThePermission_AtTheMatchingScope()
    {
        var principalId = TestData.NewPrincipalId();
        var permission = TestData.Permission();
        var scope = TestData.Scope();
        var assignment = TestData.RoleAssignment(Role.HRManager, scope, principalId: principalId);
        var grant = TestData.Grant(Role.HRManager, permission);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([grant]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, permission, scope, TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_Allows_WhenOnlyOneOfSeveralRoles_GrantsThePermissionAtTheMatchingScope()
    {
        var principalId = TestData.NewPrincipalId();
        var permission = TestData.Permission();
        var matchingScope = TestData.Scope(OrganizationalScopeLevel.Department, Guid.NewGuid());
        var otherScope = TestData.Scope(OrganizationalScopeLevel.Department, Guid.NewGuid());

        var nonMatchingAssignment = TestData.RoleAssignment(Role.Employee, otherScope, principalId: principalId);
        var matchingAssignment = TestData.RoleAssignment(Role.HRManager, matchingScope, principalId: principalId);
        var grant = TestData.Grant(Role.HRManager, permission);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([nonMatchingAssignment, matchingAssignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([grant]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, permission, matchingScope, TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_ConsidersDuplicateRoleAssignments_AsOneDistinctRole()
    {
        var principalId = TestData.NewPrincipalId();
        var permission = TestData.Permission();
        var scope = TestData.Scope();

        // The same role assigned twice at two different scopes -- only the second
        // covers the requested resource. Exercises effectiveRoles.Distinct() still
        // resolving the grant query correctly for a role appearing more than once.
        var firstAssignment = TestData.RoleAssignment(Role.HRManager, TestData.Scope(), principalId: principalId);
        var secondAssignment = TestData.RoleAssignment(Role.HRManager, scope, principalId: principalId);
        var grant = TestData.Grant(Role.HRManager, permission);

        _roleAssignmentRepository.GetByPrincipalAsync(principalId, Arg.Any<CancellationToken>())
            .Returns([firstAssignment, secondAssignment]);
        _grantRepository.GetActiveGrantsForRolesAsync(Arg.Any<IReadOnlyCollection<Role>>(), Arg.Any<CancellationToken>())
            .Returns([grant]);

        var decision = await _evaluator.EvaluateAsync(
            principalId, permission, scope, TestData.NowUtc, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_Throws_WhenRequestedPermissionIsNull()
    {
        var act = () => _evaluator.EvaluateAsync(
            TestData.NewPrincipalId(), null!, TestData.Scope(), TestData.NowUtc, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_Throws_WhenResourceScopeIsNull()
    {
        var act = () => _evaluator.EvaluateAsync(
            TestData.NewPrincipalId(), TestData.Permission(), null!, TestData.NowUtc, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
