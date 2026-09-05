using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Domain;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Domain;

public sealed class WorkflowEngineEventsTests
{
    [Fact]
    public void Trigger_RaisesWorkflowStarted_CarryingTheExpectedFields()
    {
        var definitionId = new WorkflowDefinitionId(Guid.NewGuid());
        var instance = WorkflowInstance.Trigger(TestData.TenantId, definitionId, 1, null, TestData.InitiatorUserId, TestData.NowUtc).Value;

        var raised = instance.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<WorkflowStarted>().Subject;

        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowInstanceId.Should().Be(instance.Id);
        raised.TenantId.Should().Be(TestData.TenantId);
        raised.WorkflowDefinitionId.Should().Be(definitionId);
        raised.WorkflowDefinitionVersionNumber.Should().Be(1);
    }

    [Fact]
    public void Advance_FirstCall_RaisesWorkflowSubmitted_CarryingTheExpectedFields()
    {
        var instance = TestData.SubmittedInstance();

        instance.Advance(1, TestData.NowUtc);

        var raised = instance.DomainEvents.OfType<WorkflowSubmitted>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowInstanceId.Should().Be(instance.Id);
    }

    [Fact]
    public void Reject_RaisesWorkflowRejected_CarryingTheExpectedFields()
    {
        var instance = TestData.PendingApprovalInstance();

        instance.Reject("Insufficient balance", TestData.NowUtc);

        var raised = instance.DomainEvents.OfType<WorkflowRejected>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowInstanceId.Should().Be(instance.Id);
        raised.Reason.Should().Be("Insufficient balance");
    }

    [Fact]
    public void Cancel_RaisesWorkflowCancelled_CarryingTheExpectedFields()
    {
        var instance = TestData.InProgressInstance();

        instance.Cancel("No longer needed", TestData.NowUtc);

        var raised = instance.DomainEvents.OfType<WorkflowCancelled>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowInstanceId.Should().Be(instance.Id);
        raised.Reason.Should().Be("No longer needed");
    }

    [Fact]
    public void Complete_RaisesWorkflowCompleted_CarryingTheExpectedFields()
    {
        var instance = TestData.InProgressInstance();

        instance.Complete(TestData.NowUtc);

        var raised = instance.DomainEvents.OfType<WorkflowCompleted>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowInstanceId.Should().Be(instance.Id);
    }

    [Fact]
    public void CreateTask_RaisesWorkflowAssigned_CarryingTheExpectedFields()
    {
        var instanceId = new WorkflowInstanceId(Guid.NewGuid());
        var task = WorkflowTask.Create(
            TestData.TenantId, instanceId, "Manager Approval", 1, WorkflowParticipantType.Role, "PeopleManager", null, 0, TestData.NowUtc).Value;

        var raised = task.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<WorkflowAssigned>().Subject;

        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowTaskId.Should().Be(task.Id);
        raised.WorkflowInstanceId.Should().Be(instanceId);
        raised.TenantId.Should().Be(TestData.TenantId);
    }

    [Fact]
    public void Approve_RaisesWorkflowApproved_CarryingTheExpectedFields()
    {
        var task = TestData.PendingTask();

        task.Approve(null, TestData.NowUtc);

        var raised = task.DomainEvents.OfType<WorkflowApproved>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowTaskId.Should().Be(task.Id);
        raised.WorkflowInstanceId.Should().Be(task.WorkflowInstanceId);
    }

    [Fact]
    public void Delegate_RaisesWorkflowDelegated_CarryingTheExpectedFields()
    {
        var task = TestData.PendingTask();
        var delegateToUserId = Guid.NewGuid();

        task.Delegate(delegateToUserId, "Out of office", TestData.NowUtc);

        var raised = task.DomainEvents.OfType<WorkflowDelegated>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowTaskId.Should().Be(task.Id);
        raised.DelegatedToUserId.Should().Be(delegateToUserId);
    }

    [Fact]
    public void Escalate_RaisesWorkflowEscalated_CarryingTheCurrentEscalationLevel()
    {
        var task = TestData.PendingTask(escalationLevel: 2);

        task.Escalate(TestData.NowUtc);

        var raised = task.DomainEvents.OfType<WorkflowEscalated>().Should().ContainSingle().Subject;
        raised.EventId.Should().NotBeEmpty();
        raised.OccurredOnUtc.Should().Be(TestData.NowUtc);
        raised.WorkflowTaskId.Should().Be(task.Id);
        raised.WorkflowInstanceId.Should().Be(task.WorkflowInstanceId);
        raised.EscalationLevel.Should().Be(2);
    }
}
