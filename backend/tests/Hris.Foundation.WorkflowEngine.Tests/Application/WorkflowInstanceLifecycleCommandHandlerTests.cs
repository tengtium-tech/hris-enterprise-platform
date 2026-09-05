using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class WorkflowInstanceLifecycleCommandHandlerTests
{
    private readonly IWorkflowInstanceRepository _repository = Substitute.For<IWorkflowInstanceRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.NowUtc);

    [Fact]
    public async Task Advance_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.SubmittedInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new AdvanceWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new AdvanceWorkflowInstanceCommand(instance.Id.Value, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.InProgress);
    }

    [Fact]
    public async Task Advance_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new AdvanceWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new AdvanceWorkflowInstanceCommand(Guid.NewGuid(), 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task RequestApproval_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.InProgressInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new RequestWorkflowInstanceApprovalCommandHandler(_repository);

        var result = await handler.Handle(new RequestWorkflowInstanceApprovalCommand(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.PendingApproval);
    }

    [Fact]
    public async Task ResumeAfterApproval_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.PendingApprovalInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new ResumeWorkflowInstanceAfterApprovalCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ResumeWorkflowInstanceAfterApprovalCommand(instance.Id.Value, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.InProgress);
        instance.CurrentStepOrder.Should().Be(2);
    }

    [Fact]
    public async Task Reject_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.PendingApprovalInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new RejectWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new RejectWorkflowInstanceCommand(instance.Id.Value, "Insufficient balance"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Rejected);
    }

    [Fact]
    public async Task Cancel_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.InProgressInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new CancelWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CancelWorkflowInstanceCommand(instance.Id.Value, "No longer needed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Cancelled);
    }

    [Fact]
    public async Task Withdraw_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.InProgressInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new WithdrawWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new WithdrawWorkflowInstanceCommand(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Withdrawn);
    }

    [Fact]
    public async Task Expire_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.PendingApprovalInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new ExpireWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ExpireWorkflowInstanceCommand(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Expired);
    }

    [Fact]
    public async Task Fail_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.InProgressInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new FailWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new FailWorkflowInstanceCommand(instance.Id.Value, "Downstream failure"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Failed);
    }

    [Fact]
    public async Task Complete_Succeeds_WhenInstanceExists()
    {
        var instance = TestData.InProgressInstance();
        _repository.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var handler = new CompleteWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CompleteWorkflowInstanceCommand(instance.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Completed);
    }

    [Fact]
    public async Task Complete_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new CompleteWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CompleteWorkflowInstanceCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task RequestApproval_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new RequestWorkflowInstanceApprovalCommandHandler(_repository);

        var result = await handler.Handle(new RequestWorkflowInstanceApprovalCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task ResumeAfterApproval_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new ResumeWorkflowInstanceAfterApprovalCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ResumeWorkflowInstanceAfterApprovalCommand(Guid.NewGuid(), 2), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task Reject_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new RejectWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new RejectWorkflowInstanceCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task Cancel_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new CancelWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new CancelWorkflowInstanceCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task Withdraw_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new WithdrawWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new WithdrawWorkflowInstanceCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task Expire_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new ExpireWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new ExpireWorkflowInstanceCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }

    [Fact]
    public async Task Fail_Fails_WhenInstanceDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowInstanceId>(), Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);
        var handler = new FailWorkflowInstanceCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new FailWorkflowInstanceCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InstanceNotFound);
    }
}
