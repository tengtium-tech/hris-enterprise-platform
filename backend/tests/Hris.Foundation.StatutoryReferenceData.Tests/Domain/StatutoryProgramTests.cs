using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Domain;

public sealed class StatutoryProgramTests
{
    [Fact]
    public void Register_Succeeds_AndRaisesNoEvent()
    {
        var code = TestData.NewProgramCode();
        var country = TestData.NewCountry();

        var result = StatutoryProgram.Register(code, country, "SSS Contribution Schedule", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code);
        result.Value.Country.Should().Be(country);
        result.Value.DisplayName.Should().Be("SSS Contribution Schedule");
        result.Value.RegisteredAtUtc.Should().Be(TestData.NowUtc);
        result.Value.DomainEvents.Should().BeEmpty(
            "statutory-reference-data.md names no registration event, the same asymmetry JobQueue.Register's own remarks state for itself");
    }

    [Fact]
    public void Register_Throws_WhenCodeIsNull()
    {
        var act = () => StatutoryProgram.Register(null!, TestData.NewCountry(), "SSS", TestData.NowUtc);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_Throws_WhenCountryIsNull()
    {
        var act = () => StatutoryProgram.Register(TestData.NewProgramCode(), null!, "SSS", TestData.NowUtc);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Fails_WhenDisplayNameIsMissing(string? displayName)
    {
        var result = StatutoryProgram.Register(TestData.NewProgramCode(), TestData.NewCountry(), displayName, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.DisplayNameRequired);
    }
}
