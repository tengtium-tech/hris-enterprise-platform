using FluentAssertions;
using Hris.Foundation.Tenant.Application.Dtos;
using Xunit;

namespace Hris.Foundation.Tenant.Tests.Application;

/// <summary>
/// Confirms these DTOs behave as proper records (value equality, a real
/// <c>ToString</c>) -- FluentAssertions' <c>BeEquivalentTo</c>, used throughout the
/// handler tests, never exercises a record's own generated <c>Equals</c>/
/// <c>GetHashCode</c>/<c>ToString</c>/copy-constructor, the identical gap
/// <c>AuthorizationDtoTests</c> already closes for its own sibling framework.
/// </summary>
public sealed class TenantDtoTests
{
    [Fact]
    public void TenantDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new TenantDto(Guid.NewGuid(), "acme-corp", "ACME", "Growth", "Active");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(TenantDto));
    }

    [Fact]
    public void PlatformDashboardSummaryDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new PlatformDashboardSummaryDto(
            new Dictionary<string, int> { ["Active"] = 5 },
            new Dictionary<string, int> { ["Growth"] = 3 },
            42);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(PlatformDashboardSummaryDto));
    }
}
