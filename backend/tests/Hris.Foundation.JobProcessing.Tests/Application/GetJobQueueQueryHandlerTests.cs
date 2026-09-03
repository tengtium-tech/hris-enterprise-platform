using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Queries;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class GetJobQueueQueryHandlerTests
{
    private readonly IJobQueueRepository _repository = Substitute.For<IJobQueueRepository>();
    private readonly GetJobQueueQueryHandler _handler;

    public GetJobQueueQueryHandlerTests()
    {
        _handler = new GetJobQueueQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenQueueExists()
    {
        var jobQueue = TestData.RegisteredQueue(TestData.NewQueueName("PayrollQueue"));
        _repository.GetByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>()).Returns(jobQueue);

        var result = await _handler.Handle(new GetJobQueueQuery("PayrollQueue"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("PayrollQueue");
    }

    [Fact]
    public async Task Handle_Fails_WhenQueueDoesNotExist()
    {
        _repository.GetByNameAsync(Arg.Any<JobQueueName>(), Arg.Any<CancellationToken>()).Returns((JobQueue?)null);

        var result = await _handler.Handle(new GetJobQueueQuery("PayrollQueue"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobQueueNotFound);
    }
}
