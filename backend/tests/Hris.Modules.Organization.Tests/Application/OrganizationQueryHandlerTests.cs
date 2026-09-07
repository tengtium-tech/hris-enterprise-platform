using FluentAssertions;
using Hris.Modules.Organization.Application.Queries;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class GetOrganizationQueryHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly GetOrganizationQueryHandler _handler;

    public GetOrganizationQueryHandlerTests()
    {
        _handler = new GetOrganizationQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenOrganizationExists()
    {
        var organization = TestOrganization.Create();
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new GetOrganizationQuery(organization.Id.Value, organization.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("CORP");
    }

    [Fact]
    public async Task Handle_MapsTheFullNestedHierarchy_IncludingCostCenters()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", TestOrganization.NowUtc).Value;
        organization.AddTeam(sectionId, "Sourcing", TestOrganization.NowUtc);
        organization.AddCostCenter("CC100", "IT Operations", TestOrganization.NowUtc);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new GetOrganizationQuery(organization.Id.Value, organization.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.BusinessUnits.Should().ContainSingle();
        dto.BusinessUnits[0].Divisions.Should().ContainSingle();
        dto.BusinessUnits[0].Divisions[0].Departments.Should().ContainSingle();
        dto.BusinessUnits[0].Divisions[0].Departments[0].Sections.Should().ContainSingle();
        dto.BusinessUnits[0].Divisions[0].Departments[0].Sections[0].Teams.Should().ContainSingle();
        dto.CostCenters.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenOrganizationBelongsToAnotherTenant()
    {
        var organization = TestOrganization.Create();
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new GetOrganizationQuery(organization.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.OrganizationNotFound);
    }
}

public sealed class ListOrganizationsQueryHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ListOrganizationsQueryHandler _handler;

    public ListOrganizationsQueryHandlerTests()
    {
        _handler = new ListOrganizationsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEverySummary_ForTheTenant()
    {
        var tenantId = Guid.NewGuid();
        var organization = TestOrganization.Create();
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<Hris.Modules.Organization.Domain.Organization> { organization });

        var result = await _handler.Handle(new ListOrganizationsQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}

public sealed class GetWorkLocationQueryHandlerTests
{
    private readonly IWorkLocationRepository _repository = Substitute.For<IWorkLocationRepository>();
    private readonly GetWorkLocationQueryHandler _handler;

    public GetWorkLocationQueryHandlerTests()
    {
        _handler = new GetWorkLocationQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenWorkLocationExists()
    {
        var workLocation = TestWorkLocation.Create();
        _repository.GetByIdAsync(workLocation.Id, Arg.Any<CancellationToken>()).Returns(workLocation);

        var result = await _handler.Handle(
            new GetWorkLocationQuery(workLocation.Id.Value, workLocation.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("HQ");
    }

    [Fact]
    public async Task Handle_MapsCoordinates_WhenPresent()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", "Metro Manila", "1226", "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;
        var coordinates = GeographicLocation.Create(14.5, 121.0).Value;
        var workLocation = WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), Guid.NewGuid(), "HQ", "Headquarters", Guid.NewGuid(), Guid.NewGuid(), address,
            timeZone, coordinates, TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(workLocation.Id, Arg.Any<CancellationToken>()).Returns(workLocation);

        var result = await _handler.Handle(
            new GetWorkLocationQuery(workLocation.Id.Value, workLocation.TenantId), CancellationToken.None);

        result.Value.Latitude.Should().Be(14.5);
        result.Value.Longitude.Should().Be(121.0);
    }
}

public sealed class ListWorkLocationsQueryHandlerTests
{
    private readonly IWorkLocationRepository _repository = Substitute.For<IWorkLocationRepository>();
    private readonly ListWorkLocationsQueryHandler _handler;

    public ListWorkLocationsQueryHandlerTests()
    {
        _handler = new ListWorkLocationsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEverySummary_ForTheTenant()
    {
        var tenantId = Guid.NewGuid();
        var workLocation = TestWorkLocation.Create();
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(new List<WorkLocation> { workLocation });

        var result = await _handler.Handle(new ListWorkLocationsQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}

public sealed class GetLegalEntityQueryHandlerTests
{
    private readonly ILegalEntityRepository _repository = Substitute.For<ILegalEntityRepository>();
    private readonly GetLegalEntityQueryHandler _handler;

    public GetLegalEntityQueryHandlerTests()
    {
        _handler = new GetLegalEntityQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenLegalEntityExists()
    {
        var legalEntity = TestLegalEntity.Create();
        _repository.GetByIdAsync(legalEntity.Id, Arg.Any<CancellationToken>()).Returns(legalEntity);

        var result = await _handler.Handle(
            new GetLegalEntityQuery(legalEntity.Id.Value, legalEntity.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("TTS");
    }

    [Fact]
    public async Task Handle_MapsOptionalFields_WhenPresent()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", "Metro Manila", "1226", "Philippines").Value;
        var currency = Hris.SharedKernel.CurrencyCode.Create("PHP").Value;
        var taxId = TaxIdentificationNumber.Create("TIN-1").Value;
        var legalEntity = LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", "TTS Registered",
            "REG-1", taxId, "Philippines", currency, address, TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(legalEntity.Id, Arg.Any<CancellationToken>()).Returns(legalEntity);

        var result = await _handler.Handle(
            new GetLegalEntityQuery(legalEntity.Id.Value, legalEntity.TenantId), CancellationToken.None);

        result.Value.Currency.Should().Be("PHP");
        result.Value.TaxIdentificationNumber.Should().Be("TIN-1");
        result.Value.RegisteredAddress.Should().NotBeNull();
    }
}

public sealed class ListLegalEntitiesQueryHandlerTests
{
    private readonly ILegalEntityRepository _repository = Substitute.For<ILegalEntityRepository>();
    private readonly ListLegalEntitiesQueryHandler _handler;

    public ListLegalEntitiesQueryHandlerTests()
    {
        _handler = new ListLegalEntitiesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEverySummary_ForTheTenant()
    {
        var tenantId = Guid.NewGuid();
        var legalEntity = TestLegalEntity.Create();
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(new List<LegalEntity> { legalEntity });

        var result = await _handler.Handle(new ListLegalEntitiesQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}
