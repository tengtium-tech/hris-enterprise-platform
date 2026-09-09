using FluentValidation;
using Hris.Modules.Workflow.Application.Commands;

namespace Hris.Modules.Workflow.Application.Validators;

public sealed class CreateApprovalPolicyCommandValidator : AbstractValidator<CreateApprovalPolicyCommand>
{
    public CreateApprovalPolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

/// <summary>
/// The reason is required at the shape level as well as in the aggregate, because a
/// policy change that stops requiring approval for a business process is a control
/// removal and the caller should be told that before the command is dispatched.
/// </summary>
public sealed class ConfigureApprovalPolicyCommandValidator : AbstractValidator<ConfigureApprovalPolicyCommand>
{
    public ConfigureApprovalPolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
    }
}
