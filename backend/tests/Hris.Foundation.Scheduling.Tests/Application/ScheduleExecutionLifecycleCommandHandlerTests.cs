using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

public sealed class ScheduleExecutionLifecycleCommandHandlerTests
{
    private readonly IScheduleExecutionRepository _repository = Substitute.For<IScheduleExecutionRepository>();

    [Fact]
    public async Task CompleteScheduleExecution_Succeeds_WhenExecutionExists()
    {
        var execution = TestData.TriggeredExecution();
        _repository.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        var handler = new CompleteScheduleExecutionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CompleteScheduleExecutionCommand(execution.Id.Value, 1500), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ScheduleExecutionStatus.Completed);
    }

    [Fact]
    public async Task CompleteScheduleExecution_Fails_WhenExecutionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ScheduleExecutionId>(), Arg.Any<CancellationToken>()).Returns((ScheduleExecution?)null);
        var handler = new CompleteScheduleExecutionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new CompleteScheduleExecutionCommand(Guid.NewGuid(), 1500), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleExecutionNotFound);
    }

    [Fact]
    public async Task FailScheduleExecution_Succeeds_WhenExecutionExists()
    {
        var execution = TestData.TriggeredExecution();
        _repository.GetByIdAsync(execution.Id, Arg.Any<CancellationToken>()).Returns(execution);
        var handler = new FailScheduleExecutionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new FailScheduleExecutionCommand(execution.Id.Value, "Timed out", 5000), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ScheduleExecutionStatus.Failed);
    }

    [Fact]
    public async Task FailScheduleExecution_Fails_WhenExecutionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<ScheduleExecutionId>(), Arg.Any<CancellationToken>()).Returns((ScheduleExecution?)null);
        var handler = new FailScheduleExecutionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new FailScheduleExecutionCommand(Guid.NewGuid(), "Timed out", 5000), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SchedulingErrors.ScheduleExecutionNotFound);
    }
}
