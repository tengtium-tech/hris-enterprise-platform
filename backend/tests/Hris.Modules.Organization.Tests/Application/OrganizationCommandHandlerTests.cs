using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateOrganizationCommandHandler _handler;
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public CreateOrganizationCommandHandlerTests()
    {
        _handler = new CreateOrganizationCommandHandler(_repository, new FakeTimeProvider(_now));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeAndNameAreUnique()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "CORP", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "ABC", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateOrganizationCommand(tenantId, "ABC", "CORP", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Hris.Modules.Organization.Domain.Organization>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "CORP", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateOrganizationCommand(tenantId, "ABC", "CORP", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateOrganizationCode);
    }

    [Fact]
    public async Task Handle_Fails_WhenNameAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "CORP", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "ABC", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateOrganizationCommand(tenantId, "ABC", "CORP", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateOrganizationName);
    }
}

public sealed class RenameOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RenameOrganizationCommandHandler _handler;
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RenameOrganizationCommandHandlerTests()
    {
        _handler = new RenameOrganizationCommandHandler(_repository, new FakeTimeProvider(_now));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOrganizationExists()
    {
        var organization = TestOrganization.Create();
        _repository.ExistsWithNameAsync(organization.TenantId, "New Name", organization.Id, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RenameOrganizationCommand(organization.Id.Value, organization.TenantId, "New Name"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.Name.Value.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_Fails_WhenOrganizationDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<OrganizationId>(), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Organization.Domain.Organization?)null);

        var result = await _handler.Handle(
            new RenameOrganizationCommand(Guid.NewGuid(), Guid.NewGuid(), "New Name"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.OrganizationNotFound);
    }
}

public sealed class UpdateOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly UpdateOrganizationCommandHandler _handler;
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public UpdateOrganizationCommandHandlerTests()
    {
        _handler = new UpdateOrganizationCommandHandler(_repository, new FakeTimeProvider(_now));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOrganizationExists()
    {
        var organization = TestOrganization.Create();
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new UpdateOrganizationCommand(organization.Id.Value, organization.TenantId, null, "new description"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.Description.Should().Be("new description");
    }
}

public sealed class ArchiveOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveOrganizationCommandHandler _handler;
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public ArchiveOrganizationCommandHandlerTests()
    {
        _handler = new ArchiveOrganizationCommandHandler(_repository, new FakeTimeProvider(_now));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOrganizationExistsAndHasNoActiveChildren()
    {
        var organization = TestOrganization.Create();
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveOrganizationCommand(organization.Id.Value, organization.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.Status.Should().Be(OrganizationalUnitStatus.Archived);
    }
}

public sealed class RestoreOrganizationCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RestoreOrganizationCommandHandler _handler;
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public RestoreOrganizationCommandHandlerTests()
    {
        _handler = new RestoreOrganizationCommandHandler(_repository, new FakeTimeProvider(_now));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOrganizationIsArchived()
    {
        var organization = TestOrganization.Create();
        organization.Archive(_now);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RestoreOrganizationCommand(organization.Id.Value, organization.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.Status.Should().Be(OrganizationalUnitStatus.Active);
    }
}
