using FluentAssertions;
using Hris.Foundation.JobProcessing.Domain;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Domain;

public sealed class WorkerTests
{
    [Fact]
    public void Start_Succeeds_AndRaisesWorkerStarted()
    {
        var result = Worker.Start("worker-0001", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.InstanceId.Should().Be("worker-0001");
        result.Value.Status.Should().Be(WorkerStatus.Running);
        result.Value.StartedAtUtc.Should().Be(TestData.NowUtc);
        result.Value.DomainEvents.OfType<WorkerStarted>().Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Start_Fails_WhenInstanceIdIsMissing(string? instanceId)
    {
        var result = Worker.Start(instanceId, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.WorkerInstanceIdRequired);
    }

    [Fact]
    public void Stop_Succeeds_FromRunning_AndRaisesWorkerStopped()
    {
        var worker = TestData.RunningWorker();

        var result = worker.Stop(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        worker.Status.Should().Be(WorkerStatus.Stopped);
        worker.StoppedAtUtc.Should().Be(TestData.NowUtc);
        worker.DomainEvents.OfType<WorkerStopped>().Should().ContainSingle();
    }

    [Fact]
    public void Stop_Fails_WhenAlreadyStopped()
    {
        var worker = TestData.RunningWorker();
        worker.Stop(TestData.NowUtc);

        var result = worker.Stop(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidWorkerLifecycleTransition);
    }
}
