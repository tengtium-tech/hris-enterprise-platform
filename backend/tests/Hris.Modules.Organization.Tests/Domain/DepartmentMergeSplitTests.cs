using FluentAssertions;
using Hris.Modules.Organization.Domain;
using Xunit;

namespace Hris.Modules.Organization.Tests.Domain;

/// <summary>
/// Covers DEPT-005/DEPT-006 (merge) and its symmetric split operation, including
/// the traceability pointers (<see cref="Department.MergedIntoDepartmentId"/>/
/// <see cref="Department.SplitFromDepartmentId"/>) DEPT-006/DEPT-007 require.
/// </summary>
public sealed class DepartmentMergeSplitTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MergeDepartments_Succeeds_AndArchivesEverySourceWithATraceablePointer()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var firstId = organization.AddDepartment(divisionId, "Payroll Ops", "PAY1", _now).Value;
        var secondId = organization.AddDepartment(divisionId, "Payroll Support", "PAY2", _now).Value;

        var result = organization.MergeDepartments([firstId, secondId], "Payroll", "PAY", _now);

        result.IsSuccess.Should().BeTrue();
        var survivor = organization.FindDepartment(result.Value)!;
        survivor.Name.Value.Should().Be("Payroll");
        survivor.Code.Value.Should().Be("PAY");

        organization.FindDepartment(firstId)!.Status.Should().Be(OrganizationalUnitStatus.Archived);
        organization.FindDepartment(firstId)!.MergedIntoDepartmentId.Should().Be(result.Value);
        organization.FindDepartment(secondId)!.MergedIntoDepartmentId.Should().Be(result.Value);
        organization.DomainEvents.Should().Contain(e => e is DepartmentMerged);
    }

    [Fact]
    public void MergeDepartments_Fails_WithFewerThanTwoSources()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var onlyId = organization.AddDepartment(divisionId, "Payroll", "PAY", _now).Value;

        var result = organization.MergeDepartments([onlyId], "Merged", "MRG", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DepartmentMergeRequiresAtLeastTwoSources);
    }

    [Fact]
    public void MergeDepartments_Fails_WhenASourceDoesNotExist()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var realId = organization.AddDepartment(divisionId, "Payroll", "PAY", _now).Value;

        var result = organization.MergeDepartments([realId, new DepartmentId(Guid.NewGuid())], "Merged", "MRG", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DepartmentNotFound);
    }

    [Fact]
    public void SplitDepartment_Succeeds_AndArchivesTheSourceWithTraceablePointers()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var sourceId = organization.AddDepartment(divisionId, "Human Resources", "HR", _now).Value;

        var result = organization.SplitDepartment(
            sourceId, [("Recruitment", "REC"), ("Employee Relations", "ERL")], _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        organization.FindDepartment(sourceId)!.Status.Should().Be(OrganizationalUnitStatus.Archived);
        foreach (var newDepartmentId in result.Value)
        {
            organization.FindDepartment(newDepartmentId)!.SplitFromDepartmentId.Should().Be(sourceId);
        }

        organization.DomainEvents.Should().Contain(e => e is DepartmentSplit);
    }

    [Fact]
    public void SplitDepartment_Fails_WithFewerThanTwoTargets()
    {
        var organization = CreateOrganization();
        var divisionId = AddDivision(organization);
        var sourceId = organization.AddDepartment(divisionId, "Human Resources", "HR", _now).Value;

        var result = organization.SplitDepartment(sourceId, [("Recruitment", "REC")], _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DepartmentSplitRequiresAtLeastTwoTargets);
    }

    private static DivisionId AddDivision(Hris.Modules.Organization.Domain.Organization organization)
    {
        var businessUnitId = organization.AddBusinessUnit("Corporate", _now).Value;
        return organization.AddDivision(businessUnitId, "Shared Services", _now).Value;
    }

    private static Hris.Modules.Organization.Domain.Organization CreateOrganization() =>
        Hris.Modules.Organization.Domain.Organization.Create(new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "CORP", null, null, _now).Value;
}
