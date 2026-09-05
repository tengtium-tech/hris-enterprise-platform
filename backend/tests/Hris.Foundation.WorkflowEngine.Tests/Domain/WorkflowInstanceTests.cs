using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Domain;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Domain;

public sealed class WorkflowInstanceTests
{
    [Fact]
    public void Trigger_Succeeds_InSubmitted_AndRaisesWorkflowStarted()
    {
        var definitionId = new WorkflowDefinitionId(Guid.NewGuid());

        var result = WorkflowInstance.Trigger(
            TestData.TenantId, definitionId, 1, "leave-request-0001", TestData.InitiatorUserId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WorkflowInstanceStatus.Submitted);
        result.Value.WorkflowDefinitionId.Should().Be(definitionId);
        result.Value.WorkflowDefinitionVersionNumber.Should().Be(1);
        result.Value.CurrentStepOrder.Should().Be(0);
        result.Value.DomainEvents.OfType<WorkflowStarted>().Should().ContainSingle();
    }

    [Fact]
    public void Trigger_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => WorkflowInstance.Trigger(
            Guid.Empty, new WorkflowDefinitionId(Guid.NewGuid()), 1, null, TestData.InitiatorUserId, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Trigger_Throws_WhenInitiatedByUserIdIsEmpty()
    {
        var act = () => WorkflowInstance.Trigger(
            TestData.TenantId, new WorkflowDefinitionId(Guid.NewGuid()), 1, null, Guid.Empty, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Advance_FirstCall_TransitionsToInProgress_AndRaisesWorkflowSubmitted()
    {
        var instance = TestData.SubmittedInstance();

        var result = instance.Advance(1, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.InProgress);
        instance.CurrentStepOrder.Should().Be(1);
        instance.DomainEvents.OfType<WorkflowSubmitted>().Should().ContainSingle();
    }

    [Fact]
    public void Advance_SecondCall_StaysInProgress_AndRaisesNoAdditionalEvent()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Advance(2, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.CurrentStepOrder.Should().Be(2);
        instance.DomainEvents.OfType<WorkflowSubmitted>().Should().ContainSingle("only the first Advance call raises it");
    }

    [Fact]
    public void RequestApproval_Succeeds_FromInProgress()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.RequestApproval();

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.PendingApproval);
    }

    [Fact]
    public void RequestApproval_Fails_WhenNotInProgress()
    {
        var instance = TestData.SubmittedInstance();

        var result = instance.RequestApproval();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void ResumeAfterApproval_Succeeds_FromPendingApproval()
    {
        var instance = TestData.PendingApprovalInstance();

        var result = instance.ResumeAfterApproval(2, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.InProgress);
        instance.CurrentStepOrder.Should().Be(2);
    }

    [Fact]
    public void Reject_Succeeds_FromPendingApproval_AndRaisesWorkflowRejected()
    {
        var instance = TestData.PendingApprovalInstance();

        var result = instance.Reject("Insufficient balance", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Rejected);
        instance.DomainEvents.OfType<WorkflowRejected>().Should().ContainSingle();
    }

    [Fact]
    public void Reject_Fails_WhenReasonIsMissing()
    {
        var instance = TestData.PendingApprovalInstance();

        var result = instance.Reject(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.ReasonRequired);
    }

    [Theory]
    [InlineData(WorkflowInstanceStatus.Submitted)]
    [InlineData(WorkflowInstanceStatus.InProgress)]
    [InlineData(WorkflowInstanceStatus.PendingApproval)]
    public void Cancel_Succeeds_FromAnyNonTerminalState_AndRaisesWorkflowCancelled(WorkflowInstanceStatus status)
    {
        var instance = status switch
        {
            WorkflowInstanceStatus.Submitted => TestData.SubmittedInstance(),
            WorkflowInstanceStatus.InProgress => TestData.InProgressInstance(),
            WorkflowInstanceStatus.PendingApproval => TestData.PendingApprovalInstance(),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        var result = instance.Cancel("No longer needed", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Cancelled);
        instance.DomainEvents.OfType<WorkflowCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_Fails_WhenAlreadyTerminal()
    {
        var instance = TestData.PendingApprovalInstance();
        instance.Reject("reason", TestData.NowUtc);

        var result = instance.Cancel("reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void Withdraw_Succeeds_FromNonTerminalState_AndRaisesNoEvent()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Withdraw(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Withdrawn);
        instance.DomainEvents.OfType<WorkflowCancelled>().Should().BeEmpty();
        instance.DomainEvents.OfType<WorkflowRejected>().Should().BeEmpty();
        instance.DomainEvents.OfType<WorkflowCompleted>().Should().BeEmpty();
    }

    [Fact]
    public void Expire_Succeeds_FromPendingApproval()
    {
        var instance = TestData.PendingApprovalInstance();

        var result = instance.Expire(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Expired);
    }

    [Fact]
    public void Expire_Fails_WhenNotPendingApproval()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Expire(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void Fail_Succeeds_FromNonTerminalState_AndRecordsReason()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Fail("Downstream module command failed", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Failed);
        instance.FailureReason.Should().Be("Downstream module command failed");
    }

    [Fact]
    public void Fail_Fails_WhenReasonIsMissing()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Fail(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.ReasonRequired);
    }

    [Fact]
    public void Fail_Fails_WhenAlreadyTerminal()
    {
        var instance = TestData.InProgressInstance();
        instance.Complete(TestData.NowUtc);

        var result = instance.Fail("reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void Withdraw_Fails_WhenAlreadyTerminal()
    {
        var instance = TestData.InProgressInstance();
        instance.Complete(TestData.NowUtc);

        var result = instance.Withdraw(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void Cancel_Fails_WhenReasonIsMissing()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Cancel(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.ReasonRequired);
    }

    [Fact]
    public void Advance_Fails_WhenAlreadyTerminal()
    {
        var instance = TestData.InProgressInstance();
        instance.Complete(TestData.NowUtc);

        var result = instance.Advance(2, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void ResumeAfterApproval_Fails_WhenNotPendingApproval()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.ResumeAfterApproval(2, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }

    [Fact]
    public void Complete_Succeeds_FromInProgress_AndRaisesWorkflowCompleted()
    {
        var instance = TestData.InProgressInstance();

        var result = instance.Complete(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        instance.Status.Should().Be(WorkflowInstanceStatus.Completed);
        instance.DomainEvents.OfType<WorkflowCompleted>().Should().ContainSingle();
    }

    [Fact]
    public void Complete_Fails_WhenNotInProgressOrApproved()
    {
        var instance = TestData.SubmittedInstance();

        var result = instance.Complete(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidInstanceLifecycleTransition);
    }
}
