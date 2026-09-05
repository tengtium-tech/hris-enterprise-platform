using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Domain;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Domain;

public sealed class WorkflowTaskTests
{
    [Fact]
    public void Create_Succeeds_InPending_AndRaisesWorkflowAssigned()
    {
        var instanceId = new WorkflowInstanceId(Guid.NewGuid());

        var result = WorkflowTask.Create(
            TestData.TenantId, instanceId, "Manager Approval", 1, WorkflowParticipantType.Role, "PeopleManager", null, 0, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(WorkflowTaskStatus.Pending);
        result.Value.WorkflowInstanceId.Should().Be(instanceId);
        result.Value.DomainEvents.OfType<WorkflowAssigned>().Should().ContainSingle();
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => WorkflowTask.Create(
            Guid.Empty, new WorkflowInstanceId(Guid.NewGuid()), "Manager Approval", 1, WorkflowParticipantType.Role, "PeopleManager", null, 0, TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Approve_Succeeds_FromPending_AndRaisesWorkflowApproved()
    {
        var task = TestData.PendingTask();

        var result = task.Approve("Looks good", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Approved);
        task.Comments.Should().Be("Looks good");
        task.DomainEvents.OfType<WorkflowApproved>().Should().ContainSingle();
    }

    [Fact]
    public void Approve_Fails_WhenNotPending()
    {
        var task = TestData.PendingTask();
        task.Approve(null, TestData.NowUtc);

        var result = task.Approve(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
    }

    [Fact]
    public void Reject_Succeeds_FromPending_AndRaisesNoEvent()
    {
        var task = TestData.PendingTask();

        var result = task.Reject("Not eligible", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Rejected);
        task.DomainEvents.Should().HaveCount(1, "only Create's own WorkflowAssigned; WorkflowRejected is raised once, by WorkflowInstance.Reject, not by the task");
    }

    [Fact]
    public void Reject_Fails_WhenNotPending()
    {
        var task = TestData.PendingTask();
        task.Reject(null, TestData.NowUtc);

        var result = task.Reject(null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
    }

    [Fact]
    public void Delegate_Succeeds_FromPending_AndRaisesWorkflowDelegated()
    {
        var task = TestData.PendingTask();
        var delegateToUserId = Guid.NewGuid();

        var result = task.Delegate(delegateToUserId, "Out of office", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Delegated);
        task.DelegatedToUserId.Should().Be(delegateToUserId);
        task.DomainEvents.OfType<WorkflowDelegated>().Should().ContainSingle();
    }

    [Fact]
    public void Delegate_Fails_WhenDelegateToUserIdIsEmpty()
    {
        var task = TestData.PendingTask();

        var result = task.Delegate(Guid.Empty, "reason", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DelegateToUserRequired);
    }

    [Fact]
    public void Delegate_Fails_WhenNotPending()
    {
        var task = TestData.PendingTask();
        task.Delegate(Guid.NewGuid(), null, TestData.NowUtc);

        var result = task.Delegate(Guid.NewGuid(), null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
    }

    [Fact]
    public void Escalate_ClosesTheTask_AsEscalated_AndRaisesWorkflowEscalated()
    {
        var task = TestData.PendingTask();

        var result = task.Escalate(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Escalated);
        task.DomainEvents.OfType<WorkflowEscalated>().Should().ContainSingle();
    }

    [Fact]
    public void Escalate_Fails_WhenNotPending()
    {
        var task = TestData.PendingTask();
        task.Escalate(TestData.NowUtc);

        var result = task.Escalate(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidTaskLifecycleTransition);
    }

    [Fact]
    public void Expire_Succeeds_FromPending_AndRaisesNoEvent()
    {
        var task = TestData.PendingTask();

        var result = task.Expire(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Expired);
        task.DomainEvents.Should().HaveCount(1, "only Create's own WorkflowAssigned");
    }

    [Fact]
    public void Cancel_Succeeds_FromPending_AndRaisesNoEvent()
    {
        var task = TestData.PendingTask();

        var result = task.Cancel(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(WorkflowTaskStatus.Cancelled);
        task.DomainEvents.Should().HaveCount(1, "only Create's own WorkflowAssigned");
    }
}
