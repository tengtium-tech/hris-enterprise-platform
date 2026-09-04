using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Domain;

public sealed class StatutoryProgramCodeTests
{
    [Theory]
    [InlineData("sss", "SSS")]
    [InlineData("  bir_withholding  ", "BIR_WITHHOLDING")]
    public void Create_Succeeds_AndNormalizesToUppercase(string input, string expected)
    {
        var result = StatutoryProgramCode.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenValueIsMissing(string? value)
    {
        var result = StatutoryProgramCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramCodeRequired);
    }

    [Theory]
    [InlineData("SSS-2025")]
    [InlineData("sss program")]
    public void Create_Fails_WhenValueContainsDisallowedCharacters(string value)
    {
        var result = StatutoryProgramCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramCodeInvalidFormat);
    }

    [Fact]
    public void Create_Fails_WhenValueExceedsMaxLength()
    {
        var result = StatutoryProgramCode.Create(new string('A', 51));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramCodeInvalidFormat);
    }
}
