using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateSectionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateSectionCommandHandler _handler;

    public CreateSectionCommandHandlerTests()
    {
        _handler = new CreateSectionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDepartmentExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new CreateSectionCommand(organization.Id.Value, organization.TenantId, departmentId.Value, "Recruitment"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class RenameSectionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RenameSectionCommandHandler _handler;

    public RenameSectionCommandHandlerTests()
    {
        _handler = new RenameSectionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSectionExists()
    {
        var (organization, sectionId) = BuildOrganizationWithSection();
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RenameSectionCommand(organization.Id.Value, organization.TenantId, sectionId.Value, "Renamed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private static (Hris.Modules.Organization.Domain.Organization Organization, SectionId SectionId) BuildOrganizationWithSection()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", TestOrganization.NowUtc).Value;
        return (organization, sectionId);
    }
}

public sealed class ArchiveSectionCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveSectionCommandHandler _handler;

    public ArchiveSectionCommandHandlerTests()
    {
        _handler = new ArchiveSectionCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSectionExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveSectionCommand(organization.Id.Value, organization.TenantId, sectionId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class CreateTeamCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateTeamCommandHandler _handler;

    public CreateTeamCommandHandlerTests()
    {
        _handler = new CreateTeamCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSectionExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new CreateTeamCommand(organization.Id.Value, organization.TenantId, sectionId.Value, "Sourcing"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class RenameTeamCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RenameTeamCommandHandler _handler;

    public RenameTeamCommandHandlerTests()
    {
        _handler = new RenameTeamCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTeamExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", TestOrganization.NowUtc).Value;
        var teamId = organization.AddTeam(sectionId, "Sourcing", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RenameTeamCommand(organization.Id.Value, organization.TenantId, teamId.Value, "Renamed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class ArchiveTeamCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveTeamCommandHandler _handler;

    public ArchiveTeamCommandHandlerTests()
    {
        _handler = new ArchiveTeamCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTeamExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", TestOrganization.NowUtc).Value;
        var teamId = organization.AddTeam(sectionId, "Sourcing", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveTeamCommand(organization.Id.Value, organization.TenantId, teamId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class CreateCostCenterCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateCostCenterCommandHandler _handler;

    public CreateCostCenterCommandHandlerTests()
    {
        _handler = new CreateCostCenterCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeIsUnique()
    {
        var organization = TestOrganization.Create();
        _repository.ExistsCostCenterWithCodeAsync(organization.TenantId, "CC100", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new CreateCostCenterCommand(organization.Id.Value, organization.TenantId, "CC100", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExists()
    {
        var organization = TestOrganization.Create();
        _repository.ExistsCostCenterWithCodeAsync(organization.TenantId, "CC100", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateCostCenterCommand(organization.Id.Value, organization.TenantId, "CC100", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateCostCenterCode);
    }
}

public sealed class UpdateCostCenterCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly UpdateCostCenterCommandHandler _handler;

    public UpdateCostCenterCommandHandlerTests()
    {
        _handler = new UpdateCostCenterCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCostCenterExists()
    {
        var organization = TestOrganization.Create();
        var costCenterId = organization.AddCostCenter("CC100", null, TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new UpdateCostCenterCommand(organization.Id.Value, organization.TenantId, costCenterId.Value, "updated"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class ArchiveCostCenterCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveCostCenterCommandHandler _handler;

    public ArchiveCostCenterCommandHandlerTests()
    {
        _handler = new ArchiveCostCenterCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCostCenterExists()
    {
        var organization = TestOrganization.Create();
        var costCenterId = organization.AddCostCenter("CC100", null, TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveCostCenterCommand(organization.Id.Value, organization.TenantId, costCenterId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
