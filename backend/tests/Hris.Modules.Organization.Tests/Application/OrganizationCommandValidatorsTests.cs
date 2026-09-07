using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Application.Queries;
using Hris.Modules.Organization.Application.Validators;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>IntegrationCommandValidatorsTests</c> already establishes for its own
/// framework.
/// </summary>
public sealed class OrganizationCommandValidatorsTests
{
    [Fact]
    public void CreateOrganizationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new CreateOrganizationCommandValidator();
        var valid = new CreateOrganizationCommand(Guid.NewGuid(), "ABC", "CORP", null, null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameOrganizationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNewName()
    {
        var validator = new RenameOrganizationCommandValidator();
        var valid = new RenameOrganizationCommand(Guid.NewGuid(), Guid.NewGuid(), "New Name");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { NewName = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateOrganizationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyOrganizationId()
    {
        var validator = new UpdateOrganizationCommandValidator();
        var valid = new UpdateOrganizationCommand(Guid.NewGuid(), Guid.NewGuid(), null, "desc");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { OrganizationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveOrganizationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyOrganizationId()
    {
        var validator = new ArchiveOrganizationCommandValidator();
        var valid = new ArchiveOrganizationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { OrganizationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RestoreOrganizationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyOrganizationId()
    {
        var validator = new RestoreOrganizationCommandValidator();
        var valid = new RestoreOrganizationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { OrganizationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateBusinessUnitCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new CreateBusinessUnitCommandValidator();
        var valid = new CreateBusinessUnitCommand(Guid.NewGuid(), Guid.NewGuid(), "Corporate");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameBusinessUnitCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyBusinessUnitId()
    {
        var validator = new RenameBusinessUnitCommandValidator();
        var valid = new RenameBusinessUnitCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Renamed");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { BusinessUnitId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveBusinessUnitCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyBusinessUnitId()
    {
        var validator = new ArchiveBusinessUnitCommandValidator();
        var valid = new ArchiveBusinessUnitCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { BusinessUnitId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RestoreBusinessUnitCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyBusinessUnitId()
    {
        var validator = new RestoreBusinessUnitCommandValidator();
        var valid = new RestoreBusinessUnitCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { BusinessUnitId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateDivisionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new CreateDivisionCommandValidator();
        var valid = new CreateDivisionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Software");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameDivisionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDivisionId()
    {
        var validator = new RenameDivisionCommandValidator();
        var valid = new RenameDivisionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Renamed");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { DivisionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MoveDivisionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNewBusinessUnitId()
    {
        var validator = new MoveDivisionCommandValidator();
        var valid = new MoveDivisionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { NewBusinessUnitId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveDivisionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDivisionId()
    {
        var validator = new ArchiveDivisionCommandValidator();
        var valid = new ArchiveDivisionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { DivisionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateDepartmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCode()
    {
        var validator = new CreateDepartmentCommandValidator();
        var valid = new CreateDepartmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Human Resources", "HR");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Code = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameDepartmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDepartmentId()
    {
        var validator = new RenameDepartmentCommandValidator();
        var valid = new RenameDepartmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Renamed");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { DepartmentId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MoveDepartmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNewDivisionId()
    {
        var validator = new MoveDepartmentCommandValidator();
        var valid = new MoveDepartmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { NewDivisionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MergeDepartmentsCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptySourceList()
    {
        var validator = new MergeDepartmentsCommandValidator();
        var valid = new MergeDepartmentsCommand(
            Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], "Payroll", "PAY");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { SourceDepartmentIds = [] }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SplitDepartmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNewDepartmentsList()
    {
        var validator = new SplitDepartmentCommandValidator();
        var valid = new SplitDepartmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            [new NewDepartmentSpec("Recruitment", "REC"), new NewDepartmentSpec("Employee Relations", "ERL")]);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { NewDepartments = [] }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveDepartmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDepartmentId()
    {
        var validator = new ArchiveDepartmentCommandValidator();
        var valid = new ArchiveDepartmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { DepartmentId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RestoreDepartmentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDepartmentId()
    {
        var validator = new RestoreDepartmentCommandValidator();
        var valid = new RestoreDepartmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { DepartmentId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateSectionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new CreateSectionCommandValidator();
        var valid = new CreateSectionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Recruitment");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameSectionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptySectionId()
    {
        var validator = new RenameSectionCommandValidator();
        var valid = new RenameSectionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Renamed");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { SectionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveSectionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptySectionId()
    {
        var validator = new ArchiveSectionCommandValidator();
        var valid = new ArchiveSectionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { SectionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTeamCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new CreateTeamCommandValidator();
        var valid = new CreateTeamCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Sourcing");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameTeamCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTeamId()
    {
        var validator = new RenameTeamCommandValidator();
        var valid = new RenameTeamCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Renamed");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TeamId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveTeamCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTeamId()
    {
        var validator = new ArchiveTeamCommandValidator();
        var valid = new ArchiveTeamCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TeamId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCostCenterCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCode()
    {
        var validator = new CreateCostCenterCommandValidator();
        var valid = new CreateCostCenterCommand(Guid.NewGuid(), Guid.NewGuid(), "CC100", null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Code = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateCostCenterCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCostCenterId()
    {
        var validator = new UpdateCostCenterCommandValidator();
        var valid = new UpdateCostCenterCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "desc");

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { CostCenterId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveCostCenterCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCostCenterId()
    {
        var validator = new ArchiveCostCenterCommandValidator();
        var valid = new ArchiveCostCenterCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { CostCenterId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateWorkLocationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCode()
    {
        var validator = new CreateWorkLocationCommandValidator();
        var valid = new CreateWorkLocationCommand(
            Guid.NewGuid(), "HQ", "Headquarters", Guid.NewGuid(), null, "123 Ayala Ave", null, "Makati", "Metro Manila",
            "1226", "Philippines", "Asia/Manila", null, null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Code = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateWorkLocationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new UpdateWorkLocationCommandValidator();
        var valid = new UpdateWorkLocationCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Headquarters", "123 Ayala Ave", null, "Makati", "Metro Manila", "1226",
            "Philippines", "Asia/Manila", null, null, null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveWorkLocationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyWorkLocationId()
    {
        var validator = new ArchiveWorkLocationCommandValidator();
        var valid = new ArchiveWorkLocationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { WorkLocationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RestoreWorkLocationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyWorkLocationId()
    {
        var validator = new RestoreWorkLocationCommandValidator();
        var valid = new RestoreWorkLocationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { WorkLocationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateLegalEntityCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyBusinessRegistrationNumber()
    {
        var validator = new CreateLegalEntityCommandValidator();
        var valid = new CreateLegalEntityCommand(
            Guid.NewGuid(), "TTS", "TengTium Software Inc.", null, "REG-1", null, "Philippines", null, null, null, null,
            null, null, null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { BusinessRegistrationNumber = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateLegalEntityCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new UpdateLegalEntityCommandValidator();
        var valid = new UpdateLegalEntityCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Renamed", null, null, "Philippines", null, null, null, null, null, null, null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveLegalEntityCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyLegalEntityId()
    {
        var validator = new ArchiveLegalEntityCommandValidator();
        var valid = new ArchiveLegalEntityCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { LegalEntityId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetOrganizationQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyOrganizationId()
    {
        var validator = new GetOrganizationQueryValidator();
        var valid = new GetOrganizationQuery(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { OrganizationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListOrganizationsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListOrganizationsQueryValidator();
        var valid = new ListOrganizationsQuery(Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetWorkLocationQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyWorkLocationId()
    {
        var validator = new GetWorkLocationQueryValidator();
        var valid = new GetWorkLocationQuery(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { WorkLocationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListWorkLocationsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListWorkLocationsQueryValidator();
        var valid = new ListWorkLocationsQuery(Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetLegalEntityQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyLegalEntityId()
    {
        var validator = new GetLegalEntityQueryValidator();
        var valid = new GetLegalEntityQuery(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { LegalEntityId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListLegalEntitiesQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListLegalEntitiesQueryValidator();
        var valid = new ListLegalEntitiesQuery(Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }
}
