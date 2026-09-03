using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Commands;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class JobQueueCommandHandlerTests
{
    private readonly IJobQueueRepository _repository = Substitute.For<IJobQueueRepository>();

    [Fact]
    public async Task RegisterJobQueue_Succeeds_WhenNameIsAvailable()
    {
        _repository.ExistsByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RegisterJobQueueCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new RegisterJobQueueCommand("PayrollQueue", 5, 3, 60), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<JobQueue>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterJobQueue_Fails_WhenNameIsAlreadyRegistered()
    {
        _repository.ExistsByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RegisterJobQueueCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new RegisterJobQueueCommand("PayrollQueue", 5, 3, 60), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNameAlreadyRegistered);
        await _repository.DidNotReceive().AddAsync(Arg.Any<JobQueue>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateJobQueuePolicy_Succeeds_WhenQueueExists()
    {
        var jobQueue = TestData.RegisteredQueue();
        _repository.GetByIdAsync(jobQueue.Id, Arg.Any<CancellationToken>()).Returns(jobQueue);
        var handler = new UpdateJobQueuePolicyCommandHandler(_repository);

        var result = await handler.Handle(new UpdateJobQueuePolicyCommand(jobQueue.Id.Value, 10, 5, 120), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jobQueue.MaxConcurrency.Should().Be(10);
    }

    [Fact]
    public async Task UpdateJobQueuePolicy_Fails_WhenQueueDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<JobQueueId>(), Arg.Any<CancellationToken>()).Returns((JobQueue?)null);
        var handler = new UpdateJobQueuePolicyCommandHandler(_repository);

        var result = await handler.Handle(new UpdateJobQueuePolicyCommand(Guid.NewGuid(), 10, 5, 120), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNotFound);
    }
}
