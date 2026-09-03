using FluentAssertions;
using Hris.Foundation.JobProcessing.Domain;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Domain;

public sealed class JobQueueTests
{
    [Fact]
    public void Register_Succeeds_AndRaisesNoEvent()
    {
        var name = TestData.NewQueueName("PayrollQueue");

        var result = JobQueue.Register(name, 5, 3, 60, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.MaxConcurrency.Should().Be(5);
        result.Value.DefaultMaxRetries.Should().Be(3);
        result.Value.DefaultRetryDelaySeconds.Should().Be(60);
        result.Value.CreatedAtUtc.Should().Be(TestData.NowUtc);
        result.Value.DomainEvents.Should().BeEmpty("job-processing.md's own Domain Events list names no queue-registered event");
    }

    [Fact]
    public void Register_Fails_WhenMaxConcurrencyIsLessThanOne()
    {
        var result = JobQueue.Register(TestData.NewQueueName(), 0, 3, 60, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.MaxConcurrencyOutOfRange);
    }

    [Fact]
    public void Register_Fails_WhenDefaultMaxRetriesIsNegative()
    {
        var result = JobQueue.Register(TestData.NewQueueName(), 5, -1, 60, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.DefaultMaxRetriesNegative);
    }

    [Fact]
    public void Register_Fails_WhenDefaultRetryDelayIsNegative()
    {
        var result = JobQueue.Register(TestData.NewQueueName(), 5, 3, -1, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.DefaultRetryDelayNegative);
    }

    [Fact]
    public void UpdatePolicy_Succeeds_AndReplacesTheValues()
    {
        var jobQueue = TestData.RegisteredQueue();

        var result = jobQueue.UpdatePolicy(10, 5, 120);

        result.IsSuccess.Should().BeTrue();
        jobQueue.MaxConcurrency.Should().Be(10);
        jobQueue.DefaultMaxRetries.Should().Be(5);
        jobQueue.DefaultRetryDelaySeconds.Should().Be(120);
    }

    [Fact]
    public void UpdatePolicy_Fails_WhenMaxConcurrencyIsLessThanOne()
    {
        var jobQueue = TestData.RegisteredQueue();

        var result = jobQueue.UpdatePolicy(0, 3, 60);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.MaxConcurrencyOutOfRange);
    }
}
