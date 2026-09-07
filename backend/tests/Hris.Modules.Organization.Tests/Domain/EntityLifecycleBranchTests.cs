using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

/// <summary>
/// Covers the remaining "already archived" / "not archived" / "duplicate name"
/// branches on each child entity that the happy-path tests elsewhere in this suite
/// do not reach.
/// </summary>
public sealed class EntityLifecycleBranchTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ArchiveDepartment_Fails_WhenAlreadyArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        organization.ArchiveDepartment(departmentId, _now);

        var result = organization.ArchiveDepartment(departmentId, _now);

        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void RestoreDepartment_Fails_WhenNotArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;

        var result = organization.RestoreDepartment(departmentId, _now);

        result.Error.Should().Be(OrganizationErrors.NotArchived);
    }

    [Fact]
    public void MoveDepartment_Fails_WhenDepartmentIsArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var otherDivisionId = organization.AddDivision(organization.BusinessUnits[0].Id, "Other", _now).Value;
        organization.ArchiveDepartment(departmentId, _now);

        var result = organization.MoveDepartment(departmentId, otherDivisionId, _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void ArchiveDivision_Fails_WhenAlreadyArchived()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Software", _now).Value;
        organization.ArchiveDivision(divisionId, _now);

        var result = organization.ArchiveDivision(divisionId, _now);

        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void MoveDivision_Fails_WhenDivisionIsArchived()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Software", _now).Value;
        var otherBusinessUnitId = organization.AddBusinessUnit("Operations", _now).Value;
        organization.ArchiveDivision(divisionId, _now);

        var result = organization.MoveDivision(divisionId, otherBusinessUnitId, _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void ArchiveSection_Fails_WhenAlreadyArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        organization.ArchiveSection(sectionId, _now);

        var result = organization.ArchiveSection(sectionId, _now);

        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void AddTeam_Fails_OnDuplicateNameWithinSection()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        organization.AddTeam(sectionId, "Sourcing", _now);

        var result = organization.AddTeam(sectionId, "sourcing", _now);

        result.Error.Should().Be(OrganizationErrors.DuplicateTeamName);
    }

    [Fact]
    public void AddSection_Fails_OnDuplicateNameWithinDepartment()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        organization.AddSection(departmentId, "Recruitment", _now);

        var result = organization.AddSection(departmentId, "recruitment", _now);

        result.Error.Should().Be(OrganizationErrors.DuplicateSectionName);
    }

    [Fact]
    public void ArchiveTeam_Fails_WhenAlreadyArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        var teamId = organization.AddTeam(sectionId, "Sourcing", _now).Value;
        organization.ArchiveTeam(teamId, _now);

        var result = organization.ArchiveTeam(teamId, _now);

        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void RestoreBusinessUnit_Fails_WhenNotArchived()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;

        var result = organization.RestoreBusinessUnit(businessUnitId);

        result.Error.Should().Be(OrganizationErrors.NotArchived);
    }

    [Fact]
    public void ArchiveCostCenter_Fails_WhenAlreadyArchived()
    {
        var organization = CreateOrganization();
        var costCenterId = organization.AddCostCenter("CC100", null, _now).Value;
        organization.ArchiveCostCenter(costCenterId, _now);

        var result = organization.ArchiveCostCenter(costCenterId, _now);

        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void WorkLocation_Update_Fails_WhenArchived()
    {
        var workLocation = CreateWorkLocation();
        workLocation.Archive(_now);
        var address = Address.Create("New St", null, "Makati", null, null, "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        var result = workLocation.Update("Renamed", address, timeZone, null, null, _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void WorkLocation_Archive_Fails_WhenAlreadyArchived()
    {
        var workLocation = CreateWorkLocation();
        workLocation.Archive(_now);

        var result = workLocation.Archive(_now);

        result.Error.Should().Be(OrganizationErrors.AlreadyArchived);
    }

    [Fact]
    public void WorkLocation_Update_Fails_WhenNameIsMissing()
    {
        var workLocation = CreateWorkLocation();
        var address = Address.Create("New St", null, "Makati", null, null, "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        var result = workLocation.Update(null, address, timeZone, null, null, _now);

        result.Error.Should().Be(OrganizationErrors.LocationCodeRequired);
    }

    [Fact]
    public void LegalEntity_Update_Fails_WhenNameIsMissing()
    {
        var legalEntity = CreateLegalEntity();

        var result = legalEntity.Update(null, null, null, "Philippines", null, null, _now);

        result.Error.Should().Be(OrganizationErrors.LegalEntityNameRequired);
    }

    [Fact]
    public void LegalEntity_Update_Fails_WhenCountryIsMissing()
    {
        var legalEntity = CreateLegalEntity();

        var result = legalEntity.Update("New Name", null, null, null, null, null, _now);

        result.Error.Should().Be(OrganizationErrors.AddressCountryRequired);
    }

    [Fact]
    public void Address_Fails_WhenCityIsMissing()
    {
        var result = Address.Create("123 Ayala Ave", null, null, null, null, "Philippines");

        result.Error.Should().Be(OrganizationErrors.AddressCityRequired);
    }

    [Fact]
    public void Address_Succeeds_WithEveryOptionalFieldPopulated()
    {
        var result = Address.Create("123 Ayala Ave", "Unit 4B", "Makati", "Metro Manila", "1226", "Philippines");

        result.IsSuccess.Should().BeTrue();
        result.Value.Line2.Should().Be("Unit 4B");
    }

    [Fact]
    public void LegalEntity_Create_Fails_WhenCountryIsMissing()
    {
        var result = LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", null, "REG-1", null,
            null, null, null, _now);

        result.Error.Should().Be(OrganizationErrors.AddressCountryRequired);
    }

    [Fact]
    public void WorkLocation_Create_Fails_WhenNameIsMissing()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", null, null, "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        var result = WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), Guid.NewGuid(), "HQ", null, Guid.NewGuid(), null, address, timeZone, null, _now);

        result.Error.Should().Be(OrganizationErrors.LocationCodeRequired);
    }

    [Fact]
    public void Organization_Rename_Fails_WhenNewNameIsMissing()
    {
        var organization = CreateOrganization();

        var result = organization.Rename(null, _now);

        result.Error.Should().Be(OrganizationErrors.OrganizationNameRequired);
    }

    [Fact]
    public void RenameBusinessUnit_Fails_WhenNewNameIsMissing()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;

        var result = organization.RenameBusinessUnit(businessUnitId, null, _now);

        result.Error.Should().Be(OrganizationErrors.BusinessUnitNameRequired);
    }

    [Fact]
    public void RenameDivision_Fails_WhenNewNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);

        var result = organization.RenameDivision(divisionId, null, _now);

        result.Error.Should().Be(OrganizationErrors.DivisionNameRequired);
    }

    [Fact]
    public void AddBusinessUnit_Fails_WhenNameIsMissing()
    {
        var organization = CreateOrganization();

        var result = organization.AddBusinessUnit(null, _now);

        result.Error.Should().Be(OrganizationErrors.BusinessUnitNameRequired);
    }

    [Fact]
    public void AddDivision_Fails_WhenNameIsMissing()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;

        var result = organization.AddDivision(businessUnitId, null, _now);

        result.Error.Should().Be(OrganizationErrors.DivisionNameRequired);
    }

    [Fact]
    public void AddDepartment_Fails_WhenNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);

        var result = organization.AddDepartment(divisionId, null, "HR", _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNameRequired);
    }

    [Fact]
    public void AddDepartment_Fails_WhenCodeIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);

        var result = organization.AddDepartment(divisionId, "HR", null, _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentCodeRequired);
    }

    [Fact]
    public void AddSection_Fails_WhenNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;

        var result = organization.AddSection(departmentId, null, _now);

        result.Error.Should().Be(OrganizationErrors.SectionNameRequired);
    }

    [Fact]
    public void AddTeam_Fails_WhenNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;

        var result = organization.AddTeam(sectionId, null, _now);

        result.Error.Should().Be(OrganizationErrors.TeamNameRequired);
    }

    [Fact]
    public void AddCostCenter_Fails_WhenCodeIsMissing()
    {
        var organization = CreateOrganization();

        var result = organization.AddCostCenter(null, null, _now);

        result.Error.Should().Be(OrganizationErrors.CostCenterCodeRequired);
    }

    [Fact]
    public void RenameDepartment_Fails_WhenNewNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;

        var result = organization.RenameDepartment(departmentId, null, _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNameRequired);
    }

    [Fact]
    public void RenameSection_Fails_WhenNewNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;

        var result = organization.RenameSection(sectionId, null, _now);

        result.Error.Should().Be(OrganizationErrors.SectionNameRequired);
    }

    [Fact]
    public void RenameTeam_Fails_WhenNewNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        var teamId = organization.AddTeam(sectionId, "Sourcing", _now).Value;

        var result = organization.RenameTeam(teamId, null, _now);

        result.Error.Should().Be(OrganizationErrors.TeamNameRequired);
    }

    [Fact]
    public void MergeDepartments_Fails_WhenSurvivingNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var firstId = organization.AddDepartment(divisionId, "Payroll Ops", "PAY1", _now).Value;
        var secondId = organization.AddDepartment(divisionId, "Payroll Support", "PAY2", _now).Value;

        var result = organization.MergeDepartments([firstId, secondId], null, "PAY", _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNameRequired);
    }

    [Fact]
    public void MergeDepartments_Fails_WhenSurvivingCodeIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var firstId = organization.AddDepartment(divisionId, "Payroll Ops", "PAY1", _now).Value;
        var secondId = organization.AddDepartment(divisionId, "Payroll Support", "PAY2", _now).Value;

        var result = organization.MergeDepartments([firstId, secondId], "Payroll", null, _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentCodeRequired);
    }

    [Fact]
    public void SplitDepartment_Fails_WhenANewDepartmentNameIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var sourceId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;

        var result = organization.SplitDepartment(sourceId, [(null, "REC"), ("Employee Relations", "ERL")], _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNameRequired);
    }

    [Fact]
    public void SplitDepartment_Fails_WhenANewDepartmentCodeIsMissing()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var sourceId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;

        var result = organization.SplitDepartment(sourceId, [("Recruitment", null), ("Employee Relations", "ERL")], _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentCodeRequired);
    }

    private static DivisionId AddDivision(Hris.Modules.Organization.Domain.Organization organization)
    {
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        return organization.AddDivision(businessUnitId, "Shared Services", _now).Value;
    }

    private static Hris.Modules.Organization.Domain.Organization CreateOrganization() =>
        Hris.Modules.Organization.Domain.Organization.Create(
            new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "CORP", null, null, _now).Value;

    private static WorkLocation CreateWorkLocation()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", "Metro Manila", "1226", "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        return WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), Guid.NewGuid(), "HQ", "Headquarters", Guid.NewGuid(), null, address,
            timeZone, null, _now).Value;
    }

    private static LegalEntity CreateLegalEntity() =>
        LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", null, "REG-1", null,
            "Philippines", null, null, _now).Value;
}
