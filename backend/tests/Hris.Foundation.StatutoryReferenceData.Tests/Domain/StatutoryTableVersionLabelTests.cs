using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Domain;

public sealed class StatutoryTableVersionLabelTests
{
    [Theory]
    [InlineData("2025-01")]
    [InlineData("2024-12")]
    public void Create_Succeeds_ForValidYearMonthFormat(string value)
    {
        var result = StatutoryTableVersionLabel.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenValueIsMissing(string? value)
    {
        var result = StatutoryTableVersionLabel.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.VersionLabelRequired);
    }

    [Theory]
    [InlineData("2025-13")]
    [InlineData("2025-00")]
    [InlineData("25-01")]
    [InlineData("2025/01")]
    public void Create_Fails_WhenValueIsNotYearMonthFormat(string value)
    {
        var result = StatutoryTableVersionLabel.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.VersionLabelInvalidFormat);
    }
}
