using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class WorkflowTaskLifecycleCommandHandlerTests
{
    private readonly IWorkflowTaskRepository _repository = Substitute.For<IWorkflowTaskRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.NowUtc);

    [Fact]
    public async Task Approve_Succeeds_WhenTaskExists()
    {
        var task = TestData.PendingTask();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new ApproveWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ApproveWorkflowTaskCommand(task.Id.Value, "Looks good"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Approved);
    }

    [Fact]
    public async Task Approve_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new ApproveWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ApproveWorkflowTaskCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }

    [Fact]
    public async Task Reject_Succeeds_WhenTaskExists()
    {
        var task = TestData.PendingTask();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new RejectWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new RejectWorkflowTaskCommand(task.Id.Value, "Not eligible"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Rejected);
    }

    [Fact]
    public async Task Delegate_Succeeds_WhenTaskExists()
    {
        var task = TestData.PendingTask();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new DelegateWorkflowTaskCommandHandler(_repository, _timeProvider);
        var delegateToUserId = Guid.NewGuid();

        var result = await handler.Handle(
            new DelegateWorkflowTaskCommand(task.Id.Value, delegateToUserId, "Out of office"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.DelegatedToUserId.Should().Be(delegateToUserId);
    }

    [Fact]
    public async Task Escalate_Succeeds_AndCreatesANewTask_ForTheEscalationTarget()
    {
        var task = TestData.PendingTask(escalationLevel: 0);
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new EscalateWorkflowTaskCommandHandler(_repository, _timeProvider);
        var escalateToUserId = Guid.NewGuid();

        var result = await handler.Handle(new EscalateWorkflowTaskCommand(task.Id.Value, escalateToUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Escalated);
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkflowTask>(t => t.AssignedToUserId == escalateToUserId && t.EscalationLevel == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Escalate_Fails_WhenEscalateToUserIdIsEmpty_WithoutLoadingTheTask()
    {
        var handler = new EscalateWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new EscalateWorkflowTaskCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.EscalateToUserRequired);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Escalate_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new EscalateWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new EscalateWorkflowTaskCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }

    [Fact]
    public async Task Expire_Succeeds_WhenTaskExists()
    {
        var task = TestData.PendingTask();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new ExpireWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ExpireWorkflowTaskCommand(task.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Expired);
    }

    [Fact]
    public async Task Cancel_Succeeds_WhenTaskExists()
    {
        var task = TestData.PendingTask();
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var handler = new CancelWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CancelWorkflowTaskCommand(task.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Cancelled);
    }

    [Fact]
    public async Task Reject_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new RejectWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new RejectWorkflowTaskCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }

    [Fact]
    public async Task Delegate_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new DelegateWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new DelegateWorkflowTaskCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }

    [Fact]
    public async Task Expire_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new ExpireWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ExpireWorkflowTaskCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }

    [Fact]
    public async Task Cancel_Fails_WhenTaskDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowTaskId>(), Arg.Any<CancellationToken>()).Returns((WorkflowTask?)null);
        var handler = new CancelWorkflowTaskCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CancelWorkflowTaskCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TaskNotFound);
    }
}
