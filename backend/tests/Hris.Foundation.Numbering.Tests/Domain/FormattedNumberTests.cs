using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

public sealed class FormattedNumberTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = FormattedNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.FormattedNumberRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = FormattedNumber.Create(new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.FormattedNumberTooLong);
    }

    [Fact]
    public void Create_Succeeds_WithValidValue()
    {
        var result = FormattedNumber.Create("EMP-2026-000123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("EMP-2026-000123");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var formatted = FormattedNumber.Create("EMP-2026-000123").Value;

        formatted.ToString().Should().Be("EMP-2026-000123");
    }
}
