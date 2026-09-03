using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class UpdateScheduleCommandHandlerTests
{
    private readonly IScheduleRepository _repository = Substitute.For<IScheduleRepository>();
    private readonly UpdateScheduleCommandHandler _handler;

    public UpdateScheduleCommandHandlerTests()
    {
        _handler = new UpdateScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.DraftSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);

        var command = new UpdateScheduleCommand(schedule.Id.Value, "0 6 * * *", "Asia/Manila", "AttendanceReconciliation", null, HolidayBehavior.ExecuteNormally, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.TaskType.Should().Be("AttendanceReconciliation");
    }

    [Fact]
    public async Task Handle_Fails_WhenScheduleDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ScheduleId>(), Arg.Any<CancellationToken>()).Returns((Schedule?)null);

        var command = new UpdateScheduleCommand(Guid.NewGuid(), "0 6 * * *", "Asia/Manila", "AttendanceReconciliation", null, HolidayBehavior.ExecuteNormally, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleNotFound);
    }
}
