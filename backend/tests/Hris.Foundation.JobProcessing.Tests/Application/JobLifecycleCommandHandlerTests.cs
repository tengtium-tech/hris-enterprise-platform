using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Commands;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class JobLifecycleCommandHandlerTests
{
    private readonly IJobRepository _repository = Substitute.For<IJobRepository>();

    [Fact]
    public async Task EnqueueJob_Succeeds_WhenJobExists()
    {
        var job = TestData.SubmittedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new EnqueueJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new EnqueueJobCommand(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public async Task EnqueueJob_Fails_WhenJobDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<JobId>(), Arg.Any<CancellationToken>()).Returns((Job?)null);
        var handler = new EnqueueJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new EnqueueJobCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobNotFound);
    }

    [Fact]
    public async Task MarkJobScheduled_Succeeds_WhenJobExists()
    {
        var job = TestData.QueuedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new MarkJobScheduledCommandHandler(_repository);

        var result = await handler.Handle(new MarkJobScheduledCommand(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Scheduled);
    }

    [Fact]
    public async Task StartJob_Succeeds_WhenJobExists()
    {
        var job = TestData.QueuedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new StartJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new StartJobCommand(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Running);
    }

    [Fact]
    public async Task CompleteJob_Succeeds_WhenJobExists()
    {
        var job = TestData.RunningJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new CompleteJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CompleteJobCommand(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Completed);
    }

    [Fact]
    public async Task FailJob_Succeeds_WhenJobExists()
    {
        var job = TestData.RunningJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new FailJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new FailJobCommand(job.Id.Value, "Timed out."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Failed);
    }

    [Fact]
    public async Task RetryJob_Succeeds_WhenJobExists()
    {
        var job = TestData.FailedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new RetryJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new RetryJobCommand(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public async Task RetryJob_Fails_WhenRetryLimitExceeded()
    {
        var job = TestData.FailedJob(maxRetries: 0);
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new RetryJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new RetryJobCommand(job.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.RetryLimitExceeded);
    }

    [Fact]
    public async Task MoveJobToDeadLetterQueue_Succeeds_WhenJobExists()
    {
        var job = TestData.FailedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new MoveJobToDeadLetterQueueCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new MoveJobToDeadLetterQueueCommand(job.Id.Value, "Exceeded retry limit."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.DeadLetter);
    }

    [Fact]
    public async Task CancelJob_Succeeds_WhenJobExists()
    {
        var job = TestData.QueuedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var handler = new CancelJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CancelJobCommand(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Cancelled);
    }

    [Fact]
    public async Task CancelJob_Fails_WhenJobDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<JobId>(), Arg.Any<CancellationToken>()).Returns((Job?)null);
        var handler = new CancelJobCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CancelJobCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobNotFound);
    }
}
