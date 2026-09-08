using FluentAssertions;
using Hris.Modules.Administration.Application.Queries;
using Hris.Modules.Administration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

public sealed class GetUserAccountQueryHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly GetUserAccountQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetUserAccountQueryHandlerTests()
    {
        _handler = new GetUserAccountQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenAccountExists()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _handler.Handle(new GetUserAccountQuery(account.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(account.Id.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenAccountBelongsToDifferentTenant()
    {
        var account = TestUserAccount.CreateActiveEmployeeLinked(Guid.NewGuid());
        _repository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _handler.Handle(new GetUserAccountQuery(account.Id.Value, _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }
}

public sealed class GetUserAccountByEmployeeQueryHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly GetUserAccountByEmployeeQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetUserAccountByEmployeeQueryHandlerTests()
    {
        _handler = new GetUserAccountByEmployeeQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenLinkedAccountExists()
    {
        var employeeId = Guid.NewGuid();
        var account = TestUserAccount.CreateActiveEmployeeLinked(_tenantId, employeeId);
        _repository.GetByEmployeeIdAsync(_tenantId, employeeId, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _handler.Handle(new GetUserAccountByEmployeeQuery(employeeId, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenNoLinkedAccountExists()
    {
        _repository.GetByEmployeeIdAsync(_tenantId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await _handler.Handle(new GetUserAccountByEmployeeQuery(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.UserAccountNotFound);
    }
}

public sealed class ListUserAccountsQueryHandlerTests
{
    private readonly IUserAccountRepository _repository = Substitute.For<IUserAccountRepository>();
    private readonly ListUserAccountsQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ListUserAccountsQueryHandlerTests()
    {
        _handler = new ListUserAccountsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        var active = TestUserAccount.CreateActiveEmployeeLinked(_tenantId);
        var pending = TestUserAccount.CreateEmployeeLinked(_tenantId);
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(new List<UserAccount> { active, pending });

        var result = await _handler.Handle(new ListUserAccountsQuery(_tenantId, UserAccountStatus.Active), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == active.Id.Value);
    }
}

public sealed class GetTenantRoleQueryHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly GetTenantRoleQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetTenantRoleQueryHandlerTests()
    {
        _handler = new GetTenantRoleQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenRoleExists()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _handler.Handle(new GetTenantRoleQuery(role.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("SeniorHROfficer");
    }
}

public sealed class ListTenantRolesQueryHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly ListTenantRolesQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ListTenantRolesQueryHandlerTests()
    {
        _handler = new ListTenantRolesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        var draft = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "DraftRole", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        var published = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "PublishedRole", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        published.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(new List<TenantRole> { draft, published });

        var result = await _handler.Handle(new ListTenantRolesQuery(_tenantId, TenantRoleStatus.Published), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Name == "PublishedRole");
    }
}

public sealed class GetDelegationQueryHandlerTests
{
    private readonly IAdministrativeDelegationRepository _repository = Substitute.For<IAdministrativeDelegationRepository>();
    private readonly GetDelegationQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetDelegationQueryHandlerTests()
    {
        _handler = new GetDelegationQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenDelegationExists()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(),
            [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)],
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime).AddDays(7),
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(delegation.Id, Arg.Any<CancellationToken>()).Returns(delegation);

        var result = await _handler.Handle(new GetDelegationQuery(delegation.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class ListDelegationsQueryHandlerTests
{
    private readonly IAdministrativeDelegationRepository _repository = Substitute.For<IAdministrativeDelegationRepository>();
    private readonly ListDelegationsQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ListDelegationsQueryHandlerTests()
    {
        _handler = new ListDelegationsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsAllDelegationsForTenant()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(),
            [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)],
            DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime), DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime).AddDays(7),
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(new List<AdministrativeDelegation> { delegation });

        var result = await _handler.Handle(new ListDelegationsQuery(_tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}
