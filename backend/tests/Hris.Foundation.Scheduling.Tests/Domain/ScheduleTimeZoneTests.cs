using FluentAssertions;
using Hris.Foundation.Scheduling.Domain;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Domain;

public sealed class ScheduleTimeZoneTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = ScheduleTimeZone.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.TimeZoneRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = ScheduleTimeZone.Create(new string('a', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.TimeZoneTooLong);
    }

    [Fact]
    public void Create_Succeeds_AndTrims()
    {
        var result = ScheduleTimeZone.Create("  Asia/Manila  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Asia/Manila");
    }
}
