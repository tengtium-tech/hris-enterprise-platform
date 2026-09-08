using FluentAssertions;
using Hris.Modules.Administration.Application.Commands;
using Hris.Modules.Administration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Administration.Tests.Application;

public sealed class DefineTenantRoleCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly DefineTenantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DefineTenantRoleCommandHandlerTests()
    {
        _handler = new DefineTenantRoleCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WithInitialPermissions()
    {
        var result = await _handler.Handle(
            new DefineTenantRoleCommand(_tenantId, "SeniorHROfficer", "Senior HR staff", Guid.NewGuid(), ["employee.view"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<TenantRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNameCollidesWithCanonical()
    {
        var result = await _handler.Handle(
            new DefineTenantRoleCommand(_tenantId, "HRManager", null, Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleNameCollidesWithCanonicalRole);
    }

    [Fact]
    public async Task Handle_Fails_WhenNameAlreadyExists()
    {
        _repository.ExistsWithNameAsync(_tenantId, "SeniorHROfficer", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new DefineTenantRoleCommand(_tenantId, "SeniorHROfficer", null, Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.RoleNameCollidesWithCanonicalRole);
    }
}

public sealed class AddPermissionToTenantRoleCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly AddPermissionToTenantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AddPermissionToTenantRoleCommandHandlerTests()
    {
        _handler = new AddPermissionToTenantRoleCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDraft()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _handler.Handle(
            new AddPermissionToTenantRoleCommand(role.Id.Value, _tenantId, "employee.view", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenRoleNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantRoleId>(), Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        var result = await _handler.Handle(
            new AddPermissionToTenantRoleCommand(Guid.NewGuid(), _tenantId, "employee.view", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotFound);
    }
}

public sealed class RemovePermissionFromTenantRoleCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly RemovePermissionFromTenantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RemovePermissionFromTenantRoleCommandHandlerTests()
    {
        _handler = new RemovePermissionFromTenantRoleCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenGrantExists()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        role.AddPermission("employee.view", Guid.NewGuid(), TestUserAccount.NowUtc);
        var grantId = role.PermissionGrants[0].Id.Value;
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _handler.Handle(
            new RemovePermissionFromTenantRoleCommand(role.Id.Value, _tenantId, grantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Fails_WhenRoleNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantRoleId>(), Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        var result = await _handler.Handle(
            new RemovePermissionFromTenantRoleCommand(Guid.NewGuid(), _tenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotFound);
    }
}

public sealed class DeprecateTenantRoleCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly DeprecateTenantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeprecateTenantRoleCommandHandlerTests()
    {
        _handler = new DeprecateTenantRoleCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPublished()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _handler.Handle(new DeprecateTenantRoleCommand(role.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.Status.Should().Be(TenantRoleStatus.Deprecated);
    }

    [Fact]
    public async Task Handle_Fails_WhenRoleNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantRoleId>(), Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        var result = await _handler.Handle(new DeprecateTenantRoleCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotFound);
    }
}

public sealed class PublishTenantRoleCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly PublishTenantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PublishTenantRoleCommandHandlerTests()
    {
        _handler = new PublishTenantRoleCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDraft()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _handler.Handle(new PublishTenantRoleCommand(role.Id.Value, _tenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.Status.Should().Be(TenantRoleStatus.Published);
    }

    [Fact]
    public async Task Handle_Fails_WhenRoleNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantRoleId>(), Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        var result = await _handler.Handle(new PublishTenantRoleCommand(Guid.NewGuid(), _tenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotFound);
    }
}

public sealed class ChangeTenantRolePermissionsCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly ChangeTenantRolePermissionsCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ChangeTenantRolePermissionsCommandHandlerTests()
    {
        _handler = new ChangeTenantRolePermissionsCommandHandler(_repository, new FakeTimeProvider(TestUserAccount.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPublished()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        role.Publish(Guid.NewGuid(), TestUserAccount.NowUtc);
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _handler.Handle(
            new ChangeTenantRolePermissionsCommand(role.Id.Value, _tenantId, ["employee.view"], [], Guid.NewGuid(), "Expanding scope"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.PermissionGrants.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenRoleNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantRoleId>(), Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        var result = await _handler.Handle(
            new ChangeTenantRolePermissionsCommand(Guid.NewGuid(), _tenantId, [], [], Guid.NewGuid(), "Reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotFound);
    }
}

public sealed class DeleteTenantRoleCommandHandlerTests
{
    private readonly ITenantRoleRepository _repository = Substitute.For<ITenantRoleRepository>();
    private readonly IUserAccountRepository _userAccountRepository = Substitute.For<IUserAccountRepository>();
    private readonly DeleteTenantRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteTenantRoleCommandHandlerTests()
    {
        _handler = new DeleteTenantRoleCommandHandler(_repository, _userAccountRepository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenNotReferenced()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _userAccountRepository.HasActiveAssignmentForTenantRoleAsync(_tenantId, role.Id.Value, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new DeleteTenantRoleCommand(role.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Remove(role);
    }

    [Fact]
    public async Task Handle_Fails_WhenReferencedByActiveAssignment()
    {
        var role = TenantRole.Create(
            new TenantRoleId(Guid.NewGuid()), _tenantId, "SeniorHROfficer", null, false, false, Guid.NewGuid(), TestUserAccount.NowUtc).Value;
        _repository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _userAccountRepository.HasActiveAssignmentForTenantRoleAsync(_tenantId, role.Id.Value, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteTenantRoleCommand(role.Id.Value, _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleInUseCannotBeDeleted);
    }

    [Fact]
    public async Task Handle_Fails_WhenRoleNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TenantRoleId>(), Arg.Any<CancellationToken>()).Returns((TenantRole?)null);

        var result = await _handler.Handle(new DeleteTenantRoleCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.TenantRoleNotFound);
    }
}
