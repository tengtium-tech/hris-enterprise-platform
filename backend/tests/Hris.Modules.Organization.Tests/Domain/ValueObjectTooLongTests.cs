using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

/// <summary>
/// Exercises the "exceeds maximum length" branch of every short validated Value
/// Object, the branch every other test in this suite only reaches via the
/// "required" path.
/// </summary>
public sealed class ValueObjectTooLongTests
{
    private static readonly string _tooLong = new('A', 300);

    [Fact]
    public void OrganizationName_Fails_WhenTooLong() =>
        OrganizationName.Create(_tooLong).Error.Should().Be(OrganizationErrors.OrganizationNameTooLong);

    [Fact]
    public void OrganizationCode_Fails_WhenTooLong() =>
        OrganizationCode.Create(_tooLong).Error.Should().Be(OrganizationErrors.OrganizationCodeTooLong);

    [Fact]
    public void BusinessUnitName_Fails_WhenTooLong() =>
        BusinessUnitName.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void DivisionName_Fails_WhenTooLong() =>
        DivisionName.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void DepartmentName_Fails_WhenTooLong() =>
        DepartmentName.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void DepartmentCode_Fails_WhenTooLong() =>
        DepartmentCode.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void SectionName_Fails_WhenTooLong() =>
        SectionName.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void TeamName_Fails_WhenTooLong() =>
        TeamName.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void CostCenterCode_Fails_WhenTooLong() =>
        CostCenterCode.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void LocationCode_Fails_WhenTooLong() =>
        LocationCode.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void BusinessRegistrationNumber_Fails_WhenTooLong() =>
        BusinessRegistrationNumber.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void TaxIdentificationNumber_Fails_WhenTooLong() =>
        TaxIdentificationNumber.Create(_tooLong).IsFailure.Should().BeTrue();

    [Fact]
    public void TaxIdentificationNumber_Succeeds_WithAValidValue() =>
        TaxIdentificationNumber.Create("TIN-12345").IsSuccess.Should().BeTrue();

    [Fact]
    public void TaxIdentificationNumber_Fails_WhenMissing() =>
        TaxIdentificationNumber.Create(null).Error.Should().Be(OrganizationErrors.TaxIdentificationNumberRequired);

    [Fact]
    public void BusinessUnitName_Fails_WhenMissing() =>
        BusinessUnitName.Create(null).Error.Should().Be(OrganizationErrors.BusinessUnitNameRequired);

    [Fact]
    public void DivisionName_Fails_WhenMissing() =>
        DivisionName.Create(null).Error.Should().Be(OrganizationErrors.DivisionNameRequired);

    [Fact]
    public void DepartmentName_Fails_WhenMissing() =>
        DepartmentName.Create(null).Error.Should().Be(OrganizationErrors.DepartmentNameRequired);

    [Fact]
    public void DepartmentCode_Fails_WhenMissing() =>
        DepartmentCode.Create(null).Error.Should().Be(OrganizationErrors.DepartmentCodeRequired);

    [Fact]
    public void SectionName_Fails_WhenMissing() =>
        SectionName.Create(null).Error.Should().Be(OrganizationErrors.SectionNameRequired);

    [Fact]
    public void TeamName_Fails_WhenMissing() =>
        TeamName.Create(null).Error.Should().Be(OrganizationErrors.TeamNameRequired);

    [Fact]
    public void CostCenterCode_Fails_WhenMissing() =>
        CostCenterCode.Create(null).Error.Should().Be(OrganizationErrors.CostCenterCodeRequired);

    [Fact]
    public void LocationCode_Fails_WhenMissing() =>
        LocationCode.Create(null).Error.Should().Be(OrganizationErrors.LocationCodeRequired);

    [Fact]
    public void WorkLocationTimeZone_Fails_WhenMissing() =>
        WorkLocationTimeZone.Create(null).Error.Should().Be(OrganizationErrors.TimeZoneRequired);
}
