using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class ScheduleLifecycleCommandHandlerTests
{
    private readonly IScheduleRepository _repository = Substitute.For<IScheduleRepository>();

    [Fact]
    public async Task ValidateSchedule_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.DraftSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ValidateScheduleCommandHandler(_repository);

        var result = await handler.Handle(new ValidateScheduleCommand(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Validated);
    }

    [Fact]
    public async Task ValidateSchedule_Fails_WhenScheduleDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ScheduleId>(), Arg.Any<CancellationToken>()).Returns((Schedule?)null);
        var handler = new ValidateScheduleCommandHandler(_repository);

        var result = await handler.Handle(new ValidateScheduleCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleNotFound);
    }

    [Fact]
    public async Task ApproveSchedule_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.ValidatedSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ApproveScheduleCommandHandler(_repository);

        var result = await handler.Handle(new ApproveScheduleCommand(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Approved);
    }

    [Fact]
    public async Task ActivateSchedule_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.ApprovedSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ActivateScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new ActivateScheduleCommand(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Active);
    }

    [Fact]
    public async Task PauseSchedule_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.ActiveSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new PauseScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new PauseScheduleCommand(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Paused);
    }

    [Fact]
    public async Task ResumeSchedule_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.ActiveSchedule();
        schedule.Pause(TestData.NowUtc);
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new ResumeScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new ResumeScheduleCommand(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Resumed);
    }

    [Fact]
    public async Task RetireSchedule_Succeeds_WhenScheduleExists()
    {
        var schedule = TestData.ActiveSchedule();
        _repository.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>()).Returns(schedule);
        var handler = new RetireScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new RetireScheduleCommand(schedule.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        schedule.Status.Should().Be(ScheduleStatus.Retired);
    }

    [Fact]
    public async Task RetireSchedule_Fails_WhenScheduleDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ScheduleId>(), Arg.Any<CancellationToken>()).Returns((Schedule?)null);
        var handler = new RetireScheduleCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new RetireScheduleCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleNotFound);
    }
}
