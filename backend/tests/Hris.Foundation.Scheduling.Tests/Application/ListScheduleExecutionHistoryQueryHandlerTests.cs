using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Queries;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class ListScheduleExecutionHistoryQueryHandlerTests
{
    private readonly IScheduleExecutionRepository _repository = Substitute.For<IScheduleExecutionRepository>();
    private readonly ListScheduleExecutionHistoryQueryHandler _handler;

    public ListScheduleExecutionHistoryQueryHandlerTests()
    {
        _handler = new ListScheduleExecutionHistoryQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsTheScheduleExecutionHistory()
    {
        var scheduleId = new ScheduleId(Guid.NewGuid());
        var execution = TestData.TriggeredExecution(scheduleId);
        _repository.ListByScheduleAsync(scheduleId, TestData.TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ScheduleExecution> { execution });

        var result = await _handler.Handle(new ListScheduleExecutionHistoryQuery(scheduleId.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.ScheduleExecutionId == execution.Id.Value);
    }
}
