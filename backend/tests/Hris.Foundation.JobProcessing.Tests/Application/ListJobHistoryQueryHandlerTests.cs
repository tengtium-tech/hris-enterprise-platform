using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Queries;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class ListJobHistoryQueryHandlerTests
{
    private readonly IJobRepository _repository = Substitute.For<IJobRepository>();
    private readonly ListJobHistoryQueryHandler _handler;

    public ListJobHistoryQueryHandlerTests()
    {
        _handler = new ListJobHistoryQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheQueuesOwnJobHistory()
    {
        var jobQueueId = new JobQueueId(Guid.NewGuid());
        var job = TestData.SubmittedJob(jobQueueId: jobQueueId);
        _repository.ListByQueueAsync(jobQueueId, TestData.TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Job> { job });

        var result = await _handler.Handle(new ListJobHistoryQuery(jobQueueId.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.JobId == job.Id.Value);
    }
}
