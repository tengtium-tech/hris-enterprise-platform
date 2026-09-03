using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Commands;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class SubmitJobCommandHandlerTests
{
    private readonly IJobQueueRepository _jobQueueRepository = Substitute.For<IJobQueueRepository>();
    private readonly IJobRepository _jobRepository = Substitute.For<IJobRepository>();
    private readonly SubmitJobCommandHandler _handler;

    public SubmitJobCommandHandlerTests()
    {
        _handler = new SubmitJobCommandHandler(_jobQueueRepository, _jobRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static SubmitJobCommand ValidCommand(string queueName = "PayrollQueue") =>
        new(TestData.TenantId, "PayrollCalculation", queueName, JobPriority.Normal, "payload-ref-1", TestData.UserId, null);

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewJob_UsingTheQueuesOwnDefaultMaxRetries_WhenNoneIsGiven()
    {
        var jobQueue = TestData.RegisteredQueue(defaultMaxRetries: 5);
        _jobQueueRepository.GetByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>()).Returns(jobQueue);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _jobRepository.Received(1).AddAsync(
            Arg.Is<Job>(job => job.JobQueueId == jobQueue.Id && job.MaxRetries == 5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenQueueDoesNotExist()
    {
        _jobQueueRepository.GetByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>()).Returns((JobQueue?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNotFound);
        await _jobRepository.DidNotReceive().AddAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenQueueNameIsInvalid_WithoutCallingTheJobRepository()
    {
        var result = await _handler.Handle(ValidCommand(queueName: string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNameRequired);
        await _jobQueueRepository.DidNotReceive().GetByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>());
    }
}
