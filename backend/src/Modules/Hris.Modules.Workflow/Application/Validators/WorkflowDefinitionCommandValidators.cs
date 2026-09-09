using FluentValidation;
using Hris.Modules.Workflow.Application.Commands;

namespace Hris.Modules.Workflow.Application.Validators;

/// <summary>
/// Shape validation only. Per commands.md's own guidance, no requester-exclusion or
/// graph validation appears here: the aggregate decides those, and duplicating them
/// in a validator would create a second place the rule could drift from.
/// </summary>
public sealed class AuthorWorkflowDefinitionCommandValidator : AbstractValidator<AuthorWorkflowDefinitionCommand>
{
    public AuthorWorkflowDefinitionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BusinessProcessId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.TriggerCondition).MaximumLength(2000);
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class EditDraftDefinitionCommandValidator : AbstractValidator<EditDraftDefinitionCommand>
{
    public EditDraftDefinitionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DefinitionId).NotEmpty();
        RuleFor(c => c.Steps).NotNull();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class PublishWorkflowDefinitionCommandValidator : AbstractValidator<PublishWorkflowDefinitionCommand>
{
    public PublishWorkflowDefinitionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DefinitionId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class CreateNewVersionCommandValidator : AbstractValidator<CreateNewVersionCommand>
{
    public CreateNewVersionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DefinitionId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class DeprecateWorkflowDefinitionCommandValidator : AbstractValidator<DeprecateWorkflowDefinitionCommand>
{
    public DeprecateWorkflowDefinitionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DefinitionId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
    }
}
