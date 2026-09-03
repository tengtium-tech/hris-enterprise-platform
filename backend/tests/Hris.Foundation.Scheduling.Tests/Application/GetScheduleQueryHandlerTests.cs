using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Queries;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class GetScheduleQueryHandlerTests
{
    private readonly IScheduleRepository _repository = Substitute.For<IScheduleRepository>();
    private readonly GetScheduleQueryHandler _handler;

    public GetScheduleQueryHandlerTests()
    {
        _handler = new GetScheduleQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.DraftSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);

        var result = await _handler.Handle(new GetScheduleQuery(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScheduleId.Should().Be(schedule.Id.Value);
        result.Value.Status.Should().Be(nameof(ScheduleStatus.Draft));
    }

    [Fact]
    public async Task Handle_Fails_WhenScheduleDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ScheduleId>(), Arg.Any<CancellationToken>()).Returns((Schedule?)null);

        var result = await _handler.Handle(new GetScheduleQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleNotFound);
    }
}
