using FluentAssertions;
using Hris.Foundation.JobProcessing.Domain;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Domain;

public sealed class JobQueueNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNullOrWhitespace(string? value)
    {
        var result = JobQueueName.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNameRequired);
    }

    [Fact]
    public void Create_Fails_WhenTooLong()
    {
        var result = JobQueueName.Create(new string('a', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNameTooLong);
    }

    [Fact]
    public void Create_Succeeds_AndTrims()
    {
        var result = JobQueueName.Create("  PayrollQueue  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("PayrollQueue");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        JobQueueName.Create("PayrollQueue").Value.Should().Be(JobQueueName.Create("PayrollQueue").Value);
    }
}
