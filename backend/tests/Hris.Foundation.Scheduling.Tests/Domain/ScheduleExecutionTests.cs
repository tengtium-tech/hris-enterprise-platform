using FluentAssertions;
using Hris.Foundation.Scheduling.Domain;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Domain;

public sealed class ScheduleExecutionTests
{
    [Fact]
    public void Trigger_Succeeds_AndRaisesScheduleTriggered()
    {
        var scheduleId = new ScheduleId(Guid.NewGuid());

        var result = ScheduleExecution.Trigger(scheduleId, TestData.TenantId, "job-0001", 0, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScheduleId.Should().Be(scheduleId);
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.JobIdentifier.Should().Be("job-0001");
        result.Value.RetryCount.Should().Be(0);
        result.Value.Status.Should().Be(ScheduleExecutionStatus.Triggered);
        result.Value.TriggeredAtUtc.Should().Be(TestData.NowUtc);
        result.Value.DomainEvents.OfType<ScheduleTriggered>().Should().ContainSingle();
    }

    [Fact]
    public void Trigger_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => ScheduleExecution.Trigger(new ScheduleId(Guid.NewGuid()), Guid.Empty, "job-0001", 0, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Trigger_Fails_WhenRetryCountIsNegative()
    {
        var result = ScheduleExecution.Trigger(new ScheduleId(Guid.NewGuid()), TestData.TenantId, "job-0001", -1, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.RetryCountNegative);
    }

    [Fact]
    public void Complete_Succeeds_AndRaisesScheduleCompleted()
    {
        var execution = TestData.TriggeredExecution();

        var result = execution.Complete(1500, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ScheduleExecutionStatus.Completed);
        execution.DurationMs.Should().Be(1500);
        execution.CompletedAtUtc.Should().Be(TestData.NowUtc);
        execution.DomainEvents.OfType<ScheduleCompleted>().Should().ContainSingle();
    }

    [Fact]
    public void Complete_Fails_WhenAlreadyCompleted()
    {
        var execution = TestData.TriggeredExecution();
        execution.Complete(1500, TestData.NowUtc);

        var result = execution.Complete(1500, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleExecutionTransition);
    }

    [Fact]
    public void Fail_Succeeds_AndRaisesScheduleFailed()
    {
        var execution = TestData.TriggeredExecution();

        var result = execution.Fail("Payroll calculation timed out.", 5000, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ScheduleExecutionStatus.Failed);
        execution.FailureReason.Should().Be("Payroll calculation timed out.");
        execution.DomainEvents.OfType<ScheduleFailed>().Should().ContainSingle();
    }

    [Fact]
    public void Fail_Fails_WhenReasonIsMissing()
    {
        var execution = TestData.TriggeredExecution();

        var result = execution.Fail(" ", 5000, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.FailureReasonRequired);
    }

    [Fact]
    public void Fail_Fails_WhenAlreadyCompleted()
    {
        var execution = TestData.TriggeredExecution();
        execution.Complete(1500, TestData.NowUtc);

        var result = execution.Fail("too late", 100, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.InvalidScheduleExecutionTransition);
    }
}
