using FluentAssertions;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Domain;

public sealed class ProcessPackCatalogTests
{
    private static readonly ProcessPackCode[] _corePacks =
    {
        ProcessPackCode.Organization,
        ProcessPackCode.Employee,
        ProcessPackCode.Employment,
        ProcessPackCode.SelfServiceBasic,
        ProcessPackCode.AdministrationCore,
        ProcessPackCode.ComplianceBaseline,
        ProcessPackCode.ReportingBaseline,
    };

    private static readonly ProcessPackCode[] _optionalPacks =
    {
        ProcessPackCode.TimeAndAttendance,
        ProcessPackCode.Leave,
        ProcessPackCode.Payroll,
        ProcessPackCode.Benefits,
        ProcessPackCode.Recruitment,
        ProcessPackCode.Onboarding,
        ProcessPackCode.Performance,
        ProcessPackCode.Succession,
        ProcessPackCode.Learning,
        ProcessPackCode.EmployeeRelations,
        ProcessPackCode.OffboardingAndClearance,
        ProcessPackCode.Analytics,
        ProcessPackCode.Automation,
        ProcessPackCode.DeveloperPlatform,
    };

    [Theory]
    [MemberData(nameof(CorePackData))]
    public void IsCore_ReturnsTrue_ForEveryCorePack(ProcessPackCode pack)
    {
        ProcessPackCatalog.IsCore(pack).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(OptionalPackData))]
    public void IsCore_ReturnsFalse_ForEveryOptionalPack(ProcessPackCode pack)
    {
        ProcessPackCatalog.IsCore(pack).Should().BeFalse();
    }

    [Fact]
    public void AllPacks_ContainsExactlyTwentyOnePacks_MatchingDoc014Section4()
    {
        ProcessPackCatalog.AllPacks.Should().HaveCount(21);
        ProcessPackCatalog.AllPacks.Should().Contain(_corePacks).And.Contain(_optionalPacks);
    }

    [Theory]
    [MemberData(nameof(AllPackData))]
    public void GetDisplayName_ReturnsNonEmptyName_ForEveryPack(ProcessPackCode pack)
    {
        ProcessPackCatalog.GetDisplayName(pack).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetConditionalDependencies_ReturnsTimeAndAttendanceAndLeave_ForPayroll()
    {
        ProcessPackCatalog.GetConditionalDependencies(ProcessPackCode.Payroll).Should()
            .BeEquivalentTo(new[] { ProcessPackCode.TimeAndAttendance, ProcessPackCode.Leave });
    }

    [Fact]
    public void GetConditionalDependencies_ReturnsPayroll_ForOffboardingAndClearance()
    {
        ProcessPackCatalog.GetConditionalDependencies(ProcessPackCode.OffboardingAndClearance).Should()
            .BeEquivalentTo(new[] { ProcessPackCode.Payroll });
    }

    [Fact]
    public void GetConditionalDependencies_ReturnsEmpty_ForAPackWithNoDependency()
    {
        ProcessPackCatalog.GetConditionalDependencies(ProcessPackCode.Recruitment).Should().BeEmpty();
    }

    public static IEnumerable<object[]> CorePackData() => _corePacks.Select(pack => new object[] { pack });

    public static IEnumerable<object[]> OptionalPackData() => _optionalPacks.Select(pack => new object[] { pack });

    public static IEnumerable<object[]> AllPackData() => ProcessPackCatalog.AllPacks.Select(pack => new object[] { pack });
}
