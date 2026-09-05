using FluentAssertions;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Domain;

/// <summary>
/// Spot-checks entitlement-framework.md's own Edition Default Composition table
/// (DOC-011 Section 4.1), including the one deliberate divergence between Enterprise
/// and Government -- Employee Relations at Standard for Enterprise, Advanced for
/// Government.
/// </summary>
public sealed class EditionDefaultPackCompositionTests
{
    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsEssential_ForStarterTimeAndAttendance()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Starter, ProcessPackCode.TimeAndAttendance)
            .Should().Be(MaturityLevel.Essential);
    }

    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsNull_ForStarterBenefits()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Starter, ProcessPackCode.Benefits)
            .Should().BeNull();
    }

    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsEssential_ForGrowthBenefits()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Growth, ProcessPackCode.Benefits)
            .Should().Be(MaturityLevel.Essential);
    }

    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsNull_ForGrowthAnalytics()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Growth, ProcessPackCode.Analytics)
            .Should().BeNull();
    }

    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsAdvanced_ForEnterprisePayroll()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Enterprise, ProcessPackCode.Payroll)
            .Should().Be(MaturityLevel.Advanced);
    }

    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsStandard_ForEnterpriseEmployeeRelations()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Enterprise, ProcessPackCode.EmployeeRelations)
            .Should().Be(MaturityLevel.Standard);
    }

    [Fact]
    public void TryGetDefaultMaturityLevel_ReturnsAdvanced_ForGovernmentEmployeeRelations()
    {
        EditionDefaultPackComposition
            .TryGetDefaultMaturityLevel(TenantEditionCode.Government, ProcessPackCode.EmployeeRelations)
            .Should().Be(MaturityLevel.Advanced);
    }

    [Theory]
    [InlineData(TenantEditionCode.Starter)]
    [InlineData(TenantEditionCode.Growth)]
    [InlineData(TenantEditionCode.Enterprise)]
    [InlineData(TenantEditionCode.Government)]
    public void GetDefaultComposition_NeverContainsACorePack_ForAnyEdition(TenantEditionCode edition)
    {
        var composition = EditionDefaultPackComposition.GetDefaultComposition(edition);

        composition.Keys.Should().OnlyContain(pack => !ProcessPackCatalog.IsCore(pack));
    }

    [Fact]
    public void GetDefaultComposition_ForEnterprise_ContainsEveryOptionalPack()
    {
        var composition = EditionDefaultPackComposition.GetDefaultComposition(TenantEditionCode.Enterprise);

        composition.Should().HaveCount(14);
    }
}
