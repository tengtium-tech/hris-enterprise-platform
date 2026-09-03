using FluentAssertions;
using Hris.Foundation.Scheduling.Domain;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Domain;

public sealed class ScheduleExpressionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = ScheduleExpression.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleExpressionRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = ScheduleExpression.Create(new string('a', 501));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleExpressionTooLong);
    }

    [Fact]
    public void Create_Succeeds_AndTrims()
    {
        var result = ScheduleExpression.Create("  0 0 * * *  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("0 0 * * *");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        ScheduleExpression.Create("0 0 * * *").Value.Should().Be(ScheduleExpression.Create("0 0 * * *").Value);
    }
}
