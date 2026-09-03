using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

public sealed class NumberFormatTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_Fails_WhenRunningNumberLengthOutOfRange(int length)
    {
        var result = NumberFormat.Create(length, includeYear: true, includeMonth: false, "-");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.RunningNumberLengthOutOfRange);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_Fails_WhenSeparatorMissing(string? separator)
    {
        var result = NumberFormat.Create(6, includeYear: true, includeMonth: false, separator);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeparatorRequired);
    }

    [Fact]
    public void Create_Fails_WhenSeparatorTooLong()
    {
        var result = NumberFormat.Create(6, includeYear: true, includeMonth: false, "----");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.SeparatorTooLong);
    }

    [Fact]
    public void Format_ProducesPrefixAndRunningNumberOnly_WhenYearAndMonthExcluded()
    {
        var format = NumberFormat.Create(6, includeYear: false, includeMonth: false, "-").Value;
        var prefix = NumberPrefix.Create("EMP").Value;

        var result = format.Format(prefix, 123, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be("EMP-000123");
    }

    [Fact]
    public void Format_IncludesYear_WhenEnabled()
    {
        var format = NumberFormat.Create(6, includeYear: true, includeMonth: false, "-").Value;
        var prefix = NumberPrefix.Create("EMP").Value;

        var result = format.Format(prefix, 123, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be("EMP-2026-000123");
    }

    [Fact]
    public void Format_IncludesYearAndMonth_WhenBothEnabled()
    {
        var format = NumberFormat.Create(6, includeYear: true, includeMonth: true, "-").Value;
        var prefix = NumberPrefix.Create("PAY").Value;

        var result = format.Format(prefix, 21, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be("PAY-2026-08-000021");
    }

    [Fact]
    public void Format_ZeroPadsTheRunningNumber_ToTheConfiguredLength()
    {
        var format = NumberFormat.Create(4, includeYear: false, includeMonth: false, "-").Value;
        var prefix = NumberPrefix.Create("DOC").Value;

        var result = format.Format(prefix, 7, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be("DOC-0007");
    }

    [Fact]
    public void Format_UsesTheConfiguredSeparator()
    {
        var format = NumberFormat.Create(3, includeYear: false, includeMonth: false, "/").Value;
        var prefix = NumberPrefix.Create("LV").Value;

        var result = format.Format(prefix, 5, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be("LV/005");
    }

    [Fact]
    public void Equality_IsByComponents()
    {
        var first = NumberFormat.Create(6, true, false, "-").Value;
        var second = NumberFormat.Create(6, true, false, "-").Value;

        first.Should().Be(second);
    }
}
