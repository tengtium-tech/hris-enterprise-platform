using FluentAssertions;
using Hris.Modules.Administration.Application.Commands;
using Hris.Modules.Administration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

public sealed class GrantRoleCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly ITenantRoleRepository _tenantRoleRepository = Substitute.For<ITenantRoleRepository>();
    private readonly GrantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GrantRoleCommandHandlerTests()
    {
        _handler = new GrantRoleCommandHandler(_repository, _tenantRoleRepository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenGranterHoldsSufficientAuthority()
    {
        var granter = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        granter.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HROfficer), OrganizationalScope.Create(ScopeLevel.Tenant, null).Value,
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Granter's own authority", null, true,
            TestUserAccount.NowUtc);
        var target = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _repository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _repository.GetByIdAsync(granter.Id, Arg.Any<CancellationToken>()).Returns(granter);

        var result = await _handler.Handle(
            new GrantRoleCommand(
                target.Id.Value, _tenantId, RoleKind.Canonical, CanonicalRole.HROfficer, null, ScopeLevel.Tenant, null,
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, granter.Id.Value, "Ops staffing", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.RoleAssignments.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenCanonicalRoleIsNull()
    {
        var target = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _repository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var result = await _handler.Handle(
            new GrantRoleCommand(
                target.Id.Value, _tenantId, RoleKind.Canonical, null, null, ScopeLevel.Tenant, null,
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Reason", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole);
    }

    [Fact]
    public async Task Handle_Fails_WhenTargetNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(
            new GrantRoleCommand(
                Guid.NewGuid(), _tenantId, RoleKind.Canonical, CanonicalRole.HROfficer, null, ScopeLevel.Tenant, null,
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Reason", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }

    [Fact]
    public async Task Handle_ResolvesTenantRole_WhenPublished()
    {
        var granter = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var target = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var tenantRole = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        tenantRole.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);
        _tenantRoleRepository.GetByIdAsync(tenantRole.Id, Arg.Any<CancellationToken>()).Returns(tenantRole);

        granter.GrantRole(
            RoleReference.ForTenantRole(tenantRole.Id.Value, tenantRole.Name, true).Value, OrganizationalScope.Create(ScopeLevel.Tenant, null).Value,
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Granter's own authority", null, true,
            TestUserAccount.NowUtc);

        _repository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _repository.GetByIdAsync(granter.Id, Arg.Any<CancellationToken>()).Returns(granter);

        var result = await _handler.Handle(
            new GrantRoleCommand(
                target.Id.Value, _tenantId, RoleKind.Tenant, null, tenantRole.Id.Value, ScopeLevel.Tenant, null,
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, granter.Id.Value, "Ops staffing", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.RoleAssignments[0].Role.DisplayName.Should().Be("SeniorHROfficer");
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantRoleNotPublished()
    {
        var granter = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        var target = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _repository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _repository.GetByIdAsync(granter.Id, Arg.Any<CancellationToken>()).Returns(granter);

        var tenantRole = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        _tenantRoleRepository.GetByIdAsync(tenantRole.Id, Arg.Any<CancellationToken>()).Returns(tenantRole);

        var result = await _handler.Handle(
            new GrantRoleCommand(
                target.Id.Value, _tenantId, RoleKind.Tenant, null, tenantRole.Id.Value, ScopeLevel.Tenant, null,
                DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, granter.Id.Value, "Ops staffing", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole);
    }
}

public sealed class RevokeRoleCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly RevokeRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RevokeRoleCommandHandlerTests()
    {
        _handler = new RevokeRoleCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAssignmentExists()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Ops", null, true, TestUserAccount.NowUtc);
        var assignmentId = account.RoleAssignments[0].Id.Value;
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repository.CountOtherActiveTenantAdministratorsAsync(_tenantId, account.Id.Value, Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(
            new RevokeRoleCommand(account.Id.Value, _tenantId, assignmentId, Guid.NewGuid(), "No longer needed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.RoleAssignments[0].RevokedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(
            new RevokeRoleCommand(Guid.NewGuid(), _tenantId, Guid.NewGuid(), Guid.NewGuid(), "Reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }
}

public sealed class ExpireRoleAssignmentCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly ExpireRoleAssignmentCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ExpireRoleAssignmentCommandHandlerTests()
    {
        _handler = new ExpireRoleAssignmentCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAssignmentExists()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var role = RoleReference.ForCanonical(CanonicalRole.HROfficer);
        var scope = OrganizationalScope.Create(ScopeLevel.Tenant, null).Value;
        account.GrantRole(role, scope, DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Temp", null, true, TestUserAccount.NowUtc);
        var assignmentId = account.RoleAssignments[0].Id.Value;
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _handler.Handle(new ExpireRoleAssignmentCommand(account.Id.Value, _tenantId, assignmentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.RoleAssignments[0].IsExpired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(new ExpireRoleAssignmentCommand(Guid.NewGuid(), _tenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }
}
