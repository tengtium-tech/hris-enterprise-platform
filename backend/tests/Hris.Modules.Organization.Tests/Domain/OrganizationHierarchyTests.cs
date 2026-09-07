using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

/// <summary>
/// Covers the Organization Aggregate's own child hierarchy
/// (BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt; Team, plus
/// CostCenter), including BU-001/DIV-001/DEPT-001/SEC-001-style per-parent name
/// uniqueness and the Move operations.
/// </summary>
public sealed class OrganizationHierarchyTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddBusinessUnit_Succeeds_AndRaisesEvent()
    {
        var organization = CreateOrganization();

        var result = organization.AddBusinessUnit("Corporate", _now);

        result.IsSuccess.Should().BeTrue();
        organization.BusinessUnits.Should().ContainSingle(bu => bu.Id == result.Value);
        organization.DomainEvents.Should().Contain(e => e is BusinessUnitCreated);
    }

    [Fact]
    public void AddBusinessUnit_Fails_OnDuplicateNameWithinOrganization()
    {
        var organization = CreateOrganization();
        organization.AddBusinessUnit("Corporate", _now);

        var result = organization.AddBusinessUnit("corporate", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateBusinessUnitName);
    }

    [Fact]
    public void AddBusinessUnit_Fails_WhenOrganizationIsArchived()
    {
        var organization = CreateOrganization();
        organization.Archive(_now);

        var result = organization.AddBusinessUnit("Corporate", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.ParentArchived);
    }

    [Fact]
    public void AddDivision_Fails_WhenBusinessUnitDoesNotExist()
    {
        var organization = CreateOrganization();

        var result = organization.AddDivision(new BusinessUnitId(Guid.NewGuid()), "Software", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.BusinessUnitNotFound);
    }

    [Fact]
    public void AddDivision_Fails_OnDuplicateNameWithinBusinessUnit()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        organization.AddDivision(businessUnitId, "Software", _now);

        var result = organization.AddDivision(businessUnitId, "Software", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateDivisionName);
    }

    [Fact]
    public void ArchiveBusinessUnit_PreventsAddingNewDivisions()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        organization.ArchiveBusinessUnit(businessUnitId, _now);

        var result = organization.AddDivision(businessUnitId, "Software", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.ParentArchived);
    }

    [Fact]
    public void RestoreBusinessUnit_AllowsAddingDivisionsAgain()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        organization.ArchiveBusinessUnit(businessUnitId, _now);
        organization.RestoreBusinessUnit(businessUnitId);

        var result = organization.AddDivision(businessUnitId, "Software", _now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AddDepartment_Succeeds_AndRaisesEvent()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);

        var result = organization.AddDepartment(divisionId, "Human Resources", "HR", _now);

        result.IsSuccess.Should().BeTrue();
        organization.DomainEvents.Should().Contain(e => e is DepartmentCreated);
    }

    [Fact]
    public void AddDepartment_Fails_OnDuplicateNameWithinDivision()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        organization.AddDepartment(divisionId, "Human Resources", "HR", _now);

        var result = organization.AddDepartment(divisionId, "Human Resources", "HR2", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateDepartmentName);
    }

    [Fact]
    public void MoveDivision_Succeeds_ToAnExistingBusinessUnit()
    {
        var organization = CreateOrganization();
        var sourceBusinessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        var divisionId = organization.AddDivision(sourceBusinessUnitId, "Software", _now).Value;
        var targetBusinessUnitId = organization.AddBusinessUnit("Operations", _now).Value;

        var result = organization.MoveDivision(divisionId, targetBusinessUnitId, _now);

        result.IsSuccess.Should().BeTrue();
        organization.FindDivision(divisionId)!.BusinessUnitId.Should().Be(targetBusinessUnitId);
        organization.DomainEvents.Should().Contain(e => e is DivisionMoved);
    }

    [Fact]
    public void MoveDivision_Fails_WhenTargetBusinessUnitDoesNotExist()
    {
        var organization = CreateOrganization();
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Software", _now).Value;

        var result = organization.MoveDivision(divisionId, new BusinessUnitId(Guid.NewGuid()), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.InvalidHierarchyMove);
    }

    [Fact]
    public void AddSection_And_AddTeam_SucceedThroughTheFullChain()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", _now).Value;

        var sectionResult = organization.AddSection(departmentId, "Recruitment", _now);
        sectionResult.IsSuccess.Should().BeTrue();

        var teamResult = organization.AddTeam(sectionResult.Value, "Sourcing Team", _now);
        teamResult.IsSuccess.Should().BeTrue();

        organization.FindTeam(teamResult.Value).Should().NotBeNull();
    }

    [Fact]
    public void AddCostCenter_Succeeds_AsADirectChildOfOrganization()
    {
        var organization = CreateOrganization();

        var result = organization.AddCostCenter("CC100", "IT Operations", _now);

        result.IsSuccess.Should().BeTrue();
        organization.CostCenters.Should().ContainSingle(cc => cc.Id == result.Value);
    }

    [Fact]
    public void ArchiveCostCenter_Succeeds()
    {
        var organization = CreateOrganization();
        var costCenterId = organization.AddCostCenter("CC100", null, _now).Value;

        var result = organization.ArchiveCostCenter(costCenterId, _now);

        result.IsSuccess.Should().BeTrue();
        organization.FindCostCenter(costCenterId)!.Status.Should().Be(OrganizationalUnitStatus.Archived);
    }

    private static DivisionId AddDivision(Hris.Modules.Organization.Domain.Organization organization)
    {
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        return organization.AddDivision(businessUnitId, "Software", _now).Value;
    }

    private static Hris.Modules.Organization.Domain.Organization CreateOrganization() =>
        Hris.Modules.Organization.Domain.Organization.Create(new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "CORP", null, null, _now).Value;
}
