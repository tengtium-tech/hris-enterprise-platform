using FluentAssertions;
using Hris.Modules.Administration.Application.Commands;
using Hris.Modules.Administration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

public sealed class CreateDelegationCommandHandlerTests
{
    private readonly IAdministrativeDelegationRepository _delegationRepository = Substitute.For<IAdministrativeDelegationRepository>();
    private readonly IUserAccountRepository _userAccountRepository = Substitute.For<IUserAccountRepository>();
    private readonly CreateDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateOnly _periodStart = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
    private static readonly DateOnly _periodEnd = _periodStart.AddDays(14);

    public CreateDelegationCommandHandlerTests()
    {
        _handler = new CreateDelegationCommandHandler(_delegationRepository, _userAccountRepository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDelegatorHoldsAuthorityAndNoSoDViolation()
    {
        var delegator = TestUserAccount.CreateActiveTenantAdministrator(_tenantId);
        var delegatee = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _userAccountRepository.GetByIdAsync(delegator.Id, Arg.Any<CancellationToken>()).Returns(delegator);
        _userAccountRepository.GetByIdAsync(delegatee.Id, Arg.Any<CancellationToken>()).Returns(delegatee);

        var result = await _handler.Handle(
            new CreateDelegationCommand(
                _tenantId, delegator.Id.Value, delegatee.Id.Value,
                [new DelegatedAuthorityItem(CanonicalRole.SystemAdministrator, ScopeLevel.Tenant, null)], _periodStart, _periodEnd,
                "Covering leave", null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _delegationRepository.Received(1).AddAsync(Arg.Any<AdministrativeDelegation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenAuthorityExceedsDelegatorHoldings()
    {
        var delegator = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var delegatee = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _userAccountRepository.GetByIdAsync(delegator.Id, Arg.Any<CancellationToken>()).Returns(delegator);
        _userAccountRepository.GetByIdAsync(delegatee.Id, Arg.Any<CancellationToken>()).Returns(delegatee);

        var result = await _handler.Handle(
            new CreateDelegationCommand(
                _tenantId, delegator.Id.Value, delegatee.Id.Value,
                [new DelegatedAuthorityItem(CanonicalRole.SystemAdministrator, ScopeLevel.Tenant, null)], _periodStart, _periodEnd,
                "Covering leave", null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegatedAuthorityExceedsDelegatorHoldings);
    }

    [Fact]
    public async Task Handle_Fails_WhenViolatesSeparationOfDuties()
    {
        var delegator = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        delegator.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HRManager), OrganizationalScope.Create(ScopeLevel.Tenant, null).Value, today,
            null, Guid.NewGuid(), "Holds HRManager", null, true, TestUserAccount.NowUtc);

        var delegatee = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        delegatee.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.Auditor), OrganizationalScope.Create(ScopeLevel.Tenant, null).Value, today, null,
            Guid.NewGuid(), "Independent review", null, true, TestUserAccount.NowUtc);

        _userAccountRepository.GetByIdAsync(delegator.Id, Arg.Any<CancellationToken>()).Returns(delegator);
        _userAccountRepository.GetByIdAsync(delegatee.Id, Arg.Any<CancellationToken>()).Returns(delegatee);

        var result = await _handler.Handle(
            new CreateDelegationCommand(
                _tenantId, delegator.Id.Value, delegatee.Id.Value,
                [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)], _periodStart, _periodEnd,
                "Covering leave", null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationSeparationOfDutiesViolation);
    }
}

public sealed class ActivateDelegationCommandHandlerTests
{
    private readonly IAdministrativeDelegationRepository _delegationRepository = Substitute.For<IAdministrativeDelegationRepository>();
    private readonly IUserAccountRepository _userAccountRepository = Substitute.For<IUserAccountRepository>();
    private readonly ActivateDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ActivateDelegationCommandHandlerTests()
    {
        _handler = new ActivateDelegationCommandHandler(_delegationRepository, _userAccountRepository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDelegatorStillHoldsAuthorityAndNoSoDViolation()
    {
        var delegator = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        delegator.GrantRole(
            RoleReference.ForCanonical(CanonicalRole.HRManager), OrganizationalScope.Create(ScopeLevel.Tenant, null).Value, today, null,
            Guid.NewGuid(), "Holds HRManager", null, true, TestUserAccount.NowUtc);
        var delegatee = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);

        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, delegator.Id.Value, delegatee.Id.Value,
            [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)], today, today.AddDays(7), "Covering leave",
            null, false, false, TestUserAccount.NowUtc).Value;
        _delegationRepository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);
        _userAccountRepository.GetByIdAsync(delegator.Id, Arg.Any<CancellationToken>()).Returns(delegator);
        _userAccountRepository.GetByIdAsync(delegatee.Id, Arg.Any<CancellationToken>()).Returns(delegatee);

        var result = await _handler.Handle(new ActivateDelegationCommand(delegation.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenDelegationNotFound()
    {
        _delegationRepository.GetByIdAsync(Arg.Any<DelegationId>(), Arg.Any<CancellationToken>()).Returns((AdministrativeDelegation?)null);

        var result = await _handler.Handle(new ActivateDelegationCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenDelegatorNoLongerHoldsAuthority()
    {
        var delegator = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var delegatee = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);

        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, delegator.Id.Value, delegatee.Id.Value,
            [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)], today, today.AddDays(7), "Covering leave",
            null, false, false, TestUserAccount.NowUtc).Value;
        _delegationRepository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);
        _userAccountRepository.GetByIdAsync(delegator.Id, Arg.Any<CancellationToken>()).Returns(delegator);
        _userAccountRepository.GetByIdAsync(delegatee.Id, Arg.Any<CancellationToken>()).Returns(delegatee);

        var result = await _handler.Handle(new ActivateDelegationCommand(delegation.Id.Value, _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegatedAuthorityExceedsDelegatorHoldings);
    }
}

public sealed class ExpireDelegationCommandHandlerTests
{
    private readonly IAdministrativeDelegationRepository _repository = Substitute.For<IAdministrativeDelegationRepository>();
    private readonly ExpireDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ExpireDelegationCommandHandlerTests()
    {
        _handler = new ExpireDelegationCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenActive()
    {
        var today = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(),
            [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)], today, today.AddDays(7), "Covering leave",
            null, false, false, TestUserAccount.NowUtc).Value;
        delegation.Activate(false, false, TestUserAccount.NowUtc);
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(new ExpireDelegationCommand(delegation.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Expired);
    }

    [Fact]
    public async Task Handle_Fails_WhenDelegationNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<DelegationId>(), Arg.Any<CancellationToken>()).Returns((AdministrativeDelegation?)null);

        var result = await _handler.Handle(new ExpireDelegationCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationNotFound);
    }
}

public sealed class RevokeDelegationCommandHandlerTests
{
    private readonly IAdministrativeDelegationRepository _repository = Substitute.For<IAdministrativeDelegationRepository>();
    private readonly RevokeDelegationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RevokeDelegationCommandHandlerTests()
    {
        _handler = new RevokeDelegationCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDelegationExists()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(),
            [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)],
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime).AddDays(7),
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(
            new RevokeDelegationCommand(delegation.Id.Value, _tenantId, Guid.NewGuid(), "No longer needed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Revoked);
    }

    [Fact]
    public async Task Handle_Fails_WhenDelegationNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<DelegationId>(), Arg.Any<CancellationToken>()).Returns((AdministrativeDelegation?)null);

        var result = await _handler.Handle(
            new RevokeDelegationCommand(Guid.NewGuid(), _tenantId, Guid.NewGuid(), "Reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationNotFound);
    }
}
