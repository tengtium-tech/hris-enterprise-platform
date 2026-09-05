using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Application.Queries;
using Hris.Foundation.WorkflowEngine.Application.Validators;
using Hris.Foundation.WorkflowEngine.Domain;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>StatutoryReferenceDataCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class WorkflowEngineCommandValidatorsTests
{
    [Fact]
    public void CreateWorkflowDefinitionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new CreateWorkflowDefinitionCommandValidator();
        var valid = new CreateWorkflowDefinitionCommand(
            TestData.TenantId, "Leave Approval", WorkflowTriggerType.SystemEvent, "leave.requested", TestData.NewSteps());
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateNewWorkflowDefinitionDraftVersionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDefinitionId()
    {
        var validator = new CreateNewWorkflowDefinitionDraftVersionCommandValidator();
        var valid = new CreateNewWorkflowDefinitionDraftVersionCommand(Guid.NewGuid(), TestData.NewSteps());
        var invalid = valid with { WorkflowDefinitionId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishWorkflowDefinitionVersionCommandValidator_AcceptsAValidCommand_AndRejectsAZeroVersionNumber()
    {
        var validator = new PublishWorkflowDefinitionVersionCommandValidator();
        var valid = new PublishWorkflowDefinitionVersionCommand(Guid.NewGuid(), 1);
        var invalid = valid with { VersionNumber = 0 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeprecateWorkflowDefinitionVersionCommandValidator_AcceptsAValidCommand_AndRejectsAZeroVersionNumber()
    {
        var validator = new DeprecateWorkflowDefinitionVersionCommandValidator();
        var valid = new DeprecateWorkflowDefinitionVersionCommand(Guid.NewGuid(), 1);
        var invalid = valid with { VersionNumber = 0 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TriggerWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInitiator()
    {
        var validator = new TriggerWorkflowInstanceCommandValidator();
        var valid = new TriggerWorkflowInstanceCommand(TestData.TenantId, Guid.NewGuid(), null, TestData.InitiatorUserId);
        var invalid = valid with { InitiatedByUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AdvanceWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new AdvanceWorkflowInstanceCommandValidator();
        var valid = new AdvanceWorkflowInstanceCommand(Guid.NewGuid(), 1);
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RequestWorkflowInstanceApprovalCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new RequestWorkflowInstanceApprovalCommandValidator();
        var valid = new RequestWorkflowInstanceApprovalCommand(Guid.NewGuid());
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResumeWorkflowInstanceAfterApprovalCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new ResumeWorkflowInstanceAfterApprovalCommandValidator();
        var valid = new ResumeWorkflowInstanceAfterApprovalCommand(Guid.NewGuid(), 2);
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new RejectWorkflowInstanceCommandValidator();
        var valid = new RejectWorkflowInstanceCommand(Guid.NewGuid(), "Insufficient balance");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CancelWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new CancelWorkflowInstanceCommandValidator();
        var valid = new CancelWorkflowInstanceCommand(Guid.NewGuid(), "No longer needed");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void WithdrawWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new WithdrawWorkflowInstanceCommandValidator();
        var valid = new WithdrawWorkflowInstanceCommand(Guid.NewGuid());
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExpireWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new ExpireWorkflowInstanceCommandValidator();
        var valid = new ExpireWorkflowInstanceCommand(Guid.NewGuid());
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FailWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new FailWorkflowInstanceCommandValidator();
        var valid = new FailWorkflowInstanceCommand(Guid.NewGuid(), "Downstream failure");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompleteWorkflowInstanceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new CompleteWorkflowInstanceCommandValidator();
        var valid = new CompleteWorkflowInstanceCommand(Guid.NewGuid());
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyStepName()
    {
        var validator = new CreateWorkflowTaskCommandValidator();
        var valid = new CreateWorkflowTaskCommand(
            TestData.TenantId, Guid.NewGuid(), "Manager Approval", 1, WorkflowParticipantType.Role, "PeopleManager", null);
        var invalid = valid with { StepName = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApproveWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTaskId()
    {
        var validator = new ApproveWorkflowTaskCommandValidator();
        var valid = new ApproveWorkflowTaskCommand(Guid.NewGuid(), "Looks good");
        var invalid = valid with { WorkflowTaskId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTaskId()
    {
        var validator = new RejectWorkflowTaskCommandValidator();
        var valid = new RejectWorkflowTaskCommand(Guid.NewGuid(), "Not eligible");
        var invalid = valid with { WorkflowTaskId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DelegateWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyDelegateToUserId()
    {
        var validator = new DelegateWorkflowTaskCommandValidator();
        var valid = new DelegateWorkflowTaskCommand(Guid.NewGuid(), Guid.NewGuid(), "Out of office");
        var invalid = valid with { DelegateToUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EscalateWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyEscalateToUserId()
    {
        var validator = new EscalateWorkflowTaskCommandValidator();
        var valid = new EscalateWorkflowTaskCommand(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { EscalateToUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExpireWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTaskId()
    {
        var validator = new ExpireWorkflowTaskCommandValidator();
        var valid = new ExpireWorkflowTaskCommand(Guid.NewGuid());
        var invalid = valid with { WorkflowTaskId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CancelWorkflowTaskCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTaskId()
    {
        var validator = new CancelWorkflowTaskCommandValidator();
        var valid = new CancelWorkflowTaskCommand(Guid.NewGuid());
        var invalid = valid with { WorkflowTaskId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetWorkflowDefinitionQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyDefinitionId()
    {
        var validator = new GetWorkflowDefinitionQueryValidator();
        var valid = new GetWorkflowDefinitionQuery(Guid.NewGuid());
        var invalid = valid with { WorkflowDefinitionId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListWorkflowDefinitionsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListWorkflowDefinitionsQueryValidator();
        var valid = new ListWorkflowDefinitionsQuery(TestData.TenantId);
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetWorkflowInstanceQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyInstanceId()
    {
        var validator = new GetWorkflowInstanceQueryValidator();
        var valid = new GetWorkflowInstanceQuery(Guid.NewGuid());
        var invalid = valid with { WorkflowInstanceId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListWorkflowInstanceHistoryQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListWorkflowInstanceHistoryQueryValidator();
        var valid = new ListWorkflowInstanceHistoryQuery(Guid.NewGuid(), TestData.TenantId);
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetWorkflowTaskQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTaskId()
    {
        var validator = new GetWorkflowTaskQueryValidator();
        var valid = new GetWorkflowTaskQuery(Guid.NewGuid());
        var invalid = valid with { WorkflowTaskId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListMyWorkflowTasksQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyAssignedToUserId()
    {
        var validator = new ListMyWorkflowTasksQueryValidator();
        var valid = new ListMyWorkflowTasksQuery(Guid.NewGuid(), TestData.TenantId);
        var invalid = valid with { AssignedToUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
