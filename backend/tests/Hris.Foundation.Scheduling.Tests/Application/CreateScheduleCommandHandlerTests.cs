using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class CreateScheduleCommandHandlerTests
{
    private readonly IScheduleRepository _repository = Substitute.For<IScheduleRepository>();
    private readonly CreateScheduleCommandHandler _handler;

    public CreateScheduleCommandHandlerTests()
    {
        _handler = new CreateScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static CreateScheduleCommand ValidCommand() =>
        new(TestData.TenantId, ScheduleType.CronBased, "0 0 * * *", "Asia/Manila", "PayrollProcessing", null, HolidayBehavior.ExecuteNormally, null);

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewSchedule()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Schedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenExpressionIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand() with { Expression = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleExpressionRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Schedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTaskTypeIsMissing()
    {
        var result = await _handler.Handle(ValidCommand() with { TaskType = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.TaskTypeRequired);
    }
}
