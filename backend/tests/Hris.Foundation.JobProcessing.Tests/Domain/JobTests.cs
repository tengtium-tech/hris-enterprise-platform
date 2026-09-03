using FluentAssertions;
using Hris.Foundation.JobProcessing.Domain;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Domain;

public sealed class JobTests
{
    [Fact]
    public void Submit_Succeeds_InSubmitted_AndRaisesJobSubmitted()
    {
        var jobQueueId = new JobQueueId(Guid.NewGuid());

        var result = Job.Submit(TestData.TenantId, "PayrollCalculation", jobQueueId, JobPriority.High, "payload-ref-1", TestData.UserId, 3, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.JobType.Should().Be("PayrollCalculation");
        result.Value.JobQueueId.Should().Be(jobQueueId);
        result.Value.Priority.Should().Be(JobPriority.High);
        result.Value.PayloadReference.Should().Be("payload-ref-1");
        result.Value.SubmittedByUserId.Should().Be(TestData.UserId);
        result.Value.MaxRetries.Should().Be(3);
        result.Value.RetryCount.Should().Be(0);
        result.Value.Status.Should().Be(JobStatus.Submitted);
        result.Value.DomainEvents.OfType<JobSubmitted>().Should().ContainSingle();
    }

    [Fact]
    public void Submit_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => Job.Submit(Guid.Empty, "PayrollCalculation", new JobQueueId(Guid.NewGuid()), JobPriority.Normal, null, null, 3, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_Fails_WhenJobTypeIsMissing(string? jobType)
    {
        var result = Job.Submit(TestData.TenantId, jobType, new JobQueueId(Guid.NewGuid()), JobPriority.Normal, null, null, 3, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobTypeRequired);
    }

    [Fact]
    public void Enqueue_Succeeds_FromSubmitted_AndRaisesJobQueued()
    {
        var job = TestData.SubmittedJob();

        var result = job.Enqueue(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Queued);
        job.DomainEvents.OfType<JobQueued>().Should().ContainSingle();
    }

    [Fact]
    public void Enqueue_Fails_WhenNotSubmitted()
    {
        var job = TestData.QueuedJob();

        var result = job.Enqueue(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }

    [Fact]
    public void MarkScheduled_Succeeds_FromQueued_AndRaisesNoEvent()
    {
        var job = TestData.QueuedJob();

        var result = job.MarkScheduled();

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Scheduled);
        job.DomainEvents.Should().HaveCount(2, "job-processing.md's own Domain Events list names no JobScheduled event");
    }

    [Fact]
    public void MarkScheduled_Fails_WhenNotQueued()
    {
        var job = TestData.SubmittedJob();

        var result = job.MarkScheduled();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }

    [Fact]
    public void Start_Succeeds_FromQueued_AndRaisesJobStarted()
    {
        var job = TestData.QueuedJob();

        var result = job.Start(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Running);
        job.StartedAtUtc.Should().Be(TestData.NowUtc);
        job.DomainEvents.OfType<JobStarted>().Should().ContainSingle();
    }

    [Fact]
    public void Start_Succeeds_FromScheduled()
    {
        var job = TestData.QueuedJob();
        job.MarkScheduled();

        var result = job.Start(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Running);
    }

    [Fact]
    public void Start_Fails_WhenSubmitted()
    {
        var job = TestData.SubmittedJob();

        var result = job.Start(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }

    [Fact]
    public void Complete_Succeeds_FromRunning_AndRaisesJobCompleted()
    {
        var job = TestData.RunningJob();

        var result = job.Complete(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Completed);
        job.CompletedAtUtc.Should().Be(TestData.NowUtc);
        job.DomainEvents.OfType<JobCompleted>().Should().ContainSingle();
    }

    [Fact]
    public void Complete_Fails_WhenNotRunning()
    {
        var job = TestData.QueuedJob();

        var result = job.Complete(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }

    [Fact]
    public void Fail_Succeeds_FromRunning_AndRaisesJobFailed()
    {
        var job = TestData.RunningJob();

        var result = job.Fail("Timed out.", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);
        job.FailureReason.Should().Be("Timed out.");
        job.DomainEvents.OfType<JobFailed>().Should().ContainSingle();
    }

    [Fact]
    public void Fail_Fails_WhenReasonIsMissing()
    {
        var job = TestData.RunningJob();

        var result = job.Fail(" ", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.FailureReasonRequired);
    }

    [Fact]
    public void Retry_Succeeds_FromFailed_AndRaisesJobRetried()
    {
        var job = TestData.FailedJob(maxRetries: 3);

        var result = job.Retry(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Queued);
        job.RetryCount.Should().Be(1);
        job.DomainEvents.OfType<JobRetried>().Should().ContainSingle(e => e.RetryCount == 1);
    }

    [Fact]
    public void Retry_Fails_WhenRetryLimitAlreadyReached()
    {
        var job = TestData.FailedJob(maxRetries: 1);
        job.Retry(TestData.NowUtc);
        job.Start(TestData.NowUtc);
        job.Fail("Failed again.", TestData.NowUtc);

        var result = job.Retry(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.RetryLimitExceeded);
    }

    [Fact]
    public void Retry_Fails_WhenNotFailed()
    {
        var job = TestData.QueuedJob();

        var result = job.Retry(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }

    [Fact]
    public void MoveToDeadLetterQueue_Succeeds_FromFailed_AndRaisesJobMovedToDlq()
    {
        var job = TestData.FailedJob();

        var result = job.MoveToDeadLetterQueue("Exceeded retry limit.", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.DeadLetter);
        job.FailureReason.Should().Be("Exceeded retry limit.");
        job.DomainEvents.OfType<JobMovedToDlq>().Should().ContainSingle();
    }

    [Fact]
    public void MoveToDeadLetterQueue_Fails_WhenNotFailed()
    {
        var job = TestData.QueuedJob();

        var result = job.MoveToDeadLetterQueue("reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }

    [Theory]
    [InlineData(JobStatus.Submitted)]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Running)]
    [InlineData(JobStatus.Failed)]
    public void Cancel_Succeeds_FromAnyNonTerminalState_AndRaisesJobCancelled(JobStatus status)
    {
        var job = status switch
        {
            JobStatus.Submitted => TestData.SubmittedJob(),
            JobStatus.Queued => TestData.QueuedJob(),
            JobStatus.Running => TestData.RunningJob(),
            JobStatus.Failed => TestData.FailedJob(),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        var result = job.Cancel(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Cancelled);
        job.DomainEvents.OfType<JobCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_Fails_WhenAlreadyCompleted()
    {
        var job = TestData.RunningJob();
        job.Complete(TestData.NowUtc);

        var result = job.Cancel(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.InvalidJobLifecycleTransition);
    }
}
