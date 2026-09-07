using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateDivisionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateDivisionCommandHandler _handler;

    public CreateDivisionCommandHandlerTests()
    {
        _handler = new CreateDivisionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBusinessUnitExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new CreateDivisionCommand(organization.Id.Value, organization.TenantId, businessUnitId.Value, "Software"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class RenameDivisionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RenameDivisionCommandHandler _handler;

    public RenameDivisionCommandHandlerTests()
    {
        _handler = new RenameDivisionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDivisionExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Software", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RenameDivisionCommand(organization.Id.Value, organization.TenantId, divisionId.Value, "Renamed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class MoveDivisionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly MoveDivisionCommandHandler _handler;

    public MoveDivisionCommandHandlerTests()
    {
        _handler = new MoveDivisionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBothUnitsExist()
    {
        var organization = TestOrganization.Create();
        var sourceId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(sourceId, "Software", TestOrganization.NowUtc).Value;
        var targetId = organization.AddBusinessUnit("Operations", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new MoveDivisionCommand(organization.Id.Value, organization.TenantId, divisionId.Value, targetId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.FindDivision(divisionId)!.BusinessUnitId.Should().Be(targetId);
    }
}

public sealed class ArchiveDivisionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveDivisionCommandHandler _handler;

    public ArchiveDivisionCommandHandlerTests()
    {
        _handler = new ArchiveDivisionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDivisionExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Software", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveDivisionCommand(organization.Id.Value, organization.TenantId, divisionId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.FindDivision(divisionId)!.Status.Should().Be(OrganizationalUnitStatus.Archived);
    }
}
