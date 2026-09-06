using FluentAssertions;
using Hris.Foundation.Integration.Domain;
using System.Linq;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Domain;

public sealed class IntegrationRunTests
{
    [Fact]
    public void Start_WithIntegrationCallKind_RaisesIntegrationStarted()
    {
        var connectorId = new ConnectorId(Guid.NewGuid());

        var result = IntegrationRun.Start(
            TestData.TenantId, connectorId, IntegrationRunKind.IntegrationCall, null, null, maxRetries: 3, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(IntegrationRunStatus.Started);
        var raised = result.Value.DomainEvents.OfType<IntegrationStarted>().Should().ContainSingle().Subject;
        raised.IntegrationRunId.Should().Be(result.Value.Id);
        raised.TenantId.Should().Be(TestData.TenantId);
        raised.ConnectorId.Should().Be(connectorId);
    }

    [Fact]
    public void Start_WithSynchronizationKind_RaisesSynchronizationStarted()
    {
        var connectorId = new ConnectorId(Guid.NewGuid());

        var result = IntegrationRun.Start(
            TestData.TenantId, connectorId, IntegrationRunKind.Synchronization, "Incremental", null, maxRetries: 3, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.SyncModel.Should().Be("Incremental");
        result.Value.DomainEvents.Should().ContainSingle(e => e is SynchronizationStarted);
        result.Value.DomainEvents.Should().NotContain(e => e is IntegrationStarted);
    }

    [Fact]
    public void Start_WithWebhookDeliveryKind_RaisesIntegrationStarted()
    {
        var result = IntegrationRun.Start(
            TestData.TenantId, new ConnectorId(Guid.NewGuid()), IntegrationRunKind.WebhookDelivery, null, null, maxRetries: 3,
            TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(e => e is IntegrationStarted);
    }

    [Fact]
    public void Start_CarriesTheGivenJobId()
    {
        var jobId = Guid.NewGuid();

        var result = IntegrationRun.Start(
            TestData.TenantId, new ConnectorId(Guid.NewGuid()), IntegrationRunKind.IntegrationCall, null, jobId, maxRetries: 3,
            TestData.NowUtc);

        result.Value.JobId.Should().Be(jobId);
    }

    [Fact]
    public void Start_Fails_WhenTenantIdIsEmpty()
    {
        var act = () => IntegrationRun.Start(
            Guid.Empty, new ConnectorId(Guid.NewGuid()), IntegrationRunKind.IntegrationCall, null, null, maxRetries: 3, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_WithIntegrationCallKind_RaisesIntegrationCompleted()
    {
        var run = TestData.StartedRun(runKind: IntegrationRunKind.IntegrationCall);
        run.ClearDomainEvents();

        var result = run.Complete(42, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.Completed);
        run.RecordsProcessed.Should().Be(42);
        run.CompletedAtUtc.Should().Be(TestData.NowUtc);
        var raised = run.DomainEvents.OfType<IntegrationCompleted>().Should().ContainSingle().Subject;
        raised.RecordsProcessed.Should().Be(42);
    }

    [Fact]
    public void Complete_WithSynchronizationKind_RaisesSynchronizationCompleted()
    {
        var run = TestData.StartedRun(runKind: IntegrationRunKind.Synchronization);
        run.ClearDomainEvents();

        run.Complete(10, TestData.NowUtc);

        run.DomainEvents.Should().ContainSingle(e => e is SynchronizationCompleted);
    }

    [Fact]
    public void Complete_WithWebhookDeliveryKind_RaisesWebhookDelivered_NotAGenericCompletedEvent()
    {
        var run = TestData.StartedRun(runKind: IntegrationRunKind.WebhookDelivery);
        run.ClearDomainEvents();

        run.Complete(1, TestData.NowUtc);

        run.DomainEvents.Should().ContainSingle(e => e is WebhookDelivered);
        run.DomainEvents.Should().NotContain(e => e is IntegrationCompleted);
    }

    [Fact]
    public void Complete_Fails_WhenNotStarted()
    {
        var run = TestData.FailedRun();

        var result = run.Complete(1, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
    }

    [Fact]
    public void Fail_AlwaysRaisesIntegrationFailed_RegardlessOfRunKind()
    {
        var run = TestData.StartedRun(runKind: IntegrationRunKind.Synchronization);
        run.ClearDomainEvents();

        var result = run.Fail("connection refused", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.Failed);
        run.FailureReason.Should().Be("connection refused");
        run.DomainEvents.Should().ContainSingle(e => e is IntegrationFailed);
    }

    [Fact]
    public void Fail_Fails_WhenReasonIsEmpty()
    {
        var run = TestData.StartedRun();

        var result = run.Fail(string.Empty, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.FailureReasonRequired);
    }

    [Fact]
    public void Fail_Fails_WhenNotStarted()
    {
        var run = TestData.FailedRun();

        var result = run.Fail("another reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
    }

    [Fact]
    public void Retry_Succeeds_AndReturnsToStarted_IncrementingRetryCount()
    {
        var run = TestData.FailedRun();
        run.ClearDomainEvents();

        var result = run.Retry(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.Started);
        run.RetryCount.Should().Be(1);
        run.FailureReason.Should().BeNull();
        var raised = run.DomainEvents.OfType<RetryInitiated>().Should().ContainSingle().Subject;
        raised.RetryCount.Should().Be(1);
    }

    [Fact]
    public void Retry_Fails_WhenNotFailed()
    {
        var run = TestData.StartedRun();

        var result = run.Retry(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
    }

    [Fact]
    public void Retry_Fails_WhenRetryLimitAlreadyReached()
    {
        var run = TestData.StartedRun(maxRetries: 1);
        run.Fail("first failure", TestData.NowUtc);
        run.Retry(TestData.NowUtc);
        run.Fail("second failure", TestData.NowUtc);

        var result = run.Retry(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.RetryLimitExceeded);
    }

    [Fact]
    public void MoveToDeadLetter_Succeeds_FromFailed_AndRaisesNoEvent()
    {
        var run = TestData.FailedRun();
        run.ClearDomainEvents();

        var result = run.MoveToDeadLetter();

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.DeadLetter);
        run.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MoveToDeadLetter_Fails_WhenNotFailed()
    {
        var run = TestData.StartedRun();

        var result = run.MoveToDeadLetter();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.InvalidIntegrationRunLifecycleTransition);
    }
}
