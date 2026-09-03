using FluentAssertions;
using Hris.Foundation.JobProcessing.Domain;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These ten records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- the identical shape SchedulingEventsTests already establishes.
/// </summary>
public sealed class JobProcessingEventsTests
{
    [Fact]
    public void JobSubmitted_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobSubmitted(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()), TestData.TenantId, "PayrollCalculation", new JobQueueId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobSubmitted));
    }

    [Fact]
    public void JobQueued_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobQueued(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobQueued));
    }

    [Fact]
    public void JobStarted_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobStarted(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobStarted));
    }

    [Fact]
    public void JobCompleted_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobCompleted(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobCompleted));
    }

    [Fact]
    public void JobFailed_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobFailed(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()), "Timed out");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobFailed));
    }

    [Fact]
    public void JobRetried_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobRetried(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()), 1);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobRetried));
    }

    [Fact]
    public void JobCancelled_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobCancelled(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobCancelled));
    }

    [Fact]
    public void JobMovedToDlq_HasValueEquality_AndAUsefulToString()
    {
        var original = new JobMovedToDlq(Guid.NewGuid(), TestData.NowUtc, new JobId(Guid.NewGuid()), "Exceeded retry limit");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(JobMovedToDlq));
    }

    [Fact]
    public void WorkerStarted_HasValueEquality_AndAUsefulToString()
    {
        var original = new WorkerStarted(Guid.NewGuid(), TestData.NowUtc, new WorkerId(Guid.NewGuid()), "worker-0001");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(WorkerStarted));
    }

    [Fact]
    public void WorkerStopped_HasValueEquality_AndAUsefulToString()
    {
        var original = new WorkerStopped(Guid.NewGuid(), TestData.NowUtc, new WorkerId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(WorkerStopped));
    }
}
