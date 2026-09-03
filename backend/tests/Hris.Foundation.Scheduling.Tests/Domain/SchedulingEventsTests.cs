using FluentAssertions;
using Hris.Foundation.Scheduling.Domain;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These nine records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- the identical shape SearchEventsTests already establishes.
/// </summary>
public sealed class SchedulingEventsTests
{
    [Fact]
    public void ScheduleCreated_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleCreated(Guid.NewGuid(), TestData.NowUtc, new ScheduleId(Guid.NewGuid()), TestData.TenantId, ScheduleType.CronBased);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleCreated));
    }

    [Fact]
    public void ScheduleUpdated_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleUpdated(Guid.NewGuid(), TestData.NowUtc, new ScheduleId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleUpdated));
    }

    [Fact]
    public void ScheduleActivated_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleActivated(Guid.NewGuid(), TestData.NowUtc, new ScheduleId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleActivated));
    }

    [Fact]
    public void SchedulePaused_HasValueEquality_AndAUsefulToString()
    {
        var original = new SchedulePaused(Guid.NewGuid(), TestData.NowUtc, new ScheduleId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SchedulePaused));
    }

    [Fact]
    public void ScheduleResumed_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleResumed(Guid.NewGuid(), TestData.NowUtc, new ScheduleId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleResumed));
    }

    [Fact]
    public void ScheduleRetired_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleRetired(Guid.NewGuid(), TestData.NowUtc, new ScheduleId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleRetired));
    }

    [Fact]
    public void ScheduleTriggered_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleTriggered(
            Guid.NewGuid(), TestData.NowUtc, new ScheduleExecutionId(Guid.NewGuid()), new ScheduleId(Guid.NewGuid()), TestData.TenantId);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleTriggered));
    }

    [Fact]
    public void ScheduleCompleted_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleCompleted(Guid.NewGuid(), TestData.NowUtc, new ScheduleExecutionId(Guid.NewGuid()), 1500);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleCompleted));
    }

    [Fact]
    public void ScheduleFailed_HasValueEquality_AndAUsefulToString()
    {
        var original = new ScheduleFailed(Guid.NewGuid(), TestData.NowUtc, new ScheduleExecutionId(Guid.NewGuid()), "Timed out");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ScheduleFailed));
    }
}
