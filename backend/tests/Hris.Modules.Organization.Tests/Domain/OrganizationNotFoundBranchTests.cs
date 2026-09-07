using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

/// <summary>
/// Covers the "given id does not exist on this Aggregate" branch for every
/// Organization method that resolves a child entity by id -- the branch every
/// happy-path test elsewhere in this suite never exercises.
/// </summary>
public sealed class OrganizationNotFoundBranchTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RenameBusinessUnit_Fails_WhenBusinessUnitDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RenameBusinessUnit(new BusinessUnitId(Guid.NewGuid()), "New", _now);

        result.Error.Should().Be(OrganizationErrors.BusinessUnitNotFound);
    }

    [Fact]
    public void ArchiveBusinessUnit_Fails_WhenBusinessUnitDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.ArchiveBusinessUnit(new BusinessUnitId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.BusinessUnitNotFound);
    }

    [Fact]
    public void RestoreBusinessUnit_Fails_WhenBusinessUnitDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RestoreBusinessUnit(new BusinessUnitId(Guid.NewGuid()));

        result.Error.Should().Be(OrganizationErrors.BusinessUnitNotFound);
    }

    [Fact]
    public void RenameDivision_Fails_WhenDivisionDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RenameDivision(new DivisionId(Guid.NewGuid()), "New", _now);

        result.Error.Should().Be(OrganizationErrors.DivisionNotFound);
    }

    [Fact]
    public void MoveDivision_Fails_WhenDivisionDoesNotExist()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;

        var result = organization.MoveDivision(new DivisionId(Guid.NewGuid()), businessUnitId, _now);

        result.Error.Should().Be(OrganizationErrors.DivisionNotFound);
    }

    [Fact]
    public void ArchiveDivision_Fails_WhenDivisionDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.ArchiveDivision(new DivisionId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.DivisionNotFound);
    }

    [Fact]
    public void AddDepartment_Fails_WhenDivisionDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.AddDepartment(new DivisionId(Guid.NewGuid()), "HR", "HR", _now);

        result.Error.Should().Be(OrganizationErrors.DivisionNotFound);
    }

    [Fact]
    public void RenameDepartment_Fails_WhenDepartmentDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RenameDepartment(new DepartmentId(Guid.NewGuid()), "New", _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNotFound);
    }

    [Fact]
    public void MoveDepartment_Fails_WhenDepartmentDoesNotExist()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);

        var result = organization.MoveDepartment(new DepartmentId(Guid.NewGuid()), divisionId, _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNotFound);
    }

    [Fact]
    public void MoveDepartment_Fails_WhenTargetDivisionDoesNotExist()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;

        var result = organization.MoveDepartment(departmentId, new DivisionId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.InvalidHierarchyMove);
    }

    [Fact]
    public void ArchiveDepartment_Fails_WhenDepartmentDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.ArchiveDepartment(new DepartmentId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNotFound);
    }

    [Fact]
    public void RestoreDepartment_Fails_WhenDepartmentDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RestoreDepartment(new DepartmentId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNotFound);
    }

    [Fact]
    public void AddSection_Fails_WhenDepartmentDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.AddSection(new DepartmentId(Guid.NewGuid()), "Recruitment", _now);

        result.Error.Should().Be(OrganizationErrors.DepartmentNotFound);
    }

    [Fact]
    public void RenameSection_Fails_WhenSectionDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RenameSection(new SectionId(Guid.NewGuid()), "New", _now);

        result.Error.Should().Be(OrganizationErrors.SectionNotFound);
    }

    [Fact]
    public void ArchiveSection_Fails_WhenSectionDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.ArchiveSection(new SectionId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.SectionNotFound);
    }

    [Fact]
    public void AddTeam_Fails_WhenSectionDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.AddTeam(new SectionId(Guid.NewGuid()), "Sourcing", _now);

        result.Error.Should().Be(OrganizationErrors.SectionNotFound);
    }

    [Fact]
    public void RenameTeam_Fails_WhenTeamDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.RenameTeam(new TeamId(Guid.NewGuid()), "New", _now);

        result.Error.Should().Be(OrganizationErrors.TeamNotFound);
    }

    [Fact]
    public void ArchiveTeam_Fails_WhenTeamDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.ArchiveTeam(new TeamId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.TeamNotFound);
    }

    [Fact]
    public void UpdateCostCenter_Fails_WhenCostCenterDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.UpdateCostCenter(new CostCenterId(Guid.NewGuid()), "desc", _now);

        result.Error.Should().Be(OrganizationErrors.CostCenterNotFound);
    }

    [Fact]
    public void ArchiveCostCenter_Fails_WhenCostCenterDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.ArchiveCostCenter(new CostCenterId(Guid.NewGuid()), _now);

        result.Error.Should().Be(OrganizationErrors.CostCenterNotFound);
    }

    [Fact]
    public void RenameBusinessUnit_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        organization.ArchiveBusinessUnit(businessUnitId, _now);

        var result = organization.RenameBusinessUnit(businessUnitId, "New", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void RenameDivision_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Software", _now).Value;
        organization.ArchiveDivision(divisionId, _now);

        var result = organization.RenameDivision(divisionId, "New", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void RenameDepartment_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        organization.ArchiveDepartment(departmentId, _now);

        var result = organization.RenameDepartment(departmentId, "New", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void RenameSection_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        organization.ArchiveSection(sectionId, _now);

        var result = organization.RenameSection(sectionId, "New", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void RenameTeam_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        var teamId = organization.AddTeam(sectionId, "Sourcing", _now).Value;
        organization.ArchiveTeam(teamId, _now);

        var result = organization.RenameTeam(teamId, "New", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void UpdateCostCenter_Fails_WhenArchived()
    {
        var organization = CreateOrganization();
        var costCenterId = organization.AddCostCenter("CC100", null, _now).Value;
        organization.ArchiveCostCenter(costCenterId, _now);

        var result = organization.UpdateCostCenter(costCenterId, "desc", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void AddSection_Fails_WhenDepartmentIsArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        organization.ArchiveDepartment(departmentId, _now);

        var result = organization.AddSection(departmentId, "Recruitment", _now);

        result.Error.Should().Be(OrganizationErrors.ParentArchived);
    }

    [Fact]
    public void AddTeam_Fails_WhenSectionIsArchived()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "HR", "HR", _now).Value;
        var sectionId = organization.AddSection(departmentId, "Recruitment", _now).Value;
        organization.ArchiveSection(sectionId, _now);

        var result = organization.AddTeam(sectionId, "Sourcing", _now);

        result.Error.Should().Be(OrganizationErrors.ParentArchived);
    }

    [Fact]
    public void AddCostCenter_Fails_WhenOrganizationIsArchived()
    {
        var organization = CreateOrganization();
        organization.Archive(_now);

        var result = organization.AddCostCenter("CC100", null, _now);

        result.Error.Should().Be(OrganizationErrors.ParentArchived);
    }

    [Fact]
    public void Update_Fails_WhenOrganizationIsArchived()
    {
        var organization = CreateOrganization();
        organization.Archive(_now);

        var result = organization.Update(null, "desc", _now);

        result.Error.Should().Be(OrganizationErrors.ArchivedCannotBeModified);
    }

    private static DivisionId AddDivision(Hris.Modules.Organization.Domain.Organization organization)
    {
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        return organization.AddDivision(businessUnitId, "Shared Services", _now).Value;
    }

    private static Hris.Modules.Organization.Domain.Organization CreateOrganization() =>
        Hris.Modules.Organization.Domain.Organization.Create(new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "CORP", null, null, _now).Value;
}
