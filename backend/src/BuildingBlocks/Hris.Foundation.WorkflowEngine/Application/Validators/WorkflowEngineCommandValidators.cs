using FluentValidation;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Application.Queries;

namespace Hris.Foundation.WorkflowEngine.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the Domain
/// layer's own factory/transition methods already enforce (trigger-expression shape,
/// self-approval routing, canonical role names, lifecycle-state gating) -- the
/// identical separation every other framework's own validators file states for its own
/// set.
/// </summary>
public sealed class CreateWorkflowDefinitionCommandValidator : AbstractValidator<CreateWorkflowDefinitionCommand>
{
    public CreateWorkflowDefinitionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Steps).NotEmpty();
    }
}

public sealed class CreateNewWorkflowDefinitionDraftVersionCommandValidator
    : AbstractValidator<CreateNewWorkflowDefinitionDraftVersionCommand>
{
    public CreateNewWorkflowDefinitionDraftVersionCommandValidator()
    {
        RuleFor(c => c.WorkflowDefinitionId).NotEmpty();
        RuleFor(c => c.Steps).NotEmpty();
    }
}

public sealed class PublishWorkflowDefinitionVersionCommandValidator : AbstractValidator<PublishWorkflowDefinitionVersionCommand>
{
    public PublishWorkflowDefinitionVersionCommandValidator()
    {
        RuleFor(c => c.WorkflowDefinitionId).NotEmpty();
        RuleFor(c => c.VersionNumber).GreaterThan(0);
    }
}

public sealed class DeprecateWorkflowDefinitionVersionCommandValidator : AbstractValidator<DeprecateWorkflowDefinitionVersionCommand>
{
    public DeprecateWorkflowDefinitionVersionCommandValidator()
    {
        RuleFor(c => c.WorkflowDefinitionId).NotEmpty();
        RuleFor(c => c.VersionNumber).GreaterThan(0);
    }
}

public sealed class TriggerWorkflowInstanceCommandValidator : AbstractValidator<TriggerWorkflowInstanceCommand>
{
    public TriggerWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkflowDefinitionId).NotEmpty();
        RuleFor(c => c.InitiatedByUserId).NotEmpty();
    }
}

public sealed class AdvanceWorkflowInstanceCommandValidator : AbstractValidator<AdvanceWorkflowInstanceCommand>
{
    public AdvanceWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class RequestWorkflowInstanceApprovalCommandValidator : AbstractValidator<RequestWorkflowInstanceApprovalCommand>
{
    public RequestWorkflowInstanceApprovalCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class ResumeWorkflowInstanceAfterApprovalCommandValidator : AbstractValidator<ResumeWorkflowInstanceAfterApprovalCommand>
{
    public ResumeWorkflowInstanceAfterApprovalCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class RejectWorkflowInstanceCommandValidator : AbstractValidator<RejectWorkflowInstanceCommand>
{
    public RejectWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelWorkflowInstanceCommandValidator : AbstractValidator<CancelWorkflowInstanceCommand>
{
    public CancelWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class WithdrawWorkflowInstanceCommandValidator : AbstractValidator<WithdrawWorkflowInstanceCommand>
{
    public WithdrawWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class ExpireWorkflowInstanceCommandValidator : AbstractValidator<ExpireWorkflowInstanceCommand>
{
    public ExpireWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class FailWorkflowInstanceCommandValidator : AbstractValidator<FailWorkflowInstanceCommand>
{
    public FailWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CompleteWorkflowInstanceCommandValidator : AbstractValidator<CompleteWorkflowInstanceCommand>
{
    public CompleteWorkflowInstanceCommandValidator()
    {
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class CreateWorkflowTaskCommandValidator : AbstractValidator<CreateWorkflowTaskCommand>
{
    public CreateWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkflowInstanceId).NotEmpty();
        RuleFor(c => c.StepName).NotEmpty();
    }
}

public sealed class ApproveWorkflowTaskCommandValidator : AbstractValidator<ApproveWorkflowTaskCommand>
{
    public ApproveWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.WorkflowTaskId).NotEmpty();
    }
}

public sealed class RejectWorkflowTaskCommandValidator : AbstractValidator<RejectWorkflowTaskCommand>
{
    public RejectWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.WorkflowTaskId).NotEmpty();
    }
}

public sealed class DelegateWorkflowTaskCommandValidator : AbstractValidator<DelegateWorkflowTaskCommand>
{
    public DelegateWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.WorkflowTaskId).NotEmpty();
        RuleFor(c => c.DelegateToUserId).NotEmpty();
    }
}

public sealed class EscalateWorkflowTaskCommandValidator : AbstractValidator<EscalateWorkflowTaskCommand>
{
    public EscalateWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.WorkflowTaskId).NotEmpty();
        RuleFor(c => c.EscalateToUserId).NotEmpty();
    }
}

public sealed class ExpireWorkflowTaskCommandValidator : AbstractValidator<ExpireWorkflowTaskCommand>
{
    public ExpireWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.WorkflowTaskId).NotEmpty();
    }
}

public sealed class CancelWorkflowTaskCommandValidator : AbstractValidator<CancelWorkflowTaskCommand>
{
    public CancelWorkflowTaskCommandValidator()
    {
        RuleFor(c => c.WorkflowTaskId).NotEmpty();
    }
}

public sealed class GetWorkflowDefinitionQueryValidator : AbstractValidator<GetWorkflowDefinitionQuery>
{
    public GetWorkflowDefinitionQueryValidator()
    {
        RuleFor(q => q.WorkflowDefinitionId).NotEmpty();
    }
}

public sealed class ListWorkflowDefinitionsQueryValidator : AbstractValidator<ListWorkflowDefinitionsQuery>
{
    public ListWorkflowDefinitionsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetWorkflowInstanceQueryValidator : AbstractValidator<GetWorkflowInstanceQuery>
{
    public GetWorkflowInstanceQueryValidator()
    {
        RuleFor(q => q.WorkflowInstanceId).NotEmpty();
    }
}

public sealed class ListWorkflowInstanceHistoryQueryValidator : AbstractValidator<ListWorkflowInstanceHistoryQuery>
{
    public ListWorkflowInstanceHistoryQueryValidator()
    {
        RuleFor(q => q.WorkflowDefinitionId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetWorkflowTaskQueryValidator : AbstractValidator<GetWorkflowTaskQuery>
{
    public GetWorkflowTaskQueryValidator()
    {
        RuleFor(q => q.WorkflowTaskId).NotEmpty();
    }
}

public sealed class ListMyWorkflowTasksQueryValidator : AbstractValidator<ListMyWorkflowTasksQuery>
{
    public ListMyWorkflowTasksQueryValidator()
    {
        RuleFor(q => q.AssignedToUserId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
