using FluentAssertions;
using Hris.Modules.Administration.Application.Commands;
using Hris.Modules.Administration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

public sealed class ProvisionUserAccountCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly ProvisionUserAccountCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ProvisionUserAccountCommandHandlerTests()
    {
        _handler = new ProvisionUserAccountCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WithoutInitialAssignments()
    {
        var result = await _handler.Handle(
            new ProvisionUserAccountCommand(_tenantId, AccountType.EmployeeLinked, Guid.NewGuid(), null, null, Guid.NewGuid(), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountTypeConstructionInvalid()
    {
        var result = await _handler.Handle(
            new ProvisionUserAccountCommand(_tenantId, AccountType.EmployeeLinked, null, null, null, Guid.NewGuid(), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.EmployeeIdRequiredForEmployeeLinkedAccount);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenGranterHoldsInitialAssignmentAuthority()
    {
        var provisionedBy = Guid.NewGuid();
        var granter = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        granter.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HROfficer), OrganizationalScope.Create(ScopeLevel.Tenant, null).Value,
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), null, Guid.NewGuid(), "Granter's own authority", null, true,
            TestUserAccount.NowUtc);
        _repository.GetByIdAsync(granter.Id, Arg.Any<CancellationToken>()).Returns(granter);

        var result = await _handler.Handle(
            new ProvisionUserAccountCommand(
                _tenantId, AccountType.EmployeeLinked, Guid.NewGuid(), null, null, granter.Id.Value,
                [new InitialRoleAssignmentInput(CanonicalRole.HROfficer, ScopeLevel.Tenant, null, "Ops staffing")]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenInitialAssignmentScopeInvalid()
    {
        var provisionedBy = Guid.NewGuid();

        var result = await _handler.Handle(
            new ProvisionUserAccountCommand(
                _tenantId, AccountType.EmployeeLinked, Guid.NewGuid(), null, null, provisionedBy,
                [new InitialRoleAssignmentInput(CanonicalRole.HROfficer, ScopeLevel.Department, null, "Missing target")]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.ScopeTargetRequired);
    }

    [Fact]
    public async Task Handle_AppliesGrantRulesToInitialAssignments()
    {
        var provisionedBy = Guid.NewGuid();
        _repository.GetByIdAsync(new UserAccountId(provisionedBy), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(
            new ProvisionUserAccountCommand(
                _tenantId, AccountType.EmployeeLinked, Guid.NewGuid(), null, null, provisionedBy,
                [new InitialRoleAssignmentInput(CanonicalRole.SystemAdministrator, ScopeLevel.Tenant, null, "Bootstrap")]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.InsufficientAuthorityToGrant);
    }
}

public sealed class ActivateUserAccountCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly ActivateUserAccountCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ActivateUserAccountCommandHandlerTests()
    {
        _handler = new ActivateUserAccountCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPending()
    {
        var account = TestUserAccount.CreateEmployeeLinked(_tenantId);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _handler.Handle(new ActivateUserAccountCommand(account.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(new ActivateUserAccountCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }
}

public sealed class SuspendUserAccountCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly SuspendUserAccountCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SuspendUserAccountCommandHandlerTests()
    {
        _handler = new SuspendUserAccountCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOtherAdministratorsExist()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repository.CountOtherActiveTenantAdministratorsAsync(_tenantId, account.Id.Value, Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(
            new SuspendUserAccountCommand(account.Id.Value, _tenantId, "Investigation", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(
            new SuspendUserAccountCommand(Guid.NewGuid(), _tenantId, "Reason", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenLastTenantAdministrator()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repository.CountOtherActiveTenantAdministratorsAsync(_tenantId, account.Id.Value, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.Handle(
            new SuspendUserAccountCommand(account.Id.Value, _tenantId, "Leaving", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.CannotRemoveLastTenantAdministrator);
    }
}

public sealed class ReinstateUserAccountCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly ReinstateUserAccountCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ReinstateUserAccountCommandHandlerTests()
    {
        _handler = new ReinstateUserAccountCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSuspended()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        account.Suspend(null, Guid.NewGuid(), false, TestUserAccount.NowUtc);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _handler.Handle(new ReinstateUserAccountCommand(account.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(new ReinstateUserAccountCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }
}

public sealed class DeprovisionUserAccountCommandHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly DeprovisionUserAccountCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeprovisionUserAccountCommandHandlerTests()
    {
        _handler = new DeprovisionUserAccountCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOtherAdministratorsExist()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repository.CountOtherActiveTenantAdministratorsAsync(_tenantId, account.Id.Value, Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(
            new DeprovisionUserAccountCommand(account.Id.Value, _tenantId, "Termination", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(UserAccountStatus.Deprovisioned);
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(
            new DeprovisionUserAccountCommand(Guid.NewGuid(), _tenantId, "Reason", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenLastTenantAdministrator()
    {
        var account = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _repository.CountOtherActiveTenantAdministratorsAsync(_tenantId, account.Id.Value, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.Handle(
            new DeprovisionUserAccountCommand(account.Id.Value, _tenantId, "Leaving", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.CannotRemoveLastTenantAdministrator);
    }
}
