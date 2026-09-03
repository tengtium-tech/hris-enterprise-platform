using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Queries;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class GetJobQueryHandlerTests
{
    private readonly IJobRepository _repository = Substitute.For<IJobRepository>();
    private readonly GetJobQueryHandler _handler;

    public GetJobQueryHandlerTests()
    {
        _handler = new GetJobQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenJobExists()
    {
        var job = TestData.SubmittedJob();
        _repository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var result = await _handler.Handle(new GetJobQuery(job.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.JobId.Should().Be(job.Id.Value);
        result.Value.Status.Should().Be(nameof(JobStatus.Submitted));
    }

    [Fact]
    public async Task Handle_Fails_WhenJobDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<JobId>(), Arg.Any<CancellationToken>()).Returns((Job?)null);

        var result = await _handler.Handle(new GetJobQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.JobNotFound);
    }
}
