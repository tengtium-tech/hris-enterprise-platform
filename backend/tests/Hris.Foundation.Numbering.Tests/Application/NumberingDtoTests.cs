using FluentAssertions;
using Hris.Foundation.Numbering.Application.Dtos;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

/// <summary>
/// Confirms these DTOs behave as proper records (value equality, a real
/// <c>ToString</c>) -- FluentAssertions' <c>BeEquivalentTo</c>, used throughout the
/// handler tests, never exercises a record's own generated members, the identical gap
/// <c>FileStorageDtoTests</c> already closes for its own sibling framework.
/// </summary>
public sealed class NumberingDtoTests
{
    [Fact]
    public void NumberSeriesDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberSeriesDto(Guid.NewGuid(), "employee-numbers", "EMP", 6, true, false, "-", "Annual", 42, TestData.NowUtc);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberSeriesDto));
    }

    [Fact]
    public void IssuedNumberDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new IssuedNumberDto(
            Guid.NewGuid(), Guid.NewGuid(), 1, "EMP-2026-000001", "Reserved", "Employee", "EMP-0001", TestData.NowUtc, TestData.NowUtc);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(IssuedNumberDto));
    }
}
