using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class TriggerScheduleExecutionCommandHandlerTests
{
    private readonly IScheduleRepository _scheduleRepository = Substitute.For<IScheduleRepository>();
    private readonly IScheduleExecutionRepository _executionRepository = Substitute.For<IScheduleExecutionRepository>();
    private readonly TriggerScheduleExecutionCommandHandler _handler;

    public TriggerScheduleExecutionCommandHandlerTests()
    {
        _handler = new TriggerScheduleExecutionCommandHandler(_scheduleRepository, _executionRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndDerivesTenantIdFromTheSchedule()
    {
        var schedule = TestData.ActiveSchedule();
        _scheduleRepository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);

        var result = await _handler.Handle(new TriggerScheduleExecutionCommand(schedule.Id.Value, "job-0001", 0), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _executionRepository.Received(1).AddAsync(
            Arg.Is<ScheduleExecution>(e => e.ScheduleId == schedule.Id && e.TenantId == schedule.TenantId && e.JobIdentifier == "job-0001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenScheduleDoesNotExist()
    {
        _scheduleRepository.GetByIdAsync(Arg.Any<ScheduleId>(), Arg.Any<CancellationToken>()).Returns((Schedule?)null);

        var result = await _handler.Handle(new TriggerScheduleExecutionCommand(Guid.NewGuid(), "job-0001", 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleNotFound);
        await _executionRepository.DidNotReceive().AddAsync(Arg.Any<ScheduleExecution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenRetryCountIsNegative()
    {
        var schedule = TestData.ActiveSchedule();
        _scheduleRepository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);

        var result = await _handler.Handle(new TriggerScheduleExecutionCommand(schedule.Id.Value, "job-0001", -1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.RetryCountNegative);
    }
}
