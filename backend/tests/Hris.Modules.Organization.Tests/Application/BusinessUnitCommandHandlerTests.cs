using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateBusinessUnitCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateBusinessUnitCommandHandler _handler;

    public CreateBusinessUnitCommandHandlerTests()
    {
        _handler = new CreateBusinessUnitCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOrganizationExists()
    {
        var organization = TestOrganization.Create();
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new CreateBusinessUnitCommand(organization.Id.Value, organization.TenantId, "Corporate"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.BusinessUnits.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenOrganizationDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<OrganizationId>(), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Organization.Domain.Organization?)null);

        var result = await _handler.Handle(
            new CreateBusinessUnitCommand(Guid.NewGuid(), Guid.NewGuid(), "Corporate"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.OrganizationNotFound);
    }
}

public sealed class RenameBusinessUnitCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RenameBusinessUnitCommandHandler _handler;

    public RenameBusinessUnitCommandHandlerTests()
    {
        _handler = new RenameBusinessUnitCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBusinessUnitExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RenameBusinessUnitCommand(organization.Id.Value, organization.TenantId, businessUnitId.Value, "Renamed"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.FindBusinessUnit(businessUnitId)!.Name.Value.Should().Be("Renamed");
    }
}

public sealed class ArchiveBusinessUnitCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveBusinessUnitCommandHandler _handler;

    public ArchiveBusinessUnitCommandHandlerTests()
    {
        _handler = new ArchiveBusinessUnitCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBusinessUnitExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveBusinessUnitCommand(organization.Id.Value, organization.TenantId, businessUnitId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.FindBusinessUnit(businessUnitId)!.Status.Should().Be(OrganizationalUnitStatus.Archived);
    }
}

public sealed class RestoreBusinessUnitCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RestoreBusinessUnitCommandHandler _handler;

    public RestoreBusinessUnitCommandHandlerTests()
    {
        _handler = new RestoreBusinessUnitCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBusinessUnitIsArchived()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        organization.ArchiveBusinessUnit(businessUnitId, TestOrganization.NowUtc);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RestoreBusinessUnitCommand(organization.Id.Value, organization.TenantId, businessUnitId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.FindBusinessUnit(businessUnitId)!.Status.Should().Be(OrganizationalUnitStatus.Active);
    }
}
